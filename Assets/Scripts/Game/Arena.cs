using UnityEngine;

namespace MyArcade
{
	public static class Arena
	{
		public const float HalfSize = 17f;

		public static Vector3 Clamp(Vector3 position)
		{
			position.x = Mathf.Clamp(position.x, -HalfSize, HalfSize);
			position.z = Mathf.Clamp(position.z, -HalfSize, HalfSize);
			position.y = 0f;
			return position;
		}

		public static bool IsOutside(Vector3 position, float margin = 4f)
		{
			return Mathf.Abs(position.x) > HalfSize + margin || Mathf.Abs(position.z) > HalfSize + margin;
		}

		public static Vector3 RandomHiddenEdgePoint()
		{
			Camera cam = Camera.main;
			Vector3 point = RandomEdgePoint();
			if (cam == null || cam.orthographic == false)
			{
				return point;
			}
			for (int i = 0; i < 16; i++)
			{
				Vector3 view = cam.WorldToViewportPoint(point);
				bool visible = view.x > -0.04f && view.x < 1.04f && view.y > -0.08f && view.y < 1.08f;
				if (!visible)
				{
					return point;
				}
				point = RandomEdgePoint();
			}
			return point;
		}

		public static Vector3 RandomEdgePoint()
		{
			float t = Random.Range(-HalfSize, HalfSize);
			switch (Random.Range(0, 4))
			{
				case 0: return new Vector3(t, 0f, HalfSize);
				case 1: return new Vector3(t, 0f, -HalfSize);
				case 2: return new Vector3(HalfSize, 0f, t);
				default: return new Vector3(-HalfSize, 0f, t);
			}
		}
	}
}
