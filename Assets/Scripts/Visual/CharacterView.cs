using UnityEngine;

namespace MyArcade
{
	public class CharacterView : MonoBehaviour
	{
		private static readonly Color NightTint = new Color(0.92f, 0.94f, 1f);

		private string artSet = "player"; // which drawings this character uses (see SpriteLibrary)
		private Character owner;
		private SpriteRenderer sprite;
		private Transform model;
		private Color baseColor = Color.white;
		private float baseScale = 1f;
		private float phase;

		// animation state
		private string loopClip = "idle";
		private string oneShot;
		private float clipTime;
		private SpriteLibrary.View view = SpriteLibrary.View.Front;
		private bool mirror;
		private float flashUntil;
		private bool wasDead;
		private bool bounce; // extra step bounce, only for views whose drawn legs barely move
		private bool wasParrying;
		private bool wasSlashAiming;
		private float windupStart;
		private LineRenderer laser; // red laser sight while the hero aims the pen gun
		private SpriteRenderer legs;
		private float legsTime;
		private const float HipHeight = 36f; // pixels above the feet where the sprite is cut into legs and upper body
		private static readonly System.Collections.Generic.Dictionary<int, Sprite> SplitCache = new System.Collections.Generic.Dictionary<int, Sprite>();
		private float recoil; // 1 right after a shot, fades out: the sprite kicks back a little
		private float roll; // little sway while walking
		private float tip; // how far a fallen fighter has tipped over (0..90 degrees)
		private bool attached;
		private GeneratedSpriteAnimator generated;
		private PlayerLegLayer independentLegs;
		private PixelCastAnimator pixelCast;
		private GameObject shadow;

		private void Awake()
		{
			if (Application.isPlaying)
			{
				Attach(); // characters placed in the scene by the editor: the sprite already exists
			}
		}

		private void Attach()
		{
			if (attached)
			{
				return;
			}
			owner = GetComponent<Character>();
			model = transform.Find("Model");
			if (owner == null || model == null)
			{
				return;
			}
			sprite = model.GetComponent<SpriteRenderer>();
			Transform shadowChild = model.Find("shadows");
			shadow = shadowChild != null ? shadowChild.gameObject : null;
			if (sprite == null)
			{
				return;
			}
			attached = true;
			if (PixelCastAnimator.Available(owner))
			{
				Transform pixelLaser = transform.Find("Laser");
				laser = pixelLaser != null ? pixelLaser.GetComponent<LineRenderer>() : null;
				pixelCast = GetComponent<PixelCastAnimator>();
				pixelCast.Initialize(owner, sprite, model, model.localScale.x);
				return;
			}
			if (GeneratedSpriteAnimator.Available(owner))
			{
				Transform existingLaser = transform.Find("Laser");
				laser = existingLaser != null ? existingLaser.GetComponent<LineRenderer>() : null;
				generated = GetComponent<GeneratedSpriteAnimator>();
				generated.Initialize(owner, sprite, model, model.localScale.x);
				return;
			}
			artSet = GetComponent<Brute>() != null ? "brute" : "player";
			Transform laserChild = transform.Find("Laser");
			laser = laserChild != null ? laserChild.GetComponent<LineRenderer>() : null;
			legs = model.Find("Legs") != null ? model.Find("Legs").GetComponent<SpriteRenderer>() : MakeLegs(model);
			if (owner is Player layeredPlayer)
			{
				independentLegs = GetComponent<PlayerLegLayer>();
				independentLegs.Initialize(layeredPlayer);
			}
			baseScale = model.localScale.x;
			baseColor = sprite.color;
			phase = Random.value * 6.28f;

			owner.Attacked += OnAttacked;
			if (owner is Player gunner) gunner.Shot += OnShot;
			owner.Damaged += OnDamaged;
			owner.Died += OnDied;
		}

