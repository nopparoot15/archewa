using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	// Nine more rival types. Every one is a small subclass: it changes numbers, overrides ONE method,
	// or adds an interface. Nothing in Spawner / GameManager / the weapons has to know they exist
	// (they only see the base class Enemy). This is the OOP showcase of the game.

	// ---- inheritance: same behaviour, different numbers ------------------------------------------

	/// <summary>A Chaser that is faster and hits sooner, but is smaller. Only constructor-style numbers change.</summary>
	public class Sprinter : Chaser
	{
		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 6.8f;
			windupTime = 0.28f;
			contactCooldown = 0.7f;
			displayName = "STREET RACER";
			ResetHealth();
		}
	}

	/// <summary>Overrides Behave(): adds a sideways weave on top of the normal Chaser behaviour (base.Behave()).</summary>
	public class Zigzagger : Chaser
	{
		private float phase;

		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 5.0f;
			displayName = "SWAGGERING THUG";
			ResetHealth();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			phase = UnityEngine.Random.value * 6f;
		}

		protected override void Behave()
		{
			base.Behave();
			if (DistanceToTarget() > contactRange + 1f)
			{
				Vector3 side = Vector3.Cross(Vector3.up, DirectionToTarget());
				Move(side * Mathf.Sin((Time.time + phase) * 4.5f), 1.4f);
			}
		}
	}

	/// <summary>
	/// Overrides TakeDamage() (polymorphism): a hit from the front is blocked by the shield, so you must
	/// get behind him. He also turns slowly (turnSpeed) to give you the time.
	/// </summary>
	public class Shieldman : Chaser
	{
		private float blockPoseUntil;
		public override string VisualPose => Time.time < blockPoseUntil ? "guard" : base.VisualPose;
		public override void OnSpawn() { blockPoseUntil = 0f; base.OnSpawn(); }
		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 3.8f;
			turnSpeed = 2.2f;
			contactCooldown = 1.2f;
			displayName = "SHIELD MECHANIC";
			ResetHealth();
		}

		public override void TakeDamage(int amount, GameObject source)
		{
			if (IsDead)
			{
				return;
			}
			if (source != null)
			{
				Vector3 toAttacker = source.transform.position - transform.position;
				toAttacker.y = 0f;
				if (toAttacker.sqrMagnitude > 0.01f && Vector3.Dot(transform.forward, toAttacker.normalized) > 0.35f)
				{
					blockPoseUntil = Time.time + .18f;
					GameEvents.RaiseSound("deflect");
					GameEvents.RaiseHitLanded(transform.position + Vector3.up + transform.forward, 1);
					return; // blocked
				}
			}
			base.TakeDamage(amount, source);
		}
	}

	/// <summary>Overrides Die(): when it falls, two Sprinters jump out. The Spawner listens to the event, the Splitter does not know it.</summary>
	public class Splitter : Chaser
	{
		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 4.4f;
			displayName = "GANG SWARM";
			ResetHealth();
		}

		protected override void Die()
		{
			GameEvents.RaiseEnemySplit(transform.position);
			base.Die();
		}
	}

	// ---- inheritance from Shooter: override one virtual method / change numbers ------------------

	/// <summary>Far away, long warning, very fast stone.</summary>
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

	/// <summary>Overrides Fire(): three stones in a fan instead of one.</summary>
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

	// ---- interfaces: abilities that unrelated classes can share ----------------------------------

	/// <summary>Implements IExplosive: runs in, lights a fuse, blows up and hurts the hero if close.</summary>
	public class Bomber : Enemy, IExplosive
	{
		private const float FuseTime = 0.85f;
		private const float TriggerRange = 2.4f;

		private float fuseEndsAt;

		public float BlastRadius => 3.4f;
		public override string VisualPose => fuseEndsAt > 0f ? "warn" : null;
		public override string DisplayName => "FIRECRACKER KID";

		protected override void Awake()
		{
			moveSpeed = 6.0f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			fuseEndsAt = 0f;
		}

		protected override void Behave()
		{
			FaceTarget();
			if (fuseEndsAt > 0f)
			{
				if (Time.time >= fuseEndsAt)
				{
					Explode();
				}
				return; // stands still while the fuse burns: run out of the radius
			}
			if (DistanceToTarget() > TriggerRange)
			{
				Move(DirectionToTarget());
			}
			else
			{
				fuseEndsAt = Time.time + FuseTime;
				GameEvents.RaiseBombLit(transform.position, BlastRadius);
				RaiseAttacked("windup");
				GameEvents.RaiseSound("warning");
			}
		}

		private void Explode()
		{
			fuseEndsAt = 0f;
			Vector3 center = transform.position;
			GameEvents.RaiseExploded(center, BlastRadius);
			GameEvents.RaiseSound("heavy");
			CameraRig.Shake(4f, 0.18f, 0.02f);
			SlowMotion.Pulse(0.35f, 0.25f);

			if (target != null && !target.IsDead && Vector3.Distance(target.transform.position, center) <= BlastRadius)
			{
				target.TakeDamage(2, gameObject);
			}
			TakeDamage(999, gameObject);
		}
	}

	/// <summary>Implements IBuffer: keeps its distance and shouts, making every rival near it faster for a moment. Kill it first.</summary>
	public class Commander : Enemy, IBuffer
	{
		private const float ShoutEvery = 4f;
		private const float ShoutRadius = 9f;

		private float nextShoutTime;

		public override string DisplayName => "RINGLEADER";

		protected override void Awake()
		{
			moveSpeed = 2.8f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			nextShoutTime = Time.time + 2f;
		}

		public void PulseBuff()
		{
			var all = new List<Enemy>(Enemy.AllActive);
			foreach (Enemy other in all)
			{
				if (other != this && !other.IsDead && Vector3.Distance(other.transform.position, transform.position) <= ShoutRadius)
				{
					other.ApplyBuff(1.45f, 3f);
				}
			}
			RaiseAttacked("windup");
			GameEvents.RaiseSound("warning");
		}

		protected override void Behave()
		{
			FaceTarget();
			float distance = DistanceToTarget();
			if (distance < 7f)
			{
				Move(-DirectionToTarget());
			}
			else if (distance > 10f)
			{
				Move(DirectionToTarget());
			}
			if (Time.time >= nextShoutTime)
			{
				nextShoutTime = Time.time + ShoutEvery;
				PulseBuff();
			}
		}
	}

	// ---- a different state machine with the same base class -------------------------------------

	/// <summary>Stops, snorts (warning), then rams in a straight line; after the ram it is tired for a moment.</summary>
	public class Charger : Enemy
	{
		private const float WarnTime = 0.7f;
		private const float RamTime = 0.7f;
		private const float RamSpeed = 6.5f;
		private const float TiredTime = 1.3f;

		private enum Phase { Walk, Warn, Ram, Tired }

		private Phase phase;
		private float phaseEndsAt;
		private Vector3 ramDirection;
		private bool hit;
		public override string VisualPose => phase == Phase.Warn ? "warn" : phase == Phase.Ram ? "charge" : phase == Phase.Tired ? "open" : null;

		public override string DisplayName => "RAGING BULL";

		protected override void Awake()
		{
			moveSpeed = 4.5f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			phase = Phase.Walk;
			phaseEndsAt = Time.time + 1.5f;
		}

		protected override void Behave()
		{
			switch (phase)
			{
				case Phase.Walk:
					FaceTarget();
					Move(DirectionToTarget());
					if (Time.time >= phaseEndsAt && DistanceToTarget() < 9f && DistanceToTarget() > 3f)
					{
						phase = Phase.Warn;
						phaseEndsAt = Time.time + WarnTime;
						RaiseAttacked("windup");
						GameEvents.RaiseSound("warning");
					}
					break;
				case Phase.Warn:
					FaceTarget();
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Ram;
						phaseEndsAt = Time.time + RamTime;
						ramDirection = DirectionToTarget();
						hit = false;
						RaiseAttacked("attack-kick-right");
					}
					break;
				case Phase.Ram:
					Move(ramDirection, RamSpeed / moveSpeed);
					if (!hit && DistanceToTarget() < 1.6f)
					{
						hit = true;
						target.TakeDamage(1, gameObject);
						target.ApplyKnockback(ramDirection * 10f);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, 2);
					}
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Tired;
						phaseEndsAt = Time.time + TiredTime;
					}
					break;
				default:
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Walk;
						phaseEndsAt = Time.time + 2f;
					}
					break;
			}
		}
	}

	// ---- data-driven catalog: one table says what exists, how to build it and when it appears -----

	/// <summary>One row of the catalog: how to create the enemy, how it looks, and when/how often it spawns.</summary>
	public sealed class EnemySpec
	{
		public EnemyType Type;
		public Func<GameObject, Enemy> Create;
		public Color Accent;
		public float Height;
		public int UnlockKills;     // appears after this many rivals have been beaten
		public int BaseWeight;      // spawn weight once unlocked
		public int WeightPerTier;   // extra weight for every 100 kills
		public bool InCrowd = true; // false = never picked by the random crowd (the Boss is spawned on purpose)
		public bool IsSpecial;
	}

	/// <summary>
	/// Factory + registry. Adding a new rival = write one subclass and add ONE line here;
	/// the Spawner and SceneFactory loop over this table and never change.
	/// </summary>
	public static class EnemyCatalog
	{
		public static readonly IReadOnlyList<EnemySpec> All = new List<EnemySpec>
		{
			new EnemySpec { Type = EnemyType.Chaser, Create = g => g.AddComponent<Chaser>(), Accent = new Color(1f, 0.15f, 0.2f), Height = 1.75f, UnlockKills = 0, BaseWeight = 10 },
			new EnemySpec { Type = EnemyType.Sprinter, Create = g => g.AddComponent<Sprinter>(), Accent = new Color(1f, 0.95f, 0.2f), Height = 1.5f, UnlockKills = 8, BaseWeight = 4, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Shooter, IsSpecial = true, Create = g => g.AddComponent<Shooter>(), Accent = new Color(1f, 0.85f, 0.1f), Height = 1.7f, UnlockKills = 0, BaseWeight = 3, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Zigzagger, Create = g => g.AddComponent<Zigzagger>(), Accent = new Color(0.4f, 0.8f, 1f), Height = 1.7f, UnlockKills = 15, BaseWeight = 3, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Bomber, Create = g => g.AddComponent<Bomber>(), Accent = new Color(1f, 0.45f, 0.1f), Height = 1.55f, UnlockKills = 20, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Splitter, Create = g => g.AddComponent<Splitter>(), Accent = new Color(0.9f, 0.4f, 0.9f), Height = 2.0f, UnlockKills = 25, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Assassin, IsSpecial = true, Create = g => g.AddComponent<Assassin>(), Accent = new Color(0.25f, 1f, 0.45f), Height = 1.65f, UnlockKills = 100, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Shieldman, Create = g => g.AddComponent<Shieldman>(), Accent = new Color(0.6f, 0.65f, 0.75f), Height = 1.9f, UnlockKills = 35, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Charger, Create = g => g.AddComponent<Charger>(), Accent = new Color(0.8f, 0.3f, 0.2f), Height = 2.0f, UnlockKills = 40, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Shotgunner, IsSpecial = true, Create = g => g.AddComponent<Shotgunner>(), Accent = new Color(1f, 0.6f, 0.3f), Height = 1.75f, UnlockKills = 45, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Brute, IsSpecial = true, Create = g => g.AddComponent<Brute>(), Accent = new Color(0.75f, 0.2f, 1f), Height = 2.3f, UnlockKills = 30, BaseWeight = 1, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Commander, IsSpecial = true, Create = g => g.AddComponent<Commander>(), Accent = new Color(1f, 0.8f, 0.5f), Height = 1.85f, UnlockKills = 60, BaseWeight = 1, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Sniper, IsSpecial = true, Create = g => g.AddComponent<Sniper>(), Accent = new Color(0.3f, 1f, 0.9f), Height = 1.7f, UnlockKills = 70, BaseWeight = 1, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Boss, Create = g => g.AddComponent<Boss>(), Accent = new Color(1f, 0.55f, 0.05f), Height = 3.0f, InCrowd = false }
		};

		public static EnemySpec Get(EnemyType type)
		{
			foreach (EnemySpec spec in All)
			{
				if (spec.Type == type)
				{
					return spec;
				}
			}
			return All[0];
		}

		/// <summary>Weighted random pick among the regular or the special types unlocked by the number of kills so far.</summary>
		public static EnemyType PickRandom(bool special, int normalKills, int tier)
		{
			int total = 0;
			foreach (EnemySpec spec in All)
			{
				total += WeightOf(spec, special, normalKills, tier);
			}
			if (total <= 0)
			{
				return special ? EnemyType.Shooter : EnemyType.Chaser;
			}
			int roll = UnityEngine.Random.Range(0, total);
			foreach (EnemySpec spec in All)
			{
				roll -= WeightOf(spec, special, normalKills, tier);
				if (roll < 0)
				{
					return spec.Type;
				}
			}
			return EnemyType.Chaser;
		}

		private static int WeightOf(EnemySpec spec, bool special, int normalKills, int tier)
		{
			return spec.InCrowd && spec.IsSpecial == special && normalKills >= spec.UnlockKills ? spec.BaseWeight + spec.WeightPerTier * tier : 0;
		}
	}
}
