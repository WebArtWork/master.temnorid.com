using UnityEngine;

namespace Temnorid.Master
{
	/// <summary>
	/// The horse, drawn from primitives and animated by moving the parts: breathing, eating, grooming, petting,
	/// sleeping, running, jumping, stumbling, nodding and celebrating. It grows with its level. The horse faces +Z in its own space.
	/// </summary>
	public sealed class HorseView : MonoBehaviour
	{
		[Header("Parts")]
		/// <summary>Everything that is drawn; scaled by the age stage.</summary>
		public Transform Visual;
		public Transform Body;
		public Transform Neck;
		public Transform Head;
		public Transform Tail;
		/// <summary>Front left, front right, back left, back right (pivots at the hip).</summary>
		public Transform[] Legs;
		public GameObject SleepMark;

		/// <summary>Height above the ground while jumping, set by the game.</summary>
		public float JumpOffset;

		public HorseAnim Anim { get; private set; } = HorseAnim.Idle;

		Vector3 _bodyScale;
		Quaternion _neckBase, _headBase, _tailBase;
		Quaternion[] _legBase;
		Vector3 _headScaleBase;
		float _time;
		float _animStart;
		float _animEnd;
		float _targetScale = 0.62f;
		float _targetHead = 1.3f;
		bool _ready;

		void Awake() => Init();

		void Init()
		{
			if (_ready) return;
			_ready = true;
			_bodyScale = Body.localScale;
			_neckBase = Neck.localRotation;
			_headBase = Head.localRotation;
			_tailBase = Tail.localRotation;
			_headScaleBase = Head.localScale;
			_legBase = new Quaternion[Legs.Length];
			for (var i = 0; i < Legs.Length; i++) _legBase[i] = Legs[i].localRotation;
			if (SleepMark != null) SleepMark.SetActive(false);
		}

		/// <summary>Plays a reaction for a number of seconds, then goes back to standing. Run and Jump are held by the game until it says otherwise.</summary>
		public void Play(HorseAnim anim, float seconds)
		{
			Init();
			Anim = anim;
			_animStart = _time;
			_animEnd = _time + seconds;
		}

		/// <summary>Goes back to standing at once.</summary>
		public void Stop()
		{
			Anim = HorseAnim.Idle;
			JumpOffset = 0f;
		}

		/// <summary>Shows the age stage.</summary>
		public void Apply(HorseState horse, bool snap = false)
		{
			Init();
			switch (horse.Stage)
			{
				case HorseStage.Foal: _targetScale = 0.62f; _targetHead = 1.3f; break;
				case HorseStage.Young: _targetScale = 0.85f; _targetHead = 1.12f; break;
				default: _targetScale = 1f; _targetHead = 1f; break;
			}
			if (snap)
			{
				Visual.localScale = Vector3.one * _targetScale;
				Head.localScale = _headScaleBase * _targetHead;
			}
		}

		void Update()
		{
			_time += Time.deltaTime;
			if (Anim != HorseAnim.Idle && Anim != HorseAnim.Run && Anim != HorseAnim.Jump && _time >= _animEnd) Anim = HorseAnim.Idle;

			var k = 1f - Mathf.Exp(-6f * Time.deltaTime);
			Visual.localScale = Vector3.Lerp(Visual.localScale, Vector3.one * _targetScale, k);
			Head.localScale = Vector3.Lerp(Head.localScale, _headScaleBase * _targetHead, k);
			Pose();
		}

