using UnityEngine;

namespace MyArcade
{
	/// <summary>
	/// The dash, with Akane's RegularDash numbers (DashsAndPerks.asset): speed 650 against a running speed of 180, for 0.125 s
	/// (about 3.7 units), then 0.105 s more before the next one. It only works while running, ends a parry, and gives no
	/// invulnerability: it is a quick step out of the way, nothing more.
	/// </summary>
	public class DashAbility : IAbility
	{
		private const float Duration = 0.125f;
		private const float Rest = 0.105f;
		private const float SpeedRatio = 650f / 180f; // dash speed over running speed

		private float readyAt;

		public string DisplayName => "DASH";
		public string KeyHint => "RMB";
		public float Ready01 => Mathf.Clamp01(1f - (readyAt - Time.time) / (Duration + Rest));

		public bool TryActivate(Player user, Vector3 aimDirection)
		{
			if (Time.time < readyAt || user.LastMoveDirection.sqrMagnitude < 0.01f || user.IsAiming)
			{
				return false;
			}
			readyAt = Time.time + Duration + Rest;
			user.CancelParryForDash();
			user.StartDash(user.LastMoveDirection.normalized, Duration, SpeedRatio * Player.RunSpeedRatio);
			GameEvents.RaiseSound("dash");
			GameEvents.RaiseDashStarted(user.transform.position);
			return true;
		}
	}

	/// <summary>
	/// Parry (E, same key as Akane): a guard stance of up to 1.5 s that blocks every melee hit and sends bullets that
	/// reach the hero back. The rules (rest time, ending on attack) live in Player.
	/// </summary>
	public class ParryAbility : IAbility
	{
		public string DisplayName => "PARRY";
		public string KeyHint => "E";
		public float Ready01 { get; private set; } = 1f;

		public bool TryActivate(Player user, Vector3 aimDirection)
		{
			return user.TryStartParry();
		}
	}
}
