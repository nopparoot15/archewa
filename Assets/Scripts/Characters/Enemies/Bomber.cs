using UnityEngine;

namespace MyArcade
{
	public class Bomber : Enemy, IExplosive
	{
		private const float FuseTime = 0.85f;
		private const float TriggerRange = 2.4f;

		private float fuseEndsAt;

		public float BlastRadius => 3.4f;
		public override string VisualPose => fuseEndsAt > 0f ? "warn" : null;
		public override string DisplayName => "FIRECRACKER KID";

		protected override void Awake()
		{
			moveSpeed = 6.0f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			fuseEndsAt = 0f;
		}

		protected override void Behave()
		{
			FaceTarget();
			if (fuseEndsAt > 0f)
			{
				if (Time.time >= fuseEndsAt)
				{
					Explode();
				}
				return; // stands still while the fuse burns: run out of the radius
			}
			if (DistanceToTarget() > TriggerRange)
			{
				Move(DirectionToTarget());
			}
			else
			{
				fuseEndsAt = Time.time + FuseTime;
				GameEvents.RaiseBombLit(transform.position, BlastRadius);
				RaiseAttacked("windup");
				GameEvents.RaiseSound("warning");
			}
		}

		private void Explode()
		{
			fuseEndsAt = 0f;
			Vector3 center = transform.position;
			GameEvents.RaiseExploded(center, BlastRadius);
			GameEvents.RaiseSound("heavy");
			CameraRig.Shake(4f, 0.18f, 0.02f);
			SlowMotion.Pulse(0.35f, 0.25f);

			if (target != null && !target.IsDead && Vector3.Distance(target.transform.position, center) <= BlastRadius)
			{
				target.TakeDamage(2, gameObject);
			}
			TakeDamage(999, gameObject);
		}
	}
}
