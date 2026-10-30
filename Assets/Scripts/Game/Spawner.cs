using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	/// <summary>
	/// Builds waves, creates enemies. One pool per enemy type; the Spawner does not care
	/// which subclass it spawns, it only handles the base type Enemy.
	/// </summary>
	public class Spawner : MonoBehaviour
	{
		private const int BossEveryKills = 100;
		private const int EnemiesAfterBoss = 10;

		private Player player;
		private IBulletSpawner bullets;
		private readonly Dictionary<EnemyType, ObjectPool<Enemy>> pools = new Dictionary<EnemyType, ObjectPool<Enemy>>();
		private int alive;
		private int aliveRegular;          // Akane's EnemyRegular.cont: the crowd limit only counts regular rivals
		private bool openingDone;
		private float nextRegularAt;
		private float nextSpecialAt;
		private float bossSpawnTime;
		private bool bossSpawned;

		// read by the UI (statics reset in Setup)
		public static int NormalKills { get; private set; }
		public static int NextBossAt { get; private set; }
		public static int BossesDefeated { get; private set; }
		public static bool BossActive { get; private set; }

		/// <summary>Endless mode: rivals stream in; every 100 kills the arena is cleared for a 1v1 with a boss.</summary>
		public void Setup(Player player, IBulletSpawner bullets)
		{
			this.player = player;
			this.bullets = bullets;
			NormalKills = 0;
			NextBossAt = BossEveryKills;
			BossesDefeated = 0;
			BossActive = false;
			GameEvents.EnemyKilled += OnEnemyKilled;
			GameEvents.EnemySplit += OnEnemySplit;
			Enemy.Frozen = false;
			Chaser.ResetAttackToken();
			nextRegularAt = Time.time + 1.5f;
			nextSpecialAt = Time.time + SpecialSpawnSeconds(0);
		}

		private void OnDestroy()
		{
			GameEvents.EnemyKilled -= OnEnemyKilled;
			GameEvents.EnemySplit -= OnEnemySplit;
		}

		private void OnEnemyKilled(Enemy enemy)
		{
			alive--;
			bool isBoss = enemy is Boss;
			if (isBoss)
			{
				BossActive = false;
				BossesDefeated++;
				NextBossAt = NormalKills + BossEveryKills;
				nextRegularAt = Time.time + 2.5f;
				for (int i = 0; i < EnemiesAfterBoss; i++)
				{
					TrySpawnRegular(); // enemiesSpawnedAfterDead = 10
				}
			}
			else
			{
				NormalKills++;
				if (!EnemyCatalog.Get(enemy.Kind).IsSpecial)
				{
					aliveRegular--;
				}
				TrySpawnRegular(); // every rival that falls is replaced at once (Akane: Spawner.Spawn() in every TakeRegularMelee)
			}
		}

		private void Update()
		{
			if (player == null || player.IsDead || Enemy.Frozen)
			{
				return;
			}

			if (BossActive)
			{
				if (!bossSpawned && Time.time >= bossSpawnTime)
				{
					SpawnBoss();
				}
				return; // 1v1: nothing else spawns while the boss lives
			}

			if (NormalKills >= NextBossAt)
			{
				StartBossFight();
				return;
			}

			// Akane's Spawner coroutines: a regular rival every few seconds, a special one on a slower timer
			if (Time.time >= nextRegularAt)
			{
				nextRegularAt = Time.time + Mathf.Lerp(6f, 3.5f, NormalKills / 150f);
				TrySpawnRegular();
				if (!openingDone)
				{
					openingDone = true; // a first small group so the street is not empty at the start
					TrySpawnRegular();
					TrySpawnRegular();
				}
			}
			if (Time.time >= nextSpecialAt)
			{
				nextSpecialAt = Time.time + SpecialSpawnSeconds(NormalKills);
				SpawnEnemy(EnemyCatalog.PickRandom(true, NormalKills, NormalKills / BossEveryKills));
			}
		}

		/// <summary>Akane DificultParameters, all linear in the kill count: regular limit 4 to 35 over 50 kills (plus 3 per boss here).</summary>
		private int RegularLimit()
		{
			return Mathf.RoundToInt(Mathf.Lerp(3f, 24f, NormalKills / 150f)) + BossesDefeated * 3;
		}

		private static float SpecialSpawnSeconds(int kills)
		{
			return Mathf.Lerp(25f, 8f, kills / 500f);
		}

		/// <summary>Akane: the regular rivals' speed climbs from 110 to 180 (the hero's speed) over 1000 kills.</summary>
		private float RegularSpeedScale()
		{
			return Mathf.Lerp(95f, 165f, NormalKills / 1500f) / 110f;
		}

		private void TrySpawnRegular()
		{
			if (BossActive || aliveRegular >= RegularLimit())
			{
				return;
			}
			SpawnEnemy(EnemyCatalog.PickRandom(false, NormalKills, NormalKills / BossEveryKills));
		}

		private void StartBossFight()
		{
			Enemy.RetireAll();
			alive = 0;
			aliveRegular = 0;
			BossActive = true;
			bossSpawned = false;
			bossSpawnTime = Time.time + 1.8f;
			GameEvents.RaiseWaveStarted(BossesDefeated + 1); // the UI shows the boss banner
		}

		private void SpawnBoss()
		{
			bossSpawned = true;
			Enemy boss = GetPool(EnemyType.Boss).Get(Arena.RandomHiddenEdgePoint(), Quaternion.identity);
			// each boss is tougher and quicker than the last
			boss.ApplyLevel(1f + BossesDefeated * 0.25f, 1f); // later bosses stay open for less time (see Boss.OpenSeconds)
			alive++;
		}

		/// <summary>A Splitter fell: two Sprinters jump out of it.</summary>
		private void OnEnemySplit(Vector3 at)
		{
			if (BossActive)
			{
				return;
			}
			for (int i = 0; i < 2; i++)
			{
				Vector3 offset = Quaternion.Euler(0f, i * 180f + 40f, 0f) * Vector3.forward * 1.4f;
				GetPool(EnemyType.Sprinter).Get(Arena.Clamp(at + offset), Quaternion.identity).ApplyLevel(1f, RegularSpeedScale());
				alive++;
				aliveRegular++;
			}
		}

		private void SpawnEnemy(EnemyType type)
		{
			bool special = EnemyCatalog.Get(type).IsSpecial;
			Enemy enemy = GetPool(type).Get(Arena.RandomHiddenEdgePoint(), Quaternion.identity);
			enemy.ApplyLevel(1f + NormalKills / BossEveryKills * 0.15f, special ? 1f : RegularSpeedScale());
			alive++;
			if (!special)
			{
				aliveRegular++;
			}
		}

		private ObjectPool<Enemy> GetPool(EnemyType type)
		{
			if (!pools.TryGetValue(type, out ObjectPool<Enemy> pool))
			{
				ObjectPool<Enemy> created = null;
				created = new ObjectPool<Enemy>(() =>
				{
					Enemy enemy = SceneFactory.CreateEnemy(type, transform);
					enemy.Init(player, e => created.Release(e));
					if (enemy is Shooter shooter)
					{
						shooter.SetBullets(bullets);
					}
					if (enemy is Boss gangLeader)
					{
						gangLeader.SetBullets(bullets);
					}
					return enemy;
				});
				pools[type] = created;
				pool = created;
			}
			return pool;
		}
	}
}
