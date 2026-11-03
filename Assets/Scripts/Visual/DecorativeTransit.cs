using UnityEngine;

namespace MyArcade
{
	public sealed class DecorativeTransit : MonoBehaviour
	{
		[SerializeField] private Vector3 start;
		[SerializeField] private Vector3 finish;
		[SerializeField] private float speed = 7;
		[SerializeField] private float pause = 8;
		[SerializeField] private float phase;
		public void Setup(Vector3 from, Vector3 to, float unitsPerSecond, float interval, float offset)
		{
			start = from; finish = to; speed = unitsPerSecond; pause = interval; phase = offset;
			transform.localPosition = PositionAt(0);
		}
		public Vector3 PositionAt(float time)
		{
			float duration = Vector3.Distance(start, finish) / Mathf.Max(.1f,speed);
			float cycle = Mathf.Repeat(time + phase, duration + pause);
			return Vector3.Lerp(start, finish, Mathf.Clamp01(cycle / duration));
		}
		private void Update() { transform.localPosition = PositionAt(Time.time); }
	}
}
