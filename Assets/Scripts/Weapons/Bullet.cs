using System;
using UnityEngine;

namespace MyArcade
{

	public class Bullet : MonoBehaviour, IPoolable
	{
		private const float Lifetime = 1f;
		private const float Radius = 0.18f;

		private float speed;
		private int damage;
		private Team team;
		private GameObject owner;
		private float spawnTime;
		private GameObject lastDeflector;
		private bool dodgeCounted;
		private static Player hero;
		private Action<Bullet> release;

		public Team Team => team;
		public Vector3 Direction => transform.forward;
		public int Damage => damage;
		public bool WasDeflectedBy(GameObject who) => lastDeflector == who;
		public void MarkDeflected(GameObject who) { lastDeflector = who; }

		public void Init(Action<Bullet> releaseCallback)
		{
			release = releaseCallback;
		}

		public void Fire(Vector3 direction, float speed, int damage, Team team, GameObject owner)
		{
			transform.forward = direction;
			this.speed = speed;
			this.damage = damage;
			this.team = team;
			this.owner = owner;
			lastDeflector = null;
			dodgeCounted = false;
			spawnTime = Time.time;
		}


		public void Reflect(Vector3 newDirection, Team newTeam, GameObject newOwner, int newDamage)
		{
			transform.forward = newDirection;
			team = newTeam;
			owner = newOwner;
			damage = newDamage;
			spawnTime = Time.time;
		}

		public void OnSpawn()
		{
			spawnTime = Time.time;
		}

		public void OnDespawn()
		{
			owner = null;
		}


		private void CheckDodge(float distance)
		{
			if (team != Team.Enemy || dodgeCounted)
			{
				return;
			}
			if (hero == null)
			{
				hero = UnityEngine.Object.FindFirstObjectByType<Player>();
				if (hero == null)
				{
					return;
				}
			}
			Vector3 toHero = hero.transform.position - transform.position;
			toHero.y = 0f;
			float along = Vector3.Dot(toHero, transform.forward);
			if (along > 0f && along < distance + 0.5f && (toHero - transform.forward * along).magnitude < 1.4f && !hero.IsDead)
			{
				dodgeCounted = true;
				GameEvents.RaiseBulletDodged();
			}
		}

		private void Update()
		{
			if (Enemy.Frozen)
			{
				spawnTime += Time.deltaTime; // a frozen bullet does not age
				return;
			}
			float distance = speed * Time.deltaTime;
			RaycastHit[] hits = Physics.SphereCastAll(transform.position, Radius, transform.forward, distance, ~0, QueryTriggerInteraction.Ignore);
			Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
			foreach (RaycastHit hit in hits)
			{
				IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
				if (target == null || target.IsDead || target.Team == team)
				{
					continue;
				}
				if (target is IShootable shootable && shootable.TakeShot(this))
				{
					transform.position = hit.point; // deflected or ignored: the bullet carries on
					return;
				}
				target.TakeDamage(damage, owner);
				CameraRig.Shake(1f, 0.08f, 0.015f);
				if (team == Team.Player && target is Enemy)
				{
					GameEvents.RaiseBulletHitEnemy();
				}
				GameEvents.RaiseHitLanded(hit.point, damage);
				GameEvents.RaiseSound("hit");
				release(this);
				return;
			}

			CheckDodge(distance);
			transform.position += transform.forward * distance;
			if (Time.time - spawnTime > Lifetime || Arena.IsOutside(transform.position))
			{
				release(this);
			}
		}
	}
}
