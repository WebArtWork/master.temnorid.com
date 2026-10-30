using System;
using UnityEngine;

namespace Temnorid.Master
{
	[Serializable]
	public sealed class CameraPose
	{
		public string Key;
		public Transform Anchor;
		public Transform Look;
		public float Fov = 45f;
	}

	/// <summary>
	/// The one camera. It has a handful of fixed poses (the hub, each station's overview and close view, the stable and the side of the
	/// obstacle course) and glides between them. No avatar is followed and the player never steers it.
	/// </summary>
	[RequireComponent(typeof(Camera))]
	public sealed class ViewCamera : MonoBehaviour
	{
		public CameraPose[] Poses;
		public float BlendSpeed = 7f;

		Camera _camera;
		CameraPose _current;
		bool _snap = true;

		public string Key => _current != null ? _current.Key : "";

		/// <summary>True while it is still gliding, so pointer picking in the world waits.</summary>
		public bool Blending { get; private set; }

		void Awake() => _camera = GetComponent<Camera>();

		public bool Has(string key) => Find(key) != null;

		CameraPose Find(string key)
		{
			if (Poses == null) return null;
			foreach (var pose in Poses) if (pose.Key == key) return pose;
			return null;
		}

		public void Go(string key, bool snap = false)
		{
			var pose = Find(key);
			if (pose == null) return;
			_current = pose;
			if (snap) _snap = true;
		}

		void LateUpdate()
		{
			if (_current == null) return;
			if (_camera == null) _camera = GetComponent<Camera>();
			var position = _current.Anchor.position;
			var rotation = Quaternion.LookRotation(_current.Look.position - position);
			if (_snap)
			{
				_snap = false;
				Blending = false;
				transform.SetPositionAndRotation(position, rotation);
				_camera.fieldOfView = _current.Fov;
				return;
			}
			var k = 1f - Mathf.Exp(-BlendSpeed * Time.unscaledDeltaTime);
			transform.SetPositionAndRotation(Vector3.Lerp(transform.position, position, k), Quaternion.Slerp(transform.rotation, rotation, k));
			_camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _current.Fov, k);
			Blending = (transform.position - position).sqrMagnitude > 0.02f;
		}
	}
}
