using UnityEngine;

namespace MyArcade
{
	public class BulletSpawner : MonoBehaviour, IBulletSpawner
	{
		private ObjectPool<Bullet> pool;

		private void Awake()
		{
			pool = new ObjectPool<Bullet>(() =>
			{
				Bullet bullet = EnemyFactory.CreateBullet(transform);
				bullet.Init(b => pool.Release(b));
				return bullet;
			});
		}

		public void Spawn(Vector3 position, Vector3 direction, float speed, int damage, Team team, GameObject owner)
		{
			direction.y = 0f;
			if (direction.sqrMagnitude < 0.0001f)
			{
				direction = Vector3.forward;
			}
			Bullet bullet = pool.Get(position, Quaternion.LookRotation(direction));
			bullet.Fire(direction.normalized, speed, damage, team, owner);
		}
	}
}
