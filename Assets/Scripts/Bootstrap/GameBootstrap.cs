using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyArcade
{
	/// <summary>
	/// Runs when you press Play. The scene (street, player, camera, UI, menus...) already exists in the Hierarchy and is
	/// edited there by hand. This class only CONNECTS the parts together (dependency injection) and starts the game.
	/// This is the only place that wires the classes.
	/// </summary>
	public static class GameBootstrap
	{
		public const string RootName = "MyArcadeRoot";

		private static bool skipTitleNextLoad;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Start()
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			SceneManager.sceneLoaded += OnSceneLoaded;
			Wire(SceneManager.GetActiveScene());
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Wire(scene);
		}

		/// <summary>Starts a new run at once (no title menu).</summary>
		public static void Restart()
		{
			ReloadScene(true);
		}

		/// <summary>Back to the title menu.</summary>
		public static void ReturnToTitle()
		{
			ReloadScene(false);
		}

		private static void ReloadScene(bool skipTitle)
		{
			skipTitleNextLoad = skipTitle;
			GameEvents.Clear();
			Time.timeScale = 1f;
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		private static void Wire(Scene scene)
		{
			GameObject root = GameObject.Find(RootName);
			if (root == null)
			{
				Debug.LogWarning("MyArcade: the scene has no '" + RootName + "' object.");
				return;
			}

			bool skipTitle = skipTitleNextLoad;
			skipTitleNextLoad = false;
			GameEvents.Clear();

			Camera cam = Camera.main;
			IInputSource input = new PlayerInput();

			var manager = root.GetComponentInChildren<GameManager>(true);
			var player = root.GetComponentInChildren<Player>(true);
			var bullets = root.GetComponentInChildren<BulletSpawner>(true);
			var spawner = root.GetComponentInChildren<Spawner>(true);
			var rig = root.GetComponentInChildren<CameraRig>(true);
			var ui = root.GetComponentInChildren<GameUi>(true);
			var audio = root.GetComponentInChildren<AudioDirector>(true);
			var vfx = root.GetComponentInChildren<VfxDirector>(true);
			var menus = root.GetComponentInChildren<MenuScreens>(true);

			GameSettings.Apply();

			manager.Setup(input, player); // before anything can happen, so it catches the first events

			Weapon[] weapons = { new SteelPipe() };
			IAbility[] abilities = { new DashAbility(), new ParryAbility() };
			player.Setup(input, bullets, weapons, abilities);
			vfx.Setup();
			spawner.Setup(player, bullets);
			rig.Setup(cam, player.transform);
			audio.Setup(manager, input, player);
			ui.Bind(manager, player, audio);
			menus?.Bind(manager, input, audio);

			// the player and the spawner only act while the game is being played
			manager.StateChanged += state =>
			{
				bool playing = state == GameState.Playing;
				player.enabled = playing;
				spawner.enabled = playing;
				rig.SetTitleMode(state == GameState.Title);
			};
			manager.SetState(skipTitle ? GameState.Playing : GameState.Title);
		}
	}
}
