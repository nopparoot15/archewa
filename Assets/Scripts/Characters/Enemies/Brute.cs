using UnityEngine;

namespace MyArcade
{
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

		private float grabEndsAt;
		public override string VisualPose => grabEndsAt > 0f ? "warn" : null;
		private float nextGrabTime;
		private float grabLandsAt;
		private float recoverUntil;

		protected override bool IsGiant => true;

		public override void OnSpawn()
		{
			base.OnSpawn();
			grabEndsAt = 0f;
			grabLandsAt = 0f;
			recoverUntil = 0f;
			nextGrabTime = Time.time + 0.9f;
		}

		protected override void Behave()
		{
			if (grabEndsAt <= 0f)
			{
				FaceTarget();
			}
			if (grabEndsAt > 0f)
			{
				// winding up: stands still, the grab lands at the end unless the hero has dashed away
				if (Time.time >= grabEndsAt)
				{
					grabEndsAt = 0f;
					nextGrabTime = Time.time + 1.5f;
					grabLandsAt = Time.time + 0.22f;
					RaiseAttacked("attack-melee-right");
				}
				return;
			}
			if (grabLandsAt > 0f)
			{
				if (Time.time >= grabLandsAt)
				{
					grabLandsAt = 0f;
					recoverUntil = Time.time + 0.5f;
					if (StrikeReaches(contactRange + 0.5f))
					{
						target.TakeDamage(999, gameObject);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, 4);
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
				Move(DirectionToTarget());
			}
			else if (Time.time >= nextGrabTime)
			{
				grabEndsAt = Time.time + 0.4f;
				LockStrike();
				RaiseAttacked("windup");
			}
		}
	}
}
