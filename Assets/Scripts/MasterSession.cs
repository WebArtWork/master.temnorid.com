using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Temnorid.Master
{
	public enum JobStatus
	{
		Locked,
		Available,
		Completed,
	}

	[Serializable]
	public sealed class HistoryEntry
	{
		public string professionId;
		public string id;
		public int quality;
		public int coins;
		public int xp;
		public long utcTicks;
	}

	/// <summary>What one committed result paid. The result screen only reads this; it never pays anything itself.</summary>
	public sealed class Receipt
	{
		public string AttemptId;
		public string Profession;
		public string Id;
		public int Quality;
		public int Coins;
		public int GlobalXp;
		public int SkillXp;
		public int HorseXp;
		public bool FirstTime;
		public int LevelBefore;
		public int LevelAfter;
		public int RankBefore;
		public int RankAfter;
		public int HorseLevelBefore;
		public int HorseLevelAfter;
		public bool Training;
	}

	/// <summary>
	/// The one authority over the game's state: the wallet, experience, ranks, the one active commission, the stable, the upgrades and the history.
	/// Screens read it and send commands. Every payout is a single transaction keyed by a unique attempt id, so nothing can be paid twice.
	/// </summary>
	public sealed class MasterSession
	{
		readonly Dictionary<string, int> _skill = MasterContent.Professions.ToDictionary(p => p.Id, p => 0);
		readonly List<string> _completed = new List<string>();
		readonly Dictionary<string, int> _best = new Dictionary<string, int>();
		readonly List<string> _processed = new List<string>();
		readonly List<string> _firstTrainings = new List<string>();
		readonly List<string> _upgrades = new List<string>();
		readonly List<string> _tutorials = new List<string>();
		readonly List<HistoryEntry> _history = new List<HistoryEntry>();
		int _counter;
		string _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);

		public int Coins { get; private set; } = MasterConfig.StartCoins;
		public int GlobalXp { get; private set; }
		public string SelectedProfession { get; set; } = Ids.Stable;
		public HorseState Horse { get; } = new HorseState();
		public DateTime LastUpdatedUtc { get; private set; } = DateTime.UtcNow;

		public string ActiveJobId { get; private set; }
		public string AttemptId { get; private set; }
		public AttemptData Data { get; } = new AttemptData();
		public TrainingRun ActiveTraining { get; private set; }
		public TaskOutcome LastOutcome { get; private set; }
		public Receipt LastReceipt { get; private set; }

		public IReadOnlyList<string> Completed => _completed;
		public IReadOnlyList<string> Upgrades => _upgrades;
		public IReadOnlyList<string> Processed => _processed;
		public IReadOnlyList<string> FirstTrainings => _firstTrainings;
		public IReadOnlyList<HistoryEntry> History => _history;
		public IReadOnlyList<string> TutorialsShown => _tutorials;

		public bool Has(string upgradeId) => _upgrades.Contains(upgradeId);
		public int BestScore(string id) => _best.TryGetValue(id, out var v) ? v : 0;
		public int SkillXp(string profession) => _skill.TryGetValue(profession, out var v) ? v : 0;
		public bool TrainingActive => ActiveTraining != null;

		// ---- levels and ranks ----

		public static int LevelFor(int xp)
		{
			var level = 1;
			for (var i = 1; i < MasterConfig.LevelXp.Length; i++) if (xp >= MasterConfig.LevelXp[i]) level = i + 1;
			return Math.Min(level, MasterConfig.MaxLevel);
		}

		/// <summary>0 Novice, 1 Apprentice, 2 Craftsman, 3 Master.</summary>
		public static int RankFor(int skillXp)
		{
			var rank = 0;
			for (var i = 1; i < MasterConfig.RankXp.Length; i++) if (skillXp >= MasterConfig.RankXp[i]) rank = i;
			return rank;
		}

		public int Level => LevelFor(GlobalXp);
		public int Rank(string profession) => RankFor(SkillXp(profession));
		public int CompletedCount => _completed.Count;

		// ---- commissions ----

		public JobStatus StatusOf(string jobId)
		{
			if (_completed.Contains(jobId)) return JobStatus.Completed;
			var pre = MasterContent.PrerequisiteOf(jobId);
			return pre == null || _completed.Contains(pre) ? JobStatus.Available : JobStatus.Locked;
		}

		public bool CanAccept(string jobId) => ActiveJobId == null && !TrainingActive && MasterContent.FindJob(jobId) != null && StatusOf(jobId) != JobStatus.Locked;

		string NewAttemptId() => _sessionId + "-" + (++_counter);

		/// <summary>Takes a commission (a completed one can be done again for less). Only one at a time, and never during a training.</summary>
		public bool Accept(string jobId)
		{
			if (!CanAccept(jobId)) return false;
			ActiveJobId = jobId;
			AttemptId = NewAttemptId();
			Data.Clear();
			LastOutcome = null;
			SelectedProfession = MasterContent.FindJob(jobId).Profession;
			return true;
		}

		/// <summary>After a rejection or on request: the same commission, nothing done yet, a new attempt id.</summary>
		public bool Retry()
		{
			if (ActiveJobId == null) return false;
			AttemptId = NewAttemptId();
			Data.Clear();
			LastOutcome = null;
			return true;
		}

		/// <summary>Drops only what was entered; the wallet, the completed list and the upgrades stay as they are.</summary>
		public void Abandon()
		{
			ActiveJobId = null;
			AttemptId = null;
			Data.Clear();
			LastOutcome = null;
		}

		public TaskOutcome Preview()
		{
			if (ActiveJobId == null) return null;
			return TaskScoring.Evaluate(MasterContent.FindJob(ActiveJobId), Data, _upgrades);
		}

		public bool ReadyToSubmit => ActiveJobId != null && Preview().Complete;

		/// <summary>
		/// Judges the active commission. A rejection pays nothing and costs nothing, and the commission stays active for a retry. A success is one transaction:
		/// the attempt id is marked, the reward computed and applied, the completion and best score recorded, and the commission closed, so pressing it twice pays once.
		/// </summary>
		public Receipt Submit()
		{
			if (ActiveJobId == null) return null;
			var job = MasterContent.FindJob(ActiveJobId);
			var outcome = TaskScoring.Evaluate(job, Data, _upgrades);
			if (!outcome.Complete) return null;
			LastOutcome = outcome;
			if (!outcome.Success) return null;
			if (_processed.Contains(AttemptId)) return null;

			var first = !_completed.Contains(job.Id);
			var receipt = new Receipt { AttemptId = AttemptId, Profession = job.Profession, Id = job.Id, Quality = outcome.Quality, FirstTime = first };
			if (first)
			{
				receipt.Coins = Math.Max(1, (int)Math.Floor(job.BaseCoins * outcome.Quality / 100.0));
				receipt.GlobalXp = job.GlobalXp;
				receipt.SkillXp = job.SkillXp;
			}
			else
			{
				receipt.Coins = Math.Max(1, (int)Math.Floor(job.BaseCoins * MasterConfig.RepeatCoinFactor * outcome.Quality / 100.0 + 1e-9));
				receipt.GlobalXp = Math.Max(1, (int)Math.Floor(job.GlobalXp * MasterConfig.RepeatXpFactor));
				receipt.SkillXp = MasterConfig.RepeatSkillXp;
			}
			receipt.LevelBefore = Level;
			receipt.RankBefore = Rank(job.Profession);
			_processed.Add(AttemptId);
			Coins += receipt.Coins;
			GlobalXp += receipt.GlobalXp;
			_skill[job.Profession] = SkillXp(job.Profession) + receipt.SkillXp;
			if (first) _completed.Add(job.Id);
			if (outcome.Quality > BestScore(job.Id)) _best[job.Id] = outcome.Quality;
			receipt.LevelAfter = Level;
			receipt.RankAfter = Rank(job.Profession);
			AddHistory(job.Profession, job.Id, receipt.Quality, receipt.Coins, receipt.GlobalXp);
			LastReceipt = receipt;
			ActiveJobId = null;
			AttemptId = null;
			Data.Clear();
			return receipt;
		}

		void AddHistory(string profession, string id, int quality, int coins, int xp)
		{
			_history.Add(new HistoryEntry { professionId = profession, id = id, quality = quality, coins = coins, xp = xp, utcTicks = DateTime.UtcNow.Ticks });
			while (_history.Count > MasterConfig.HistoryLength) _history.RemoveAt(0);
		}

		// ---- upgrades ----

		public bool CanBuy(string upgradeId)
		{
			var u = MasterContent.FindUpgrade(upgradeId);
			return u != null && !Has(upgradeId) && Coins >= u.Price;
		}

		/// <summary>Bought once and charged once; the wallet never goes below zero.</summary>
		public bool Buy(string upgradeId)
		{
			if (!CanBuy(upgradeId)) return false;
			Coins -= MasterContent.FindUpgrade(upgradeId).Price;
			_upgrades.Add(upgradeId);
			return true;
		}

		public bool TutorialShown(string profession) => _tutorials.Contains(profession);

		public void MarkTutorial(string profession)
		{
			if (!_tutorials.Contains(profession)) _tutorials.Add(profession);
		}

		// ---- the stable: time ----

		/// <summary>Time passing while the game is open.</summary>
		public void Tick(double seconds) => Horse.Tick(seconds);

		/// <summary>
		/// Time away: up to 8 hours since the last time this was applied, counted once. A clock set backwards counts as none.
		/// Calling it again with the same moment changes nothing, so reloading a scene cannot apply it twice.
		/// </summary>
		public void ApplyElapsed(DateTime utcNow)
		{
			var seconds = (utcNow - LastUpdatedUtc).TotalSeconds;
			Horse.Tick(Math.Min(Math.Max(0.0, seconds), MasterConfig.MaxOfflineSeconds));
			if (utcNow > LastUpdatedUtc) LastUpdatedUtc = utcNow;
		}

		/// <summary>Marks "now" as applied, for the time the game has been open and ticked.</summary>
		public void Touch(DateTime utcNow) => LastUpdatedUtc = utcNow > LastUpdatedUtc ? utcNow : LastUpdatedUtc;

		// ---- the stable: care ----

		CareResult Blocked() => TrainingActive ? CareResult.Fail("care.training") : null;

		public CareResult FeedHay()
		{
			var blocked = Blocked();
			if (blocked != null) return blocked;
			if (Coins < MasterConfig.HayCost) return CareResult.Fail("care.coins");
			Coins -= MasterConfig.HayCost;
			Horse.AddSatiety(30);
			Horse.AddTrust(2);
			return new CareResult { Ok = true, Message = "care.hay", Anim = HorseAnim.Eat, Seconds = 2 };
		}

		/// <summary>The free ration: always there, so a poor player is never stuck.</summary>
		public CareResult FeedRation()
		{
			var blocked = Blocked();
			if (blocked != null) return blocked;
			Horse.AddSatiety(15);
			return new CareResult { Ok = true, Message = "care.ration", Anim = HorseAnim.Eat, Seconds = 2 };
		}

		public CareResult Groom(DateTime utcNow)
		{
			var blocked = Blocked();
			if (blocked != null) return blocked;
			Horse.AddCleanliness(Has(Ids.Brush) ? 35 : 25);
			var trust = utcNow.Ticks >= Horse.GroomTrustReadyTicks;
			if (trust)
			{
				Horse.AddTrust(3);
				Horse.GroomTrustReadyTicks = utcNow.Ticks + TimeSpan.FromSeconds(MasterConfig.TrustCooldownSeconds).Ticks;
			}
			return new CareResult { Ok = true, Message = trust ? "care.groom.trust" : "care.groom", Anim = HorseAnim.Groom, Seconds = 3 };
		}

		public CareResult Pet(DateTime utcNow)
		{
			var blocked = Blocked();
			if (blocked != null) return blocked;
			var trust = utcNow.Ticks >= Horse.PetReadyTicks;
			if (trust)
			{
				Horse.AddTrust(5);
				Horse.PetReadyTicks = utcNow.Ticks + TimeSpan.FromSeconds(MasterConfig.TrustCooldownSeconds).Ticks;
			}
			return new CareResult { Ok = true, Message = trust ? "care.pet.trust" : "care.pet", Anim = HorseAnim.Pet, Seconds = 2 };
		}

		public CareResult Rest()
		{
			var blocked = Blocked();
			if (blocked != null) return blocked;
			Horse.AddEnergy(40);
			return new CareResult { Ok = true, Message = "care.rest", Anim = HorseAnim.Sleep, Seconds = 8 };
		}

		// ---- the stable: training ----

		/// <summary>Why a training cannot start now: a key of <see cref="Loc"/>, or null when it can.</summary>
		public string TrainingBlock(string kind)
		{
			if (TrainingActive) return "train.busy";
			if (ActiveJobId != null) return "train.job";
			if (Horse.Energy < TrainingRules.EnergyCost(kind)) return "train.energy";
			if (Horse.Satiety < 20) return "train.satiety";
			return null;
		}

		/// <summary>Starts a training run. The energy is paid here, once, and the run has an id that can be paid only once.</summary>
		public TrainingRun StartTraining(string kind)
		{
			if (!MasterContent.Trainings.Contains(kind) || TrainingBlock(kind) != null) return null;
			Horse.AddEnergy(-TrainingRules.EnergyCost(kind));
			ActiveTraining = new TrainingRun { RunId = "T" + NewAttemptId(), Kind = kind, Dirty = Horse.Cleanliness < 20 };
			return ActiveTraining;
		}

		/// <summary>Leaves a run early: nothing is paid and the energy stays spent.</summary>
		public void AbortTraining() => ActiveTraining = null;

		/// <summary>Pays a finished run once: cleared hurdles (0 to 8) or hits (0 to 5). The experience for the player and the horse is the same number.</summary>
		public Receipt CompleteTraining(string runId, int result)
		{
			if (ActiveTraining == null || ActiveTraining.RunId != runId || _processed.Contains(runId)) return null;
			var run = ActiveTraining;
			result = Math.Max(0, Math.Min(TrainingRules.Max(run.Kind), result));
			var receipt = new Receipt
			{
				AttemptId = runId,
				Profession = Ids.Stable,
				Id = run.Kind,
				Training = true,
				Quality = TrainingRules.Quality(run.Kind, result),
				Coins = TrainingRules.Coins(run.Kind, result, run.Dirty),
				GlobalXp = TrainingRules.Xp(run.Kind, result),
				LevelBefore = Level,
				RankBefore = Rank(Ids.Stable),
				HorseLevelBefore = Horse.Level,
			};
			receipt.HorseXp = receipt.GlobalXp;
			receipt.FirstTime = !_firstTrainings.Contains(run.Kind);
			receipt.SkillXp = MasterConfig.TrainingSkillXp + (receipt.FirstTime ? MasterConfig.TrainingFirstBonus : 0);
			_processed.Add(runId);
			if (receipt.FirstTime) _firstTrainings.Add(run.Kind);
			Coins += receipt.Coins;
			GlobalXp += receipt.GlobalXp;
			Horse.Xp += receipt.HorseXp;
			_skill[Ids.Stable] = SkillXp(Ids.Stable) + receipt.SkillXp;
			if (receipt.Quality > BestScore(run.Kind)) _best[run.Kind] = receipt.Quality;
			receipt.LevelAfter = Level;
			receipt.RankAfter = Rank(Ids.Stable);
			receipt.HorseLevelAfter = Horse.Level;
			AddHistory(Ids.Stable, run.Kind, receipt.Quality, receipt.Coins, receipt.GlobalXp);
			LastReceipt = receipt;
			ActiveTraining = null;
			return receipt;
		}

		// ---- saving ----

		public MasterSave ToSave(DateTime utcNow)
		{
			var skillIds = _skill.Keys.ToArray();
			var bestIds = _best.Keys.ToArray();
			return new MasterSave
			{
				saveVersion = MasterConfig.SaveVersion,
				coins = Coins,
				globalXp = GlobalXp,
				masterLevel = Level,
				selectedProfessionId = SelectedProfession,
				skillIds = skillIds,
				skillValues = skillIds.Select(id => _skill[id]).ToArray(),
				completedCommissionIds = _completed.ToArray(),
				bestIds = bestIds,
				bestValues = bestIds.Select(id => _best[id]).ToArray(),
				activeCommissionId = ActiveJobId ?? "",
				attemptId = AttemptId ?? "",
				dataKeys = Data.All.Select(p => p.Key).ToArray(),
				dataValues = Data.All.Select(p => p.Value).ToArray(),
				processedAttemptIds = _processed.ToArray(),
				firstCompletedTrainingIds = _firstTrainings.ToArray(),
				purchasedUpgradeIds = _upgrades.ToArray(),
				tutorialsShown = _tutorials.ToArray(),
				attemptCounter = _counter,
				sessionId = _sessionId,
				horseName = Horse.Name,
				satiety = Horse.SatietyExact,
				cleanliness = Horse.CleanlinessExact,
				energy = Horse.EnergyExact,
				trust = Horse.TrustExact,
				horseXp = Horse.Xp,
				groomTrustReadyTicks = Horse.GroomTrustReadyTicks,
				petReadyTicks = Horse.PetReadyTicks,
				lastUpdatedUtcTicks = (utcNow > LastUpdatedUtc ? utcNow : LastUpdatedUtc).Ticks,
				history = _history.ToArray(),
			};
		}

		/// <summary>
		/// Rebuilds the state from a save. Anything missing gets its start value, anything unknown (a commission or upgrade this version does not have)
		/// is ignored, and numbers are brought back into range, so an old or edited save never breaks the game or the wallet.
		/// </summary>
		public void Load(MasterSave s)
		{
			if (s == null) return;
			Coins = Math.Max(0, s.coins);
			GlobalXp = Math.Max(0, s.globalXp);
			SelectedProfession = MasterContent.FindProfession(s.selectedProfessionId) != null ? s.selectedProfessionId : Ids.Stable;
			foreach (var p in MasterContent.Professions) _skill[p.Id] = 0;
			if (s.skillIds != null && s.skillValues != null)
				for (var i = 0; i < Math.Min(s.skillIds.Length, s.skillValues.Length); i++)
					if (_skill.ContainsKey(s.skillIds[i])) _skill[s.skillIds[i]] = Math.Max(0, s.skillValues[i]);
			_completed.Clear();
			foreach (var id in s.completedCommissionIds ?? new string[0]) if (MasterContent.FindJob(id) != null && !_completed.Contains(id)) _completed.Add(id);
			_best.Clear();
			if (s.bestIds != null && s.bestValues != null)
				for (var i = 0; i < Math.Min(s.bestIds.Length, s.bestValues.Length); i++)
					if (MasterContent.FindJob(s.bestIds[i]) != null || MasterContent.Trainings.Contains(s.bestIds[i])) _best[s.bestIds[i]] = Math.Max(0, Math.Min(100, s.bestValues[i]));
			_processed.Clear();
			_processed.AddRange((s.processedAttemptIds ?? new string[0]).Distinct());
			_firstTrainings.Clear();
			foreach (var id in s.firstCompletedTrainingIds ?? new string[0]) if (MasterContent.Trainings.Contains(id) && !_firstTrainings.Contains(id)) _firstTrainings.Add(id);
			_upgrades.Clear();
			foreach (var id in s.purchasedUpgradeIds ?? new string[0]) if (MasterContent.FindUpgrade(id) != null && !_upgrades.Contains(id)) _upgrades.Add(id);
			_tutorials.Clear();
			foreach (var id in s.tutorialsShown ?? new string[0]) if (MasterContent.FindProfession(id) != null && !_tutorials.Contains(id)) _tutorials.Add(id);
			_counter = Math.Max(0, s.attemptCounter);
			_sessionId = string.IsNullOrEmpty(s.sessionId) ? _sessionId : s.sessionId;

			Horse.Name = string.IsNullOrWhiteSpace(s.horseName) ? MasterConfig.StartHorseName : s.horseName.Trim();
			Horse.SetNeeds(s.satiety, s.cleanliness, s.energy, s.trust);
			Horse.Xp = Math.Max(0, s.horseXp);
			Horse.GroomTrustReadyTicks = s.groomTrustReadyTicks;
			Horse.PetReadyTicks = s.petReadyTicks;
			LastUpdatedUtc = s.lastUpdatedUtcTicks > 0 && s.lastUpdatedUtcTicks < DateTime.MaxValue.Ticks ? new DateTime(s.lastUpdatedUtcTicks, DateTimeKind.Utc) : DateTime.UtcNow;

			_history.Clear();
			foreach (var h in s.history ?? new HistoryEntry[0]) if (h != null && (MasterContent.FindJob(h.id) != null || MasterContent.Trainings.Contains(h.id))) _history.Add(h);
			while (_history.Count > MasterConfig.HistoryLength) _history.RemoveAt(0);

			ActiveTraining = null;
			Data.Clear();
			ActiveJobId = null;
			AttemptId = null;
			LastOutcome = null;
			var job = MasterContent.FindJob(s.activeCommissionId);
			if (job != null && StatusOf(job.Id) != JobStatus.Locked && !string.IsNullOrEmpty(s.attemptId) && !_processed.Contains(s.attemptId))
			{
				ActiveJobId = job.Id;
				AttemptId = s.attemptId;
				if (s.dataKeys != null && s.dataValues != null)
					for (var i = 0; i < Math.Min(s.dataKeys.Length, s.dataValues.Length); i++) Data.Set(s.dataKeys[i], s.dataValues[i]);
			}
		}
	}

	/// <summary>What is written to disk: plain ids, flags and numbers, never scene objects. Sets are lists and ids are strings, so an enum can be reordered safely.</summary>
	[Serializable]
	public sealed class MasterSave
	{
		public int saveVersion;
		public int coins;
		public int globalXp;
		public int masterLevel;
		public string selectedProfessionId;
		public string[] skillIds;
		public int[] skillValues;
		public string[] completedCommissionIds;
		public string[] bestIds;
		public int[] bestValues;
		public string activeCommissionId;
		public string attemptId;
		public string[] dataKeys;
		public string[] dataValues;
		public string[] processedAttemptIds;
		public string[] firstCompletedTrainingIds;
		public string[] purchasedUpgradeIds;
		public string[] tutorialsShown;
		public int attemptCounter;
		public string sessionId;
		public string horseName;
		public double satiety = MasterConfig.StartSatiety;
		public double cleanliness = MasterConfig.StartCleanliness;
		public double energy = MasterConfig.StartEnergy;
		public double trust = MasterConfig.StartTrust;
		public int horseXp;
		public long groomTrustReadyTicks;
		public long petReadyTicks;
		public long lastUpdatedUtcTicks;
		public HistoryEntry[] history;
	}

	/// <summary>Volume, window mode: kept apart from the progress so they are remembered from the title screen on.</summary>
	[Serializable]
	public sealed class MasterSettings
	{
		public float masterVolume = 0.8f;
		public float sfxVolume = 0.9f;
		public bool fullscreen = true;
	}
}
