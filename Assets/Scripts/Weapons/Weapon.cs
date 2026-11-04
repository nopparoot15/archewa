using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public abstract class Weapon
	{
		public string DisplayName { get; }
		protected readonly float interval;
		protected readonly int damage;
		private float nextUseTime;

		protected Weapon(string displayName, float interval, int damage)
		{
			DisplayName = displayName;
			this.interval = interval;
			this.damage = damage;
		}

		public string TryUse(AttackContext context)
		{
			if (Time.time < nextUseTime)
			{
				return null;
			}
			float scale = context.Stats != null ? context.Stats.AttackIntervalScale : 1f;
			nextUseTime = Time.time + interval * scale;
			Perform(context);
			return NextClip();
		}

		protected abstract void Perform(AttackContext context);

		protected abstract string NextClip();

		protected int Damage(AttackContext context)
		{
			return damage + (context.Stats != null ? context.Stats.DamageBonus : 0);
		}
	}

	public abstract class MeleeWeapon : Weapon, IBulletDeflector
	{
		private readonly float reach;
		private readonly float arcDegrees;
		private readonly float knockbackForce;
		private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

		protected MeleeWeapon(string displayName, float interval, int damage, float reach, float arcDegrees, float knockbackForce)
			: base(displayName, interval, damage)
		{
			this.reach = reach;
			this.arcDegrees = arcDegrees;
			this.knockbackForce = knockbackForce;
		}

		protected override void Perform(AttackContext context)
		{
			alreadyHit.Clear();
			Vector3 forward = context.Direction;
			forward.y = 0f;
			forward.Normalize();
			int dealt = Damage(context);
			GameEvents.RaiseAttackSwung(context.Origin, forward, reach, arcDegrees);

			IDamageable closest = null;
			Collider closestCollider = null;
			float closestDistance = float.MaxValue;
			Collider[] around = Physics.OverlapSphere(context.Origin, reach);
			foreach (Collider collider in around)
			{
				Vector3 toTarget = collider.transform.position - context.Origin;
				toTarget.y = 0f;
				if (toTarget.sqrMagnitude > 0.01f && Vector3.Angle(forward, toTarget) > arcDegrees * 0.5f)
				{
					continue;
				}

				// a flying stone? knock it back
				Bullet bullet = collider.GetComponent<Bullet>();
				if (bullet != null)
				{
					Deflect(bullet, context);
					continue;
				}

				IDamageable target = collider.GetComponentInParent<IDamageable>();
				if (target == null || target.IsDead || target.Team == context.Team || alreadyHit.Contains(target))
				{
					continue;
				}
				alreadyHit.Add(target);
				float distance = toTarget.magnitude;
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closest = target;
					closestCollider = collider;
				}
			}

			if (closest != null)
			{
				Vector3 away = closestCollider.transform.position - context.Origin;
				away.y = 0f;
				int healthBefore = closest is Character character ? character.Health : -1;
				closest.TakeDamage(dealt, context.Owner);
				bool damaged = !(closest is Character hitCharacter) || hitCharacter.Health < healthBefore;
				if (damaged && closest is IKnockbackable pushable)
				{
					pushable.ApplyKnockback(away.normalized * knockbackForce);
				}
				GameEvents.RaiseHitLanded(closestCollider.bounds.center, dealt);
				if (damaged && context.Team == Team.Player)
				{
					SlowMotion.HitFreeze();
					GameEvents.RaiseSwingConnected();
				}
				GameEvents.RaiseSound("hit");
			}
			GameEvents.RaiseSound("swing");
		}

		public bool Deflect(Bullet bullet, AttackContext context)
		{
			if (bullet.Team == context.Team)
			{
				return false;
			}
			Vector3 back = context.Direction;
			back.y = 0f;
			bullet.Reflect(back.normalized, context.Team, context.Owner, Damage(context) + 1);
			GameEvents.RaiseDeflected(bullet.transform.position);
			GameEvents.RaiseSound("deflect");
			return true;
		}
	}

	public class SteelPipe : MeleeWeapon
	{
		public SteelPipe() : base("STEEL PIPE", 0.3f, 3, 2.3f, 100f, 8f)
		{
		}

		protected override string NextClip()
		{
			return "attack-melee-right";
		}
	}
}
