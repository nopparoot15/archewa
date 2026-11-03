using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public class Commander : Enemy, IBuffer
	{
		private const float ShoutEvery = 4f;
		private const float ShoutRadius = 9f;

		private float nextShoutTime;

		public override string DisplayName => "RINGLEADER";

		protected override void Awake()
		{
			moveSpeed = 2.8f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			nextShoutTime = Time.time + 2f;
		}

		public void PulseBuff()
		{
			var all = new List<Enemy>(Enemy.AllActive);
			foreach (Enemy other in all)
			{
				if (other != this && !other.IsDead && Vector3.Distance(other.transform.position, transform.position) <= ShoutRadius)
				{
					other.ApplyBuff(1.45f, 3f);
				}
			}
			RaiseAttacked("windup");
			GameEvents.RaiseSound("warning");
		}

		protected override void Behave()
		{
			FaceTarget();
			float distance = DistanceToTarget();
			if (distance < 7f)
			{
				Move(-DirectionToTarget());
			}
			else if (distance > 10f)
			{
				Move(DirectionToTarget());
			}
			if (Time.time >= nextShoutTime)
			{
				nextShoutTime = Time.time + ShoutEvery;
				PulseBuff();
			}
		}
	}
}
