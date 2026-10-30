using System;

namespace Temnorid.Master
{
	/// <summary>
	/// The obstacle run with no Unity objects: eight obstacles at fixed times, one jump button. An obstacle is
	/// cleared when the horse is high enough as it passes. Hitting one costs only the points; the run goes on.
	/// </summary>
	public sealed class ObstacleRunLogic
	{
		public const int ObstacleCount = 8;
		public const float JumpSeconds = 0.7f;
		public const float JumpPeak = 1.4f;
		/// <summary>The horse clears an obstacle when it is at least this share of the way up.</summary>
		public const float ClearShare = 0.5f;
		public const float RunSpeed = 4.2f;
		public const float FirstObstacle = 6f;
		public const float Gap = 5.5f;
		public const float EndPause = 2f;

		public static float ObstacleTime(int index) => FirstObstacle + index * Gap;

		float _jumpStart = -10f;
		int _next;

		public float Time { get; private set; }
		public int Cleared { get; private set; }
		public int Hit { get; private set; }
		public bool Finished { get; private set; }
		public int Score => Cleared * 10;
		public int Remaining => ObstacleCount - _next;

		/// <summary>Raised when an obstacle goes by: its index and whether the horse cleared it.</summary>
		public event Action<int, bool> Resolved;

		public bool Jumping => Time - _jumpStart < JumpSeconds && Time >= _jumpStart;
		public float JumpHeight => Jumping ? JumpPeak * (float)Math.Sin(Math.PI * (Time - _jumpStart) / JumpSeconds) : 0f;

		/// <summary>0 to 1 progress through the run.</summary>
		public float Progress => Math.Min(1f, Time / (ObstacleTime(ObstacleCount - 1) + EndPause));

		/// <summary>Starts a jump if the horse is on the ground. Returns whether it jumped.</summary>
		public bool PressJump()
		{
			if (Finished || Jumping) return false;
			_jumpStart = Time;
			return true;
		}

		public void Update(float deltaTime)
		{
			if (Finished) return;
			Time += deltaTime;
			while (_next < ObstacleCount && Time >= ObstacleTime(_next))
			{
				// The height at the moment the obstacle passes, using the time it was due.
				var cleared = HeightAt(ObstacleTime(_next)) >= JumpPeak * ClearShare;
				if (cleared) Cleared++;
				else Hit++;
				Resolved?.Invoke(_next, cleared);
				_next++;
			}
			if (_next >= ObstacleCount && Time >= ObstacleTime(ObstacleCount - 1) + EndPause) Finished = true;
		}

		float HeightAt(float time)
		{
			var since = time - _jumpStart;
			if (since < 0f || since >= JumpSeconds) return 0f;
			return JumpPeak * (float)Math.Sin(Math.PI * since / JumpSeconds);
		}
	}

	/// <summary>
	/// The command timing with no Unity objects: five rounds, a marker sweeping a bar, and a target zone.
	/// Press when the marker is inside the zone for 20 points; a miss gives none.
	/// </summary>
	public sealed class CommandTimingLogic
	{
		public const int Rounds = 5;
		public const float ZoneWidth = 0.2f;
		public const float RoundTimeout = 7f;
		public const float PauseSeconds = 0.9f;

		static readonly float[] Centres = { 0.35f, 0.62f, 0.25f, 0.72f, 0.5f };
		static readonly float[] Speeds = { 0.45f, 0.6f, 0.75f, 0.9f, 1.05f };

		float _roundTime;
		float _pause;
		bool _waitingForPress = true;

		public int Round { get; private set; }
		public int Score { get; private set; }
		public bool Finished { get; private set; }
		public int Hits { get; private set; }

		/// <summary>True briefly after a round ends, while the result is shown.</summary>
		public bool ShowingResult => !_waitingForPress && !Finished;
		public bool LastHit { get; private set; }

		public float ZoneCentre => Centres[Math.Min(Round, Rounds - 1)];

		/// <summary>The marker's place on the bar, 0 (left) to 1 (right).</summary>
		public float Marker
		{
			get
			{
				var cycle = _roundTime * Speeds[Math.Min(Round, Rounds - 1)];
				var wrapped = cycle % 2f;
				return wrapped <= 1f ? wrapped : 2f - wrapped;
			}
		}

		public bool InZone => Math.Abs(Marker - ZoneCentre) <= ZoneWidth * 0.5f;

		/// <summary>Raised when a round ends: whether it was a hit.</summary>
		public event Action<bool> RoundEnded;

		public void Update(float deltaTime)
		{
			if (Finished) return;
			if (_waitingForPress)
			{
				_roundTime += deltaTime;
				if (_roundTime >= RoundTimeout) End(false);
				return;
			}
			_pause -= deltaTime;
			if (_pause > 0f) return;
			Round++;
			if (Round >= Rounds)
			{
				Finished = true;
				return;
			}
			_roundTime = 0f;
			_waitingForPress = true;
		}

		/// <summary>The action button. Returns true when it was a hit.</summary>
		public bool Press()
		{
			if (Finished || !_waitingForPress) return false;
			var hit = InZone;
			End(hit);
			return hit;
		}

		void End(bool hit)
		{
			_waitingForPress = false;
			_pause = PauseSeconds;
			LastHit = hit;
			if (hit)
			{
				Score += 20;
				Hits++;
			}
			RoundEnded?.Invoke(hit);
		}
	}
}
