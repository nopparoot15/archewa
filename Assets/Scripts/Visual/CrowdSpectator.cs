using UnityEngine;

namespace MyArcade
{
	// just scenery: no collision, no AI
	public sealed class CrowdSpectator : MonoBehaviour
	{
		private const string ArtFolder = "Environment/ThaiAlleyV4/";
		[SerializeField] private int appearance;
		[SerializeField] private float phase;
		private SpriteRenderer drawing;
		private readonly Sprite[] frames = new Sprite[4];

		public void Setup(int type, float offset)
		{
			appearance = type;
			phase = offset;
			Load();
		}
		private void Awake() { Load(); }
		private void Load()
		{
			drawing = GetComponent<SpriteRenderer>();
			for (int i = 0; i < frames.Length; i++)
				frames[i] = Resources.Load<Sprite>(ArtFolder + "Crowd/spectator_" + appearance + "_" + i);
			drawing.sprite = frames[Mathf.FloorToInt(phase) % frames.Length];
			drawing.sortingOrder = -5;
			drawing.color = new Color(.85f, .88f, .97f);
			var lighting = Resources.Load<Material>(ArtFolder + "ActorLighting");
			if (lighting != null) drawing.sharedMaterial = lighting;
		}
		private void LateUpdate()
		{
			if (drawing == null) return;
			int frame = (int)(Time.time * 2.4f + phase) % frames.Length;
			if (frames[frame] != null) drawing.sprite = frames[frame];
			if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
		}
	}
}
