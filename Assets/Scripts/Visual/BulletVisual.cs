using UnityEngine;

namespace MyArcade
{
    public sealed class BulletVisual : MonoBehaviour
    {
        private MeshRenderer shell;
        private TrailRenderer trail;
        private MaterialPropertyBlock tint;

        private void Awake()
        {
            shell = transform.Find("Shell").GetComponent<MeshRenderer>();
            trail = GetComponentInChildren<TrailRenderer>(true);
            tint = new MaterialPropertyBlock();
        }

        public void SetTeam(Team team)
        {
            Color color = team == Team.Player
                ? new Color(0.22f, 0.88f, 1f)
                : new Color(1f, 0.38f, 0.08f);
            tint.SetColor("_TintColor", color * 0.65f);
            shell.SetPropertyBlock(tint);
            trail.Clear();
            trail.startColor = new Color(color.r, color.g, color.b, 0.75f);
            trail.endColor = new Color(color.r, color.g, color.b, 0f);
            trail.emitting = true;
        }

        public void ClearTrail()
        {
            trail.emitting = false;
            trail.Clear();
        }
    }
}
