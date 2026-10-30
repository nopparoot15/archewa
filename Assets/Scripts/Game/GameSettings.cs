using UnityEngine;

namespace MyArcade
{
	/// <summary>The player's options (saved with PlayerPrefs): sound volume, screen shake, fullscreen, vsync and the fps counter.</summary>
	public static class GameSettings
	{
		private const string VolumeKey = "MyArcade.SfxVolume";
		private const string ShakeKey = "MyArcade.ScreenShake";
		private const string FullscreenKey = "MyArcade.Fullscreen";
		private const string VsyncKey = "MyArcade.Vsync";
		private const string FpsKey = "MyArcade.ShowFps";

		/// <summary>0..10 in steps of one.</summary>
		public static int Volume { get => PlayerPrefs.GetInt(VolumeKey, 10); set => PlayerPrefs.SetInt(VolumeKey, Mathf.Clamp(value, 0, 10)); }
		public static bool ScreenShake { get => PlayerPrefs.GetInt(ShakeKey, 1) == 1; set => PlayerPrefs.SetInt(ShakeKey, value ? 1 : 0); }
		public static bool Fullscreen { get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1; set => PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); }
		public static bool Vsync { get => PlayerPrefs.GetInt(VsyncKey, 1) == 1; set => PlayerPrefs.SetInt(VsyncKey, value ? 1 : 0); }
		public static bool ShowFps { get => PlayerPrefs.GetInt(FpsKey, 0) == 1; set => PlayerPrefs.SetInt(FpsKey, value ? 1 : 0); }

		public static float Volume01 => Volume / 10f;

		/// <summary>Pushes the saved video options to Unity.</summary>
		public static void Apply()
		{
			Screen.fullScreen = Fullscreen;
			QualitySettings.vSyncCount = Vsync ? 1 : 0;
		}
	}
}
