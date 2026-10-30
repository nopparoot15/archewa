using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyArcade
{

	public class MenuScreens : MonoBehaviour
	{
		private static readonly Color Lit = new Color(.96f, .30f, .36f);
		private static readonly Color Unlit = new Color(.94f, .92f, .86f, .9f);

		/// <summary>True for the frame in which Esc was used to go back inside a menu (so the pause toggle ignores it).</summary>
		public static bool BackHandled => backFrame == Time.frameCount || subScreenOpen;

		private static int backFrame = -1;
		private static bool subScreenOpen;

		private enum Screen { None, Title, Pause, Options, HowToPlay, Credits }

		private sealed class Panel
		{
			public Transform Root;
			public readonly List<RectTransform> Items = new List<RectTransform>();
			public readonly List<TextMeshProUGUI> Labels = new List<TextMeshProUGUI>();
			public readonly List<Image> Marks = new List<Image>();
		}

		private readonly Dictionary<Screen, Panel> panels = new Dictionary<Screen, Panel>();
		private GameManager game;
		private IInputSource input;
		private AudioDirector sound;
		private Screen current = Screen.None;
		private Screen backTo = Screen.None;     // where BACK of Options / How to play / Credits returns
		private int selected;
		private TextMeshProUGUI fpsLabel;
		private float fpsTimer;
		private int fpsFrames;

		public void Bind(GameManager manager, IInputSource inputSource, AudioDirector audioDirector)
		{
			game = manager;
			input = inputSource;
			sound = audioDirector;
			subScreenOpen = false;
			backFrame = -1;

			Transform canvas = transform.Find("Canvas");
			Register(Screen.Title, canvas.Find("Title/TitleMenu"));
			Register(Screen.Pause, canvas.Find("Pause/PauseMenu"));
			Register(Screen.Options, canvas.Find("Options"));
			Register(Screen.HowToPlay, canvas.Find("HowToPlay"));
			Register(Screen.Credits, canvas.Find("Credits"));
			Transform fps = canvas.Find("FpsLabel");
			fpsLabel = fps != null ? fps.GetComponentInChildren<TextMeshProUGUI>(true) : null;

			game.StateChanged += OnStateChanged;
			OnStateChanged(game.State);
		}

		private void OnDestroy()
		{
			if (game != null)
			{
				game.StateChanged -= OnStateChanged;
			}
			subScreenOpen = false;
		}

		private void Register(Screen screen, Transform root)
		{
			var panel = new Panel { Root = root };
			for (int i = 0; root != null && i < root.childCount; i++)
			{
				Transform item = root.Find("Item" + i);
				if (item == null)
				{
					break;
				}
				panel.Items.Add((RectTransform)item);
				panel.Labels.Add(item.Find("Label").GetComponent<TextMeshProUGUI>());
				Transform mark = item.Find("Mark");
				panel.Marks.Add(mark != null ? mark.GetComponent<Image>() : null);
			}
			panels[screen] = panel;
		}

		// ---- which screen is open ----------------------------------------------------------

		private void OnStateChanged(GameState state)
		{
			subScreenOpen = false;
			if (state == GameState.Title)
			{
				Open(Screen.Title);
			}
			else if (state == GameState.Paused)
			{
				Open(Screen.Pause);
			}
			else
			{
				Open(Screen.None);
			}
		}

		private void Open(Screen screen)
		{
			current = screen;
			selected = 0;
			foreach (KeyValuePair<Screen, Panel> pair in panels)
			{
				if (pair.Value.Root != null)
				{
					pair.Value.Root.gameObject.SetActive(pair.Key == screen);
				}
			}
			// the Pause and Title roots are switched by GameUi; their menu list is hidden while a sub screen is open
			subScreenOpen = screen == Screen.Options || screen == Screen.HowToPlay || screen == Screen.Credits;
			RefreshOptions();
			Highlight();
		}

		private void OpenSub(Screen screen)
		{
			backTo = current;
			Open(screen);
		}

		private void GoBack()
		{
			backFrame = Time.frameCount;
			GameEvents.RaiseSound("ui_cancel");
			Open(backTo == Screen.None ? Screen.Title : backTo);
		}

		// ---- every frame --------------------------------------------------------------------

		private void Update()
		{
			UpdateFps();
			if (current == Screen.None || input == null)
			{
				return;
			}
			Panel panel = panels[current];
			if (panel.Items.Count == 0)
			{
				if (input.PausePressed || input.ConfirmPressed || input.PointerPressed)
				{
					HandleBackOrConfirmOnTextScreen();
				}
				return;
			}

			bool back = input.PausePressed && (current == Screen.Options || current == Screen.HowToPlay || current == Screen.Credits);
			if (back)
			{
				GoBack();
				return;
			}

			int move = input.MenuMove;
			if (move != 0)
			{
				selected = (selected + move + panel.Items.Count) % panel.Items.Count;
				GameEvents.RaiseSound("ui_select");
				Highlight();
			}

			// the mouse picks the item under it, a click uses it
			Vector2 pointer = input.PointerScreenPosition;
			for (int i = 0; i < panel.Items.Count; i++)
			{
				if (RectTransformUtility.RectangleContainsScreenPoint(panel.Items[i], pointer, null) && lastPointer != pointer)
				{
					if (selected != i)
					{
						selected = i;
						GameEvents.RaiseSound("ui_select");
						Highlight();
					}
				}
			}
			lastPointer = pointer;

			int adjust = input.MenuAdjust;
			if (adjust != 0)
			{
				Activate(adjust);
			}
			else if (input.ConfirmPressed)
			{
				Activate(1);
			}
			else if (input.PointerPressed && RectTransformUtility.RectangleContainsScreenPoint(panel.Items[selected], pointer, null))
			{
				Activate(1);
			}
		}

		private Vector2 lastPointer;

		private void HandleBackOrConfirmOnTextScreen()
		{
			if (current == Screen.HowToPlay || current == Screen.Credits)
			{
				GoBack();
			}
		}

		private void Highlight()
		{
			if (!panels.TryGetValue(current, out Panel panel))
			{
				return;
			}
			for (int i = 0; i < panel.Labels.Count; i++)
			{
				bool on = i == selected;
				panel.Labels[i].color = on ? Lit : Unlit;
				Image mark = panel.Marks[i];
				if (mark != null)
				{
					mark.enabled = on;
					if (on && panel.Labels[i].alignment == TextAlignmentOptions.Center)
					{
						// centred lists: the selection bar sits just left of the text, wherever the text starts
						float half = panel.Labels[i].preferredWidth * 0.5f;
						RectTransform rect = mark.rectTransform;
						rect.anchorMin = new Vector2(0.5f, 0.15f);
						rect.anchorMax = new Vector2(0.5f, 0.85f);
						rect.offsetMin = new Vector2(-half - 40f, 0f);
						rect.offsetMax = new Vector2(-half - 30f, 0f);
					}
				}
			}
		}

		// ---- what each item does -----------------------------------------------------------

		private void Activate(int direction)
		{
			bool isBack = current == Screen.HowToPlay || current == Screen.Credits || (current == Screen.Options && selected == 5);
			GameEvents.RaiseSound(isBack ? "ui_cancel" : "ui_confirm");
			switch (current)
			{
				case Screen.Title:
					TitleItem(selected);
					break;
				case Screen.Pause:
					PauseItem(selected);
					break;
				case Screen.Options:
					OptionsItem(selected, direction);
					break;
				default:
					GoBack();
					break;
			}
		}

		private void TitleItem(int index)
		{
			switch (index)
			{
				case 0: game.SetState(GameState.Playing); break;               // PLAY
				case 1: OpenSub(Screen.HowToPlay); break;                      // TUTORIAL (how to play)
				case 2: OpenSub(Screen.Options); break;                        // OPTIONS
				case 3: OpenSub(Screen.Credits); break;                        // CREDITS
				case 4: Quit(); break;                                         // EXIT
			}
		}

		private void PauseItem(int index)
		{
			switch (index)
			{
				case 0: game.SetState(GameState.Playing); break;               // RESUME
				case 1: GameBootstrap.Restart(); break;                        // RESTART
				case 2: OpenSub(Screen.Options); break;                        // OPTIONS
				case 3: OpenSub(Screen.HowToPlay); break;                      // HOW TO PLAY
				case 4: GameBootstrap.ReturnToTitle(); break;                  // MAIN MENU
			}
		}

		private void OptionsItem(int index, int direction)
		{
			switch (index)
			{
				case 0: GameSettings.Volume += direction; sound?.RefreshVolume(); break;
				case 1: GameSettings.ScreenShake = !GameSettings.ScreenShake; break;
				case 2: GameSettings.Fullscreen = !GameSettings.Fullscreen; GameSettings.Apply(); break;
				case 3: GameSettings.Vsync = !GameSettings.Vsync; GameSettings.Apply(); break;
				case 4: GameSettings.ShowFps = !GameSettings.ShowFps; break;
				case 5: GoBack(); return;                                      // BACK
			}
			RefreshOptions();
		}

		private void RefreshOptions()
		{
			if (!panels.TryGetValue(Screen.Options, out Panel panel) || panel.Labels.Count < 6)
			{
				return;
			}
			panel.Labels[0].text = "SOUND VOLUME   < " + GameSettings.Volume + " >";
			panel.Labels[1].text = "SCREEN SHAKE   " + (GameSettings.ScreenShake ? "ON" : "OFF");
			panel.Labels[2].text = "FULLSCREEN   " + (GameSettings.Fullscreen ? "ON" : "OFF");
			panel.Labels[3].text = "VSYNC   " + (GameSettings.Vsync ? "ON" : "OFF");
			panel.Labels[4].text = "SHOW FPS   " + (GameSettings.ShowFps ? "ON" : "OFF");
			panel.Labels[5].text = "BACK";
		}

		private static void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		// ---- the fps counter (OPTIONS > SHOW FPS) ---------------------------------------------

		private void UpdateFps()
		{
			if (fpsLabel == null)
			{
				return;
			}
			fpsLabel.gameObject.SetActive(GameSettings.ShowFps);
			fpsFrames++;
			fpsTimer += Time.unscaledDeltaTime;
			if (fpsTimer >= 0.5f)
			{
				fpsLabel.text = Mathf.RoundToInt(fpsFrames / fpsTimer) + " FPS";
				fpsFrames = 0;
				fpsTimer = 0f;
			}
		}
	}
}
