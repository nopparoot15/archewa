using System;
using UnityEngine;

namespace MyArcade
{
	public abstract class Character : MonoBehaviour, IDamageable, IKnockbackable
	{
		// protected: subclasses may change their own stats, outside code may not (encapsulation)
		protected float moveSpeed = 4f;
		protected int maxHealth = 3;

		private Vector3 knockback;
		private float lastMoveSpeed;

		public int Health { get; private set; }
		public int MaxHealth => maxHealth;
		public bool IsDead => Health <= 0;

		public float Speed01 { get; private set; }

		public abstract Team Team { get; }
		public abstract string DisplayName { get; }

		public event Action<string> Attacked; // animation clip to play
		public event Action Damaged;
		public event Action Died;

		protected virtual void Awake()
		{
			Health = maxHealth;
		}

		protected void ResetHealth()
		{
			Health = maxHealth;
			knockback = Vector3.zero;
		}

		public virtual void Move(Vector3 direction, float speedMultiplier = 1f)
		{
			if (direction.sqrMagnitude > 1f)
			{
				direction.Normalize();
			}
			float speed = moveSpeed * speedMultiplier;
			Vector3 next = transform.position + direction * speed * Time.deltaTime;
			transform.position = Arena.Clamp(next);
			lastMoveSpeed = direction.magnitude * speedMultiplier;
		}

		protected void FinishFrame()
		{
			if (knockback.sqrMagnitude > 0.01f)
			{
				transform.position = Arena.Clamp(transform.position + knockback * Time.deltaTime);
				knockback = Vector3.Lerp(knockback, Vector3.zero, 10f * Time.deltaTime);
			}
			Speed01 = Mathf.Lerp(Speed01, lastMoveSpeed, 12f * Time.deltaTime);
			lastMoveSpeed = 0f;
		}

		public void ApplyKnockback(Vector3 impulse)
		{
			impulse.y = 0f;
			knockback += impulse;
		}

		public virtual void TakeDamage(int amount, GameObject source)
		{
			if (IsDead)
			{
				return;
			}
			Health = Mathf.Max(0, Health - amount);
			Damaged?.Invoke();
			OnDamaged(source);
			if (IsDead)
			{
				Died?.Invoke();
				Die();
			}
		}

		protected void RaiseAttacked(string clip)
		{
			Attacked?.Invoke(clip);
		}

		protected virtual void OnDamaged(GameObject source)
		{
		}

		protected abstract void Die();
	}
}
