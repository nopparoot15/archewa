using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
    /// <summary>Animation-only component for the reviewed V2 cast. Gameplay emits poses; this component never deals damage.</summary>
    public sealed class GeneratedSpriteAnimator : MonoBehaviour
    {
        private Character owner;
        private Player player;
        private SpriteRenderer visual;
        private Transform model;
        private string set;
        private float scale;
        private string action;
        private float actionTime;
        private float clock;
        private float flashUntil;
        private string previousLoop;
        private float loopTime;
        private bool wasAlive;
        private readonly Dictionary<string, Sprite> frames = new Dictionary<string, Sprite>();
        private string direction = "front";
        private bool flip;

        public static bool Available(Character character)
        {
            string id = character is Player ? "player" : ((Enemy)character).Kind.ToString().ToLowerInvariant();
            return Resources.Load<TextAsset>("Sprites/v2_" + id + "/ready") != null;
        }

        public void Initialize(Character character, SpriteRenderer renderer, Transform root, float size)
        {
            owner = character;
            player = owner as Player;
            visual = renderer;
            model = root;
            scale = size;
            set = "v2_" + (player != null ? "player" : ((Enemy)owner).Kind.ToString().ToLowerInvariant());
            foreach (SpriteRenderer child in root.GetComponentsInChildren<SpriteRenderer>())
                if (child != renderer) child.enabled = false;
            owner.Attacked += OnAttack;
            owner.Damaged += OnDamage;
            owner.Died += OnDeath;
            if (player != null) player.Shot += OnShot;
            visual.color = Color.white;
            wasAlive = !owner.IsDead;
            Draw("idle", "front", 0);
        }

        private Sprite Frame(string clip, string facing, int index)
        {
            string key = clip + "/" + facing + "_" + Mathf.Clamp(index, 0, 3).ToString("00");
            if (!frames.TryGetValue(key, out Sprite result))
            {
                result = Resources.Load<Sprite>("Sprites/" + set + "/" + key);
                frames[key] = result;
            }
            return result;
        }

        private void Draw(string clip, string facing, int index)
        {
            Sprite next = Frame(clip, facing, index);
            bool sideOnly = next == null;
            if (sideOnly) next = Frame(clip, "right", index);
            if (next == null && clip == "special") next = Frame("attack", "right", index) ?? Frame("guard", "right", 0);
            if (next == null && clip == "attack") next = Frame("attack", "front", index);
            if (next == null && clip == "run") next = Frame("idle", facing, index);
            if (next == null) next = Frame("idle", facing, 0) ?? Frame("idle", "front", 0);
            if (next != null) visual.sprite = next;
            visual.flipX = flip && (facing == "right" || sideOnly);
        }

        private void Facing()
        {
            Vector3 forward = owner.transform.forward;
            Camera cam = Camera.main;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right;
            Vector3 ahead = cam != null ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized : Vector3.forward;
            float side = Vector3.Dot(forward, right);
            float away = Vector3.Dot(forward, ahead);
            direction = Mathf.Abs(side) > Mathf.Abs(away) * 0.7f ? "right" : away > 0f ? "back" : "front";
            flip = side < 0f;
        }

        private void Update()
        {
            if (owner == null || visual == null) return;
            if (Enemy.Frozen && player == null) return;
            if (!owner.IsDead && !wasAlive)
            {
                action = null;
                actionTime = 0f;
                flashUntil = 0f;
            }
            wasAlive = !owner.IsDead;
            Facing();
            float dt = player != null && player.Slash != null && player.Slash.IsSlashing ? Time.unscaledDeltaTime : Time.deltaTime;
            clock += dt;
            string loop = owner.Speed01 > 0.12f ? "run" : "idle";
            int? fixedFrame = null;

            if (player != null && player.Slash != null)
            {
                if (player.Slash.IsUlting)
                {
                    loop = "ult_enter";
                    if (action != "ult_strike") fixedFrame = 3;
                }
                else if (player.Slash.IsSlashing) loop = "slash_cut";
                else if (player.Slash.IsAiming) { loop = "slash_ready"; fixedFrame = 3; }
                else if (player.IsDashing) loop = "dash";
                else if (player.IsParrying) { loop = "guard"; fixedFrame = 2; }
                else if (player.IsAiming) { loop = "shoot"; fixedFrame = 0; }
            }
            if (owner is Enemy enemy && enemy.VisualPose != null)
            {
                loop = "special";
                switch (enemy.VisualPose)
                {
                    case "warn": fixedFrame = 0; break;
                    case "guard": fixedFrame = 0; break;
                    case "charge": fixedFrame = 1; break;
                    case "open": fixedFrame = 3; break;
                    case "shoot": fixedFrame = 2; break;
                }
            }
            if (owner.IsDead) { loop = "die"; action = "die"; }
            if (previousLoop != loop) { loopTime = 0f; previousLoop = loop; }
            loopTime += dt;
            int frame = (int)(loopTime * (loop == "run" ? 12f : 6f)) % 4;
            string clip = loop;
            if (fixedFrame.HasValue) frame = Mathf.Min(fixedFrame.Value, (int)(loopTime * 14f));
            if (action != null)
            {
                actionTime += dt;
                float fps = action == "die" ? 5f : action == "hurt" ? 15f : 20f;
                int actionFrame = (int)(actionTime * fps);
                if (actionFrame >= 4 && action != "die") action = null;
                else { clip = action; frame = Mathf.Min(actionFrame, 3); }
            }
            Draw(clip, direction, frame);
            visual.color = Time.time < flashUntil ? new Color(1f, 0.4f, 0.4f) : owner.IsDead ? new Color(0.65f, 0.62f, 0.66f) : Color.white;
            model.localPosition = Vector3.zero;
            model.localScale = Vector3.one * scale;
        }

        private void LateUpdate()
        {
            if (model != null && Camera.main != null) model.rotation = Camera.main.transform.rotation;
        }

        private void Play(string clip) { action = clip; actionTime = 0f; }
        private void OnAttack(string clip)
        {
            if (clip == "windup") { Play("special"); return; }
            if (clip.StartsWith("v2:")) { Play(clip.Substring(3)); return; }
            if (player != null && player.Slash != null && player.Slash.IsSlashing)
                Play(player.Slash.IsUlting ? "ult_strike" : "slash_cut");
            else Play(owner is Shooter ? "special" : "attack");
        }
        private void OnShot() { Play("shoot"); }
        private void OnDamage() { flashUntil = Time.time + .12f; if (!owner.IsDead) Play("hurt"); }
        private void OnDeath() { Play("die"); }
        private void OnDestroy()
        {
            if (owner != null) { owner.Attacked -= OnAttack; owner.Damaged -= OnDamage; owner.Died -= OnDeath; }
            if (player != null) player.Shot -= OnShot;
        }
    }
}
