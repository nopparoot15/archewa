using UnityEngine;

namespace MyArcade
{
	/// <summary>Marks an attack that the guard (parry) cannot stop, like Akane's assassin charge that kills on contact.</summary>
	public interface IUnparryable
	{
	}

	/// <summary>
	/// Akane's assassin (EnemyAssassin.cs): walks up, stops, warns for one second, then dashes in a straight line at 3x the
	/// hero's speed and kills on contact (the guard does not help, only a roll does). Until the dash ends it parries every
	/// swing and sends bullets back; for the short moment of braking after the dash it is open and dies to one hit.
	/// </summary>
	public class Assassin : Enemy, IShootable, IUnparryable
	{
		// Main Stage values: speed 110/180 of the hero, warning 1 s, charge 550 (about 25 units/s) for 1 s, 4 s between
		// attacks, a bullet it deflects flies back within +-45 degrees. The braking pause is 0.3 s + 0.6 s.
		private const float AttackRange = 6.9f;
		private const float WarnTime = 1.3f;
		private const float ChargeSpeed = 21f;
		private const float ChargeTime = 1f;
		private const float BrakingTime = 1.2f;
		private const float AttackDelay = 4f;
		private const float DeflectAngle = 45f;

		private enum Phase { Approach, Warn, Charge, Braking }

		private Phase phase;
		private float phaseEndsAt;
		private float nextAttackAt;
		private Vector3 chargeDirection;
		private bool hit;

		public override string DisplayName => "ASSASSIN";
		public bool IsOpen => phase == Phase.Braking;
		public override string VisualPose => phase == Phase.Warn ? "warn" : phase == Phase.Charge ? "charge" : phase == Phase.Braking ? "open" : null;

		protected override void Awake()
		{
			moveSpeed = 5.07f;
			maxHealth = 1;
			base.Awake();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			phase = Phase.Approach;
			nextAttackAt = Time.time + 1.5f;
		}

