using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public class AudioDirector : MonoBehaviour
	{
		private const int SampleRate = 22050;
		private const string MuteKey = "MyArcade.Mute";

		private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
		private readonly Dictionary<string, AudioClip[]> variants = new Dictionary<string, AudioClip[]>(); // real recordings, several per sound
		[SerializeField] private AudioSource[] voices;
		[SerializeField] private AudioSource guardVoice;
		private int nextVoice;
		private GameManager game;
		private IInputSource input;
		private bool muted;
		private Player guardOwner;
		private bool guardPaused;

		public void Setup(GameManager manager, IInputSource inputSource, Player owner)
		{
			game = manager;
			input = inputSource;
			guardOwner = owner;
			muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;

			// small mixing buffers: the default ones add ~80-100 ms between "play" and hearing it, which made hits, shots and
			// the dash sound late against the animation
			AudioConfiguration config = AudioSettings.GetConfiguration();
			config.dspBufferSize = 256;
			AudioSettings.Reset(config);

			BuildClips();
			LoadRecordings();
			GameEvents.SoundRequested += Play;
			GameEvents.EnemyKilled += _ => Play("ko");
			GameEvents.WaveStarted += _ => Play("wave");
			GameEvents.BossCharging += _ => Play("warning");
			GameEvents.PlayerDied += () => Play("gameover");
			ApplyMute();
		}

		private void OnDestroy()
		{
			GameEvents.SoundRequested -= Play;
		}

		private void Update()
		{
			UpdateGuardSound();
			if (input != null && input.MutePressed)
			{
				muted = !muted;
				PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
				ApplyMute();
			}
		}

		private void ApplyMute()
		{
			AudioListener.volume = muted ? 0f : GameSettings.Volume01;
		}

		public bool Muted => muted;

		public void RefreshVolume()
		{
			ApplyMute();
		}

		private void LoadRecordings()
		{
			foreach (string id in new[] { "swing", "shot", "empty", "hit", "heavy", "dash", "ko", "deflect", "parry", "warning", "wave", "gameover", "ui_select", "ui_confirm", "ui_cancel" })
			{
				AudioClip[] found = Resources.LoadAll<AudioClip>("Audio/" + id);
				if (id == "deflect") found = Array.FindAll(found, clip => clip.name.StartsWith("deflect", StringComparison.OrdinalIgnoreCase));
				if (found != null && found.Length > 0)
				{
					variants[id] = found;
				}
			}
		}

		private void UpdateGuardSound()
		{
			if (guardVoice == null || guardOwner == null) return;
			if (game.State == GameState.Paused)
			{
				if (guardVoice.isPlaying && !guardPaused) { guardVoice.Pause(); guardPaused = true; }
				return;
			}
			if (guardPaused) { guardVoice.UnPause(); guardPaused = false; }
			if (guardVoice.isPlaying && (!guardOwner.IsParrying || guardOwner.IsDead || game.State != GameState.Playing))
			{
				guardVoice.volume = Mathf.MoveTowards(guardVoice.volume, 0f, Time.unscaledDeltaTime * 7.5f);
				if (guardVoice.volume <= .001f) guardVoice.Stop();
			}
		}

		private void Play(string id)
		{
			if (id == "parry_ready")
			{
				if (guardVoice != null && guardVoice.clip != null)
				{
					guardVoice.volume = .75f;
					guardVoice.pitch = 1f;
					guardPaused = false;
					guardVoice.Play();
				}
				return;
			}
			AudioClip clip;
			if (variants.TryGetValue(id, out AudioClip[] recordings))
			{
				clip = recordings[UnityEngine.Random.Range(0, recordings.Length)];
			}
			else if (voices == null || !clips.TryGetValue(id, out clip))
			{
				return;
			}
			if (voices == null)
			{
				return;
			}
			AudioSource voice = voices[nextVoice];
			nextVoice = (nextVoice + 1) % voices.Length;
			voice.pitch = id == "parry" ? 1f : 0.97f + UnityEngine.Random.value * 0.06f;
			voice.PlayOneShot(clip, 0.9f);
		}

		private void BuildClips()
		{
			clips["swing"] = Make("swing", 0.12f, t => Mathf.Lerp(900f, 250f, t), 0.9f, 2.0f, 0.22f);
			clips["empty"] = Make("empty", 0.05f, t => Mathf.Lerp(420f, 260f, t), 0.3f, 2.4f, 0.2f);
			clips["shot"] = Make("shot", 0.13f, t => Mathf.Lerp(950f, 180f, t), 0.5f, 3.2f, 0.8f);
			clips["hit"] = Make("hit", 0.15f, t => Mathf.Lerp(190f, 60f, t), 0.5f, 2.6f, 0.7f);
			clips["heavy"] = Make("heavy", 0.26f, t => Mathf.Lerp(130f, 38f, t), 0.45f, 2.0f, 0.95f);
			clips["dash"] = Make("dash", 0.2f, t => Mathf.Lerp(700f, 180f, t), 0.85f, 1.6f, 0.3f);
			clips["hurt"] = Make("hurt", 0.3f, t => Mathf.Lerp(240f, 100f, t), 0.25f, 1.8f, 0.7f);
			clips["ko"] = Make("ko", 0.22f, t => Mathf.Lerp(330f, 90f, t), 0.3f, 2.2f, 0.5f);
			clips["deflect"] = Notes("deflect", 0.06f, new[] { 1320f, 1760f }, 0.45f);
			clips["parry"] = clips["deflect"];
			clips["wave"] = Notes("wave", 0.2f, new[] { 196f, 294f, 392f }, 0.4f, true);
			clips["warning"] = Make("warning", 0.7f, t => 300f + Mathf.Sin(t * 18f) * 120f, 0.1f, 0.4f, 0.5f);
			clips["gameover"] = Notes("gameover", 0.28f, new[] { 392f, 330f, 262f, 196f }, 0.5f, true);
		}

		private static AudioClip Make(string name, float seconds, Func<float, float> frequency, float noise, float decay, float volume)
		{
			int count = Mathf.CeilToInt(seconds * SampleRate);
			var data = new float[count];
			var random = new System.Random(name.GetHashCode());
			float phase = 0f;
			float lowpass = 0f;
			for (int i = 0; i < count; i++)
			{
				float t = i / (float)count;
				phase += 2f * Mathf.PI * frequency(t) / SampleRate;
				float tone = Mathf.Sin(phase);
				float white = (float)(random.NextDouble() * 2.0 - 1.0);
				lowpass += (white - lowpass) * Mathf.Lerp(0.9f, 0.15f, t);
				float sample = tone * (1f - noise) + lowpass * noise;
				data[i] = sample * Mathf.Pow(1f - t, decay) * volume;
			}
			return ToClip(name, data);
		}

		private static AudioClip Notes(string name, float noteSeconds, float[] pitches, float volume, bool saw = false)
		{
			int per = Mathf.CeilToInt(noteSeconds * SampleRate);
			var data = new float[per * pitches.Length];
			for (int n = 0; n < pitches.Length; n++)
			{
				float phase = 0f;
				for (int i = 0; i < per; i++)
				{
					phase += pitches[n] / SampleRate;
					float wave = saw ? (phase % 1f) * 2f - 1f : Mathf.Sin(phase * 2f * Mathf.PI);
					float env = Mathf.Pow(1f - i / (float)per, 1.4f);
					data[n * per + i] = wave * env * volume * (saw ? 0.55f : 1f);
				}
			}
			return ToClip(name, data);
		}

		private static AudioClip ToClip(string name, float[] data)
		{
			var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
			clip.SetData(data, 0);
			return clip;
		}
	}
}
