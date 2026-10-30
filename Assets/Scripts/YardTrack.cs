using UnityEngine;

namespace Temnorid.Master
{
	/// <summary>
	/// The obstacle course as a treadmill: the horse stays where the fixed camera looks and the jumps (and the ground marks) come towards it.
	/// A jump that was hit is knocked over. The track's own position is where the horse stands.
	/// </summary>
	public sealed class YardTrack : MonoBehaviour
	{
		public Transform[] Obstacles;
		public Transform[] Marks;
		public float MarkGap = 6f;

		bool[] _knocked;

		void Awake() => _knocked = new bool[Obstacles.Length];

		/// <summary>Puts everything where it is after the run has lasted <paramref name="time"/> seconds.</summary>
		public void Place(float time)
		{
			if (_knocked == null) _knocked = new bool[Obstacles.Length];
			for (var i = 0; i < Obstacles.Length; i++)
			{
				var z = (ObstacleRunLogic.ObstacleTime(i) - time) * ObstacleRunLogic.RunSpeed;
				var position = new Vector3(0f, _knocked[i] ? -0.15f : 0f, z + (_knocked[i] ? 0.4f : 0f));
				Obstacles[i].localPosition = position;
				Obstacles[i].localRotation = _knocked[i] ? Quaternion.Euler(70f, 0f, 0f) : Quaternion.identity;
				Obstacles[i].gameObject.SetActive(z > -8f && z < 60f);
			}
			if (Marks == null) return;
			foreach (var m in Marks) m.gameObject.SetActive(true);
			var total = MarkGap * Marks.Length;
			for (var i = 0; i < Marks.Length; i++)
			{
				var z = (i * MarkGap - time * ObstacleRunLogic.RunSpeed) % total;
				if (z < -MarkGap * 2f) z += total;
				Marks[i].localPosition = new Vector3(Marks[i].localPosition.x, Marks[i].localPosition.y, z);
			}
		}

		/// <summary>Shows or hides the whole course (it is only there during the obstacle run).</summary>
		public void Show(bool visible)
		{
			foreach (var o in Obstacles) o.gameObject.SetActive(visible);
			if (Marks != null) foreach (var m in Marks) m.gameObject.SetActive(visible);
		}

		public void Knock(int index)
		{
			if (index < 0 || index >= Obstacles.Length) return;
			if (_knocked == null) _knocked = new bool[Obstacles.Length];
			_knocked[index] = true;
		}

		public void ResetAll()
		{
			if (_knocked == null) _knocked = new bool[Obstacles.Length];
			for (var i = 0; i < _knocked.Length; i++) _knocked[i] = false;
			Place(0f);
		}
	}
}
