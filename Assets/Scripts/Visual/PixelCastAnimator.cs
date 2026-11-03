using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public sealed class PixelCastAnimator : MonoBehaviour
	{
		private const string ArtFolder = "Environment/ThaiAlleyV4/";
		private Character owner;
		private Player player;
		private PixelAnimationSet set;
		private PixelAnimationSet gunSet;
		private SpriteRenderer visual;
		private SpriteRenderer legs;
		private Transform model;
		private PlayerLegLayer playerRun;
		private float scale;
		private string action;
		private string actionDirection;
		private bool actionFlip;
		private float actionTime;
		private string previousLoop;
		private float loopTime;
		private float runPhase;
		private Vector3 previousPosition;
		private bool wasAlive;
		private float flashUntil;
		private static readonly Color Night = new Color(.92f, .94f, 1f);
		private readonly Dictionary<int, Sprite> upperParts = new Dictionary<int, Sprite>();

		public static bool Available(Character character) => PixelAnimationSet.Load(PixelAnimationSet.Id(character)) != null;

		public void Initialize(Character character, SpriteRenderer renderer, Transform root, float size)
		{
			owner = character; player = character as Player;
			set = PixelAnimationSet.Load(PixelAnimationSet.Id(owner));
			visual = renderer; model = root; scale = size;
			var child = root.Find("Legs");
			legs = child != null ? child.GetComponent<SpriteRenderer>() : null;
			foreach (var part in root.GetComponentsInChildren<SpriteRenderer>()) if (part != renderer) part.enabled = false;
			if (player != null)
			{
				gunSet = PixelAnimationSet.LoadGun();
				playerRun = GetComponent<PlayerLegLayer>();
				playerRun.Initialize(player);
				player.Shot += OnShot;
			}
			owner.Attacked += OnAttack; owner.Damaged += OnDamage; owner.Died += OnDeath;
			ResetVisual();
			visual.sprite = set.Frame(set.Resolve("idle", "front"), 0);
			visual.color = Night;
			var lighting = Resources.Load<Material>(ArtFolder + "ActorLighting");
			if (lighting != null)
				foreach (var part in root.GetComponentsInChildren<SpriteRenderer>()) part.sharedMaterial = lighting;
		}

		private void OnEnable() { if (owner != null) ResetVisual(); }
		private void ResetVisual()
		{
			action = null; actionTime = loopTime = runPhase = flashUntil = 0f;
			previousLoop = null; previousPosition = transform.position; wasAlive = !owner.IsDead;
			if (legs != null) legs.enabled = false;
		}

		private static void Facing(Vector3 forward, out string direction, out bool flip)
		{
			Camera cam = Camera.main;
			Vector3 right = cam != null ? cam.transform.right : Vector3.right;
			Vector3 ahead = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
			float side = Vector3.Dot(forward, right), away = Vector3.Dot(forward, ahead);
			direction = Mathf.Abs(side) > Mathf.Abs(away) * .65f ? "right" : away > 0 ? "back" : "front";
			flip = side < 0f;
		}

		private static string GunFacing(Vector3 forward)
		{
			var cam = Camera.main;
			Vector3 right = cam != null ? cam.transform.right : Vector3.right;
			Vector3 ahead = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
			float angle = Mathf.Atan2(Vector3.Dot(forward, right), -Vector3.Dot(forward, ahead)) * Mathf.Rad2Deg;
			int sector = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;
			switch (sector)
			{
				case 1: return "front_right";
				case 2: return "right";
				case 3: return "back_right";
				case 4: return "back";
				case 5: return "back_left";
				case 6: return "left";
				case 7: return "front_left";
				default: return "front";
			}
		}

		private void Update()
		{
			if (owner == null || set == null || visual == null) return;
			if (owner is Enemy && Enemy.Frozen) return;
			if (!owner.IsDead && !wasAlive) ResetVisual();
			wasAlive = !owner.IsDead;
			if (playerRun != null) playerRun.Tick();
			Vector3 travel = transform.position - previousPosition; travel.y = 0;
			previousPosition = transform.position;
			// fast charges: don't cycle a whole stride between frames
			float stride = Mathf.Max(1f, scale * SpriteLibrary.StandingHeight * 1.1f);
			float advance = Mathf.Min(1f, Mathf.Min(travel.magnitude * 8f / stride, Mathf.Max(0f, Time.deltaTime) * 20f));
			float sign = Vector3.Dot(travel, transform.forward) < 0f ? -1f : 1f;
			runPhase = Mathf.Repeat(runPhase + advance * sign, 8f);
			float dt = player != null && player.Slash != null && player.Slash.IsSlashing ? Time.unscaledDeltaTime : Time.deltaTime;
			Facing(transform.forward, out string direction, out bool flip);
			string loop = owner.Speed01 > .12f ? "run" : "idle";
			int hold = -1;
			if (player != null)
			{
				var slash = player.Slash;
				if (slash != null && slash.IsUlting) { loop = "ult_enter"; hold = 3; }
				else if (slash != null && slash.IsSlashing) loop = "slash_cut";
				else if (slash != null && slash.IsAiming) { loop = "slash_ready"; hold = 3; }
				else if (player.IsDashing) loop = "dash";
				else if (player.IsParrying) { loop = "guard"; hold = 3; }
				else if (player.IsAiming)
				{
					loop = gunSet != null ? "aim" : "shoot";
					hold = gunSet != null ? -1 : 2;
					if (gunSet != null) { direction = GunFacing(transform.forward); flip = false; }
				}
			}
			string cue = owner is Enemy enemy ? enemy.VisualPose : null;
			if (cue != null)
			{
				loop = "special";
				hold = cue == "charge" ? 1 : cue == "shoot" ? 2 : cue == "open" ? 3 : 0;
			}
			if (owner.IsDead && action != "die") Play("die");
			if (loop != previousLoop) { previousLoop = loop; loopTime = 0; }
			loopTime += dt;
			var clipSet = loop == "aim" && gunSet != null ? gunSet : set;
			var clip = clipSet.Resolve(loop, direction);
			int index = clip != null ? (int)(loopTime * clip.fps) : 0;
			if (clip != null) index = hold >= 0 ? Mathf.Min(index, hold) : clip.loop ? index % clip.count : Mathf.Min(index, clip.count - 1);
			if (loop == "run" && clip != null) index = (int)runPhase % clip.count;
			bool actionActive = false;
			if (action != null)
			{
				var actionSet = action == "shoot" && gunSet != null ? gunSet : set;
				if (action == "shoot" && gunSet != null)
				{
					actionDirection = GunFacing(transform.forward);
					actionFlip = false;
				}
				var chosen = actionSet.Resolve(action, actionDirection);
				actionTime += dt;
				int f = chosen != null ? (int)(actionTime * chosen.fps) : 0;
				if (chosen == null || (f >= chosen.count && action != "die")) action = null;
				else if (cue == null || action == "die" || action == "hurt" || (cue == "shoot" && action == "special"))
				{
					clip = chosen; clipSet = actionSet; index = Mathf.Min(f, chosen.count - 1);
					direction = actionDirection; flip = actionFlip; actionActive = true;
				}
			}
			// moving charge/shout uses the full running drawing
			// a full keypose here would make the enemy slide with frozen feet
			if (owner is Enemy && !owner.IsDead && travel.sqrMagnitude > .000001f && owner.Speed01 > .12f &&
				clip != null && (clip.name == "special" || clip.name == "attack") && (cue == null || cue == "charge"))
			{
				Facing(transform.forward, out direction, out flip);
				var moving = set.Resolve("run", direction);
				if (moving != null) { clip = moving; index = (int)runPhase % moving.count; }
			}
			if (clip == null) return;
			if (legs != null) legs.enabled = false;
			bool canLayer = player != null && !owner.IsDead && !player.IsDashing &&
				(player.Slash == null || (!player.Slash.IsAiming && !player.Slash.IsSlashing)) &&
				(clip.name == "run" || clip.name == "attack" || clip.name == "guard" ||
				 ((clip.name == "aim" || clip.name == "shoot") && owner.Speed01 > .12f));
			if (canLayer && playerRun != null && legs != null)
			{
				Vector3 move = player.LastMoveDirection != Vector3.zero ? player.LastMoveDirection : transform.forward;
				Facing(move, out string legDirection, out bool legFlip);
				var legView = legDirection == "back" ? SpriteLibrary.View.Back : legDirection == "front" ? SpriteLibrary.View.Front : SpriteLibrary.View.Right;
				if (playerRun.TryGet(legView, out Sprite lower))
				{
					Sprite top = clip.name == "run" && !actionActive ? playerRun.Upper(legView) : Upper(clipSet.Frame(clip, index));
					if (top != null)
					{
						visual.sprite = top;
						visual.flipX = clip.name == "run" && !actionActive ? legDirection == "right" && legFlip : clip.direction == "right" && flip;
						legs.sprite = lower; legs.flipX = legDirection == "right" && legFlip; legs.enabled = true;
					}
				}
			}
			if (legs == null || !legs.enabled)
			{
				var sprite = clipSet.Frame(clip, index);
				if (sprite != null) visual.sprite = sprite;
				visual.flipX = clip.direction == "right" && flip;
			}
			bool flash = Time.time < flashUntil;
			Color color = flash ? new Color(1f, .3f, .25f) : owner.IsDead ? Night * new Color(.65f, .65f, .7f, 1f) : Night;
			if (!flash && player != null && player.IsInvulnerable && !owner.IsDead && (int)(Time.time * 10) % 2 == 0) color.a = .6f;
			visual.color = color; if (legs != null) legs.color = color;
			model.localPosition = Vector3.zero; model.localScale = Vector3.one * scale;
		}

		private Sprite Upper(Sprite whole)
		{
			if (whole == null) return null;
			if (upperParts.TryGetValue(whole.GetInstanceID(), out Sprite result)) return result;
			Rect r = whole.rect; const float cut = 36f;
			var rect = new Rect(r.x, r.y + cut, r.width, r.height - cut);
			result = Sprite.Create(whole.texture, rect, new Vector2(whole.pivot.x / rect.width, -cut / rect.height), whole.pixelsPerUnit, 0, SpriteMeshType.FullRect);
			upperParts[whole.GetInstanceID()] = result;
			return result;
		}

		private void LateUpdate() { if (model != null && Camera.main != null) model.rotation = Camera.main.transform.rotation; }
		private void Play(string clip)
		{
			Facing(transform.forward, out actionDirection, out actionFlip);
			action = clip; actionTime = 0f;
			if (player != null && clip == "attack") actionTime = 2f / 40f;
		}
		private void OnAttack(string name)
		{
			if (name.StartsWith("v2:")) { Play(name.Substring(3)); return; }
			if (name == "windup") { Play("special"); return; }
			if (owner is Enemy enemy && enemy.VisualPose == "shoot")
			{
				Play("special");
				actionTime = 2f / set.Resolve("special", actionDirection).fps;
				return;
			}
			if (player != null && player.Slash != null && player.Slash.IsSlashing)
				Play(player.Slash.IsUlting ? "ult_strike" : "slash_cut");
			else Play("attack");
		}
		private void OnShot() { Play("shoot"); actionTime = gunSet != null ? 0f : 3f / 32f; }
		private void OnDamage() { flashUntil = Time.time + .12f; if (!owner.IsDead) Play("hurt"); }
		private void OnDeath() { Play("die"); }
		private void OnDestroy()
		{
			if (owner != null) { owner.Attacked -= OnAttack; owner.Damaged -= OnDamage; owner.Died -= OnDeath; }
			if (player != null) player.Shot -= OnShot;
			foreach (var sprite in upperParts.Values) if (sprite != null) Destroy(sprite);
		}
	}
}
