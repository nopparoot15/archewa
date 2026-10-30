using UnityEngine;

namespace MyArcade
{
	/// <summary>Which side an object fights for. Attacks only hurt the opposite team.</summary>
	public enum Team
	{
		Player,
		Enemy
	}

	/// <summary>
	/// Anything that can take damage. Weapons and bullets know ONLY this interface,
	/// so they work on the player, every enemy type, and any future target (crate, boss...).
	/// </summary>
	public interface IDamageable
	{
		Team Team { get; }
		bool IsDead { get; }
		void TakeDamage(int amount, GameObject source);
	}


	public interface IShootable
	{
		bool TakeShot(Bullet bullet);
	}

	/// <summary>Optional ability: can be pushed back by a hit.</summary>
	public interface IKnockbackable
	{
		void ApplyKnockback(Vector3 impulse);
	}

	/// <summary>Objects that are recycled by an <see cref="ObjectPool{T}"/> instead of destroyed.</summary>
	public interface IPoolable
	{
		void OnSpawn();
		void OnDespawn();
	}

	/// <summary>
	/// Where player commands come from. Player depends on this interface, not on the keyboard,
	/// so a gamepad, an AI or a replay could be plugged in without touching Player.
	/// </summary>
	public interface IInputSource
	{
		Vector3 MoveDirection { get; }
		Vector2 PointerScreenPosition { get; }
		bool AttackHeld { get; }
		/// <summary>Held = aim the pen gun (Shift); the attack button then shoots instead of swinging.</summary>
		bool AimHeld { get; }
		/// <summary>0-based weapon slot pressed this frame, or -1.</summary>
		int WeaponSlotPressed { get; }
		bool RestartPressed { get; }
		bool StartPressed { get; }
		/// <summary>-1 = up, +1 = down, pressed this frame (menus).</summary>
		int MenuMove { get; }
		/// <summary>-1 = left, +1 = right, pressed this frame (menu sliders and switches).</summary>
		int MenuAdjust { get; }
		/// <summary>Enter or Space in a menu.</summary>
		bool ConfirmPressed { get; }
		/// <summary>Left mouse button went down this frame (menu click).</summary>
		bool PointerPressed { get; }
		bool DashPressed { get; }
		/// <summary>Space: start aiming the special slash (when a part of the bar is full).</summary>
		bool SpecialPressed { get; }
		bool ParryPressed { get; }
		bool PausePressed { get; }
		bool MutePressed { get; }
	}

	/// <summary>Anything that can create bullets (used by ranged weapons).</summary>
	public interface IBulletSpawner
	{
		void Spawn(Vector3 position, Vector3 direction, float speed, int damage, Team team, GameObject owner);
	}

	/// <summary>
	/// A special move of the player (dash, parry...). The player owns a list of these and
	/// the UI draws one bar per ability using only this interface.
	/// </summary>
	public interface IAbility
	{
		string DisplayName { get; }
		string KeyHint { get; }
		/// <summary>0 = not ready, 1 = ready (cooldown or charge progress).</summary>
		float Ready01 { get; }
		bool TryActivate(Player user, Vector3 aimDirection);
	}


	public interface IBulletDeflector
	{
		/// <summary>Returns true if the bullet was sent back.</summary>
		bool Deflect(Bullet bullet, AttackContext context);
	}

	/// <summary>Something that blows up and hurts everything around it (the Bomber). Only the radius is public.</summary>
	public interface IExplosive
	{
		float BlastRadius { get; }
	}

	/// <summary>Something that strengthens other rivals around it (the Commander's speed shout).</summary>
	public interface IBuffer
	{
		void PulseBuff();
	}

	/// <summary>Everything a weapon needs to know about one attack.</summary>
	public readonly struct AttackContext
	{
		public readonly GameObject Owner;
		public readonly Team Team;
		public readonly Vector3 Origin;
		public readonly Vector3 Direction;
		public readonly IBulletSpawner Bullets;
		public readonly PlayerStats Stats; // null for enemies

		public AttackContext(GameObject owner, Team team, Vector3 origin, Vector3 direction, IBulletSpawner bullets, PlayerStats stats = null)
		{
			Owner = owner;
			Team = team;
			Origin = origin;
			Direction = direction;
			Bullets = bullets;
			Stats = stats;
		}
	}
}
