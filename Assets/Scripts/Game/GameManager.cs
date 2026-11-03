using System;
using UnityEngine;

namespace MyArcade
{
	public enum GameState
	{
		Title,
		Playing,
		Paused,
		GameOver
	}

	public class GameManager : MonoBehaviour
	{
		private const string BestKey = "MyArcade.BestKills";
		private const string BestComboKey = "MyArcade.BestCombo";
		private const string BestBoldnessKey = "MyArcade.BestBoldness";
		private const float ComboWindow = 1.5f;

		private IInputSource input;
		private float comboExpires;

		public GameState State { get; private set; } = GameState.Title;
		public int Score { get; private set; }
		public int Best { get; private set; }
		public int Wave { get; private set; }
		public int Combo { get; private set; }
		public int BestCombo { get; private set; }
		public float ComboTimeLeft => Combo > 0 ? Mathf.Clamp01((comboExpires - Time.time) / ComboWindow) : 0f;
		public int Health { get; private set; }
		public int MaxHealth { get; private set; }
		public int WeaponSlot { get; private set; }
		public int Kills { get; private set; }
		public bool NewRecord { get; private set; }

		public int Boldness { get; private set; }
		public int BestBoldness { get; private set; }
		public int BestMaxCombo { get; private set; }
		public int Attacks { get; private set; }
		public int Hits { get; private set; }
		public float Accuracy => Attacks > 0 ? Hits / (float)Attacks : 0f;
		public event Action<int> BoldnessAdded;

		public event Action<GameState> StateChanged;

		public static int LastKnownHealth { get; private set; }

		private Player player;

		public void Setup(IInputSource input, Player owner)
		{
			this.input = input;
			player = owner;
			Best = PlayerPrefs.GetInt(BestKey, 0);
			BestMaxCombo = PlayerPrefs.GetInt(BestComboKey, 0);
			BestBoldness = PlayerPrefs.GetInt(BestBoldnessKey, 0);
			SlowMotion.Reset();
			GameEvents.EnemyKilled += OnEnemyKilled;
			GameEvents.WaveStarted += w => Wave = w;
			GameEvents.PlayerHealthChanged += OnHealth;
			GameEvents.PlayerDied += OnPlayerDied;
			GameEvents.WeaponChanged += slot => WeaponSlot = slot;

			GameEvents.BulletDodged += () => AddBoldness(30);
			GameEvents.Deflected += _ => AddBoldness(30);
			GameEvents.BulletHitEnemy += () => AddBoldness(1);
			GameEvents.SlashLanded += _ => AddBoldness(10);
			GameEvents.UltimateKilled += count => AddBoldness(3 * count);
			GameEvents.SwingStarted += () => Attacks++;
			GameEvents.SwingConnected += () => Hits++;
		}

		private void AddBoldness(int points)
		{
			Boldness += points;
			BoldnessAdded?.Invoke(points);
		}

		private void OnHealth(int hp, int max)
		{
			if (hp < Health)
			{
				SetCombo(0); // getting hit breaks the combo
			}
			Health = hp;
			MaxHealth = max;
			LastKnownHealth = hp;
		}

		private void OnEnemyKilled(Enemy enemy)
		{
			Kills++;
			if (enemy is Assassin || enemy is Brute)
			{
				AddBoldness(25);
			}
			SetCombo(Combo + 1);
			BestCombo = Mathf.Max(BestCombo, Combo);
			comboExpires = Time.time + ComboWindow;
			Score++;
			if (enemy is Boss)
			{
				SlowMotion.Pulse(0.22f, 1.3f);
			}
			else if (Combo > 0 && Combo % 10 == 0)
			{
				SlowMotion.Pulse(0.4f, 0.5f);
			}
		}

		private void OnPlayerDied()
		{
			if (Score > Best)
			{
				Best = Score;
				NewRecord = true;
				PlayerPrefs.SetInt(BestKey, Best);
			}
			if (BestCombo > BestMaxCombo)
			{
				BestMaxCombo = BestCombo;
				PlayerPrefs.SetInt(BestComboKey, BestMaxCombo);
			}
			if (Boldness > BestBoldness)
			{
				BestBoldness = Boldness;
				PlayerPrefs.SetInt(BestBoldnessKey, BestBoldness);
			}
			ChangeState(GameState.GameOver);
		}

		private void SetCombo(int value)
		{
			Combo = value;
			GameEvents.RaiseComboChanged(Combo);
		}

		private void ChangeState(GameState next)
		{
			State = next;
			StateChanged?.Invoke(next);
		}

		public void SetState(GameState next)
		{
			ChangeState(next);
		}

		private void OnDestroy()
		{
			Time.timeScale = 1f;
			Time.fixedDeltaTime = 0.02f;
			SlowMotion.Reset();
		}

		private void Update()
		{
			// pause stops time; otherwise slow-motion sets the speed
			SlowMotion.Tick();
			bool frozen = State == GameState.Paused;
			float scale = frozen ? 0f : SlowMotion.Scale;
			Time.timeScale = scale;
			Time.fixedDeltaTime = 0.02f * Mathf.Max(scale, 0.05f);

			if (input == null)
			{
				return;
			}

			switch (State)
			{
				case GameState.Title:
					break; // the title menu (MenuScreens) starts the game
				case GameState.Playing:
					if (input.PausePressed)
					{
						ChangeState(GameState.Paused);
						break;
					}
					if (Combo > 0 && Time.time > comboExpires)
					{
						SetCombo(0);
					}
					break;
				case GameState.Paused:
					if (input.PausePressed && !MenuScreens.BackHandled)
					{
						ChangeState(GameState.Playing);
					}
					else if (input.RestartPressed)
					{
						GameBootstrap.Restart();
					}
					break;
				case GameState.GameOver:
					if (input.RestartPressed)
					{
						GameBootstrap.Restart();
					}
					break;
			}
		}
	}
}
