using System.Collections.Generic;
using UnityEngine;

namespace MyArcade
{
	public class VfxDirector : MonoBehaviour
	{
		private const int BloodLimit = 90;
		private const int CorpseLimit = 40;
		private const int FuseRingLimit = 4;

		[SerializeField] private ParticleSystem spark;
		[SerializeField] private ParticleSystem star;
		[SerializeField] private ParticleSystem dust;
		[SerializeField] private ParticleSystem poof;
		[SerializeField] private ParticleSystem deflect;
		[SerializeField] private ParticleSystem fireball;
		[SerializeField] private ParticleSystem ring;
		[SerializeField] private ParticleSystem ember;
		[SerializeField] private ParticleSystem smoke;
		[SerializeField] private SlashArc slash;
		[SerializeField] private Light blastLight;
		[SerializeField] private GameObject bloodPrefab;
		[SerializeField] private GameObject corpsePrefab;
		[SerializeField] private GameObject fuseRingPrefab;
		[SerializeField] private Material[] bloodMaterials;
		[SerializeField] private Material sootMaterial;

		private readonly List<Transform> bloodDecals = new List<Transform>();
		private readonly List<SpriteRenderer> corpses = new List<SpriteRenderer>();
		private readonly List<Transform> fuseRings = new List<Transform>();
		private readonly float[] fuseRingUntil = new float[FuseRingLimit];
		private MaterialPropertyBlock fuseTint;
		private float blastLightLife;
		private int nextBlood;
		private int nextCorpse;

		private void Awake()
		{
			fuseTint = new MaterialPropertyBlock();
		}

		public void Setup()
		{
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
			UpdateBlastLight();
			UpdateFuseRings();
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

		private void Emit(ParticleSystem system, Vector3 position, int count)
		{
			system.Emit(new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true }, count);
		}

		private void OnHit(Vector3 position, int damage)
		{
			Emit(spark, position, 8 + damage * 4);
			Emit(star, position, damage >= 3 ? 3 : 1);
		}

		private void OnDeflected(Vector3 position)
		{
			Emit(deflect, position, 18);
			Emit(star, position, 2);
		}

		private void OnDash(Vector3 position)
		{
			Emit(dust, position + Vector3.up * 0.15f, 10);
		}

		private void OnSwing(Vector3 origin, Vector3 direction, float reach, float arc)
		{
			slash.Show(origin, direction, reach, arc);
		}

		private void OnKilled(Enemy enemy)
		{
			Vector3 p = enemy.transform.position + Vector3.up * 0.9f;
			SplatBlood(enemy.transform.position, enemy is Boss);
			StartCoroutine(LeaveCorpse(enemy));
			Emit(poof, p, enemy is Boss ? 40 : 16);
			Emit(star, p, enemy is Boss ? 10 : 4);
		}

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
			SpriteRenderer body;
			if (corpses.Count < CorpseLimit)
			{
				body = Instantiate(corpsePrefab, transform).GetComponent<SpriteRenderer>();
				corpses.Add(body);
			}
			else
			{
				body = corpses[nextCorpse];
				nextCorpse = (nextCorpse + 1) % CorpseLimit;
			}
			// the drawn "fallen" frame, standing in the world exactly like the fighter's own sprite (it is a lying figure drawn upright)
			body.sprite = source.sprite;
			body.flipX = source.flipX;
			body.color = new Color(0.5f, 0.45f, 0.52f);
			body.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
			body.transform.localScale = source.transform.lossyScale;
			body.gameObject.SetActive(true);
		}

		// the oldest splash makes room for the newest
		private Transform NextDecal()
		{
			if (bloodDecals.Count < BloodLimit)
			{
				bloodDecals.Add(Instantiate(bloodPrefab, transform).transform);
				nextBlood = bloodDecals.Count - 1;
			}
			else
			{
				nextBlood = (nextBlood + 1) % BloodLimit;
			}
			return bloodDecals[nextBlood];
		}

		private void SplatBlood(Vector3 position, bool big)
		{
			int count = big ? 3 : 1;
			for (int i = 0; i < count; i++)
			{
				Transform decal = NextDecal();
				Vector2 spread = Random.insideUnitCircle * (big ? 1.4f : 0.5f);
				decal.position = new Vector3(position.x + spread.x, 0.03f + nextBlood * 0.0004f, position.z + spread.y);
				decal.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
				decal.localScale = Vector3.one * (big ? Random.Range(2.4f, 3.6f) : Random.Range(1.2f, 2.2f));
				decal.GetComponent<Renderer>().sharedMaterial = bloodMaterials[Random.Range(0, bloodMaterials.Length)];
				decal.gameObject.SetActive(true);
			}
		}

		private void SplatScorch(Vector3 position, float radius)
		{
			Transform decal = NextDecal();
			decal.position = new Vector3(position.x, 0.025f + nextBlood * 0.0004f, position.z);
			decal.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
			decal.localScale = Vector3.one * radius * 1.7f;
			decal.GetComponent<Renderer>().sharedMaterial = sootMaterial;
			decal.gameObject.SetActive(true);
		}

		private void OnBombLit(Vector3 position, float radius)
		{
			int slot = fuseRings.FindIndex(r => !r.gameObject.activeSelf);
			if (slot < 0)
			{
				if (fuseRings.Count >= FuseRingLimit)
				{
					return;
				}
				fuseRings.Add(Instantiate(fuseRingPrefab, transform).transform);
				slot = fuseRings.Count - 1;
			}
			Transform fuse = fuseRings[slot];
			fuse.position = new Vector3(position.x, 0.05f, position.z);
			fuse.localScale = Vector3.one * radius * 2f;
			fuse.gameObject.SetActive(true);
			fuseRingUntil[slot] = Time.time + 0.9f; // the fuse time
		}

		private void OnExploded(Vector3 position, float radius)
		{
			Vector3 core = position + Vector3.up * 0.8f;
			Emit(fireball, core, 34);
			Emit(ring, new Vector3(position.x, 0.12f, position.z), 70);
			Emit(ember, core, 46);
			Emit(spark, core, 30);
			Emit(smoke, core, 9);
			Emit(star, core, 3);
			SplatScorch(position, radius);
			blastLight.transform.position = core + Vector3.up * 0.8f;
			blastLight.range = radius * 3.6f;
			blastLightLife = 0.45f;
			blastLight.enabled = true;
		}

		private void UpdateBlastLight()
		{
			if (blastLightLife <= 0f)
			{
				return;
			}
			blastLightLife -= Time.unscaledDeltaTime;
			float k = Mathf.Clamp01(blastLightLife / 0.45f);
			blastLight.intensity = 14f * k * k;
			blastLight.color = Color.Lerp(new Color(1f, 0.25f, 0.08f), new Color(1f, 0.8f, 0.4f), k);
			if (blastLightLife <= 0f)
			{
				blastLight.enabled = false;
			}
		}

		private void UpdateFuseRings()
		{
			for (int i = 0; i < fuseRings.Count; i++)
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
				fuseTint.SetColor("_TintColor", tint * 0.6f);
				fuseRings[i].GetComponent<Renderer>().SetPropertyBlock(fuseTint);
			}
		}
	}
}
