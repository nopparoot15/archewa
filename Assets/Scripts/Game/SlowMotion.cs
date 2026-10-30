using UnityEngine;

namespace MyArcade
{
	/// <summary>
	/// Brief slow-motion for dramatic moments (boss down, big combo, deflect).
	/// Others only ask: <c>SlowMotion.Pulse(0.3f, 0.6f)</c>. GameManager reads <see cref="Scale"/> every frame,
	/// so slow-motion and pause never fight over Time.timeScale.
	/// </summary>
	public static class SlowMotion
	{
		private static float scale = 1f;
		private static float holdUntil;      // unscaled time until which the slow scale is kept
		private const float RecoverSpeed = 4f;
		private static bool snapBack;

		public static float Scale => scale;

		/// <summary>Slows time to <paramref name="targetScale"/> for <paramref name="seconds"/> (real seconds), then eases back.</summary>
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

		/// <summary>Call once per frame with unscaled delta time.</summary>
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
