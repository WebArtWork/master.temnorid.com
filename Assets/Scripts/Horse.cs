using System;

namespace Temnorid.Master
{
	public enum HorseStage
	{
		Foal,
		Young,
		Adult,
	}

	/// <summary>How the horse is shown reacting. The view turns each into a small procedural animation.</summary>
	public enum HorseAnim
	{
		Idle,
		Eat,
		Groom,
		Pet,
		Sleep,
		Run,
		Jump,
		Stumble,
		Nod,
		Celebrate,
	}

	public static class HorseProgress
	{
		public static int LevelFor(int xp)
		{
			var level = 1;
			for (var i = 1; i < MasterConfig.HorseLevelXp.Length; i++) if (xp >= MasterConfig.HorseLevelXp[i]) level = i + 1;
			return Math.Min(level, 5);
		}

		public static HorseStage StageFor(int level) => level >= 5 ? HorseStage.Adult : level >= 3 ? HorseStage.Young : HorseStage.Foal;
	}

	/// <summary>
	/// The horse: four needs kept as fractions so slow change adds up, its experience and the times its trust can next be earned.
	/// What is shown is rounded, never floored, so a fresh 70 stays 70.
	/// </summary>
	public sealed class HorseState
	{
		double _satiety = MasterConfig.StartSatiety;
		double _cleanliness = MasterConfig.StartCleanliness;
		double _energy = MasterConfig.StartEnergy;
		double _trust = MasterConfig.StartTrust;

		public string Name = MasterConfig.StartHorseName;
		public int Xp;
		/// <summary>UTC ticks before which grooming and petting give no more trust.</summary>
		public long GroomTrustReadyTicks;
		public long PetReadyTicks;

		public int Satiety => Whole(_satiety);
		public int Cleanliness => Whole(_cleanliness);
		public int Energy => Whole(_energy);
		public int Trust => Whole(_trust);
		public double SatietyExact => _satiety;
		public double CleanlinessExact => _cleanliness;
		public double EnergyExact => _energy;
		public double TrustExact => _trust;
		public int Level => HorseProgress.LevelFor(Xp);
		public HorseStage Stage => HorseProgress.StageFor(Level);

		static int Whole(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
		static double Clamp(double value) => Math.Max(0.0, Math.Min(100.0, value));

		public void AddSatiety(double amount) => _satiety = Clamp(_satiety + amount);
		public void AddCleanliness(double amount) => _cleanliness = Clamp(_cleanliness + amount);
		public void AddEnergy(double amount) => _energy = Clamp(_energy + amount);
		public void AddTrust(double amount) => _trust = Clamp(_trust + amount);

		/// <summary>Time passing: slow hunger and dirt, slow rest. Trust does not change.</summary>
		public void Tick(double seconds)
		{
			if (seconds <= 0.0) return;
			var hours = seconds / 3600.0;
			_satiety = Clamp(_satiety - 2.0 * hours);
			_cleanliness = Clamp(_cleanliness - 1.0 * hours);
			_energy = Clamp(_energy + 5.0 * hours);
		}

		public void SetNeeds(double satiety, double cleanliness, double energy, double trust)
		{
			_satiety = Clamp(satiety);
			_cleanliness = Clamp(cleanliness);
			_energy = Clamp(energy);
			_trust = Clamp(trust);
		}
	}

	public sealed class CareResult
	{
		public bool Ok;
		/// <summary>A key of <see cref="Loc"/> for what to tell the player.</summary>
		public string Message;
		public HorseAnim Anim;
		public double Seconds;

		public static CareResult Fail(string message) => new CareResult { Ok = false, Message = message };
	}

	/// <summary>One training run: its id (paid once), what it is and whether the horse was dirty when it began.</summary>
	public sealed class TrainingRun
	{
		public string RunId;
		public string Kind;
		public bool Dirty;
	}

	/// <summary>The pure reward rules of the two trainings.</summary>
	public static class TrainingRules
	{
		public static int EnergyCost(string kind) => kind == Ids.Obstacles ? 20 : 10;
		public static int Max(string kind) => kind == Ids.Obstacles ? 8 : 5;

		public static int Coins(string kind, int result, bool dirty)
		{
			var coins = kind == Ids.Obstacles ? 10 + 2 * result : 8 + 3 * result;
			return dirty ? (int)Math.Floor(coins * 0.8) : coins;
		}

		public static int Xp(string kind, int result) => kind == Ids.Obstacles ? 10 + 3 * result : 8 + 4 * result;
		public static int Quality(string kind, int result) => kind == Ids.Obstacles ? (int)Math.Round(100.0 * result / 8.0, MidpointRounding.AwayFromZero) : 20 * result;
	}
}
