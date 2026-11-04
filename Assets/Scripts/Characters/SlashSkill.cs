using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public class SlashSkill : IDisposable
	{
		public const int Parts = 3;

		private const float GainPerHit = 0.15f;
		private const float GainPerComboStep = 0.0035f;
		private const int MaxComboBonus = 20;
		private const float AimSeconds = 6f * 0.7f; // real seconds you have to pick the line
		private const float AimTimeScale = 0.7f;
		private const float MaxRange = 13f;
		private const float MinRange = 2.5f;
		private const float SlashRadius = 1.0f; // half the width of the cut
		private const int GiantDamage = 6;
		private const float DrainPerSecond = 0.07f;
		private const float UltDrainPerSecond = 0.85f;

		private readonly Player owner;
		private readonly LineRenderer zoneLine; // wide, faint: where the cut will land
		private readonly LineRenderer aimLine; // thin, bright: the path
		private readonly LineRenderer reticle; // circle at the end point
		private readonly LineRenderer trailPrefab;
		private readonly LineRenderer flashLine; // the white streak left by the slash
		private int combo;
		private float aimSecondsLeft;
		private bool attackWasHeld;
		private Vector3 target;

		public float Points { get; private set; }
		private int checkpoint; // whole parts already earned: the leak never goes below them
		public bool IsAiming { get; private set; }
		public bool IsSlashing { get; private set; }
		public bool IsUlting { get; private set; }

		private readonly List<Enemy> ultPool = new List<Enemy>(); // rivals on screen that may still be struck
		private readonly List<Enemy> ultHit = new List<Enemy>(); // rivals struck so far: they all fall when it ends
		private readonly List<LineRenderer> ultLines = new List<LineRenderer>();

		public float MoveBoost => IsAiming ? Mathf.Min(1f / Mathf.Max(Time.timeScale, 0.05f), 8f) : 1f;

		public SlashSkill(Player owner, LineRenderer zone, LineRenderer aim, LineRenderer reticleLine, LineRenderer flash, LineRenderer trail)
		{
			this.owner = owner;
			zoneLine = zone;
			aimLine = aim;
			reticle = reticleLine;
			flashLine = flash;
			trailPrefab = trail;
			GameEvents.EnemyKilled += OnKill;
			GameEvents.Deflected += OnDeflect;
			GameEvents.ComboChanged += OnCombo;
		}

		public void Dispose()
		{
			GameEvents.EnemyKilled -= OnKill;
			GameEvents.Deflected -= OnDeflect;
			GameEvents.ComboChanged -= OnCombo;
			SetAimVisible(false);
			flashLine.enabled = false;
		}

		private void OnKill(Enemy enemy) { Gain(GainPerHit + Mathf.Min(combo, MaxComboBonus) * GainPerComboStep); }
		private void OnDeflect(Vector3 at) { Gain(GainPerHit); }
		private void OnCombo(int value) { combo = value; }

		private void Gain(float amount)
		{
			if (IsAiming || IsSlashing)
			{
				return;
			}
			Points = Mathf.Min(Parts, Points + amount);
			checkpoint = Mathf.Max(checkpoint, Mathf.FloorToInt(Points + 0.0001f));
		}

		public void Tick(IInputSource input)
		{
			if (IsUlting)
			{
				UltTick(input);
				return;
			}
			if (IsSlashing)
			{
				return;
			}
			if (!IsAiming)
			{
				// a part that is not full yet slowly empties again; a part that is full is a checkpoint and stays
				if (Points > checkpoint)
				{
					Points = Mathf.Max(checkpoint, Points - DrainPerSecond * Time.deltaTime);
				}
				if (input.SpecialPressed && Points >= 1f && !owner.IsDashing)
				{
					BeginAim();
				}
				return;
			}

			aimSecondsLeft -= Time.unscaledDeltaTime;
			SlowMotion.Pulse(AimTimeScale, 0.12f); // asked every frame, so time stays slow while we aim
			UpdateTarget(input);
			DrawAim();
			CameraRig.Instance?.LeanToward(target - owner.transform.position);
			Vector3 look = target - owner.transform.position;
			look.y = 0f;
			if (look.sqrMagnitude > 0.04f)
			{
				owner.transform.forward = look.normalized; // the hero turns to the line while he crouches
			}

			bool held = input.AttackHeld;
			bool clicked = held && !attackWasHeld; // must release first: the click that starts the mode is not the cut
			attackWasHeld = held;

			if (input.DashPressed && Points >= Parts - 0.001f)
			{
				BeginUltimate();
			}
			else if (input.SpecialPressed || aimSecondsLeft <= 0f)
			{
				Cancel();
			}
			else if (clicked)
			{
				owner.StartCoroutine(SlashRoutine());
			}
		}

		public void Cancel()
		{
			IsAiming = false;
			CameraRig.Instance?.LeanToward(Vector3.zero);
			SetAimVisible(false);
		}

		private void BeginAim()
		{
			IsAiming = true;
			aimSecondsLeft = AimSeconds;
			attackWasHeld = true;
			SetAimVisible(true);
			GameEvents.RaiseSound("warning");
		}

		private void UpdateTarget(IInputSource input)
		{
			Vector3 from = owner.transform.position;
			Vector3 to = owner.PointerOnGround(from + owner.transform.forward * MinRange);
			Vector3 delta = to - from;
			delta.y = 0f;
			if (delta.magnitude < MinRange)
			{
				delta = (delta.sqrMagnitude > 0.01f ? delta.normalized : owner.transform.forward) * MinRange;
			}
			target = Arena.Clamp(from + Vector3.ClampMagnitude(delta, MaxRange));
		}

		private void BeginUltimate()
		{
			IsAiming = false;
			SetAimVisible(false);
			CameraRig.Instance?.LeanToward(Vector3.zero);
			Vector3 from = owner.transform.position;
			owner.transform.position = target;
			owner.GrantInvulnerability(30f);
			owner.RestoreStamina();
			Enemy.Frozen = true; // everything stands still
			ultPool.Clear();
			ultHit.Clear();
			Camera cam = Camera.main;
			foreach (Enemy enemy in Enemy.AllActive)
			{
				if (enemy.IsDead || cam == null)
				{
					continue;
				}
				Vector3 view = cam.WorldToViewportPoint(enemy.transform.position);
				if (view.z > 0f && view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f)
				{
					ultPool.Add(enemy); // only what is on screen can be hit
				}
			}
			IsUlting = true;
			IsSlashing = true;
			owner.PlaySkillPose("ult_enter");
			GameEvents.RaiseSound("heavy");
			CameraRig.Shake(4f, 0.15f, 0.025f);
			StartFlash(from, target);
		}

		private void UltTick(IInputSource input)
		{
			Points = Mathf.Max(0f, Points - UltDrainPerSecond * Time.deltaTime);
			bool held = input.AttackHeld;
			bool clicked = held && !attackWasHeld;
			attackWasHeld = held;
			if (clicked && ultPool.Count > 0)
			{
				// every click strikes a random rival on screen; they all die together when the bar runs out
				int index = UnityEngine.Random.Range(0, ultPool.Count);
				Enemy struck = ultPool[index];
				ultPool.RemoveAt(index);
				ultHit.Add(struck);
				LineRenderer line = UnityEngine.Object.Instantiate(trailPrefab);
				line.SetPosition(0, owner.transform.position + Vector3.up * 0.7f);
				line.SetPosition(1, struck.transform.position + Vector3.up * 0.7f);
				line.enabled = true;
				ultLines.Add(line);
				owner.PlaySlashPose();
				GameEvents.RaiseSound("swing");
				CameraRig.Shake(2f, 0.1f, 0.015f);
			}
			if (Points <= 0f || (ultPool.Count == 0 && ultHit.Count > 0))
			{
				IsUlting = false; // the last frame of the input loop; the routine below finishes it
				owner.StartCoroutine(EndUltimate());
			}
		}

		private IEnumerator EndUltimate()
		{
			owner.PlaySkillPose("ult_end");
			yield return WaitForActiveSeconds(0.5f);
			Points = 0f;
			checkpoint = 0;
			Enemy.Frozen = false;
			foreach (LineRenderer line in ultLines)
			{
				if (line != null)
				{
					UnityEngine.Object.Destroy(line.gameObject);
				}
			}
			ultLines.Clear();
			foreach (Enemy enemy in ultHit)
			{
				if (enemy == null || enemy.IsDead)
				{
					continue;
				}
				enemy.TakeSlayerHit();
				GameEvents.RaiseHitLanded(enemy.transform.position + Vector3.up, 4);
			}
			if (ultHit.Count > 0)
			{
				GameEvents.RaiseUltimateKilled(ultHit.Count);
				GameEvents.RaiseSound("heavy");
				SlowMotion.Pulse(0.25f, 0.3f);
			}
			ultHit.Clear();
			ultPool.Clear();
			owner.SetInvulnerableFor(0.6f); // back to normal
			IsSlashing = false;
		}

		private IEnumerator SlashRoutine()
		{
			IsSlashing = true;
			IsAiming = false;
			CameraRig.Instance?.LeanToward(Vector3.zero);
			Points = Mathf.Max(0f, Points - 1f);
			checkpoint = Mathf.FloorToInt(Points + 0.0001f);
			SetAimVisible(false);

			Vector3 from = owner.transform.position;
			Vector3 to = target;
			Vector3 direction = to - from;
			direction.y = 0f;
			float distance = direction.magnitude;
			direction = distance > 0.01f ? direction / distance : owner.transform.forward;

			var victims = new List<Enemy>();
			foreach (RaycastHit hit in Physics.SphereCastAll(from + Vector3.up * 0.8f, SlashRadius, direction, distance))
			{
				Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
				if (enemy != null && !enemy.IsDead && !victims.Contains(enemy))
				{
					victims.Add(enemy);
				}
			}

			owner.GrantInvulnerability(1.2f);
			owner.RestoreStamina();
			Enemy.Frozen = true;
			SlowMotion.Pulse(0.05f, 0.22f); // the freeze before the cut lands
			owner.transform.position = to;
			owner.transform.forward = direction;
			owner.PlaySlashPose();
			GameEvents.RaiseSound("swing");
			StartFlash(from, to);

			yield return WaitForActiveSeconds(0.3f);

			Enemy.Frozen = false;
			int hits = 0;
			foreach (Enemy enemy in victims)
			{
				if (enemy != null && !enemy.IsDead)
				{
					int healthBefore = enemy.Health;
					enemy.TakeDamage(enemy is Brute ? GiantDamage : 99, owner.gameObject);
					if (enemy.Health < healthBefore)
					{
						hits++;
						GameEvents.RaiseHitLanded(enemy.transform.position + Vector3.up, 4);
					}
				}
			}
			if (hits > 0)
			{
				GameEvents.RaiseSlashLanded(hits);
				CameraRig.Shake(2f, 0.1f, 0.015f);
				GameEvents.RaiseSound("heavy");
				SlowMotion.Pulse(0.25f, 0.25f);
			}

			IsSlashing = false;
		}

		// Keep the real-time skill timing, but stop its clock while gameplay is paused.
		private IEnumerator WaitForActiveSeconds(float seconds)
		{
			float elapsed = 0f;
			while (elapsed < seconds)
			{
				yield return null;
				if (owner.enabled)
				{
					elapsed += Time.unscaledDeltaTime;
				}
			}
		}

		private void SetAimVisible(bool visible)
		{
			zoneLine.enabled = visible;
			aimLine.enabled = visible;
			reticle.enabled = visible;
		}

		private void DrawAim()
		{
			Vector3 from = owner.transform.position + Vector3.up * 0.12f;
			Vector3 to = target + Vector3.up * 0.12f;
			zoneLine.SetPosition(0, from);
			zoneLine.SetPosition(1, to);
			aimLine.SetPosition(0, from);
			aimLine.SetPosition(1, to);
			float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
			for (int i = 0; i < reticle.positionCount; i++)
			{
				float angle = i / (float)(reticle.positionCount - 1) * Mathf.PI * 2f;
				float radius = 0.55f + pulse * 0.12f;
				reticle.SetPosition(i, to + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
			}
		}

		private void StartFlash(Vector3 from, Vector3 to)
		{
			flashLine.SetPosition(0, from + Vector3.up * 0.7f);
			flashLine.SetPosition(1, to + Vector3.up * 0.7f);
			flashLine.enabled = true;
			owner.StartCoroutine(FadeFlash());
		}

		private IEnumerator FadeFlash()
		{
			const float duration = 0.4f;
			for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
			{
				while (!owner.enabled)
				{
					yield return null;
				}
				float k = 1f - t / duration;
				var color = new Color(1f, 1f, 1f, k);
				flashLine.startColor = color;
				flashLine.endColor = color;
				flashLine.widthMultiplier = 0.7f * k;
				yield return null;
			}
			flashLine.enabled = false;
		}
	}
}