		protected override void Behave()
		{
			switch (phase)
			{
				case Phase.Approach:
					FaceTarget();
					if (DistanceToTarget() > AttackRange)
					{
						Move(DirectionToTarget());
					}
					else if (Time.time >= nextAttackAt)
					{
						phase = Phase.Warn;
						phaseEndsAt = Time.time + WarnTime;
						RaiseAttacked("windup");
						GameEvents.RaiseSound("warning");
					}
					break;
				case Phase.Warn:
					FaceTarget(); // stands still and keeps looking at the hero; the dash direction is taken when it starts
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Charge;
						phaseEndsAt = Time.time + ChargeTime;
						chargeDirection = DirectionToTarget();
						hit = false;
						RaiseAttacked("attack-kick-left");
					}
					break;
				case Phase.Charge:
					Move(chargeDirection, ChargeSpeed / moveSpeed);
					if (!hit && DistanceToTarget() < 1.5f)
					{
						hit = true;
						target.TakeDamage(1, gameObject);
						GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, 2);
						phaseEndsAt = Time.time; // Akane: the charge ends at once when it kills
					}
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Braking;
						phaseEndsAt = Time.time + BrakingTime;
						nextAttackAt = Time.time + AttackDelay;
						GameEvents.RaiseSound("warning");
					}
					break;
				default:
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Approach;
					}
					break;
			}
		}

		/// <summary>Melee: only the braking pause lets a swing through; any other time it parries (sparks, no damage).</summary>
		public override void TakeDamage(int amount, GameObject source)
		{
			if (IsDead)
			{
				return;
			}
			if (!IsOpen)
			{
				GameEvents.RaiseDeflected(transform.position + Vector3.up);
				GameEvents.RaiseSound("deflect");
				return;
			}
			base.TakeDamage(amount, source);
		}

		/// <summary>Bullets: sent back at the shooter (random angle) unless the assassin is open.</summary>
		public bool TakeShot(Bullet bullet)
		{
			if (bullet.WasDeflectedBy(gameObject))
			{
				return true;
			}
			bullet.MarkDeflected(gameObject);
			if (IsOpen)
			{
				return false;
			}
			Vector3 back = Quaternion.Euler(0f, Random.Range(-DeflectAngle, DeflectAngle), 0f) * -bullet.Direction;
			bullet.Reflect(back, Team, gameObject, bullet.Damage);
			GameEvents.RaiseDeflected(transform.position + Vector3.up);
			GameEvents.RaiseSound("deflect");
			return true;
		}
	}

	/// <summary>
	/// The gang leader, after Akane's Katsuro (Boss.cs). He has a guard: he shrugs off everything (sparks, no damage) until a
	/// special attack of his own ends. He leaps toward you in a zigzag, then picks melee (50%), a three-shot burst (25%) or
	/// the special: a warning, then three dashes at you. After two normal attacks the next one is always the special, and when
	/// it ends his guard breaks: he is pushed back and stays open for a short time that gets shorter with every boss.
	/// </summary>
	public class Boss : Brute
	{
		// Main Stage: zigzag 5 jumps of 0.15 s at 500 (about 23 units/s) over 1 s, speed 200/180, 2 attacks before the
		// special, 3 sub-charges 0.46 s apart after a 1 s warning, open for 1.15 / 0.7 / 0.52 s (easy, medium, hard)
		private const float ZigzagSpeed = 23f;
		private const float ZigzagJump = 0.15f;
		private const int ZigzagJumps = 5;
		private const float MeleeChance = 0.5f;
		private const float ShootChance = 0.25f;
		private const float MeleeRange = 2.4f;
		private const float MeleeWindup = 0.35f;
		private const float MeleeDelay = 1f;
		private const float ShotWarn = 1.3f;
		private const float ShotGap = 0.3f;
		private const int ShotsPerAttack = 2;
		private const float ShotLock = 0.12f;
		private const float BulletSpeed = 92f;
		private const int AttacksBeforeSpecial = 2;
		private const float ChargeWarn = 1.3f;
		private const int SubCharges = 3;
		private const float SubChargeGap = 0.46f;
		private const float SubChargeTime = 0.35f;
		private const float ChargeSpeed = 21f;
		private const float PushSpeed = 22f;
		private const float PushTime = 0.08f;

		private enum Phase { Zigzag, MeleeWind, MeleeRest, ShotWarn, Shots, ChargeWarn, Charge, Pushed, Open }

		private IBulletSpawner bullets;
		private Phase phase;
		private float phaseEndsAt;
		private float nextShotAt;
		private int shotsLeft;
		private int jumpsDone;
		private float jumpEndsAt;
		private float jumpSign = 1f;
		private Vector3 jumpDirection;
		private int attacksCount;
		private int chargesDone;
		private Vector3 chargeDirection;
		private bool chargeHit;
		private Vector3 aimDirection;

		public override string DisplayName => "GANG LEADER";

		/// <summary>True while his guard is broken: only now does damage count (the UI colours his bar).</summary>
		public bool IsOpen => phase == Phase.Open;
		public override string VisualPose => phase == Phase.Open ? "open" : phase == Phase.Charge ? "charge" : phase == Phase.Shots ? "shoot" : phase == Phase.ShotWarn || phase == Phase.ChargeWarn || phase == Phase.MeleeWind ? "warn" : null;

		public void SetBullets(IBulletSpawner spawner)
		{
			bullets = spawner;
		}

		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 9.2f;       // 200 against the hero's 180
			maxHealth = 12;
			contactRange = MeleeRange;
			displayName = "GANG LEADER";
			ResetHealth();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			attacksCount = 0;
			StartZigzag();
			GameEvents.RaiseBossChanged(this);
		}

		private float OpenSeconds()
		{
			int beaten = Spawner.BossesDefeated;
			return beaten <= 0 ? 1.7f : beaten == 1 ? 1.3f : 1.0f;
		}

		private void StartZigzag()
		{
			phase = Phase.Zigzag;
			jumpsDone = 0;
			jumpEndsAt = 0f;
		}

		protected override void Behave()
		{
			switch (phase)
			{
				case Phase.Zigzag: UpdateZigzag(); break;
				case Phase.MeleeWind: UpdateMeleeWind(); break;
				case Phase.MeleeRest:
					FaceTarget();
					if (Time.time >= phaseEndsAt)
					{
						StartZigzag();
					}
					break;
				case Phase.ShotWarn: UpdateShotWarn(); break;
				case Phase.Shots: UpdateShots(); break;
				case Phase.ChargeWarn:
					FaceTarget();
					if (Time.time >= phaseEndsAt)
					{
						BeginSubCharge();
					}
					break;
				case Phase.Charge: UpdateCharge(); break;
				case Phase.Pushed:
					Move(pushDirection, PushSpeed / moveSpeed);
					if (Time.time >= phaseEndsAt)
					{
						phase = Phase.Open;
						phaseEndsAt = Time.time + OpenSeconds();
					}
					break;
				default:
					FaceTarget();
					if (Time.time >= phaseEndsAt)
					{
						attacksCount = 0;
						StartZigzag();
					}
					break;
			}
		}

		private Vector3 pushDirection;

		// ---- leaping toward the hero in a zigzag -------------------------------------------------

		private void UpdateZigzag()
		{
			FaceTarget();
			if (DistanceToTarget() <= MeleeRange + 0.5f || jumpsDone >= ZigzagJumps)
			{
				ChooseAttack();
				return;
			}
			if (Time.time < jumpEndsAt)
			{
				Move(jumpDirection, ZigzagSpeed / moveSpeed);
				return;
			}
			jumpsDone++;
			jumpSign = -jumpSign;
			jumpDirection = Quaternion.Euler(0f, 45f * jumpSign, 0f) * DirectionToTarget();
			jumpEndsAt = Time.time + ZigzagJump + 0.05f;
		}

		private void ChooseAttack()
		{
			if (attacksCount >= AttacksBeforeSpecial)
			{
				phase = Phase.ChargeWarn;
				phaseEndsAt = Time.time + ChargeWarn;
				chargesDone = 0;
				RaiseAttacked("windup");
				GameEvents.RaiseBossCharging(transform.position);
				return;
			}
			attacksCount++;
			float roll = Random.value;
			if (roll < MeleeChance || bullets == null)
			{
				phase = Phase.MeleeWind;
				phaseEndsAt = Time.time + MeleeWindup;
				RaiseAttacked("windup");
			}
			else if (roll < MeleeChance + ShootChance)
			{
				phase = Phase.ShotWarn;
				phaseEndsAt = Time.time + ShotWarn;
				aimDirection = DirectionToTarget();
				RaiseAttacked("windup");
			}
			else
			{
				attacksCount--; // the remaining quarter is the special attack: it does not count as a normal attack
				phase = Phase.ChargeWarn;
				phaseEndsAt = Time.time + ChargeWarn;
				chargesDone = 0;
				RaiseAttacked("windup");
				GameEvents.RaiseBossCharging(transform.position);
			}
		}

		// ---- melee ------------------------------------------------------------------------------------

		private void UpdateMeleeWind()
		{
			FaceTarget();
			if (DistanceToTarget() > MeleeRange)
			{
				Move(DirectionToTarget());
			}
			if (Time.time < phaseEndsAt)
			{
				return;
			}
			RaiseAttacked("attack-melee-right");
			if (DistanceToTarget() <= MeleeRange + 0.6f)
			{
				target.TakeDamage(contactDamage, gameObject);
				GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, contactDamage);
			}
			phase = Phase.MeleeRest;
			phaseEndsAt = Time.time + MeleeDelay;
		}

		// ---- three-shot burst (same rules as the shooter) ---------------------------------------------

		private void UpdateShotWarn()
		{
			aimDirection = DirectionToTarget();
			FaceTarget();
			if (Time.time >= phaseEndsAt)
			{
				phase = Phase.Shots;
				shotsLeft = ShotsPerAttack;
				nextShotAt = Time.time + ShotLock;
				RaiseAttacked("attack-melee-right");
			}
		}

		private void UpdateShots()
		{
			FaceTarget();
			if (Time.time < nextShotAt - ShotLock)
			{
				aimDirection = DirectionToTarget();
			}
			if (Time.time < nextShotAt)
			{
				return;
			}
			bullets?.Spawn(transform.position + Vector3.up * 0.9f + aimDirection * 0.8f, aimDirection, BulletSpeed, 1, Team, gameObject);
			GameEvents.RaiseSound("shot");
			shotsLeft--;
			nextShotAt = Time.time + ShotGap;
			if (shotsLeft <= 0)
			{
				phase = Phase.MeleeRest;
				phaseEndsAt = Time.time + ShotGap;
			}
		}

		// ---- the special: three dashes, then the guard breaks -----------------------------------------

		private void BeginSubCharge()
		{
			phase = Phase.Charge;
			phaseEndsAt = Time.time + SubChargeTime;
			chargeDirection = DirectionToTarget();
			chargeHit = false;
			RaiseAttacked("attack-kick-right");
		}

		private void UpdateCharge()
		{
			if (Time.time < phaseEndsAt)
			{
				Move(chargeDirection, ChargeSpeed / moveSpeed);
				if (!chargeHit && DistanceToTarget() < 2.0f)
				{
					chargeHit = true;
					target.TakeDamage(contactDamage, gameObject);
					GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, contactDamage * 2);
				}
				return;
			}
			chargesDone++;
			if (chargesDone < SubCharges)
			{
				if (Time.time >= phaseEndsAt + SubChargeGap)
				{
					BeginSubCharge();
				}
				return;
			}
			// the special is over: the guard breaks and he is thrown back
			Vector3 away = transform.position - target.transform.position;
			away.y = 0f;
			pushDirection = Quaternion.Euler(0f, Random.Range(-45f, 45f), 0f) * (away.sqrMagnitude > 0.01f ? away.normalized : -transform.forward);
			phase = Phase.Pushed;
			phaseEndsAt = Time.time + PushTime;
			RaiseAttacked("windup");
			GameEvents.RaiseHitLanded(transform.position + Vector3.up, 4);
			CameraRig.Shake(3f, 0.15f, 0.02f); // Akane Boss.StartVulnerableMode
			SlowMotion.Pulse(0.3f, 0.12f);
		}

		// ---- the guard --------------------------------------------------------------------------------

		public override void TakeDamage(int amount, GameObject source)
		{
			if (IsDead)
			{
				return;
			}
			if (!IsOpen)
			{
				GameEvents.RaiseDeflected(transform.position + Vector3.up);
				GameEvents.RaiseSound("deflect");
				return;
			}
			base.TakeDamage(amount, source);
		}

		/// <summary>Akane Boss.TakeSlayerHit: the ultimate goes through his guard.</summary>
		public void TakeSlayerHit()
		{
			if (!IsDead)
			{
				base.TakeDamage(6, gameObject);
			}
		}

		protected override void Die()
		{
			GameEvents.RaiseBossChanged(null);
			base.Die();
		}
	}
}
