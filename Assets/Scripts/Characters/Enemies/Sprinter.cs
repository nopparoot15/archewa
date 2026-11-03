using UnityEngine;

namespace MyArcade
{
	public class Sprinter : Chaser
	{
		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 6.8f;
			windupTime = 0.34f;
			contactCooldown = 0.7f;
			displayName = "STREET RACER";
			ResetHealth();
		}
	}
}
