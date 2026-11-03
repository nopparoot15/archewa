using UnityEngine;

namespace MyArcade
{
	public static class Util
	{
		public static void Kill(Object target)
		{
			if (target == null)
			{
				return;
			}
			if (Application.isPlaying)
			{
				Object.Destroy(target);
			}
			else
			{
				Object.DestroyImmediate(target);
			}
		}
	}
}
