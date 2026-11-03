using UnityEngine;

namespace MyArcade
{
	public class Shieldman : Chaser
	{
		private float blockPoseUntil;
		public override string VisualPose => Time.time < blockPoseUntil ? "guard" : base.VisualPose;
		public override void OnSpawn() { blockPoseUntil = 0f; base.OnSpawn(); }
		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 3.8f;
			turnSpeed = 2.2f;
			contactCooldown = 1.2f;
			displayName = "SHIELD MECHANIC";
			ResetHealth();
		}

		public override void TakeDamage(int amount, GameObject source)
		{
			if (IsDead)
			{
				return;
			}
			if (source != null)
			{
				Vector3 toAttacker = source.transform.position - transform.position;
				toAttacker.y = 0f;
				if (toAttacker.sqrMagnitude > 0.01f && Vector3.Dot(transform.forward, toAttacker.normalized) > 0.35f)
				{
					blockPoseUntil = Time.time + .18f;
					GameEvents.RaiseSound("deflect");
					GameEvents.RaiseHitLanded(transform.position + Vector3.up + transform.forward, 1);
					return; // blocked
				}
			}
			base.TakeDamage(amount, source);
		}
	}
}
