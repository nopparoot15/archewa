using UnityEngine;

namespace MyArcade
{
	public class Charger : Enemy
	{
		private const float WarnTime = 0.7f;
		private const float RamTime = 0.7f;
		private const float RamSpeed = 6.5f;
		private const float TiredTime = 1.3f;

		private enum Phase { Walk, Warn, Ram, Tired }

		private Phase phase;
		private float phaseEndsAt;
		private Vector3 ramDirection;
		private bool hit;
		public override string VisualPose => phase == Phase.Warn ? "warn" : phase == Phase.Ram ? "charge" : phase == Phase.Tired ? "open" : null;

		public override string DisplayName => "RAGING BULL";

		protected override void Awake()
		{
			moveSpeed = 4.5f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			phase = Phase.Walk;
			phaseEndsAt = Time.time + 1.5f;
		}

		protected override void Behave()
		{
			switch (phase)
			{
				case Phase.Walk:
					FaceTarget();
					Move(DirectionToTarget());
					if (Time.time >= phaseEndsAt && DistanceToTarget() < 9f && DistanceToTarget() > 3f)
					{
						phase = Phase.Warn;
						phaseEndsAt = Time.time + WarnTime;
						RaiseAttacked("windup");
						GameEvents.RaiseSound("warning");
					}
					break;
				case Phase.Warn:
					FaceTarget();
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Ram;
						phaseEndsAt = Time.time + RamTime;
						ramDirection = DirectionToTarget();
						hit = false;
						RaiseAttacked("attack-kick-right");
					}
					break;
				case Phase.Ram:
					Move(ramDirection, RamSpeed / moveSpeed);
					if (!hit && DistanceToTarget() < 1.6f)
					{
						hit = true;
						target.TakeDamage(1, gameObject);
						target.ApplyKnockback(ramDirection * 10f);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, 2);
					}
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Tired;
						phaseEndsAt = Time.time + TiredTime;
					}
					break;
				default:
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Walk;
						phaseEndsAt = Time.time + 2f;
					}
					break;
			}
		}
	}
}
