using UnityEngine;

namespace MyArcade
{
	public class Boss : Brute
	{
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

		private enum Phase { Zigzag, MeleeWind, MeleeSwing, MeleeRest, ShotWarn, Shots, ChargeWarn, Charge, Pushed, Open }

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

		public bool IsOpen => phase == Phase.Open;
		public override string VisualPose => phase == Phase.Open ? "open" : phase == Phase.Charge ? "charge" : phase == Phase.Shots ? "shoot" : phase == Phase.ShotWarn || phase == Phase.ChargeWarn || phase == Phase.MeleeWind ? "warn" : null;

		public void SetBullets(IBulletSpawner spawner)
		{
			bullets = spawner;
		}

		protected override void Awake()
		{
			base.Awake();
			moveSpeed = 9.2f;
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
				case Phase.MeleeSwing: UpdateMeleeSwing(); break;
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
				LockStrike();
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

		private void UpdateMeleeWind()
		{
			if (DistanceToTarget() > MeleeRange)
			{
				Move(DirectionToTarget());
			}
			if (Time.time < phaseEndsAt)
			{
				return;
			}
			RaiseAttacked("attack-melee-right");
			phase = Phase.MeleeSwing;
			phaseEndsAt = Time.time + 0.2f;
		}

		private void UpdateMeleeSwing()
		{
			if (Time.time < phaseEndsAt)
			{
				return;
			}
			if (StrikeReaches(MeleeRange + 0.6f))
			{
				target.TakeDamage(contactDamage, gameObject);
				GameEvents.RaiseHitLanded(target.transform.position + Vector3.up, contactDamage);
			}
			phase = Phase.MeleeRest;
			phaseEndsAt = Time.time + MeleeDelay;
		}

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
				phase = Phase.ChargeWarn;
				phaseEndsAt = Time.time + SubChargeGap;
				RaiseAttacked("windup");
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
			CameraRig.Shake(3f, 0.15f, 0.02f);
			SlowMotion.Pulse(0.3f, 0.12f);
		}

		public override void TakeDamage(int amount, GameObject source)
		{
			if (IsDead)
			{
				return;
			}
			if (!IsOpen)
			{
				GameEvents.RaiseEnemyGuarded(transform.position + Vector3.up);
				GameEvents.RaiseSound("deflect");
				return;
			}
			base.TakeDamage(amount, source);
		}

		public override void TakeSlayerHit()
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
