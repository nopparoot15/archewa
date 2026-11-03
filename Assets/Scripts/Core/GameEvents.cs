using System;
using UnityEngine;

namespace MyArcade
{
	public static class GameEvents
	{
		public static event Action<Enemy> EnemyKilled;
		public static event Action<int, int> PlayerHealthChanged; // current, max
		public static event Action PlayerDied;
		public static event Action<int> WaveStarted;
		public static event Action<int> WeaponChanged; // slot index
		public static event Action<Vector3, int> HitLanded; // where, damage (camera shake, sparks)
		public static event Action<int> ComboChanged;
		public static event Action<Enemy> BossChanged; // boss appeared (or null when it is gone)
		public static event Action<Vector3> BossCharging; // boss warns before charging
		public static event Action<string> SoundRequested; // "hit", "deflect"...
		public static event Action<Vector3> Deflected; // a stone was hit back
		public static event Action<Vector3, Vector3, float, float> AttackSwung; // origin, direction, reach, arc (for the swing trail)
		public static event Action<Vector3> DashStarted;
		public static event Action BulletDodged; // an enemy bullet flew past the hero
		public static event Action BulletHitEnemy; // one of the hero's bullets hit a rival
		public static event Action<int> SlashLanded; // the dragon slash cut this many rivals
		public static event Action<int> UltimateKilled; // the ultimate finished this many rivals
		public static event Action SwingStarted; // the hero swung the pipe
		public static event Action SwingConnected; // ...and that swing hit someone
		public static event Action<Vector3, float> BombLit; // a Bomber lit its fuse: where, blast radius
		public static event Action<Vector3, float> Exploded; // a Bomber blew up: where, blast radius
		public static event Action<Vector3> EnemySplit; // a Splitter died: the spawner adds its offspring

		public static void RaiseEnemyKilled(Enemy enemy) { EnemyKilled?.Invoke(enemy); }
		public static void RaisePlayerHealthChanged(int current, int max) { PlayerHealthChanged?.Invoke(current, max); }
		public static void RaisePlayerDied() { PlayerDied?.Invoke(); }
		public static void RaiseWaveStarted(int wave) { WaveStarted?.Invoke(wave); }
		public static void RaiseWeaponChanged(int slot) { WeaponChanged?.Invoke(slot); }
		public static void RaiseHitLanded(Vector3 position, int damage) { HitLanded?.Invoke(position, damage); }
		public static void RaiseComboChanged(int combo) { ComboChanged?.Invoke(combo); }
		public static void RaiseBossChanged(Enemy boss) { BossChanged?.Invoke(boss); }
		public static void RaiseBossCharging(Vector3 position) { BossCharging?.Invoke(position); }
		public static void RaiseSound(string id) { SoundRequested?.Invoke(id); }
		public static void RaiseDeflected(Vector3 position) { Deflected?.Invoke(position); }
		public static void RaiseAttackSwung(Vector3 origin, Vector3 direction, float reach, float arc) { AttackSwung?.Invoke(origin, direction, reach, arc); }
		public static void RaiseDashStarted(Vector3 position) { DashStarted?.Invoke(position); }
		public static void RaiseBulletDodged() { BulletDodged?.Invoke(); }
		public static void RaiseBulletHitEnemy() { BulletHitEnemy?.Invoke(); }
		public static void RaiseSlashLanded(int count) { SlashLanded?.Invoke(count); }
		public static void RaiseUltimateKilled(int count) { UltimateKilled?.Invoke(count); }
		public static void RaiseSwingStarted() { SwingStarted?.Invoke(); }
		public static void RaiseSwingConnected() { SwingConnected?.Invoke(); }
		public static void RaiseBombLit(Vector3 position, float radius) { BombLit?.Invoke(position, radius); }
		public static void RaiseExploded(Vector3 position, float radius) { Exploded?.Invoke(position, radius); }
		public static void RaiseEnemySplit(Vector3 position) { EnemySplit?.Invoke(position); }

		public static void Clear()
		{
			EnemyKilled = null;
			PlayerHealthChanged = null;
			PlayerDied = null;
			WaveStarted = null;
			WeaponChanged = null;
			HitLanded = null;
			ComboChanged = null;
			BossChanged = null;
			BossCharging = null;
			SoundRequested = null;
			Deflected = null;
			AttackSwung = null;
			DashStarted = null;
			EnemySplit = null;
			BombLit = null;
			Exploded = null;
			BulletDodged = null;
			BulletHitEnemy = null;
			SlashLanded = null;
			UltimateKilled = null;
			SwingStarted = null;
			SwingConnected = null;
		}
	}
}
