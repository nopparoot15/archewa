using UnityEngine;

namespace MyArcade
{
	public class Assassin : Enemy, IShootable, IUnparryable
	{
		private const float AttackRange = 6.9f;
		private const float WarnTime = 1.3f;
		private const float ChargeSpeed = 21f;
		private const float ChargeTime = 1f;
		private const float BrakingTime = 1.2f;
		private const float AttackDelay = 4f;
		private const float DeflectAngle = 45f;

		private enum Phase { Approach, Warn, Charge, Braking }

		private Phase phase;
		private float phaseEndsAt;
		private float nextAttackAt;
		private Vector3 chargeDirection;
		private bool hit;

		public override string DisplayName => "ASSASSIN";
		public bool IsOpen => phase == Phase.Braking;
		public override string VisualPose => phase == Phase.Warn ? "warn" : phase == Phase.Charge ? "charge" : phase == Phase.Braking ? "open" : null;

		protected override void Awake()
		{
			moveSpeed = 5.07f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			phase = Phase.Approach;
			nextAttackAt = Time.time + 1.5f;
		}

		protected override void Behave()
		{
			switch (phase)
			{
				case Phase.Approach:
					FaceTarget();
					if (DistanceToTarget() > AttackRange)
					{
						Move(DirectionToTarget());
					}
					else if (Time.time >= nextAttackAt)
					{
						phase = Phase.Warn;
						phaseEndsAt = Time.time + WarnTime;
						RaiseAttacked("windup");
						GameEvents.RaiseSound("warning");
					}
					break;
				case Phase.Warn:
					FaceTarget(); // stands still and keeps looking at the hero; the dash direction is taken when it starts
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Charge;
						phaseEndsAt = Time.time + ChargeTime;
						chargeDirection = DirectionToTarget();
						hit = false;
						RaiseAttacked("attack-kick-left");
					}
					break;
				case Phase.Charge:
					Move(chargeDirection, ChargeSpeed / moveSpeed);
					if (!hit && DistanceToTarget() < 1.5f)
					{
						hit = true;
						target.TakeDamage(1, gameObject);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, 2);
						phaseEndsAt = Time.time;
					}
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Braking;
						phaseEndsAt = Time.time + BrakingTime;
						nextAttackAt = Time.time + AttackDelay;
						GameEvents.RaiseSound("warning");
					}
					break;
				default:
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Approach;
					}
					break;
			}
		}

		public override void TakeDamage(int amount, GameObject source)
		{
			if (IsDead)
			{
				return;
			}
			if (!IsOpen)
			{
				GameEvents.RaiseDeflected(transform.position + Vector3.up);
				GameEvents.RaiseSound("deflect");
				return;
			}
			base.TakeDamage(amount, source);
		}

		public bool TakeShot(Bullet bullet)
		{
			if (bullet.WasDeflectedBy(gameObject))
			{
				return true;
			}
			bullet.MarkDeflected(gameObject);
			if (IsOpen)
			{
				return false;
			}
			Vector3 back = Quaternion.Euler(0f, Random.Range(-DeflectAngle, DeflectAngle), 0f) * -bullet.Direction;
			bullet.Reflect(back, Team, gameObject, bullet.Damage);
			GameEvents.RaiseDeflected(transform.position + Vector3.up);
			GameEvents.RaiseSound("deflect");
			return true;
		}
	}
}
