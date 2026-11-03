using UnityEngine;

namespace MyArcade
{
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
		private const float SwingSeconds = 0.18f; // from the swing starting to it landing: time to dodge it
		private const float RecoverSeconds = 0.35f;
		private float windupEndsAt;
		private float swingLandsAt;
		private float recoverUntil;
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
			swingLandsAt = 0f;
			recoverUntil = 0f;
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
			if (windupEndsAt <= 0f)
			{
				FaceTarget();
			}
			if (windupEndsAt > 0f)
			{
				// winding up: stands still with the weapon raised; the swing lands at the end if the hero is still close
				if (Time.time >= windupEndsAt)
				{
					windupEndsAt = 0f;
					nextHitTime = Time.time + contactCooldown;
					swingLandsAt = Time.time + SwingSeconds;
					RaiseAttacked(Random.value > 0.5f ? "attack-melee-right" : "attack-melee-left");
				}
				return;
			}
			if (swingLandsAt > 0f)
			{
				// the weapon comes down where the enemy was looking; it misses if the hero got out of that spot
				if (Time.time >= swingLandsAt)
				{
					swingLandsAt = 0f;
					recoverUntil = Time.time + RecoverSeconds;
					if (StrikeReaches(contactRange + 0.4f))
					{
						target.TakeDamage(contactDamage, gameObject);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, contactDamage);
					}
				}
				return;
			}
			if (Time.time < recoverUntil)
			{
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
				LockStrike();
				RaiseAttacked("windup");
			}
		}
	}
}
