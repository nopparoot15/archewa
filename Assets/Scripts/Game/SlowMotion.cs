using UnityEngine;

namespace MyArcade
{
	public static class SlowMotion
	{
		private static float scale = 1f;
		private static float holdUntil; // unscaled time until which the slow scale is kept
		private const float RecoverSpeed = 4f;
		private static bool snapBack;

		public static float Scale => scale;

		public static void Pulse(float targetScale, float seconds)
		{
			scale = Mathf.Min(scale, Mathf.Clamp(targetScale, 0.05f, 1f));
			holdUntil = Mathf.Max(holdUntil, Time.unscaledTime + seconds);
		}

		public static void HitFreeze()
		{
			Pulse(0.1f, 0.045f);
			snapBack = true;
		}

		public static void Reset()
		{
			scale = 1f;
			holdUntil = 0f;
		}

		public static void Tick()
		{
			if (Time.unscaledTime > holdUntil && scale < 1f)
			{
				scale = snapBack ? 1f : Mathf.MoveTowards(scale, 1f, RecoverSpeed * Time.unscaledDeltaTime);
				snapBack = false;
			}
		}
	}
}
