using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public sealed class EnemySpec
	{
		public EnemyType Type;
		public float Height;
		public int UnlockKills; // appears after this many rivals have been beaten
		public int BaseWeight; // spawn weight once unlocked
		public int WeightPerTier; // extra weight for every 100 kills
		public bool InCrowd = true; // false = never picked by the random crowd (the Boss is spawned on purpose)
		public bool IsSpecial;
	}

	public static class EnemyCatalog
	{
		public static readonly IReadOnlyList<EnemySpec> All = new List<EnemySpec>
		{
			new EnemySpec { Type = EnemyType.Chaser, Height = 1.75f, UnlockKills = 0, BaseWeight = 10 },
			new EnemySpec { Type = EnemyType.Sprinter, Height = 1.5f, UnlockKills = 8, BaseWeight = 4, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Shooter, IsSpecial = true, Height = 1.7f, UnlockKills = 0, BaseWeight = 3, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Zigzagger, Height = 1.7f, UnlockKills = 15, BaseWeight = 3, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Bomber, Height = 1.55f, UnlockKills = 20, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Splitter, Height = 2.0f, UnlockKills = 25, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Assassin, IsSpecial = true, Height = 1.65f, UnlockKills = 100, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Shieldman, Height = 1.9f, UnlockKills = 35, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Charger, Height = 2.0f, UnlockKills = 40, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Shotgunner, IsSpecial = true, Height = 1.75f, UnlockKills = 45, BaseWeight = 2, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Brute, IsSpecial = true, Height = 2.3f, UnlockKills = 30, BaseWeight = 1, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Commander, IsSpecial = true, Height = 1.85f, UnlockKills = 60, BaseWeight = 1, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Sniper, IsSpecial = true, Height = 1.7f, UnlockKills = 70, BaseWeight = 1, WeightPerTier = 1 },
			new EnemySpec { Type = EnemyType.Boss, Height = 3.0f, InCrowd = false }
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
