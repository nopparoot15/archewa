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
		private BulletVisual visual;
		private static int syncedFrame = -1;

		private void Awake()
		{
			visual = GetComponent<BulletVisual>();
		}

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
			visual?.SetTeam(team);
			Physics.SyncTransforms();
			Vector3 origin = transform.position;
			if (owner != null && owner.GetComponent<Character>() != null)
			{
				origin = owner.transform.position;
				origin.y = transform.position.y;
			}
			Vector3 muzzleTravel = transform.position - origin;
			TryHitAlong(origin, muzzleTravel.sqrMagnitude > 0.0001f ? muzzleTravel.normalized : direction,
				muzzleTravel.magnitude);
		}

		public void Reflect(Vector3 newDirection, Team newTeam, GameObject newOwner, int newDamage)
		{
			transform.forward = newDirection;
			team = newTeam;
			owner = newOwner;
			damage = newDamage;
			spawnTime = Time.time;
			visual?.SetTeam(team);
		}

		public void OnSpawn()
		{
			spawnTime = Time.time;
			visual?.ClearTrail();
		}

		public void OnDespawn()
		{
			owner = null;
			visual?.ClearTrail();
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

		private void LateUpdate()
		{
			if (Enemy.Frozen)
			{
				spawnTime += Time.deltaTime; // a frozen bullet does not age
				return;
			}
			if (syncedFrame != Time.frameCount)
			{
				Physics.SyncTransforms();
				syncedFrame = Time.frameCount;
			}
			Travel(speed * Time.deltaTime);
		}

		private void Travel(float distance)
		{
			if (TryHitAlong(transform.position, transform.forward, distance)) return;
			CheckDodge(distance);
			transform.position += transform.forward * distance;
			if (Time.time - spawnTime > Lifetime || Arena.IsOutside(transform.position)) release(this);
		}

		private bool TryHitAlong(Vector3 origin, Vector3 direction, float distance)
		{
			Collider[] overlaps = Physics.OverlapSphere(origin, Radius, ~0, QueryTriggerInteraction.Ignore);
			Array.Sort(overlaps, (a, b) => a.bounds.SqrDistance(origin).CompareTo(b.bounds.SqrDistance(origin)));
			foreach (Collider overlap in overlaps)
				if (ResolveHit(overlap, overlap.ClosestPoint(origin))) return true;
			if (distance <= 0f) return false;
			RaycastHit[] hits = Physics.SphereCastAll(origin, Radius, direction, distance, ~0, QueryTriggerInteraction.Ignore);
			Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
			foreach (RaycastHit hit in hits)
				if (ResolveHit(hit.collider, hit.point)) return true;
			return false;
		}

		private bool ResolveHit(Collider collider, Vector3 point)
		{
			IDamageable target = collider.GetComponentInParent<IDamageable>();
			if (target == null || target.IsDead || target.Team == team)
			{
				return false;
			}
			if (target is IShootable shootable && shootable.TakeShot(this))
			{
				transform.position = point;
				return true;
			}
			int healthBefore = target is Character character ? character.Health : -1;
			target.TakeDamage(damage, owner);
			bool damaged = !(target is Character hitCharacter) || hitCharacter.Health < healthBefore;
			CameraRig.Shake(1f, 0.08f, 0.015f);
			if (damaged && team == Team.Player && target is Enemy)
			{
				GameEvents.RaiseBulletHitEnemy();
			}
			GameEvents.RaiseHitLanded(point, damage);
			GameEvents.RaiseSound("hit");
			release(this);
			return true;
		}
	}
}
