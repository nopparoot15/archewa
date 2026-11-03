using UnityEngine;

namespace MyArcade
{
	public class PlayerStats
	{
		public int DamageBonus { get; set; }
		public float AttackIntervalScale { get; set; } = 1f;
		public float DashCooldownScale { get; set; } = 1f;
		public float MoveSpeedScale { get; set; } = 1f;
		public float AmmoRegenScale { get; set; } = 1f;

		public void Reset()
		{
			DamageBonus = 0;
			AttackIntervalScale = 1f;
			DashCooldownScale = 1f;
			MoveSpeedScale = 1f;
			AmmoRegenScale = 1f;
		}
	}
}
