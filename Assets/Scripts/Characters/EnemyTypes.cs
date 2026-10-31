using UnityEngine;

namespace MyArcade
{
	/// <summary>Rival mechanic student: runs at the player and throws punches.</summary>
	public class Chaser : Enemy
	{
		protected float contactRange = 2.0f;
		protected int contactDamage = 1;
		protected float contactCooldown = 1.0f;
		protected string displayName = "MECHANIC";
		protected float windupTime = 0.48f;
		private const float AttackTokenSeconds = 0.35f;
		private static float attackTokenFreeAt;


		private const float RepositionRadius = 250f * 8.32f / 180f;
		private const float RepositionSeconds = 5f;
		private Vector3 detourOffset;
		private float detourUntil;
		private float nextHitTime;
		private float windupEndsAt;
		public override string VisualPose => windupEndsAt > 0f ? "warn" : null;

		public override string DisplayName => displayName;

		public static void ResetAttackToken()
		{
			attackTokenFreeAt = 0f;
		}

		protected override void Awake()
		{
			moveSpeed = 5.0f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			nextHitTime = Time.time + 0.8f;
			windupEndsAt = 0f;
			detourUntil = 0f;
		}

		protected override void OnBlockedByCloser()
		{
			if (Time.time >= detourUntil)
			{
				Vector2 around = Random.insideUnitCircle * RepositionRadius;
				detourOffset = new Vector3(around.x, 0f, around.y);
				detourUntil = Time.time + RepositionSeconds;
			}
		}

		/// <summary>Where this rival walks: the hero, or the detour point while spreading out.</summary>
		private Vector3 DirectionToGoal()
		{
			if (Time.time < detourUntil)
			{
				Vector3 goal = target.transform.position + detourOffset - transform.position;
				goal.y = 0f;
				if (goal.sqrMagnitude > 1f)
				{
					return goal.normalized;
				}
			}
			return DirectionToTarget();
		}

