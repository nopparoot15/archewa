using System.Collections;
using UnityEngine;

namespace MyArcade
{

	public class CameraRig : MonoBehaviour
	{

		private const float UnitsPerPixel = 8.32f / 180f;
		private const float OrthoSize = 180f * UnitsPerPixel;
		private const float Pitch = 60f;
		private const float FollowSpeed = 3f;
		private const float Distance = 40f;
		private const float AimSight = 125f * UnitsPerPixel;

		public static CameraRig Instance { get; private set; }

		private Camera cam;
		private Transform target;
		private Vector3 focus;
		private Vector3 sightOffset;
		private bool titleMode = true;
		private float orbitAngle;
		[SerializeField, Range(.55f,.85f)] private float titleSubjectScreenX = .73f;
		private Vector3 shakeKick;

		public void Setup(Camera camera, Transform followTarget)
		{
			Instance = this;
			cam = camera;
			target = followTarget;
			focus = target.position;
		}

		public void SetTitleMode(bool on)
		{
			titleMode = on;
			if (cam != null && !on)
			{
				cam.orthographic = true;
				cam.orthographicSize = OrthoSize;
				cam.nearClipPlane = 0.3f;
				cam.farClipPlane = 260f;
			}
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}


		public void LeanToward(Vector3 direction)
		{
			sightOffset = direction.sqrMagnitude > 0.001f ? direction.normalized * AimSight : Vector3.zero;
		}


		public static void Shake(float magnitude, float duration, float frequency = 0.05f)
		{
			if (Instance != null && GameSettings.ScreenShake)
			{
				Instance.StartCoroutine(Instance.ShakeRoutine(magnitude * UnitsPerPixel, duration, Mathf.Clamp01(frequency)));
			}
		}

		private IEnumerator ShakeRoutine(float magnitude, float duration, float frequency)
		{
			float end = Time.unscaledTime + duration;
			while (Time.unscaledTime < end)
			{
				Vector2 kick = Random.insideUnitCircle.normalized * magnitude;
				shakeKick += new Vector3(kick.x, 0f, kick.y);
				yield return new WaitForSecondsRealtime(frequency);
			}
		}

		private void LateUpdate()
		{
			if (cam == null || target == null)
			{
				return;
			}

			if (titleMode)
			{
				orbitAngle += Time.deltaTime * 6f;
				Quaternion orbitRotation = Quaternion.Euler(0f, orbitAngle, 0f);
				Vector3 orbit = orbitRotation * new Vector3(0f, 7f, -16f);
				float viewHalfWidth = new Vector3(0f, 5.5f, -16f).magnitude * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad) * cam.aspect;
				Vector3 framing = -(orbitRotation * Vector3.right) * ((titleSubjectScreenX - .5f) * 2f * viewHalfWidth);
				Vector3 desired = target.position + framing + orbit;
				cam.transform.position = Vector3.Lerp(cam.transform.position, desired, 1f - Mathf.Exp(-6f * Time.deltaTime));
				cam.transform.rotation = Quaternion.LookRotation((target.position + framing + Vector3.up * 1.5f) - cam.transform.position);
				return;
			}

			cam.orthographic = true;
			cam.orthographicSize = OrthoSize;

			// follow like GameCamera.Update: Lerp toward the hero (plus the lean), in real time so slow-motion does not slow the camera
			Vector3 goal = new Vector3(target.position.x, 0f, target.position.z) + sightOffset;
			focus = Vector3.Lerp(focus, goal, Mathf.Clamp01(Time.unscaledDeltaTime * FollowSpeed));

			// clamp to the street: the view never shows more than a small margin outside it
			float halfWidth = OrthoSize * cam.aspect;
			float halfDepth = OrthoSize / Mathf.Sin(Pitch * Mathf.Deg2Rad);
			float limitX = Mathf.Max(0f, Arena.HalfSize + 3f - halfWidth);
			float limitZ = Mathf.Max(0f, Arena.HalfSize + 3f - halfDepth);
			focus.x = Mathf.Clamp(focus.x, -limitX, limitX);
			focus.z = Mathf.Clamp(focus.z, -limitZ, limitZ);


			focus += shakeKick;
			shakeKick = Vector3.zero;

			Quaternion rotation = Quaternion.Euler(Pitch, 0f, 0f);
			cam.transform.rotation = rotation;
			cam.transform.position = focus - rotation * Vector3.forward * Distance;
		}
	}
}
