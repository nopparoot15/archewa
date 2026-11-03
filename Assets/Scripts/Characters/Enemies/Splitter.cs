using UnityEngine;

namespace MyArcade
{
	public class Splitter : Chaser
	{
		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 4.4f;
			displayName = "GANG SWARM";
			ResetHealth();
		}

		protected override void Die()
		{
			GameEvents.RaiseEnemySplit(transform.position);
			base.Die();
		}
	}
}
