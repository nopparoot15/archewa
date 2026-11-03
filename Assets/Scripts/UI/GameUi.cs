using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyArcade
{
	public class GameUi : MonoBehaviour
	{
		private static readonly Color Cyan = new Color(0.35f, 0.95f, 1f);
		private static readonly Color Yellow = new Color(1f, 0.85f, 0.15f);
		private static readonly Color Dim = new Color(0.25f, 0.45f, 0.5f, 0.9f);

		[SerializeField] private GameObject titleRoot;
		[SerializeField] private GameObject hudRoot;
		[SerializeField] private GameObject pauseRoot;
		[SerializeField] private GameObject overRoot;
		[SerializeField] private GameObject bossRoot;
		[SerializeField] private TextMeshProUGUI popupText;
		[SerializeField] private CanvasGroup popupGroup;
		[SerializeField] private Image screenTint;

		[SerializeField] private TextMeshProUGUI scoreText;
		[SerializeField] private TextMeshProUGUI bestText;
		[SerializeField] private TextMeshProUGUI comboText;
		[SerializeField] private GameObject comboBarRoot;
		[SerializeField] private RectTransform comboBarFill;
		[SerializeField] private TextMeshProUGUI bannerText;
		[SerializeField] private TextMeshProUGUI overScore;
		[SerializeField] private TextMeshProUGUI overDetail;
		[SerializeField] private TextMeshProUGUI overRecord;
		[SerializeField] private TextMeshProUGUI muteText;
		[SerializeField] private RectTransform bossFill;
		[SerializeField] private CanvasGroup bannerGroup;
		[SerializeField] private CanvasGroup hintGroup;
		[SerializeField] private RectTransform[] slashFill;
		[SerializeField] private Image[] slashFillImage;
		[SerializeField] private Image[] ammoPips;
		[SerializeField] private RectTransform cooldownLine;
		[SerializeField] private RectTransform cooldownFill;
		[SerializeField] private RectTransform canvasRect;

		private GameManager game;
		private Player player;
		private AudioDirector audioDirector;
		private Enemy boss;
		private float popupTimer;
		private float hurtFlash;
		private int lastHealth = -1;
		private float bannerTimer;
		private float hintTimer = 14f;

		public void Bind(GameManager manager, Player owner, AudioDirector audio)
		{
			game = manager;
			player = owner;
			audioDirector = audio;
			game.StateChanged += OnStateChanged;
			game.BoldnessAdded += OnBoldness;
			GameEvents.WaveStarted += OnWave;
			GameEvents.BossChanged += OnBoss;
			GameEvents.Deflected += OnDeflect;
			OnStateChanged(game.State);
		}

		private void OnDestroy()
		{
			if (game != null)
			{
				game.StateChanged -= OnStateChanged;
			}
			GameEvents.WaveStarted -= OnWave;
			GameEvents.BossChanged -= OnBoss;
			GameEvents.Deflected -= OnDeflect;
		}

		private void ShowPopup(string text, Color color)
		{
			popupText.text = text;
			popupText.color = color;
			popupTimer = 1.3f;
		}

		private void OnBoldness(int points)
		{
			if (points >= 10)
			{
				ShowPopup("+" + points + " BOLD", new Color(1f, 0.85f, 0.3f)); // small bonuses (1 point) stay silent
			}
		}

		private void OnDeflect(Vector3 at)
		{
			ShowPopup("DEFLECTED!", new Color(0.7f, 1f, 1f));
		}

		private void OnStateChanged(GameState state)
		{
			titleRoot.SetActive(state == GameState.Title);
			hudRoot.SetActive(state == GameState.Playing || state == GameState.Paused);
			pauseRoot.SetActive(state == GameState.Paused);
			overRoot.SetActive(state == GameState.GameOver);
			if (state == GameState.GameOver)
			{
				overScore.text = game.Score.ToString("N0") + "\nx" + game.BestCombo + "\n" + game.Boldness.ToString("N0") + "\n" + Mathf.RoundToInt(game.Accuracy * 100f) + "%";
				overRecord.text = game.Best.ToString("N0") + "\nx" + game.BestMaxCombo + "\n" + game.BestBoldness.ToString("N0") + "\n";
				overDetail.text = game.NewRecord ? "NEW RECORD!" : "";
			}
		}

		private void OnWave(int wave)
		{
			bannerText.text = "GANG LEADER LVL " + wave + "\n<size=45%>ONE ON ONE</size>";
			bannerTimer = 2.4f;
		}

		private void OnBoss(Enemy enemy)
		{
			boss = enemy;
			if (bossRoot != null)
			{
				bossRoot.SetActive(enemy != null);
			}
		}

		private void UpdateCooldownLine()
		{
			if (cooldownLine == null || player == null || player.Abilities == null || player.Abilities.Length == 0 || Camera.main == null)
			{
				return;
			}
			float ready = player.Stamina;
			cooldownLine.gameObject.SetActive(ready < 0.999f && !player.IsDead);
			if (ready >= 0.999f)
			{
				return;
			}
			cooldownFill.GetComponent<Image>().color = ready < 0.25f ? new Color(1f, 0.3f, 0.25f) : ready < 0.5f ? Yellow : Cyan;
			Vector3 screen = Camera.main.WorldToScreenPoint(player.transform.position);
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local))
			{
				cooldownLine.anchoredPosition = local + new Vector2(0f, -22f);
			}
			cooldownFill.anchorMax = new Vector2(Mathf.Clamp01(ready), 1f);
		}

		private void Update()
		{
			if (game == null)
			{
				return;
			}

			if (bannerTimer > 0f)
			{
				bannerTimer -= Time.deltaTime;
				bannerGroup.alpha = Mathf.Clamp01(Mathf.Min(bannerTimer * 2f, (2.4f - bannerTimer) * 4f));
			}
			else
			{
				bannerGroup.alpha = 0f;
			}

			// full-screen tint: cyan in slow-motion, red flash when hurt
			if (game.Health < lastHealth && lastHealth >= 0)
			{
				hurtFlash = 0.45f;
			}
			lastHealth = game.Health;
			hurtFlash = Mathf.Max(0f, hurtFlash - Time.unscaledDeltaTime);
			float slow = 1f - SlowMotion.Scale;
			Color tint = hurtFlash > 0f ? new Color(1f, 0.05f, 0.1f, hurtFlash * 0.8f) : new Color(0.1f, 0.8f, 1f, slow * 0.22f);
			screenTint.color = tint;

			if (popupTimer > 0f)
			{
				popupTimer -= Time.unscaledDeltaTime;
				popupGroup.alpha = Mathf.Clamp01(popupTimer * 3f);
			}
			else
			{
				popupGroup.alpha = 0f;
			}

			if (!hudRoot.activeSelf)
			{
				return;
			}

			scoreText.text = game.Score.ToString("N0");
			bestText.text = Mathf.Max(game.Best, game.Score).ToString("N0");
			comboText.text = game.Combo >= 2 ? "x" + game.Combo : "";
			comboBarRoot.SetActive(game.Combo >= 2);
			comboBarFill.anchorMax = new Vector2(game.ComboTimeLeft, 1f);
			muteText.text = audioDirector != null && audioDirector.Muted ? "SOUND: OFF (PRESS M)" : "";

			if (player != null && ammoPips != null)
			{
				for (int i = 0; i < ammoPips.Length; i++)
				{
					float fill = Mathf.Clamp01(player.Ammo - i);
					ammoPips[i].color = fill >= 1f ? Cyan : Color.Lerp(new Color(0.08f, 0.2f, 0.25f, 0.9f), Dim, fill);
				}
			}

			if (player != null && player.Slash != null)
			{
				float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 10f);
				for (int i = 0; i < slashFill.Length; i++)
				{
					float fill = Mathf.Clamp01(player.Slash.Points - i);
					slashFill[i].anchorMax = new Vector2(fill, 1f);
					Color ready = player.Slash.IsAiming ? Color.Lerp(Color.white, Yellow, pulse) : Yellow;
					slashFillImage[i].color = fill >= 1f ? ready : new Color(0.7f, 0.82f, 0.9f, 0.9f);
				}
			}

			UpdateCooldownLine();

			if (boss != null && !boss.IsDead && bossRoot.activeSelf)
			{
				bossFill.anchorMax = new Vector2(Mathf.Clamp01(boss.Health / (float)boss.MaxHealth), bossFill.anchorMax.y);
				bossFill.GetComponent<Image>().color = boss is Boss leader && leader.IsOpen ? Cyan : new Color(1f, 0.35f, 0.2f); // his guard is down: hit him now
			}

			if (hintGroup != null)
			{
				hintTimer = game.State == GameState.Playing ? hintTimer - Time.deltaTime : hintTimer;
				hintGroup.alpha = Mathf.Clamp01(hintTimer);
			}
		}
	}
}