		protected override void Behave()
		{
			FaceTarget();
			if (windupEndsAt > 0f)
			{
				// winding up: stands still with the weapon raised; the swing lands at the end if the hero is still close
				if (Time.time >= windupEndsAt)
				{
					windupEndsAt = 0f;
					nextHitTime = Time.time + contactCooldown;
					RaiseAttacked(Random.value > 0.5f ? "attack-melee-right" : "attack-melee-left");
					if (DistanceToTarget() <= contactRange + 0.4f)
					{
						target.TakeDamage(contactDamage, gameObject);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, contactDamage);
					}
				}
				return;
			}
			if (DistanceToTarget() > contactRange)
			{
				Move(DirectionToGoal());
			}
			else if (Time.time >= nextHitTime && Time.time >= attackTokenFreeAt)
			{
				attackTokenFreeAt = Time.time + AttackTokenSeconds;
				windupEndsAt = Time.time + windupTime;
				RaiseAttacked("windup");
			}
		}
	}

	/// <summary>
	/// Inheritance two levels deep: a Brute (final-year senior) IS a Chaser, only slower, tougher and
	/// hits harder. It reuses Chaser.Behave() unchanged; only the numbers differ.
	/// </summary>
	public class Brute : Chaser
	{
		protected override void Awake()
		{
			base.Awake(); // let Chaser set its defaults first...
			moveSpeed = 3.2f;
			maxHealth = 15;
			contactRange = 1.7f;
			contactDamage = 2;
			contactCooldown = 1.5f;
			displayName = "FINAL-YEAR SENIOR";
			ResetHealth();
		}

		// ---- the grab: a giant cannot be parried; hit him, then dash out of his reach ---------------
		private float grabEndsAt;
		public override string VisualPose => grabEndsAt > 0f ? "warn" : null;
		private float nextGrabTime;

		protected override bool IsGiant => true;

		public override void OnSpawn()
		{
			base.OnSpawn();
			grabEndsAt = 0f;
			nextGrabTime = Time.time + 0.9f;
		}

		protected override void Behave()
		{
			FaceTarget();
			if (grabEndsAt > 0f)
			{
				// winding up: stands still, the grab lands at the end unless the hero has dashed away
				if (Time.time >= grabEndsAt)
				{
					grabEndsAt = 0f;
					nextGrabTime = Time.time + 1.5f;
					RaiseAttacked("attack-melee-right");
					if (DistanceToTarget() <= contactRange + 0.5f)
					{
						target.TakeDamage(999, gameObject);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, 4);
					}
				}
				return;
			}
			if (DistanceToTarget() > contactRange)
			{
				Move(DirectionToTarget());
			}
			else if (Time.time >= nextGrabTime)
			{
				grabEndsAt = Time.time + 0.4f;
				RaiseAttacked("windup");
			}
		}
	}

	/// <summary>Rival electrician student and shoots stones. Same base class, different Behave().</summary>
	public class Shooter : Enemy
	{
		// protected: Sniper and Shotgunner change these numbers (and Fire) instead of rewriting the whole class
		protected float preferredDistance = 8f;

		protected float aimTime = 1.5f;       // the red laser follows the hero this long (dash or parry as the shots come)
		protected float lockTime = 0.16f;    // the direction is frozen this long before every shot
		protected float shotGap = 0.3f;
		protected int shotsPerBurst = 2;
		protected float cooldown = 5f;
		protected float bulletSpeed = 92f;
		protected float shootRange = 14f;

		private IBulletSpawner bullets;
		private float aimEndsAt;          // end of the warning
		private float nextShotTime;       // when the next shot of the burst leaves
		private int shotsLeft;
		private float nextAimTime;
		private Vector3 aimDirection;

		public override string DisplayName => "ELECTRICIAN";

		/// <summary>True while the red laser is on (the shot is coming); AimDirection is where it points.</summary>
		public bool IsAimingShot => aimEndsAt > 0f || shotsLeft > 0;
		public override string VisualPose => aimEndsAt > 0f ? "warn" : shotsLeft > 0 ? "shoot" : null;
		public Vector3 AimDirection => aimDirection;

		public override void OnSpawn()
		{
			base.OnSpawn();
			aimEndsAt = 0f;
			shotsLeft = 0;
			nextAimTime = Time.time + 1.2f;
		}

		public void SetBullets(IBulletSpawner spawner)
		{
			bullets = spawner;
		}

		protected override void Awake()
		{
			moveSpeed = 4.2f;
			maxHealth = 1;
			base.Awake();
		}

		/// <summary>Polymorphism: the base shooter fires one stone; subclasses decide what a "shot" is.</summary>
		protected virtual void Fire(Vector3 direction)
		{
			Shoot(direction, bulletSpeed);
		}

		protected void Shoot(Vector3 direction, float speed)
		{
			bullets.Spawn(transform.position + Vector3.up * 0.9f + direction * 0.8f, direction, speed, 1, Team, gameObject);
		}

		protected override void Behave()
		{
			FaceTarget();
			float distance = DistanceToTarget();
			bool busy = aimEndsAt > 0f || shotsLeft > 0;
			if (!busy)
			{
				if (distance > preferredDistance + 1.5f)
				{
					Move(DirectionToTarget());
				}
				else if (distance < preferredDistance - 2.5f)
				{
					Move(-DirectionToTarget());
				}
			}

			if (bullets == null)
			{
				return;
			}
			if (aimEndsAt > 0f)
			{
				aimDirection = DirectionToTarget(); // the laser follows the hero for the whole warning
				if (Time.time >= aimEndsAt)
				{
					aimEndsAt = 0f;
					shotsLeft = shotsPerBurst;
					nextShotTime = Time.time + lockTime;
					RaiseAttacked("attack-melee-right");
				}
			}
			else if (shotsLeft > 0)
			{
				if (Time.time < nextShotTime - lockTime)
				{
					aimDirection = DirectionToTarget(); // re-aimed for every shot, frozen just before it
				}
				if (Time.time >= nextShotTime)
				{
					Fire(aimDirection);
					GameEvents.RaiseSound("shot");
					shotsLeft--;
					nextShotTime = Time.time + shotGap;
					if (shotsLeft == 0)
					{
						nextAimTime = Time.time + cooldown;
					}
					else
					{
						RaiseAttacked("attack-melee-right");
					}
				}
			}
			else if (distance < shootRange && Time.time >= nextAimTime)
			{
				aimEndsAt = Time.time + aimTime;
				aimDirection = DirectionToTarget();
				RaiseAttacked("windup");
			}
		}
	}
}
