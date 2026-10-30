using UnityEngine;
using UnityEngine.Rendering;

namespace MyArcade
{
	/// <summary>
	/// Factory pattern: ONE place that knows how to build each object (which model, which colour, which size).
	/// Game classes (Player, Enemy...) never load a model themselves.
	/// </summary>
	public static class SceneFactory
	{

		public static readonly Color PlayerColor = new Color(0.1f, 0.9f, 1f);

		private static readonly System.Collections.Generic.Dictionary<(Color, float), Material> cache =
			new System.Collections.Generic.Dictionary<(Color, float), Material>();

		public static Material MakeMaterial(Color color)
		{
			return Build(color, 0f);
		}

		/// <summary>Creates a plain or glowing material (cached while playing).</summary>
		public static Material Build(Color color, float emission)
		{
			if (Application.isPlaying && cache.TryGetValue((color, emission), out Material cached) && cached != null)
			{
				return cached;
			}
			Material made = emission > 0f ? BuildGlow(color, emission) : BuildPlain(color);
			if (Application.isPlaying)
			{
				cache[(color, emission)] = made;
			}
			return made;
		}

		private static Material BuildPlain(Color color)
		{
			var material = new Material(FindLitShader());
			material.color = color;
			if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
			if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.15f);
			if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
			return material;
		}

		/// <summary>A material that glows (neon): bright colour plus emission.</summary>
		public static Material MakeGlowMaterial(Color color, float intensity)
		{
			return Build(color, intensity);
		}

		private static Material BuildGlow(Color color, float intensity)
		{
			Material material = BuildPlain(color);
			material.EnableKeyword("_EMISSION");
			material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
			material.SetColor("_EmissionColor", color * intensity);
			return material;
		}

		private static Shader FindLitShader()
		{
			Shader shader = null;
			if (GraphicsSettings.currentRenderPipeline != null)
			{
				shader = Shader.Find("Universal Render Pipeline/Lit");
			}
			if (shader == null) shader = Shader.Find("Standard");
			if (shader == null) shader = Shader.Find("Sprites/Default");
			return shader;
		}

		public static Bullet CreateBullet(Transform parent)
		{
			GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			go.name = "Stone";
			go.transform.SetParent(parent, false);
			go.transform.localScale = Vector3.one * 0.28f;
			go.GetComponent<SphereCollider>().isTrigger = true;
			var body = go.AddComponent<Rigidbody>();
			body.isKinematic = true;
			body.useGravity = false;
			go.GetComponent<Renderer>().sharedMaterial = MakeGlowMaterial(new Color(1f, 0.55f, 0.15f), 1.2f);
			Bullet bullet = go.AddComponent<Bullet>();
			go.SetActive(false);
			return bullet;
		}

		public static Enemy CreateEnemy(EnemyType type, Transform parent)
		{
			EnemySpec spec = EnemyCatalog.Get(type); // the catalog knows which class, colour and size each type has
			var go = new GameObject(type.ToString());
			go.transform.SetParent(parent, false);
			Enemy enemy = spec.Create(go);
			enemy.Kind = type;
			AddBody(go, spec.Height);
			go.AddComponent<CharacterView>().Build(enemy, "", spec.Accent, spec.Height);
			go.SetActive(false); // the pool activates it when needed
			return enemy;
		}

		/// <summary>Invisible capsule used for hit detection.</summary>
		private static void AddBody(GameObject go, float height)
		{
			var capsule = go.AddComponent<CapsuleCollider>();
			capsule.height = height;
			capsule.radius = Mathf.Max(0.4f, height * 0.25f);
			capsule.center = new Vector3(0f, height * 0.5f, 0f);
		}
	}
}