		private void Update()
		{
			if (shadow != null && owner != null)
			{
				shadow.SetActive(!owner.IsDead);
			}
			if (generated != null || pixelCast != null) return;
			if (independentLegs != null) independentLegs.Tick();
			if (owner == null || sprite == null)
			{
				return;
			}

			if (wasDead && !owner.IsDead)
			{
				wasDead = false; // pooled enemy came back to life
				tip = 0f;
				oneShot = null;
			}

			if (owner is Player parrier)
			{
				if (parrier.IsParrying && !wasParrying && !owner.IsDead)
				{
					StartParry();
				}
				wasParrying = parrier.IsParrying;

				// choosing the slash line: crouch into the ready stance and keep it, turned toward the mouse
				bool aimingSlash = parrier.Slash != null && parrier.Slash.IsAiming;
				if (aimingSlash && !wasSlashAiming && SpriteLibrary.Has(artSet, "slash_ready_side"))
				{
					oneShot = "slash_ready_side";
					clipTime = 0f;
				}
				else if (!aimingSlash && wasSlashAiming && oneShot == "slash_ready_side")
				{
					oneShot = null;
				}
				if (aimingSlash && oneShot == "slash_ready_side")
				{
					view = SpriteLibrary.View.Right;
					Camera slashCam = Camera.main;
					Vector3 slashRight = slashCam != null ? slashCam.transform.right : Vector3.right;
					float slashSide = Vector3.Dot(transform.forward, slashRight);
					if (Mathf.Abs(slashSide) > 0.15f) mirror = slashSide < 0f;
				}
				wasSlashAiming = aimingSlash;
			}

			if (oneShot == "hurt_side" && clipTime > 2f && owner.Speed01 > 0.3f)
			{
				oneShot = null; // running again: do not slide around stuck in the flinch
			}

			PickView();
			ChooseClip();
			Advance();
			ApplyLook();
		}

