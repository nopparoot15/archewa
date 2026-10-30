using UnityEngine;

namespace MyArcade
{
	/// <summary>A bright curved swish drawn in front of the attacker for a split second (the "slash" of a swing).</summary>
	public class SlashArc : MonoBehaviour
	{
		private const float Duration = 0.14f;
		private const int Segments = 16;

		private Mesh mesh;
		private Material material;
		private float age = Duration;

		public void Build(Material baseMaterial)
		{
			mesh = new Mesh { name = "SlashArc" };
			var filter = gameObject.AddComponent<MeshFilter>();
			filter.sharedMesh = mesh;
			var renderer = gameObject.AddComponent<MeshRenderer>();
			material = new Material(baseMaterial);
			renderer.sharedMaterial = material;
			renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			gameObject.SetActive(false);
		}

		public void Show(Vector3 origin, Vector3 direction, float reach, float arcDegrees)
		{
			direction.y = 0f;
			if (direction.sqrMagnitude < 0.01f)
			{
				return;
			}
			direction.Normalize();
			float start = -arcDegrees * 0.5f;
			float inner = reach * 0.55f;
			var vertices = new Vector3[(Segments + 1) * 2];
			var uvs = new Vector2[vertices.Length];
			var triangles = new int[Segments * 6];
			for (int i = 0; i <= Segments; i++)
			{
				float t = i / (float)Segments;
				Vector3 dir = Quaternion.Euler(0f, start + t * arcDegrees, 0f) * direction;
				float taper = Mathf.Sin(t * Mathf.PI); // thin at both ends
				vertices[i * 2] = origin + dir * inner;
				vertices[i * 2 + 1] = origin + dir * Mathf.Lerp(inner, reach, 0.35f + 0.65f * taper);
				uvs[i * 2] = new Vector2(t, 0f);
				uvs[i * 2 + 1] = new Vector2(t, 1f);
			}
			for (int i = 0; i < Segments; i++)
			{
				int a = i * 2;
				int k = i * 6;
				triangles[k] = a; triangles[k + 1] = a + 1; triangles[k + 2] = a + 2;
				triangles[k + 3] = a + 2; triangles[k + 4] = a + 1; triangles[k + 5] = a + 3;
			}
			mesh.Clear();
			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.triangles = triangles;
			mesh.RecalculateBounds();
			transform.position = new Vector3(0f, 0.9f, 0f);
			age = 0f;
			gameObject.SetActive(true);
		}

		private void Update()
		{
			age += Time.unscaledDeltaTime;
			float t = age / Duration;
			if (t >= 1f)
			{
				gameObject.SetActive(false);
				return;
			}
			float alpha = 1f - t;
			Color c = new Color(0.85f, 0.95f, 1f, alpha);
			if (material.HasProperty("_Color")) material.color = c;
			if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", new Color(c.r, c.g, c.b, alpha * 0.9f));
		}
	}
}