		void Pose()
		{
			var t = _time;
			var duration = Mathf.Max(0.01f, _animEnd - _animStart);
			var u = Mathf.Clamp01((t - _animStart) / duration);

			var bodyPitch = 0f;
			var bodyRoll = 0f;
			var lift = 0f;
			var neck = 0f;
			var head = 0f;
			var headYaw = Mathf.Sin(t * 0.5f) * 12f;
			var tail = Mathf.Sin(t * 2f) * 14f;
			var legs = new float[4];
			var breathe = 1f + Mathf.Sin(t * 2f) * 0.012f;
			var sleeping = false;

			switch (Anim)
			{
				case HorseAnim.Eat:
					neck = 48f + Mathf.Sin(t * 9f) * 5f;
					head = 12f;
					headYaw = 0f;
					break;
				case HorseAnim.Groom:
					bodyRoll = Mathf.Sin(t * 13f) * 2.5f;
					neck = 6f;
					head = Mathf.Sin(t * 6f) * 8f;
					tail = Mathf.Sin(t * 9f) * 25f;
					break;
				case HorseAnim.Pet:
					neck = 14f + Mathf.Sin(t * 5f) * 10f;
					head = 10f;
					headYaw = Mathf.Sin(t * 3f) * 6f;
					tail = Mathf.Sin(t * 10f) * 30f;
					break;
				case HorseAnim.Nod:
					head = Mathf.Sin(u * Mathf.PI * 2f) * 24f;
					neck = Mathf.Sin(u * Mathf.PI) * 8f;
					break;
				case HorseAnim.Sleep:
					sleeping = true;
					lift = -0.3f;
					neck = 55f;
					head = 12f;
					headYaw = 0f;
					bodyRoll = Mathf.Sin(t * 1.4f) * 1.5f;
					breathe = 1f + Mathf.Sin(t * 1.4f) * 0.025f;
					legs[0] = legs[1] = -55f;
					legs[2] = legs[3] = 55f;
					break;
				case HorseAnim.Run:
					lift = Mathf.Abs(Mathf.Sin(t * 8f)) * 0.12f;
					legs[0] = Mathf.Sin(t * 8f) * 38f;
					legs[3] = Mathf.Sin(t * 8f) * 38f;
					legs[1] = Mathf.Sin(t * 8f + Mathf.PI) * 38f;
					legs[2] = Mathf.Sin(t * 8f + Mathf.PI) * 38f;
					neck = -8f + Mathf.Sin(t * 8f) * 4f;
					tail = -20f + Mathf.Sin(t * 8f) * 8f;
					headYaw = 0f;
					break;
				case HorseAnim.Jump:
					bodyPitch = -12f;
					legs[0] = legs[1] = -50f;
					legs[2] = legs[3] = 40f;
					neck = -12f;
					tail = -25f;
					headYaw = 0f;
					break;
				case HorseAnim.Stumble:
					bodyPitch = Mathf.Sin(u * Mathf.PI) * 20f;
					legs[0] = legs[1] = Mathf.Sin(u * Mathf.PI) * 25f;
					neck = Mathf.Sin(u * Mathf.PI) * 18f;
					headYaw = 0f;
					break;
				case HorseAnim.Celebrate:
					var hop = Mathf.Abs(Mathf.Sin(t * 6f));
					lift = hop * 0.35f;
					bodyPitch = -hop * 14f;
					legs[0] = legs[1] = -hop * 45f;
					neck = -10f;
					tail = Mathf.Sin(t * 12f) * 30f;
					break;
			}

			if (SleepMark != null && SleepMark.activeSelf != sleeping) SleepMark.SetActive(sleeping);
			if (sleeping && SleepMark != null)
			{
				SleepMark.transform.localPosition = new Vector3(0f, 2.3f + Mathf.Sin(t * 2f) * 0.08f, 1.1f);
				if (Camera.main != null) SleepMark.transform.rotation = Camera.main.transform.rotation;
			}

			Visual.localPosition = new Vector3(0f, lift + JumpOffset, 0f);
			Body.localScale = new Vector3(_bodyScale.x, _bodyScale.y * breathe, _bodyScale.z);
			Body.localRotation = Quaternion.Euler(bodyPitch, 0f, bodyRoll);
			Neck.localRotation = _neckBase * Quaternion.Euler(neck, 0f, 0f);
			Head.localRotation = _headBase * Quaternion.Euler(head, headYaw, 0f);
			Tail.localRotation = _tailBase * Quaternion.Euler(0f, tail, 0f);
			for (var i = 0; i < Legs.Length; i++) Legs[i].localRotation = _legBase[i] * Quaternion.Euler(legs[i], 0f, 0f);
		}
	}
}
