using UnityEngine;

namespace MyArcade
{
	public static class EnemyFactory
	{
		public static Bullet CreateBullet(Transform parent)
		{
			GameObject prefab = Resources.Load<GameObject>("Projectiles/StreetBullet");
			if (prefab == null)
				throw new System.InvalidOperationException("StreetBullet projectile prefab is missing.");
			GameObject go = Object.Instantiate(prefab, parent);
			go.SetActive(false);
			return go.GetComponent<Bullet>();
		}

		public static Enemy CreateEnemy(EnemyType type, Transform parent)
		{
			GameObject prefab = Resources.Load<GameObject>("Enemies/" + type);
			if (prefab == null)
				throw new System.InvalidOperationException("Enemy prefab is missing: " + type);
			GameObject go = Object.Instantiate(prefab, parent);
			go.name = type.ToString();
			Enemy enemy = go.GetComponent<Enemy>();
			enemy.Kind = type;
			go.SetActive(false); // the pool activates it when needed
			return enemy;
		}
	}
}
