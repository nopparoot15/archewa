using UnityEngine;

namespace MyArcade
{
	public class Shooter : Enemy
	{
		// protected: Sniper and Shotgunner change these numbers (and Fire) instead of rewriting the whole class
		protected float preferredDistance = 8f;

		protected float aimTime = 1.5f; // the red laser follows the hero this long (dash or parry as the shots come)
		protected float lockTime = 0.16f; // the direction is frozen this long before every shot
		protected float shotGap = 0.3f;
		protected int shotsPerBurst = 2;
		protected float cooldown = 5f;
		protected float bulletSpeed = 92f;
		protected float shootRange = 14f;

		private IBulletSpawner bullets;
		private float aimEndsAt; // end of the warning
		private float nextShotTime; // when the next shot of the burst leaves
		private int shotsLeft;
		private float nextAimTime;
		private Vector3 aimDirection;

		public override string DisplayName => "ELECTRICIAN";

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
