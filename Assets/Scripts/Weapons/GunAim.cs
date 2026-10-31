using System;
using UnityEngine;

namespace MyArcade
{
    public static class GunAim
    {
        public const float Height = 0.9f;

        public static Vector3 Point(Camera camera, Vector2 pointer, Vector3 fallback)
        {
            if (camera == null) return fallback;
            Physics.SyncTransforms();
            Ray ray = camera.ScreenPointToRay(pointer);
            var hits = Physics.SphereCastAll(ray, 0.12f, camera.farClipPlane, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Collider selected = null;
            float nearest = float.MaxValue;
            foreach (var hit in hits)
            {
                var enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead) continue;
                float depth = Vector3.Dot(enemy.transform.position - camera.transform.position, camera.transform.forward);
                if (depth <= 0f || depth >= nearest) continue;
                selected = hit.collider;
                nearest = depth;
            }

            // A billboard's visible head can extend past its upright physics capsule.
            // Pick the visible body without enlarging the damage collider.
            foreach (var enemy in Enemy.AllActive)
            {
                if (enemy == null || enemy.IsDead) continue;
                var body = enemy.GetComponent<Collider>();
                if (body == null || !body.enabled) continue;
                Vector3 origin = enemy.transform.position;
                float depth = Vector3.Dot(origin - camera.transform.position, camera.transform.forward);
                if (depth <= 0f || depth >= nearest) continue;
                float height = EnemyCatalog.Get(enemy.Kind).Height * enemy.transform.lossyScale.y;
                float halfWidth = Mathf.Max(body.bounds.extents.x, body.bounds.extents.z) * 1.1f;
                Vector3 lower = camera.WorldToScreenPoint(origin - camera.transform.right * halfWidth);
                Vector3 upper = camera.WorldToScreenPoint(origin + camera.transform.right * halfWidth +
                    camera.transform.up * (height + 0.08f));
                var bounds = Rect.MinMaxRect(Mathf.Min(lower.x, upper.x), Mathf.Min(lower.y, upper.y),
                    Mathf.Max(lower.x, upper.x), Mathf.Max(lower.y, upper.y));
                if (!bounds.Contains(pointer)) continue;
                selected = body;
                nearest = depth;
            }
            if (selected != null)
            {
                Vector3 point = selected.bounds.center;
                point.y = Height;
                return point;
            }
            var plane = new Plane(Vector3.up, Vector3.up * Height);
            return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : fallback;
        }
    }
}
