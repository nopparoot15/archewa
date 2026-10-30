using UnityEngine;

namespace MyArcade
{
	/// <summary>
	/// Tunable numbers. Weapons, abilities and the player READ these instead of having
	/// fixed values, so new tuning never needs a change in the weapon classes (open/closed principle).
	/// </summary>
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
