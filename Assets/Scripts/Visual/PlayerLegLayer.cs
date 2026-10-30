using UnityEngine;

namespace MyArcade
{
    /// <summary>
    /// Distance-driven leg cycles with matched layers cut from complete body frames.
    /// Both layers use the same 200x128 canvas, bottom-centre pivot and a shared waist height.
    /// </summary>
    public sealed class PlayerLegLayer : MonoBehaviour
    {
        private const string Folder = "Sprites/player_legs_v1/";
        private const int FrameCount = 8;
        private readonly Sprite[][] frames = { new Sprite[FrameCount], new Sprite[FrameCount], new Sprite[FrameCount] };
        private readonly Sprite[][] upper = { new Sprite[4], new Sprite[4], new Sprite[4] };
        private Player player;
        private Vector3 lastPosition;
        private float phase;
        private bool ready;
        private bool initialized;

        public bool Ready => ready;

        public void Initialize(Player owner)
        {
            player = owner;
            lastPosition = owner.transform.position;
            ready = true;
            bool fallbackReady = Resources.Load<TextAsset>(Folder + "ready") != null;
            string[] directions = { "front", "back", "right" };
            for (int d = 0; d < directions.Length; d++)
            {
                if (TryLoadPaired(directions[d], d)) continue;
                ready &= fallbackReady;
                frames[d] = new Sprite[FrameCount];
                upper[d] = new Sprite[4];
                for (int i = 0; i < FrameCount; i++)
                {
                    frames[d][i] = Resources.Load<Sprite>(Folder + directions[d] + "_" + i.ToString("00"));
                    ready &= frames[d][i] != null;
                }
                for (int i = 0; i < 4; i++)
                    upper[d][i] = Resources.Load<Sprite>(Folder + "upper_" + directions[d] + "_" + i.ToString("00"));
            }
            initialized = true;
        }

        private bool TryLoadPaired(string direction, int index)
        {
            string pairedFolder = "Sprites/player_run_" + direction + "_v2/";
            if (Resources.Load<TextAsset>(pairedFolder + "ready") == null) return false;
            {
                var pairedLegs = new Sprite[FrameCount];
                var pairedUpper = new Sprite[FrameCount];
                bool complete = true;
                for (int i = 0; i < FrameCount; i++)
                {
                    pairedLegs[i] = Resources.Load<Sprite>(pairedFolder + "lower_" + i.ToString("00"));
                    pairedUpper[i] = Resources.Load<Sprite>(pairedFolder + "upper_" + i.ToString("00"));
                    complete &= pairedLegs[i] != null && pairedUpper[i] != null;
                }
                if (complete)
                {
                    frames[index] = pairedLegs;
                    upper[index] = pairedUpper;
                }
                return complete;
            }
        }

        /// <summary>Call once per rendered update, including during full-body moves, so a teleport never advances the run phase.</summary>
        public void Tick()
        {
            if (!initialized || player == null) return;
            Vector3 delta = player.transform.position - lastPosition;
            delta.y = 0f;
            lastPosition = player.transform.position;
            bool fullBody = player.IsDead || player.IsDashing || (player.Slash != null && (player.Slash.IsAiming || player.Slash.IsSlashing));
            if (!fullBody && delta.sqrMagnitude < 4f && delta.sqrMagnitude > .000001f)
            {
                // Advance by distance travelled rather than wall time: stopping and slow motion cannot make the feet keep running.
                phase = Mathf.Repeat(phase + delta.magnitude * FrameCount / 3.6f, FrameCount);
            }
        }

        public bool TryGet(SpriteLibrary.View direction, out Sprite sprite)
        {
            sprite = null;
            if (!ready || player == null || player.IsDead || player.IsDashing || player.Speed01 <= .12f) return false;
            if (player.Slash != null && (player.Slash.IsAiming || player.Slash.IsSlashing)) return false;
            sprite = frames[(int)direction][Mathf.FloorToInt(phase) % FrameCount];
            return sprite != null;
        }

        public Sprite Upper(SpriteLibrary.View direction)
        {
            var sequence = upper[(int)direction];
            return sequence[Mathf.FloorToInt(phase * sequence.Length / FrameCount) % sequence.Length];
        }
    }
}
