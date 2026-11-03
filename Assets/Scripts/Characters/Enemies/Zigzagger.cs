using UnityEngine;

namespace MyArcade
{
	public class Zigzagger : Chaser
	{
		private float phase;

		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 5.0f;
			displayName = "SWAGGERING THUG";
			ResetHealth();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			phase = UnityEngine.Random.value * 6f;
		}

		protected override void Behave()
		{
			base.Behave();
			if (DistanceToTarget() > contactRange + 1f)
			{
				Vector3 side = Vector3.Cross(Vector3.up, DirectionToTarget());
				Move(side * Mathf.Sin((Time.time + phase) * 4.5f), 1.4f);
			}
		}
	}
}
