using UnityEngine;

namespace MyArcade
{
	public class Shotgunner : Shooter
	{
		public override string DisplayName => "SHORT-GUN MECHANIC";

		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 2.8f;
			preferredDistance = 5.5f;
			aimTime = 0.8f;
			shotsPerBurst = 1;
			cooldown = 2.6f;
			bulletSpeed = 60f;
			shootRange = 10f;
		}

		protected override void Fire(Vector3 direction)
		{
			for (int i = -1; i <= 1; i++)
			{
				Shoot(Quaternion.Euler(0f, i * 18f, 0f) * direction, bulletSpeed);
			}
		}
	}
}
