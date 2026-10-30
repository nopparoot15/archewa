using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	/// <summary>
	/// All the hit effects: sparks, cartoon impact stars, dust puffs, knock-out smoke, deflect flash
	/// and the swing arc of a punch or pipe. Nothing here knows about enemies or weapons: it only listens to
	/// GameEvents (Observer) and draws. Effects are particle systems built in code and reused (pooled).
	/// The look is imitated from Akane's hit sparks / blood-splash / slash style, redrawn in 3D for this street.
	/// </summary>
	public class VfxDirector : MonoBehaviour
	{
		private const int PoolSize = 10;

		private enum Kind { Spark, Star, Dust, Poof, Deflect, Fireball, Ring, Ember, Smoke }

		private readonly Dictionary<Kind, ParticleSystem[]> pools = new Dictionary<Kind, ParticleSystem[]>();
		private readonly Dictionary<Kind, int> next = new Dictionary<Kind, int>();
		private Texture2D softCircle;
		private Texture2D star;
		private Material additive;
		private Material alpha;
		private SlashArc slash;
		private Light blastLight;
		private float blastLightLife;
		private Material sootMaterial;
		private Transform[] fuseRings;
		private float[] fuseRingUntil;
		private Material fuseMaterial;
		private Texture2D[] bloodTextures;
		private Material[] bloodMaterials;
		private Transform[] bloodDecals;
		private int nextBlood;
		private const int BloodLimit = 90;
		private const int CorpseLimit = 40;
		private SpriteRenderer[] corpses;
		private int nextCorpse;

		public void Setup()
		{
			softCircle = MakeCircle(64, 0.15f);
			star = MakeStar(64);
			additive = MakeMaterial(true, softCircle);
			alpha = MakeMaterial(false, softCircle);
			Material starMaterial = MakeMaterial(true, star);

			BuildPool(Kind.Spark, additive, ConfigureSpark);
			BuildPool(Kind.Star, starMaterial, ConfigureStar);
			BuildPool(Kind.Dust, alpha, ConfigureDust);
			BuildPool(Kind.Poof, alpha, ConfigurePoof);
			BuildPool(Kind.Deflect, additive, ConfigureDeflect);
			BuildPool(Kind.Fireball, additive, ConfigureFireball);
			BuildPool(Kind.Ring, additive, ConfigureRing);
			BuildPool(Kind.Ember, additive, ConfigureEmber);
			BuildPool(Kind.Smoke, alpha, ConfigureSmoke);
			BuildBlastExtras();

			BuildBlood();

			slash = new GameObject("SlashArc").AddComponent<SlashArc>();
			slash.transform.SetParent(transform, false);
			slash.Build(alpha);

			GameEvents.HitLanded += OnHit;
			GameEvents.Deflected += OnDeflected;
			GameEvents.EnemyKilled += OnKilled;
			GameEvents.AttackSwung += OnSwing;
			GameEvents.DashStarted += OnDash;
			GameEvents.BombLit += OnBombLit;
			GameEvents.Exploded += OnExploded;
		}

		private void Update()
		{
			UpdateBlastExtras();
		}

		private void OnDestroy()
		{
			GameEvents.HitLanded -= OnHit;
			GameEvents.Deflected -= OnDeflected;
			GameEvents.EnemyKilled -= OnKilled;
			GameEvents.AttackSwung -= OnSwing;
			GameEvents.DashStarted -= OnDash;
			GameEvents.BombLit -= OnBombLit;
			GameEvents.Exploded -= OnExploded;
		}

		// ---- reacting to the game ----------------------------------------------------------

		private void OnHit(Vector3 position, int damage)
		{
			Emit(Kind.Spark, position, 8 + damage * 4);
			Emit(Kind.Star, position, damage >= 3 ? 3 : 1);
		}

		private void OnDeflected(Vector3 position)
		{
			Emit(Kind.Deflect, position, 18);
			Emit(Kind.Star, position, 2);
		}

		private void OnKilled(Enemy enemy)
		{
			Vector3 p = enemy.transform.position + Vector3.up * 0.9f;
			SplatBlood(enemy.transform.position, enemy is Boss);
			StartCoroutine(LeaveCorpse(enemy));
			Emit(Kind.Poof, p, enemy is Boss ? 40 : 16);
			Emit(Kind.Star, p, enemy is Boss ? 10 : 4);
		}

		/// <summary>Once the fall animation is over the body stays lying on the street (Akane never clears them).</summary>
		private System.Collections.IEnumerator LeaveCorpse(Enemy enemy)
		{
			yield return new WaitForSeconds(0.95f);
			if (enemy == null)
			{
				yield break;
			}
			SpriteRenderer source = null;
			foreach (SpriteRenderer candidate in enemy.GetComponentsInChildren<SpriteRenderer>())
			{
				if (candidate.sprite != null && candidate.name != "Legs")
				{
					source = candidate;
					break;
				}
			}
			if (source == null)
			{
				yield break;
			}
			if (corpses == null)
			{
				corpses = new SpriteRenderer[CorpseLimit];
				var group = new GameObject("Corpses").transform;
				group.SetParent(transform, false);
				for (int i = 0; i < CorpseLimit; i++)
				{
					var go = new GameObject("Corpse");
					go.transform.SetParent(group, false);
					corpses[i] = go.AddComponent<SpriteRenderer>();
					corpses[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
					go.SetActive(false);
				}
			}
			SpriteRenderer body = corpses[nextCorpse];
			nextCorpse = (nextCorpse + 1) % corpses.Length;
			// the drawn "fallen" frame, standing in the world exactly like the fighter's own sprite (it is a lying figure drawn upright)
			body.sprite = source.sprite;
			body.flipX = source.flipX;
			body.color = new Color(0.5f, 0.45f, 0.52f);
			body.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
			body.transform.localScale = source.transform.lossyScale;
			body.sortingOrder = -10; // always behind the living fighters
			body.gameObject.SetActive(true);
		}

		// ---- blood on the floor: stays for the whole fight (Akane leaves every splash and body on the street) ----

		private void BuildBlood()
		{
			bloodTextures = new Texture2D[4];
			bloodMaterials = new Material[4];
			for (int i = 0; i < bloodTextures.Length; i++)
			{
				bloodTextures[i] = MakeBloodTexture(64, 100 + i * 37);
				var material = new Material(Shader.Find("Sprites/Default"));
				material.mainTexture = bloodTextures[i];
				bloodMaterials[i] = material;
			}
			bloodDecals = new Transform[BloodLimit];
			var group = new GameObject("BloodDecals").transform;
			group.SetParent(transform, false);
			for (int i = 0; i < BloodLimit; i++)
			{
				var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
				quad.name = "Blood";
				Util.Kill(quad.GetComponent<Collider>());
				quad.transform.SetParent(group, false);
				quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
				quad.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				quad.SetActive(false);
				bloodDecals[i] = quad.transform;
			}
		}

		private void SplatBlood(Vector3 position, bool big)
		{
			int count = big ? 3 : 1;
			for (int i = 0; i < count; i++)
			{
				Transform decal = bloodDecals[nextBlood];
				nextBlood = (nextBlood + 1) % bloodDecals.Length; // the oldest splash makes room for the newest
				Vector2 spread = Random.insideUnitCircle * (big ? 1.4f : 0.5f);
				decal.position = new Vector3(position.x + spread.x, 0.03f + nextBlood * 0.0004f, position.z + spread.y);
				decal.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
				decal.localScale = Vector3.one * (big ? Random.Range(2.4f, 3.6f) : Random.Range(1.2f, 2.2f));
				decal.GetComponent<Renderer>().sharedMaterial = bloodMaterials[Random.Range(0, bloodMaterials.Length)];
				decal.gameObject.SetActive(true);
			}
		}

		private static Texture2D MakeBloodTexture(int size, int seed)
		{
			var random = new System.Random(seed);
			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
			var pixels = new Color32[size * size];
			var blobs = new System.Collections.Generic.List<Vector3>(); // x, y, radius
			blobs.Add(new Vector3(size * 0.5f, size * 0.5f, size * 0.2f));
			for (int i = 0; i < 14; i++)
			{
				double angle = random.NextDouble() * Mathf.PI * 2f;
				float distance = (float)(0.12 + random.NextDouble() * 0.32) * size;
				blobs.Add(new Vector3(size * 0.5f + Mathf.Cos((float)angle) * distance, size * 0.5f + Mathf.Sin((float)angle) * distance, (float)(0.02 + random.NextDouble() * 0.07) * size));
			}
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float alpha = 0f;
					foreach (Vector3 blob in blobs)
					{
						float d = Vector2.Distance(new Vector2(x, y), new Vector2(blob.x, blob.y));
						alpha = Mathf.Max(alpha, Mathf.Clamp01((blob.z - d) / 1.5f));
					}
					pixels[y * size + x] = new Color32(120, 8, 14, (byte)Mathf.RoundToInt(alpha * 210f));
				}
			}
			texture.SetPixels32(pixels);
			texture.Apply();
			return texture;
		}

		private void OnSwing(Vector3 origin, Vector3 direction, float reach, float arc)
		{
			slash.Show(origin, direction, reach, arc);
		}

		// ---- the Bomber: a glowing warning ring while the fuse burns, then a coloured blast with a flash of light ----

		private void BuildBlastExtras()
		{
			var lightObject = new GameObject("BlastLight");
			lightObject.transform.SetParent(transform, false);
			blastLight = lightObject.AddComponent<Light>();
			blastLight.type = LightType.Point;
			blastLight.color = new Color(1f, 0.55f, 0.18f);
			blastLight.shadows = LightShadows.None;
			blastLight.enabled = false;

			var soot = new Material(Shader.Find("Sprites/Default"));
			soot.mainTexture = MakeSootTexture(64);
			sootMaterial = soot;

			fuseMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Additive") ?? Shader.Find("Sprites/Default"));
			fuseMaterial.mainTexture = MakeRingTexture(128);
			fuseRings = new Transform[4];
			fuseRingUntil = new float[4];
			var group = new GameObject("FuseRings").transform;
			group.SetParent(transform, false);
			for (int i = 0; i < fuseRings.Length; i++)
			{
				var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
				quad.name = "FuseRing";
				Util.Kill(quad.GetComponent<Collider>());
				quad.transform.SetParent(group, false);
				quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
				var renderer = quad.GetComponent<Renderer>();
				renderer.sharedMaterial = fuseMaterial;
				renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				quad.SetActive(false);
				fuseRings[i] = quad.transform;
			}
		}

		private void OnBombLit(Vector3 position, float radius)
		{
			for (int i = 0; i < fuseRings.Length; i++)
			{
				if (!fuseRings[i].gameObject.activeSelf)
				{
					fuseRings[i].position = new Vector3(position.x, 0.05f, position.z);
					fuseRings[i].localScale = Vector3.one * radius * 2f;
					fuseRings[i].gameObject.SetActive(true);
					fuseRingUntil[i] = Time.time + 0.9f; // the fuse time
					return;
				}
			}
		}

		private void OnExploded(Vector3 position, float radius)
		{
			Vector3 core = position + Vector3.up * 0.8f;
			Emit(Kind.Fireball, core, 34);
			Emit(Kind.Ring, new Vector3(position.x, 0.12f, position.z), 70);
			Emit(Kind.Ember, core, 46);
			Emit(Kind.Spark, core, 30);
			Emit(Kind.Smoke, core, 9);
			Emit(Kind.Star, core, 3);
			SplatScorch(position, radius);
			blastLight.transform.position = core + Vector3.up * 0.8f;
			blastLight.range = radius * 3.6f;
			blastLightLife = 0.45f;
			blastLight.enabled = true;
		}

		private void UpdateBlastExtras()
		{
			if (blastLightLife > 0f)
			{
				blastLightLife -= Time.unscaledDeltaTime;
				float k = Mathf.Clamp01(blastLightLife / 0.45f);
				blastLight.intensity = 14f * k * k;
				blastLight.color = Color.Lerp(new Color(1f, 0.25f, 0.08f), new Color(1f, 0.8f, 0.4f), k);
				if (blastLightLife <= 0f)
				{
					blastLight.enabled = false;
				}
			}
			for (int i = 0; fuseRings != null && i < fuseRings.Length; i++)
			{
				if (!fuseRings[i].gameObject.activeSelf)
				{
					continue;
				}
				float left = fuseRingUntil[i] - Time.time;
				if (left <= 0f)
				{
					fuseRings[i].gameObject.SetActive(false);
					continue;
				}
				// a faster and faster pulse of orange to red
				float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (14f + (0.9f - left) * 26f));
				Color tint = Color.Lerp(new Color(1f, 0.35f, 0.05f), new Color(1f, 0.9f, 0.3f), pulse);
				if (fuseMaterial.HasProperty("_TintColor"))
				{
					fuseMaterial.SetColor("_TintColor", tint * 0.6f);
				}
				else
				{
					fuseMaterial.color = tint;
				}
			}
		}

		private void SplatScorch(Vector3 position, float radius)
		{
			Transform decal = bloodDecals[nextBlood];
			nextBlood = (nextBlood + 1) % bloodDecals.Length;
			decal.position = new Vector3(position.x, 0.025f + nextBlood * 0.0004f, position.z);
			decal.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
			decal.localScale = Vector3.one * radius * 1.7f;
			decal.GetComponent<Renderer>().sharedMaterial = sootMaterial;
			decal.gameObject.SetActive(true);
		}

		private static Texture2D MakeSootTexture(int size)
		{
			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
			var pixels = new Color32[size * size];
			var random = new System.Random(7);
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = (x - size * 0.5f) / (size * 0.5f);
					float dy = (y - size * 0.5f) / (size * 0.5f);
					float d = Mathf.Sqrt(dx * dx + dy * dy);
					float edge = 0.78f + (float)random.NextDouble() * 0.12f;
					float alpha = Mathf.Clamp01((edge - d) * 5f) * (1f - d * 0.45f);
					pixels[y * size + x] = new Color32(18, 14, 12, (byte)Mathf.RoundToInt(alpha * 190f));
				}
			}
			texture.SetPixels32(pixels);
			texture.Apply();
			return texture;
		}

		private static Texture2D MakeRingTexture(int size)
		{
			var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
			var pixels = new Color32[size * size];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float dx = (x - size * 0.5f) / (size * 0.5f);
					float dy = (y - size * 0.5f) / (size * 0.5f);
					float d = Mathf.Sqrt(dx * dx + dy * dy);
					float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.93f) * 22f);          // thin bright edge
					float fill = d < 0.93f ? 0.12f * (1f - d) : 0f;                        // faint inside
					byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(ring + fill) * 255f);
					pixels[y * size + x] = new Color32(255, 255, 255, a);
				}
			}
			texture.SetPixels32(pixels);
			texture.Apply();
			return texture;
		}

		private static void ConfigureFireball(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6.5f);
			main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.55f), new Color(1f, 0.5f, 0.1f));
			main.gravityModifier = -0.25f;
			Sphere(system, 0.5f);
			Fade(system, new Color(1f, 0.9f, 0.5f), new Color(0.55f, 0.12f, 0.03f));
			var size = system.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.3f)));
		}

		/// <summary>The shockwave: dots thrown outward on the ground in a flat circle, so it reads as an expanding ring.</summary>
		private static void ConfigureRing(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = 0.38f;
			main.startSpeed = 11f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.35f), new Color(1f, 0.45f, 0.1f));
			var shape = system.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Circle;
			shape.radius = 0.25f;
			shape.radiusThickness = 0f;
			shape.rotation = new Vector3(90f, 0f, 0f); // lie flat on the street
			Fade(system, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.25f, 0.05f));
			Shrink(system);
		}

		private static void ConfigureEmber(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 12f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.17f);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.35f, 0.05f));
			main.gravityModifier = 1.1f;
			Sphere(system, 0.3f);
			Fade(system, new Color(1f, 0.9f, 0.4f), new Color(0.8f, 0.1f, 0.02f));
			Shrink(system);
			renderer.renderMode = ParticleSystemRenderMode.Stretch;
			renderer.lengthScale = 1.6f;
			renderer.velocityScale = 0.05f;
		}

		private static void ConfigureSmoke(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2.5f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.7f);
			main.startColor = new Color(0.22f, 0.2f, 0.22f, 0.32f);
			main.gravityModifier = -0.12f;
			Sphere(system, 0.6f);
			Fade(system, new Color(0.25f, 0.2f, 0.2f), new Color(0.05f, 0.05f, 0.06f));
			var size = system.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
		}

		private void OnDash(Vector3 position)
		{
			Emit(Kind.Dust, position + Vector3.up * 0.15f, 10);
		}

		// ---- particle systems ---------------------------------------------------------------

		private void Emit(Kind kind, Vector3 position, int count)
		{
			if (!pools.TryGetValue(kind, out ParticleSystem[] pool))
			{
				return;
			}
			int index = next[kind];
			next[kind] = (index + 1) % pool.Length;
			ParticleSystem system = pool[index];
			system.transform.position = position;
			system.Emit(count);
		}

		private void BuildPool(Kind kind, Material material, System.Action<ParticleSystem, ParticleSystemRenderer> configure)
		{
			var pool = new ParticleSystem[PoolSize];
			for (int i = 0; i < PoolSize; i++)
			{
				var go = new GameObject("Vfx_" + kind);
				go.transform.SetParent(transform, false);
				var system = go.AddComponent<ParticleSystem>();
				system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				var main = system.main;
				main.playOnAwake = false;
				main.loop = false;
				main.simulationSpace = ParticleSystemSimulationSpace.World;
				var emission = system.emission;
				emission.enabled = false;
				var renderer = go.GetComponent<ParticleSystemRenderer>();
				renderer.sharedMaterial = material;
				renderer.renderMode = ParticleSystemRenderMode.Billboard;
				configure(system, renderer);
				pool[i] = system;
			}
			pools[kind] = pool;
			next[kind] = 0;
		}

		private static void Fade(ParticleSystem system, Color from, Color to)
		{
			var colorOverLifetime = system.colorOverLifetime;
			colorOverLifetime.enabled = true;
			var gradient = new Gradient();
			gradient.SetKeys(
				new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
				new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
			colorOverLifetime.color = gradient;
		}

		private static void Shrink(ParticleSystem system)
		{
			var size = system.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
		}

		private static void Sphere(ParticleSystem system, float radius)
		{
			var shape = system.shape;
			shape.enabled = true;
			shape.shapeType = ParticleSystemShapeType.Sphere;
			shape.radius = radius;
		}

		private static void ConfigureSpark(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.2f);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f), new Color(1f, 0.5f, 0.15f));
			main.gravityModifier = 0.6f;
			Sphere(system, 0.12f);
			Fade(system, Color.white, new Color(1f, 0.3f, 0.1f));
			Shrink(system);
			renderer.renderMode = ParticleSystemRenderMode.Stretch;
			renderer.lengthScale = 1.8f;
			renderer.velocityScale = 0.05f;
		}

		private static void ConfigureStar(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = 0.22f;
			main.startSpeed = 0f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
			main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
			main.startColor = new Color(1f, 0.95f, 0.6f);
			var shape = system.shape;
			shape.enabled = false;
			var size = system.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
			Fade(system, Color.white, new Color(1f, 0.6f, 0.2f));
		}

		private static void ConfigureDust(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.75f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 2f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
			main.startColor = new Color(0.75f, 0.72f, 0.8f, 0.55f);
			main.gravityModifier = -0.05f;
			Sphere(system, 0.25f);
			Fade(system, new Color(0.8f, 0.78f, 0.85f), new Color(0.4f, 0.38f, 0.5f));
			var size = system.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.5f));
		}

		private static void ConfigurePoof(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.95f, 1f, 0.9f), new Color(0.7f, 0.65f, 0.9f, 0.8f));
			main.gravityModifier = -0.1f;
			Sphere(system, 0.3f);
			Fade(system, new Color(1f, 1f, 1f), new Color(0.35f, 0.3f, 0.5f));
			var size = system.sizeOverLifetime;
			size.enabled = true;
			size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.4f));
		}

		private static void ConfigureDeflect(ParticleSystem system, ParticleSystemRenderer renderer)
		{
			var main = system.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
			main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
			main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 1f, 1f), new Color(0.2f, 0.7f, 1f));
			Sphere(system, 0.15f);
			Fade(system, Color.white, new Color(0.1f, 0.5f, 1f));
			Shrink(system);
			renderer.renderMode = ParticleSystemRenderMode.Stretch;
			renderer.lengthScale = 2.4f;
			renderer.velocityScale = 0.06f;
		}

		// ---- textures and materials made in code (no image files) --------------------------------

		private static Material MakeMaterial(bool additiveBlend, Texture2D texture)
		{
			Shader shader = additiveBlend ? Shader.Find("Legacy Shaders/Particles/Additive") : Shader.Find("Legacy Shaders/Particles/Alpha Blended");
			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}
			var material = new Material(shader) { mainTexture = texture };
			if (material.HasProperty("_TintColor"))
			{
				material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
			}
			return material;
		}

		private static Texture2D MakeCircle(int size, float edge)
		{
			var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
			var px = new Color32[size * size];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
					float a = Mathf.Clamp01((1f - d) / edge);
					a = a * a * (3f - 2f * a);
					px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
				}
			}
			tex.SetPixels32(px);
			tex.Apply();
			return tex;
		}

		/// <summary>Five-point cartoon star (the "POW" burst of old comics).</summary>
		private static Texture2D MakeStar(int size)
		{
			var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
			var px = new Color32[size * size];
			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					Vector2 p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f) * 2f;
					float angle = Mathf.Atan2(p.y, p.x);
					float radius = p.magnitude;
					float spike = 0.45f + 0.5f * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 2.5f)), 3f);
					float a = Mathf.Clamp01((spike - radius) * 12f);
					px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
				}
			}
			tex.SetPixels32(px);
			tex.Apply();
			return tex;
		}
	}
}
