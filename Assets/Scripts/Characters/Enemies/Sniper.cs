using UnityEngine;

namespace MyArcade
{
	public class Sniper : Shooter
	{
		public override string DisplayName => "ABANDONED-BUILDING SNIPER";

		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 2.0f;
			preferredDistance = 11f;
			aimTime = 1.4f;
			shotsPerBurst = 1;
			cooldown = 3.2f;
			bulletSpeed = 120f;
			shootRange = 18f;
		}
	}
}
