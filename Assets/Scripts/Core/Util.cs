using UnityEngine;

namespace MyArcade
{
	public static class Util
	{
		/// <summary>Destroy that also works in edit mode (when building the scene from the editor menu).</summary>
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