		private void UpdateLaser()
		{
			if (laser == null)
			{
				return;
			}
			bool on = owner is Shooter shooter && shooter.IsAimingShot && !owner.IsDead;
			laser.enabled = on;
			if (!on)
			{
				return;
			}
			Vector3 dir = ((Shooter)owner).AimDirection;
			dir.y = 0f;
			dir.Normalize();
			Vector3 start = transform.position + Vector3.up * 0.9f + dir * 0.8f;
			Vector3 end = start + dir * 22f;
			if (Physics.Raycast(start, dir, out RaycastHit hit, 22f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<Character>() != owner)
			{
				end = hit.point;
			}
			laser.SetPosition(0, start);
			laser.SetPosition(1, end);
		}

		private void LateUpdate()
		{
			UpdateLaser();
			if (generated != null || pixelCast != null) return;
			if (model != null)
			{
				FaceCamera(model);
			}
		}

		private void FaceCamera(Transform t)
		{
			Camera cam = Camera.main;
			Quaternion look = cam != null ? cam.transform.rotation : Quaternion.Euler(34f, 0f, 0f);
			t.rotation = look * Quaternion.Euler(0f, 0f, (mirror ? tip : -tip) + roll);
		}

		private void PickView()
		{
			if (oneShot != null || owner.IsDead)
			{
				return; // keep the view of the swing / the fall
			}
			Vector3 f = transform.forward;
			Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
			Vector3 camUp = Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized : Vector3.forward;
			float side = Vector3.Dot(f, camRight);
			float away = Vector3.Dot(f, camUp);
			// diagonals count as "side" (the best drawn view); a little hysteresis stops flicker at the border
			float sideBias = view == SpriteLibrary.View.Right ? 0.45f : 0.65f;
			if (Mathf.Abs(side) > Mathf.Abs(away) * sideBias)
			{
				view = SpriteLibrary.View.Right;
				mirror = side < 0f;
			}
			else
			{
				view = away > 0f ? SpriteLibrary.View.Back : SpriteLibrary.View.Front;
				mirror = false;
			}
		}

		private void ChooseClip()
		{
			string wanted = "idle";
			float s = owner.Speed01;
			if (s > 0.12f) wanted = "run"; // no walking: any movement is a run
			if (owner is Player player)
			{
				if (player.IsAiming && !player.IsDashing) wanted = "aim";
				else if (player.IsDashing) wanted = "dash";
			}
			if (wanted != loopClip)
			{
				loopClip = wanted;
				if (oneShot == null) clipTime = 0f;
			}
		}

		private void Advance()
		{
			if (oneShot == "windup_hold")
			{
				if (Time.time - windupStart > 1.3f || owner.IsDead)
				{
					oneShot = null; // never stay stuck in the pose
				}
				else
				{
					ShowWindup();
					return;
				}
			}
			if (oneShot == null && loopClip == "aim")
			{
				ShowAim();
				return;
			}
			string clip = oneShot ?? loopClip;
			float speedScale = 1f;
			bounce = false;
			bool useSide = false;
			if (oneShot == null && (clip == "walk" || clip == "idle"))
			{
				if (view == SpriteLibrary.View.Front)
				{
					clip += clip == "walk" ? "_front" : "_front";
				}
				else if (view == SpriteLibrary.View.Back && clip == "walk")
				{
					clip = "walk_back";
				}
				else
				{
					// no drawn back-view idle yet: the side idle stands in
					clip += "_side";
					useSide = true;
				}
			}
			if (oneShot == null && clip == "dash")
			{
				// the dash is drawn from the side only; face the way the fighter is going
				clip = "dash_side";
				useSide = true;
				Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
				float side = Vector3.Dot(transform.forward, camRight);
				if (Mathf.Abs(side) > 0.15f) mirror = side < 0f;
			}
			else if (oneShot == null && clip == "run")
			{
				if (view == SpriteLibrary.View.Front)
				{
					clip = "run_front";
				}
				else if (view == SpriteLibrary.View.Back)
				{
					clip = "run_back";
				}
				else
				{
					clip = "run_side";
					useSide = true;
				}
			}
			SpriteLibrary.ClipInfo info = SpriteLibrary.Info(artSet, clip);
			clipTime += Time.deltaTime * info.Fps * speedScale;
			int frame;
			if (oneShot != null)
			{
				frame = Mathf.Min((int)clipTime, info.Frames - 1);
				bool holdGuard = (oneShot == "parry_side" && owner is Player guard && guard.IsParrying)
					|| (oneShot == "slash_ready_side" && owner is Player aimer && aimer.Slash != null && aimer.Slash.IsAiming);
				if (holdGuard)
				{
					clipTime = info.Frames - 0.01f;
				}
				else if (clipTime >= info.Frames && !owner.IsDead)
				{
					oneShot = null;
					clipTime = 0f;
					clip = loopClip;
					frame = 0;
				}
			}
			else
			{
				frame = (int)clipTime % SpriteLibrary.Info(artSet, clip).Frames;
			}

			SpriteLibrary.View shown = useSide ? SpriteLibrary.View.Right : view;
			bool flip = mirror;
			mirror = flip;
			Sprite next = SpriteLibrary.Get(artSet, clip, shown, frame);
			sprite.flipX = flip;

			// legs are a separate layer, same cycle for running/attacking/guarding
			bool splitAllowed = oneShot == null || oneShot == "pipe_swing" || oneShot == "parry_side" || oneShot == "shoot_side";
			if (splitAllowed && next != null && independentLegs != null && independentLegs.Ready && owner is Player movingPlayer)
			{
				bool legMirror;
				SpriteLibrary.View legDirection = ViewFor(movingPlayer.LastMoveDirection != Vector3.zero ? movingPlayer.LastMoveDirection : transform.forward, out legMirror);
				if (independentLegs.TryGet(legDirection, out Sprite separateLegs))
				{
					sprite.sprite = (oneShot == null ? independentLegs.Upper(legDirection) : null) ?? SplitSprite(next, true);
					if (oneShot == null) sprite.flipX = legMirror;
					legs.sprite = separateLegs;
					legs.flipX = legMirror;
					legs.enabled = true;
					return;
				}
			}

			// swinging while running: the upper body plays the swing, the legs keep running (split at the hips)
			bool layered = oneShot == "pipe_swing" && owner is Player mover && owner.Speed01 > 0.12f && legs != null;
			if (layered)
			{
				Player runner = (Player)owner;
				bool legMirror;
				SpriteLibrary.View legView = ViewFor(runner.LastMoveDirection != Vector3.zero ? runner.LastMoveDirection : transform.forward, out legMirror);
				string legClip = legView == SpriteLibrary.View.Front ? "run_front" : legView == SpriteLibrary.View.Back ? "run_back" : "run_side";
				SpriteLibrary.View legShown = legView == SpriteLibrary.View.Right ? SpriteLibrary.View.Right : legView;
				legsTime += Time.deltaTime * SpriteLibrary.Info(artSet, legClip).Fps;
				Sprite legSprite = SpriteLibrary.Get(artSet, legClip, legShown, (int)legsTime % SpriteLibrary.Info(artSet, legClip).Frames);
				if (next != null && legSprite != null)
				{
					sprite.sprite = SplitSprite(next, true);
					legs.sprite = SplitSprite(legSprite, false);
					legs.flipX = legMirror;
					legs.enabled = true;
					return;
				}
			}
			if (legs != null) legs.enabled = false;
			if (next != null)
			{
				sprite.sprite = next;
			}
		}

		private static SpriteLibrary.View ViewFor(Vector3 dir, out bool mirrored)
		{
			Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
			Vector3 camUp = Camera.main != null ? Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized : Vector3.forward;
			float side = Vector3.Dot(dir, camRight);
			float away = Vector3.Dot(dir, camUp);
			mirrored = false;
			if (Mathf.Abs(side) > Mathf.Abs(away) * 0.65f)
			{
				mirrored = side < 0f;
				return SpriteLibrary.View.Right;
			}
			return away > 0f ? SpriteLibrary.View.Back : SpriteLibrary.View.Front;
		}

		private SpriteRenderer MakeLegs(Transform parent)
		{
			var go = new GameObject("Legs");
			go.transform.SetParent(parent, false);
			SpriteRenderer r = go.AddComponent<SpriteRenderer>();
			r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			r.receiveShadows = false;
			r.sortingOrder = 0;
			r.enabled = false;
			legs = r;
			return r;
		}

		private static Sprite SplitSprite(Sprite whole, bool upper)
		{
			int key = whole.GetInstanceID() * 2 + (upper ? 1 : 0);
			if (SplitCache.TryGetValue(key, out Sprite part) && part != null)
			{
				return part;
			}
			Rect r = whole.rect;
			float cut = HipHeight;
			Rect rect = upper ? new Rect(r.x, r.y + cut, r.width, r.height - cut) : new Rect(r.x, r.y, r.width, cut);
			Vector2 pivot = upper ? new Vector2(whole.pivot.x / rect.width, -cut / rect.height) : new Vector2(whole.pivot.x / rect.width, 0f);
			part = Sprite.Create(whole.texture, rect, pivot, whole.pixelsPerUnit, 0, SpriteMeshType.FullRect);
			SplitCache[key] = part;
			return part;
		}

		private void ApplyLook()
		{
			// breathing: a tiny squash that also hides how few idle frames there are
			float breathe = 1f + 0.018f * Mathf.Sin(Time.time * 2.6f + phase);
			bool windingUp = oneShot == "windup_hold";
			float squash = windingUp ? 0.93f : 1f; // crouches a little while it winds up
			model.localScale = new Vector3(baseScale * (windingUp ? 1.05f : 1f), baseScale * squash * (owner.IsDead ? 1f : breathe), baseScale);

			// a step bounce and sway while moving, so the walk reads even where the drawn legs barely move
			bool moving = bounce && oneShot == null && !owner.IsDead && owner.Speed01 > 0.12f;
			float cycle = Time.time * (owner.Speed01 > 1.15f ? 11f : 8f);
			recoil = Mathf.MoveTowards(recoil, 0f, Time.deltaTime * 9f);
			Vector3 kick = -transform.forward * (0.22f * recoil);
			model.localPosition = kick + new Vector3(0f, moving ? Mathf.Abs(Mathf.Sin(cycle)) * 0.09f * baseScale : 0f, 0f);
			roll = moving ? Mathf.Sin(cycle) * 3.5f : 0f;

			if (owner.IsDead && !SpriteLibrary.Has(artSet, "die_side"))
			{
				tip = Mathf.MoveTowards(tip, 90f, Time.deltaTime * 420f); // no drawn fall: tip over
			}

			bool flash = Time.time < flashUntil;
			// after a hit the hero is briefly invulnerable: a soft see-through pulse, not a hard flash
			bool blink = owner is Player player && player.IsInvulnerable && !owner.IsDead && Mathf.FloorToInt(Time.time * 10f) % 2 == 0;
			Color c = flash ? new Color(1f, 0.3f, 0.25f) : baseColor;
			if (!flash && windingUp)
			{
				c = Color.Lerp(baseColor, new Color(1f, 0.2f, 0.15f), 0.45f + 0.45f * Mathf.Sin(Time.time * 28f)); // attack warning: pulse red
			}
			if (blink && !flash)
			{
				c.a = 0.6f;
			}
			if (owner.IsDead)
			{
				c = Color.Lerp(baseColor, new Color(0.35f, 0.35f, 0.4f), SpriteLibrary.Has(artSet, "die_side") ? 0.3f : Mathf.Clamp01(tip / 90f));
			}
			sprite.color = c;
			if (legs != null) legs.color = c;
			if (independentLegs != null && independentLegs.Ready && legs != null && legs.enabled)
			{
				// ground and waist stay fixed or the drawn layers would drift apart
				model.localScale = Vector3.one * baseScale;
				model.localPosition = Vector3.zero;
				roll = 0f;
			}
		}

		private void OnAttacked(string clip)
		{
			if (clip == "windup")
			{
				// the wind-up: hold the raised-weapon pose so the hero can see the hit coming
				oneShot = "windup_hold";
				windupStart = Time.time;
				view = SpriteLibrary.View.Right;
				Camera windCam = Camera.main;
				Vector3 windRight = windCam != null ? windCam.transform.right : Vector3.right;
				float windSide = Vector3.Dot(transform.forward, windRight);
				if (Mathf.Abs(windSide) > 0.15f) mirror = windSide < 0f;
				return;
			}
			// every swing uses the pipe animation (there are no punches or kicks any more)
			oneShot = "pipe_swing";
			clipTime = artSet == "player" ? 2f : 0f; // the hero skips his first wind-up frame: damage is dealt right away, the strike (frame 4) must follow immediately
		}

		private void StartParry()
		{
			// drawn from the side only: face the way the fighter looks (left or right)
			oneShot = "parry_side";
			clipTime = 0f;
			view = SpriteLibrary.View.Right;
			Camera cam = Camera.main;
			Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
			float side = Vector3.Dot(transform.forward, camRight);
			if (Mathf.Abs(side) > 0.15f) mirror = side < 0f;
		}

		private void ShowWindup()
		{
			bool ownArt = artSet != "player";
			Sprite pose = ownArt
				? SpriteLibrary.Get(artSet, "idle_side", SpriteLibrary.View.Right, 0)
				: SpriteLibrary.Get(artSet, "pipe_swing", SpriteLibrary.View.Right, 0);
			if (pose != null) sprite.sprite = pose;
			sprite.flipX = mirror;
			if (legs != null) legs.enabled = false;
		}

		private void ShowAim()
		{
			// holding the gun out (frame 2 of the shooting clip), facing where the fighter aims
			Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
			float side = Vector3.Dot(transform.forward, camRight);
			if (Mathf.Abs(side) > 0.15f) mirror = side < 0f;
			view = SpriteLibrary.View.Right;
			Sprite aim = SpriteLibrary.Get(artSet, "shoot_side", SpriteLibrary.View.Right, 2);
			if (aim != null) sprite.sprite = aim;
			sprite.flipX = mirror;
			if (legs != null) legs.enabled = false;
		}

		private void OnShot()
		{
			recoil = 1f;
			oneShot = "shoot_side";
			clipTime = 3f; // start at the muzzle flash
			view = SpriteLibrary.View.Right;
			Camera cam = Camera.main;
			Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
			float side = Vector3.Dot(transform.forward, camRight);
			if (Mathf.Abs(side) > 0.15f) mirror = side < 0f;
		}

		private void OnDamaged()
		{
			flashUntil = Time.time + 0.15f;
			bool enemyFlinch = !(owner is Player) && !owner.IsDead && oneShot == null && SpriteLibrary.Has(artSet, "hurt_side");
			if ((owner is Player hurtPlayer && !hurtPlayer.IsDead) || enemyFlinch)
			{
				// the hero flinches, falls to a knee and gets back up (enemies only flash red)
				oneShot = "hurt_side";
				clipTime = 0f;
				view = SpriteLibrary.View.Right;
				Camera cam = Camera.main;
				Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
				float side = Vector3.Dot(transform.forward, camRight);
				if (Mathf.Abs(side) > 0.15f) mirror = side < 0f;
			}
		}

		private void OnDied()
		{
			wasDead = true;
			// the drawn fall (side view); it stops on the last frame, lying down
			oneShot = "die_side";
			clipTime = 0f;
			view = SpriteLibrary.View.Right;
			Camera cam = Camera.main;
			Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
			float side = Vector3.Dot(transform.forward, camRight);
			if (Mathf.Abs(side) > 0.15f) mirror = side < 0f;
			tip = 0f;
		}

		private void OnDestroy()
		{
			if (owner != null)
			{
				owner.Attacked -= OnAttacked;
				owner.Damaged -= OnDamaged;
				owner.Died -= OnDied;
			}
		}
	}
}
