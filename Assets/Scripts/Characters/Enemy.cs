using System;
using UnityEngine;

namespace MyArcade
{
	public enum EnemyType
	{
		Chaser,
		Shooter,
		Brute,
		Assassin,
		Boss,
		Sprinter,
		Zigzagger,
		Bomber,
		Shieldman,
		Commander,
		Sniper,
		Shotgunner,
		Splitter,
		Charger
	}

	/// <summary>
	/// Base of all rival students. The shared life cycle (find the player, die, give score, return to the pool)
	/// is written once here. Each subclass only decides how it BEHAVES (abstract method).
	/// </summary>
	public abstract class Enemy : Character, IPoolable
	{
		private static readonly System.Collections.Generic.List<Enemy> Active = new System.Collections.Generic.List<Enemy>();

		protected float turnSpeed = 10f; // how fast it can turn to face the hero (a slow turner can be hit from behind)
		private float buffUntil;
		private float unbuffedSpeed;
		protected Player target;
		private Action<Enemy> release;
		private bool baseCaptured;
		private int baseHealth;
		private float baseSpeed;

		public override Team Team => Team.Enemy;
		public EnemyType Kind { get; set; }
		/// <summary>Optional animation cue. Subclasses expose their state without the renderer depending on concrete AI classes.</summary>
		public virtual string VisualPose => null;


		public static bool Frozen { get; set; }

		public void Init(Player target, Action<Enemy> releaseCallback)
		{
			this.target = target;
			release = releaseCallback;
		}

		public virtual void OnSpawn()
		{
			EndBuff();
			ResetHealth();
		}

		/// <summary>A Commander's shout: faster for a few seconds, then back to normal.</summary>
		public void ApplyBuff(float speedMultiplier, float seconds)
		{
			if (buffUntil <= 0f)
			{
				unbuffedSpeed = moveSpeed;
				moveSpeed *= speedMultiplier;
			}
			buffUntil = Time.time + seconds;
		}

		private void EndBuff()
		{
			if (buffUntil > 0f)
			{
				moveSpeed = unbuffedSpeed;
				buffUntil = 0f;
			}
		}

		public static System.Collections.Generic.IReadOnlyList<Enemy> AllActive => Active;

		public virtual void OnDespawn()
		{
		}

		/// <summary>Sends every living rival back to its pool without counting a kill (used before a boss fight).</summary>
		public static void RetireAll()
		{
			var copy = new System.Collections.Generic.List<Enemy>(Active);
			foreach (Enemy enemy in copy)
			{
				if (!enemy.IsDead && enemy.release != null)
				{
					enemy.release(enemy);
				}
			}
		}

		private void OnEnable()
		{
			Active.Add(this);
		}

		private void OnDisable()
		{
			Active.Remove(this);
		}

		protected virtual bool IsGiant => false;

		/// <summary>Round difficulty: tougher and faster rivals as the rounds go on.</summary>
		public void ApplyLevel(float healthScale, float speedScale)
		{
			if (!baseCaptured)
			{
				baseCaptured = true;
				baseHealth = maxHealth;
				baseSpeed = moveSpeed;
			}
			maxHealth = IsGiant ? Mathf.Max(1, Mathf.RoundToInt(baseHealth * healthScale)) : 1; // everyone but the giants falls to one hit
			moveSpeed = baseSpeed * speedScale;
			ResetHealth();
		}

		/// <summary>Keeps the crowd from stacking into one pile: each rival gently pushes away from close neighbours.</summary>
		private void Separate()
		{
			Vector3 push = Vector3.zero;
			Vector3 mine = transform.position;
			for (int i = 0; i < Active.Count; i++)
			{
				Enemy other = Active[i];
				if (other == this || other.IsDead)
				{
					continue;
				}
				Vector3 away = mine - other.transform.position;
				away.y = 0f;
				float distance = away.magnitude;
				if (distance < 1.8f)
				{
					if (DistanceFromHero(other) < DistanceFromHero(this))
					{
						OnBlockedByCloser();
					}
					push += (distance > 0.001f ? away / distance : Vector3.right) * (1.8f - distance);
				}
			}
			if (push.sqrMagnitude > 0f)
			{
				transform.position = Arena.Clamp(mine + push * 5f * Time.deltaTime);
			}
		}

		private static float DistanceFromHero(Enemy enemy)
		{
			return enemy.target != null ? (enemy.target.transform.position - enemy.transform.position).sqrMagnitude : 0f;
		}


		protected virtual void OnBlockedByCloser()
		{
		}

		private void Update()
		{
			if (Frozen)
			{
				return;
			}
			if (buffUntil > 0f && Time.time >= buffUntil)
			{
				EndBuff();
			}
			if (IsDead || target == null || target.IsDead)
			{
				FinishFrame();
				return;
			}
			Behave();
			Separate();
			FinishFrame();
		}

		/// <summary>What this enemy does every frame. Every subclass must answer this.</summary>
		protected abstract void Behave();

		protected Vector3 DirectionToTarget()
		{
			Vector3 delta = target.transform.position - transform.position;
			delta.y = 0f;
			return delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.zero;
		}

		protected float DistanceToTarget()
		{
			Vector3 delta = target.transform.position - transform.position;
			delta.y = 0f;
			return delta.magnitude;
		}

		protected void FaceTarget()
		{
			Vector3 dir = DirectionToTarget();
			if (dir != Vector3.zero)
			{
				transform.forward = Vector3.Slerp(transform.forward, dir, turnSpeed * Time.deltaTime);
			}
		}

		protected override void Die()
		{
			GameEvents.RaiseEnemyKilled(this);
			// give the death animation time to play, then recycle
			Invoke(nameof(Recycle), 1.1f);
		}

		private void Recycle()
		{
			release(this);
		}
	}
}
