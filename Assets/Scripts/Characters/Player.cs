using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public class Player : Character, IShootable
	{
		private const float HitInvulnerability = 0.9f;
		public const float RunSpeedRatio = 1.6f; // running multiplier on the base move speed
		private const float RunSpeed = RunSpeedRatio;
		public const float ParryWindow = 1.5f;
		private const float ParryDelay = 0.3f;
		private const float AttackDeflectFrame = 0.22f; // a swing also sends a bullet that reaches the hero toward the nearest rival
		private const float DeflectAngleJitter = 8f;
		private float parryReadyAt;
		private float attackDeflectUntil;

		public const int MaxAmmo = 8;
		private const float AmmoPerSecond = 8f / 30f;
		private const float AmmoPerKill = 0.45f;
		private const float ShotDelay = 0.2f;
		private const float BulletSpeed = 92f;
		private const int BulletDamage = 3;

		private const float StaminaPerSwing = 0.07f;
		private const float StaminaChargeDelay = 1f;
		private const float StaminaChargeSpeed = 2f;
		private float staminaChargeAt;
		private const float AimMoveScale = 0f; // standing still while aiming the pen gun
		private float nextShotTime;
		private float nextGaspTime;

		private IInputSource input;
		private IBulletSpawner bullets;
		private Weapon[] weapons;
		private IAbility[] abilities;
		private int currentWeapon;
		private float invulnerableUntil;
		private Vector3 attackDirection = Vector3.forward;
		private float dashUntil;
		private Vector3 dashDirection;
		private float dashSpeedMultiplier;
		private float parryUntil;

		public PlayerStats Stats { get; } = new PlayerStats();
		public override Team Team => Team.Player;
		public override string DisplayName => "VOCATIONAL STUDENT";
		public Weapon CurrentWeapon => weapons[currentWeapon];
		public Weapon[] Weapons => weapons;
		public IAbility[] Abilities => abilities;
		[SerializeField] private LineRenderer slashZone;
		[SerializeField] private LineRenderer slashAim;
		[SerializeField] private LineRenderer slashReticle;
		[SerializeField] private LineRenderer slashFlash;
		[SerializeField] private LineRenderer ultTrail;
		public SlashSkill Slash { get; private set; }
		public bool IsInvulnerable => Time.time < invulnerableUntil;
		public bool IsDashing => Time.time < dashUntil;
		public bool IsParrying => Time.time < parryUntil;
		public Vector3 LastMoveDirection { get; private set; }
		public float Ammo { get; private set; } = MaxAmmo;
		public float Stamina { get; private set; } = 1f;
		public bool IsAiming { get; private set; }
		public event Action Shot;

		public void Setup(IInputSource input, IBulletSpawner bullets, Weapon[] weapons, IAbility[] abilities)
		{
			this.input = input;
			this.bullets = bullets;
			this.weapons = weapons;
			this.abilities = abilities;
			currentWeapon = 0;
			Stats.Reset();
			Slash?.Dispose();
			Slash = new SlashSkill(this, slashZone, slashAim, slashReticle, slashFlash, ultTrail);
			GameEvents.EnemyKilled += OnEnemyKilledAmmo;
			GameEvents.RaisePlayerHealthChanged(Health, MaxHealth);
			GameEvents.RaiseWeaponChanged(currentWeapon);
		}

		protected override void Awake()
		{
			moveSpeed = 5.2f;
			maxHealth = 1;
			base.Awake();
		}

		private void Update()
		{
			if (IsDead || input == null)
			{
				return;
			}

			Vector3 aim = AttackDirection();

			Slash.Tick(input);
			if (Slash.IsSlashing)
			{
				FinishFrame();
				return;
			}

			if (IsDashing)
			{
				Move(dashDirection, dashSpeedMultiplier);
			}
			else
			{
				Vector3 wish = input.MoveDirection;
				LastMoveDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : Vector3.zero;
				IsAiming = input.AimHeld;
				Move(wish, RunSpeed * Stats.MoveSpeedScale * (IsAiming ? AimMoveScale : 1f) * Slash.MoveBoost);
			}

			// the fighter faces the mouse (and swings toward it); while dashing it faces the dash
			Vector3 gunTarget = IsAiming && input != null
				? GunAim.Point(Camera.main, input.PointerScreenPosition, transform.position + aim)
				: transform.position + aim;
			Vector3 face = IsDashing ? dashDirection : aim != Vector3.zero ? aim : LastMoveDirection;
			if (IsAiming && !IsDashing)
			{
				Vector3 toMouse = gunTarget - transform.position;
				toMouse.y = 0f;
				if (toMouse.sqrMagnitude > 0.04f)
				{
					face = toMouse.normalized;
				}
			}
			if (face != Vector3.zero)
			{
				transform.forward = IsAiming && !IsDashing ? face : Vector3.Slerp(transform.forward, face, 18f * Time.deltaTime); // the aim follows the mouse at once
			}
			Ammo = Mathf.Min(MaxAmmo, Ammo + AmmoPerSecond * Stats.AmmoRegenScale * Time.deltaTime);
			if (Stamina < 1f && Time.time >= staminaChargeAt)
			{
				Stamina = Mathf.Min(1f, Stamina + StaminaChargeSpeed * Time.deltaTime);
			}

			if (Slash.IsAiming)
			{
				FinishFrame(); // choosing the slash line: no swinging, shooting, dashing or parrying
				return;
			}

			int slot = input.WeaponSlotPressed;
			if (slot >= 0 && slot < weapons.Length && slot != currentWeapon)
			{
				currentWeapon = slot;
				GameEvents.RaiseWeaponChanged(currentWeapon);
			}

			if (input.DashPressed)
			{
				abilities[0].TryActivate(this, aim);
			}
			if (input.ParryPressed)
			{
				abilities[1].TryActivate(this, aim);
			}
			if (parryUntil > 0f && Time.time >= parryUntil)
			{
				EndParry();
			}

			if (IsAiming && input.AttackHeld && !IsDashing)
			{
				Vector3 gunAim = gunTarget - transform.position;
				gunAim.y = 0f;
				gunAim = gunAim.sqrMagnitude > 0.04f ? gunAim.normalized : aim;
				if (Time.time >= nextShotTime)
				{
					nextShotTime = Time.time + ShotDelay;
					if (Ammo >= 1f)
					{
						Ammo -= 1f;
						transform.forward = gunAim;
						Vector3 muzzle = transform.position + Vector3.up * 0.9f + gunAim * 0.8f;
						bullets?.Spawn(muzzle, gunAim, BulletSpeed, BulletDamage + Stats.DamageBonus, Team, gameObject);
						GameEvents.RaiseSound("shot");
						Shot?.Invoke();
					}
					else
					{
						GameEvents.RaiseSound("empty"); // out of rounds: a dry click
					}
				}
			}
			else if (input.AttackHeld && aim != Vector3.zero && !IsDashing)
			{
				// the swing goes toward the mouse, where the fighter faces
				Vector3 facing = aim;
				if (Stamina < StaminaPerSwing)
				{
					if (Time.time >= nextGaspTime)
					{
						nextGaspTime = Time.time + 0.5f;
						GameEvents.RaiseSound("empty"); // out of breath
					}
				}
				else
				{
					var context = new AttackContext(gameObject, Team, transform.position + Vector3.up * 0.7f, facing, bullets, Stats);
					string clip = CurrentWeapon.TryUse(context);
					if (clip != null)
					{
						Stamina = Mathf.Max(0f, Stamina - StaminaPerSwing);
						staminaChargeAt = Time.time + StaminaChargeDelay;
						attackDeflectUntil = Time.time + AttackDeflectFrame;
						GameEvents.RaiseSwingStarted();
						if (IsParrying)
						{
							EndParry(); // attacking drops the guard
						}
						RaiseAttacked(clip);
					}
				}
			}

			FinishFrame();
		}

		public bool TryStartParry()
		{
			if (IsParrying || IsDashing || Time.time < parryReadyAt || Slash.IsAiming || Slash.IsSlashing)
			{
				return false;
			}
			parryUntil = Time.time + ParryWindow;
			RaiseAttacked("attack-melee-left");
			GameEvents.RaiseSound("parry_ready");
			return true;
		}

		public void CancelParryForDash()
		{
			if (IsParrying)
			{
				EndParry();
			}
		}

		private void EndParry()
		{
			parryUntil = 0f;
			parryReadyAt = Time.time + ParryDelay;
		}

		public bool TakeShot(Bullet bullet)
		{
			if (bullet.WasDeflectedBy(gameObject))
			{
				return true;
			}
			bullet.MarkDeflected(gameObject);
			Vector3 at = transform.position + Vector3.up * 0.9f;
			if (IsParrying)
			{
				Vector3 back = Quaternion.Euler(0f, UnityEngine.Random.Range(-DeflectAngleJitter, DeflectAngleJitter), 0f) * -bullet.Direction;
				bullet.Reflect(back, Team, gameObject, 2 + Stats.DamageBonus);
				GameEvents.RaiseDeflected(at);
				GameEvents.RaiseSound("deflect");
				return true;
			}
			if (Time.time < attackDeflectUntil)
			{
				Enemy nearest = null;
				float best = float.MaxValue;
				foreach (Enemy enemy in Enemy.AllActive)
				{
					float distance = (enemy.transform.position - transform.position).sqrMagnitude;
					if (!enemy.IsDead && distance < best)
					{
						best = distance;
						nearest = enemy;
					}
				}
				Vector3 toward = nearest != null ? nearest.transform.position - transform.position : -bullet.Direction;
				toward.y = 0f;
				bullet.Reflect(toward.normalized, Team, gameObject, bullet.Damage * 100);
				GameEvents.RaiseDeflected(at);
				GameEvents.RaiseSound("deflect");
				SlowMotion.Pulse(0.3f, 0.12f);
				return true;
			}
			return false;
		}

		public void StartDash(Vector3 direction, float duration, float speedMultiplier)
		{
			dashDirection = direction;
			dashUntil = Time.time + duration;
			GrantInvulnerability(duration + 0.04f); // the dash itself is a safe moment
			dashSpeedMultiplier = speedMultiplier;
			transform.forward = direction;
		}

		private Vector3 AttackDirection()
		{
			Vector3 aim = PointerOnGround(transform.position + attackDirection) - transform.position;
			aim.y = 0f;
			if (aim.sqrMagnitude > 0.01f)
			{
				attackDirection = aim.normalized;
			}
			return attackDirection;
		}

		public Vector3 PointerOnGround(Vector3 fallback)
		{
			Camera cam = Camera.main;
			if (cam == null || input == null)
			{
				return fallback;
			}
			Ray ray = cam.ScreenPointToRay(input.PointerScreenPosition);
			var ground = new Plane(Vector3.up, Vector3.zero);
			return ground.Raycast(ray, out float distance) ? ray.GetPoint(distance) : fallback;
		}

		private void OnEnemyKilledAmmo(Enemy enemy)
		{
			Ammo = Mathf.Min(MaxAmmo, Ammo + AmmoPerKill * Stats.AmmoRegenScale);
		}

		public void RestoreStamina()
		{
			Stamina = 1f;
		}

		public void GrantInvulnerability(float seconds)
		{
			invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
		}

		public void SetInvulnerableFor(float seconds)
		{
			invulnerableUntil = Time.time + seconds;
		}

		public void PlaySlashPose()
		{
			RaiseAttacked("attack-melee-right");
		}

		public void PlaySkillPose(string clip)
		{
			RaiseAttacked("v2:" + clip);
		}

		protected override void OnDamaged(GameObject source)
		{
			invulnerableUntil = Time.time + HitInvulnerability;
			GameEvents.RaisePlayerHealthChanged(Health, MaxHealth);
			GameEvents.RaiseSound("hurt");
		}

		public override void TakeDamage(int amount, GameObject source)
		{
			if (IsInvulnerable)
			{
				return;
			}
			if (IsParrying && !(source != null && source.GetComponent<IUnparryable>() != null))
			{
				CameraRig.Shake(2f, 0.1f, 0.015f);
				GameEvents.RaiseDeflected(transform.position + Vector3.up);
				GameEvents.RaiseSound("parry");
				if (source != null)
				{
					GameEvents.RaiseHitLanded(Vector3.Lerp(transform.position, source.transform.position, 0.5f) + Vector3.up, 1);
				}
				return;
			}
			base.TakeDamage(amount, source);
		}

		protected override void Die()
		{
			Slash.Cancel();
			SlowMotion.Pulse(0.2f, 1.0f);
			GameEvents.RaisePlayerDied();
		}

		private void OnDestroy()
		{
			Slash?.Dispose();
			GameEvents.EnemyKilled -= OnEnemyKilledAmmo;
			if (abilities == null)
			{
				return;
			}
			foreach (IAbility ability in abilities)
			{
				(ability as IDisposable)?.Dispose();
			}
		}
	}
}
