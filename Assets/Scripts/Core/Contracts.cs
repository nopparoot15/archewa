using UnityEngine;

namespace MyArcade
{
	public enum Team
	{
		Player,
		Enemy
	}

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

	public interface IKnockbackable
	{
		void ApplyKnockback(Vector3 impulse);
	}

	public interface IPoolable
	{
		void OnSpawn();
		void OnDespawn();
	}

	public interface IInputSource
	{
		Vector3 MoveDirection { get; }
		Vector2 PointerScreenPosition { get; }
		bool AttackHeld { get; }
		bool AimHeld { get; }
		int WeaponSlotPressed { get; }
		bool RestartPressed { get; }
		bool StartPressed { get; }
		int MenuMove { get; }
		int MenuAdjust { get; }
		bool ConfirmPressed { get; }
		bool PointerPressed { get; }
		bool DashPressed { get; }
		bool SpecialPressed { get; }
		bool ParryPressed { get; }
		bool PausePressed { get; }
		bool MutePressed { get; }
	}

	public interface IBulletSpawner
	{
		void Spawn(Vector3 position, Vector3 direction, float speed, int damage, Team team, GameObject owner);
	}

	public interface IAbility
	{
		string DisplayName { get; }
		string KeyHint { get; }
		float Ready01 { get; }
		bool TryActivate(Player user, Vector3 aimDirection);
	}

	public interface IBulletDeflector
	{
		bool Deflect(Bullet bullet, AttackContext context);
	}

	public interface IExplosive
	{
		float BlastRadius { get; }
	}

	public interface IBuffer
	{
		void PulseBuff();
	}

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
