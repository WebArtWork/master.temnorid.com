using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Temnorid.Master
{
	/// <summary>The numbers of the game in one place.</summary>
	public static class MasterConfig
	{
		public const int SaveVersion = 1;
		public const int StartCoins = 100;
		public const int MaxLevel = 5;
		public static readonly int[] LevelXp = { 0, 75, 180, 330, 500 };
		public static readonly int[] RankXp = { 0, 15, 35, 70 };
		public const int HistoryLength = 25;
		public const double RepeatCoinFactor = 0.35;
		public const float RepeatXpFactor = 0.25f;
		public const int RepeatSkillXp = 5;
		public const float TraceZeroError = 0.15f;
		public const int ResamplePoints = 64;
		public const int MinTracePoints = 10;
		public const int ScribeMinTrace = 50;
		public const int TailorMinCut = 40;
		public const int SuccessQuality = 60;
		public const float SealTolerance = 0.12f;
		public const int GuideBonus = 10;
		public const int BenchBonus = 5;

		// The stable.
		public const int StartSatiety = 70;
		public const int StartCleanliness = 70;
		public const int StartEnergy = 80;
		public const int StartTrust = 40;
		public const string StartHorseName = "Коник";
		public static readonly int[] HorseLevelXp = { 0, 100, 250, 450, 700 };
		public const int HayCost = 10;
		public const double MaxOfflineSeconds = 8.0 * 3600.0;
		public const double TrustCooldownSeconds = 30.0;
		public const int TrainingSkillXp = 5;
		public const int TrainingFirstBonus = 10;
	}

	/// <summary>Stable ids of professions, upgrades, trainings and the shared choices.</summary>
	public static class Ids
	{
		public const string Stable = "stable";
		public const string Scribe = "scribe";
		public const string Forge = "forge";
		public const string Workshop = "workshop";
		public const string Cooking = "cooking";
		public const string Tailor = "tailor";
		public const string Apothecary = "apothecary";

		public const string Obstacles = "STA-OBS";
		public const string Commands = "STA-CMD";

		public const string Guide = "UP_GUIDE";
		public const string Bench = "UP_BENCH";
		public const string Brush = "UP_BRUSH";

		public const string Gentle = "GENTLE";
		public const string Quick = "QUICK";
		public const string Wool = "WOOL";
		public const string Linen = "LINEN";
		public const string Sun = "SUN";
		public const string Moon = "MOON";
		public const string Leaf = "LEAF";
		public const string Accept = "ACCEPT";
		public const string Flag = "FLAG";
		public const string Academy = "ACADEMY";
		public const string Village = "VILLAGE";
		public const string Merchant = "MERCHANT";
	}

	public sealed class ProfessionDef
	{
		public string Id;
		/// <summary>Whether it has commissions (the stable has two training modes instead).</summary>
		public bool HasJobs;
	}

	public sealed class UpgradeDef
	{
		public string Id;
		public int Price;
	}

	/// <summary>
	/// One authored commission. Which fields are used depends on the profession; the answer keys are developer data and are never shown
	/// before the result. Every id is English and stable, every shown text is looked up in <see cref="Loc"/>.
	/// </summary>
	public sealed class JobDef
	{
		public string Id;
		public string Profession;
		public int BaseCoins;
		public int GlobalXp;
		public int SkillXp;

		// Scribe: the two documents, the key, the mark and the seal.
		public string Source;
		public string Reference;
		public string Answer;
		public string SealId;
		public Vector2 SealTarget;
		/// <summary>The path to trace: the scribe's mark or the tailor's pattern.</summary>
		public string PathId;

		// Forge, workshop, cooking, apothecary.
		public float Target;
		public Vector2[] Hammer;
		/// <summary>What is offered: parts, ingredients or herbs.</summary>
		public string[] Options;
		/// <summary>The right answer: the parts in order, or the right set of ingredients or herbs.</summary>
		public string[] Correct;
		/// <summary>The finish (forge), the fabric (tailor) or the label (apothecary) the order asks for.</summary>
		public string Choice;
	}

	/// <summary>The authored game: seven professions, twelve commissions, three upgrades. Nothing is random.</summary>
	public static class MasterContent
	{
		public static readonly ProfessionDef[] Professions =
		{
			new ProfessionDef { Id = Ids.Stable },
			new ProfessionDef { Id = Ids.Scribe, HasJobs = true },
			new ProfessionDef { Id = Ids.Forge, HasJobs = true },
			new ProfessionDef { Id = Ids.Workshop, HasJobs = true },
			new ProfessionDef { Id = Ids.Cooking, HasJobs = true },
			new ProfessionDef { Id = Ids.Tailor, HasJobs = true },
			new ProfessionDef { Id = Ids.Apothecary, HasJobs = true },
		};

		public static readonly string[] Trainings = { Ids.Obstacles, Ids.Commands };

		public static readonly UpgradeDef[] Upgrades =
		{
			new UpgradeDef { Id = Ids.Guide, Price = 50 },
			new UpgradeDef { Id = Ids.Bench, Price = 80 },
			new UpgradeDef { Id = Ids.Brush, Price = 60 },
		};

		public static readonly string[] Herbs = { "MINT", "CHAMOMILE", "THYME", "ROSEMARY" };

		public static readonly JobDef[] Jobs =
		{
			new JobDef { Id = "SCR-01", Profession = Ids.Scribe, BaseCoins = 20, GlobalXp = 10, SkillXp = 15,
				Source = "Академія. Учнів: 8. День: 3.", Reference = "Журнал занять: день 3, присутні 8 учнів.", Answer = Ids.Accept,
				PathId = "stroke_arc", SealId = Ids.Academy, SealTarget = new Vector2(0.72f, 0.30f) },
			new JobDef { Id = "SCR-02", Profession = Ids.Scribe, BaseCoins = 28, GlobalXp = 14, SkillXp = 20,
				Source = "Відвантажено: 14 мішків зерна.", Reference = "Розписка візника: 12 мішків зерна.", Answer = Ids.Flag,
				PathId = "stroke_hook", SealId = Ids.Village, SealTarget = new Vector2(0.30f, 0.30f) },

			new JobDef { Id = "FOR-01", Profession = Ids.Forge, BaseCoins = 24, GlobalXp = 12, SkillXp = 15, Target = 0.45f, Choice = Ids.Gentle,
				Hammer = new[] { new Vector2(0.30f, 0.45f), new Vector2(0.50f, 0.35f), new Vector2(0.70f, 0.45f) } },
			new JobDef { Id = "FOR-02", Profession = Ids.Forge, BaseCoins = 34, GlobalXp = 16, SkillXp = 20, Target = 0.62f, Choice = Ids.Quick,
				Hammer = new[] { new Vector2(0.25f, 0.50f), new Vector2(0.50f, 0.50f), new Vector2(0.75f, 0.50f) } },

			new JobDef { Id = "WRK-01", Profession = Ids.Workshop, BaseCoins = 24, GlobalXp = 12, SkillXp = 15, Target = 0.50f,
				Options = new[] { "LEVER", "PIN", "SPRING", "GEAR" }, Correct = new[] { "PIN", "LEVER", "SPRING" } },
			new JobDef { Id = "WRK-02", Profession = Ids.Workshop, BaseCoins = 34, GlobalXp = 16, SkillXp = 20, Target = 0.65f,
				Options = new[] { "AXLE", "GEAR", "CRANK", "BOLT" }, Correct = new[] { "AXLE", "GEAR", "CRANK" } },

			new JobDef { Id = "CK-01", Profession = Ids.Cooking, BaseCoins = 20, GlobalXp = 10, SkillXp = 15, Target = 0.50f,
				Options = new[] { "WATER", "CARROT", "ONION", "APPLE" }, Correct = new[] { "WATER", "CARROT", "ONION" } },
			new JobDef { Id = "CK-02", Profession = Ids.Cooking, BaseCoins = 28, GlobalXp = 14, SkillXp = 20, Target = 0.60f,
				Options = new[] { "WATER", "BARLEY", "MILK", "SALT" }, Correct = new[] { "WATER", "BARLEY", "MILK" } },

			new JobDef { Id = "TLR-01", Profession = Ids.Tailor, BaseCoins = 22, GlobalXp = 11, SkillXp = 15, Choice = Ids.Wool, PathId = "cut_rectangle" },
			new JobDef { Id = "TLR-02", Profession = Ids.Tailor, BaseCoins = 32, GlobalXp = 15, SkillXp = 20, Choice = Ids.Linen, PathId = "cut_cape" },

			new JobDef { Id = "APT-01", Profession = Ids.Apothecary, BaseCoins = 20, GlobalXp = 10, SkillXp = 15, Target = 0.50f, Choice = Ids.Leaf,
				Options = Herbs, Correct = new[] { "MINT", "CHAMOMILE" } },
			new JobDef { Id = "APT-02", Profession = Ids.Apothecary, BaseCoins = 30, GlobalXp = 14, SkillXp = 20, Target = 0.65f, Choice = Ids.Sun,
				Options = Herbs, Correct = new[] { "THYME", "ROSEMARY" } },
		};

		public static JobDef FindJob(string id) => Jobs.FirstOrDefault(j => j.Id == id);
		public static ProfessionDef FindProfession(string id) => Professions.FirstOrDefault(p => p.Id == id);
		public static UpgradeDef FindUpgrade(string id) => Upgrades.FirstOrDefault(u => u.Id == id);
		public static IEnumerable<JobDef> JobsOf(string profession) => Jobs.Where(j => j.Profession == profession);

		/// <summary>The second commission of a profession opens after the first is completed.</summary>
		public static string PrerequisiteOf(string jobId)
		{
			var job = FindJob(jobId);
			var own = JobsOf(job.Profession).ToList();
			var index = own.IndexOf(job);
			return index > 0 ? own[index - 1].Id : null;
		}

		// ---- the authored paths, as reference points in a [0,1] x [0,1] panel (y up) ----

		static readonly Dictionary<string, Vector2[]> Paths = BuildPaths();

		public static Vector2[] Path(string id) => Paths[id];
		public static IEnumerable<string> PathIds => Paths.Keys;

		static Vector2[] Sample(int n, Func<float, Vector2> f)
		{
			var points = new Vector2[n];
			for (var i = 0; i < n; i++) points[i] = f(i / (float)(n - 1));
			return points;
		}

		static Vector2[] Join(params Vector2[][] parts) => parts.SelectMany(p => p).ToArray();

		static Vector2[] Line(Vector2 a, Vector2 b, int n) => Sample(n, t => Vector2.Lerp(a, b, t));

		static Dictionary<string, Vector2[]> BuildPaths()
		{
			var d = new Dictionary<string, Vector2[]>();
			d["stroke_arc"] = Sample(14, t => new Vector2(0.1f + 0.8f * t, 0.3f + 0.4f * Mathf.Sin(Mathf.PI * t)));
			d["stroke_hook"] = Join(
				Sample(6, t => new Vector2(0.32f, Mathf.Lerp(0.85f, 0.42f, t))),
				Sample(10, t =>
				{
					var angle = Mathf.Lerp(Mathf.PI, -0.2f, t);
					return new Vector2(0.47f + 0.15f * Mathf.Cos(angle), 0.42f - 0.18f * Mathf.Sin(angle));
				}));
			// A rounded rectangle for the pouch, drawn clockwise from the top left.
			d["cut_rectangle"] = Join(
				Line(new Vector2(0.22f, 0.8f), new Vector2(0.78f, 0.8f), 7),
				Line(new Vector2(0.8f, 0.76f), new Vector2(0.8f, 0.24f), 5),
				Line(new Vector2(0.78f, 0.2f), new Vector2(0.22f, 0.2f), 7),
				Line(new Vector2(0.2f, 0.24f), new Vector2(0.2f, 0.76f), 5));
			// A symmetric cape: a neck, angled shoulders, straight sides and a curved lower edge.
			d["cut_cape"] = Join(
				Line(new Vector2(0.42f, 0.85f), new Vector2(0.58f, 0.85f), 4),
				Line(new Vector2(0.58f, 0.85f), new Vector2(0.82f, 0.68f), 4),
				Line(new Vector2(0.82f, 0.68f), new Vector2(0.86f, 0.3f), 5),
				Sample(9, t => new Vector2(0.86f - 0.72f * t, 0.3f - 0.1f * Mathf.Sin(Mathf.PI * t))),
				Line(new Vector2(0.14f, 0.3f), new Vector2(0.18f, 0.68f), 5),
				Line(new Vector2(0.18f, 0.68f), new Vector2(0.42f, 0.85f), 4));
			return d;
		}
	}
}
