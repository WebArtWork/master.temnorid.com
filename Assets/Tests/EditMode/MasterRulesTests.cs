using System;
using ITKamianets.Engine.Save;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Temnorid.Master.Tests
{
	public sealed class MasterRulesTests
	{
		static readonly DateTime T0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		static void FillScribe(MasterSession s, string choice, int trace, string seal, Vector2 pos)
		{
			s.Data.Set("choice", choice);
			s.Data.Set("trace", trace);
			s.Data.Set("seal", seal);
			s.Data.Set("sealpos", pos);
		}

		static MasterSession Scribe01(int trace = 100)
		{
			var s = new MasterSession();
			Assert.IsTrue(s.Accept("SCR-01"));
			FillScribe(s, Ids.Accept, trace, Ids.Academy, new Vector2(0.72f, 0.30f));
			return s;
		}

		[Test]
		public void StartState()
		{
			var s = new MasterSession();
			Assert.AreEqual(100, s.Coins);
			Assert.AreEqual(1, s.Level);
			Assert.AreEqual(70, s.Horse.Satiety);
			Assert.AreEqual(70, s.Horse.Cleanliness);
			Assert.AreEqual(80, s.Horse.Energy);
			Assert.AreEqual(40, s.Horse.Trust);
			Assert.AreEqual(12, MasterContent.Jobs.Length);
			Assert.AreEqual(7, MasterContent.Professions.Length);
		}

		[Test]
		public void SecondJobLockedUntilFirstDone()
		{
			var s = new MasterSession();
			Assert.AreEqual(JobStatus.Available, s.StatusOf("SCR-01"));
			Assert.AreEqual(JobStatus.Locked, s.StatusOf("SCR-02"));
			Assert.IsFalse(s.Accept("SCR-02"));
		}

		[Test]
		public void ScribeFirstPayout()
		{
			var s = Scribe01();
			var r = s.Submit();
			Assert.IsNotNull(r);
			Assert.AreEqual(100, r.Quality);
			Assert.AreEqual(20, r.Coins);
			Assert.AreEqual(100 + 20, s.Coins);
			Assert.AreEqual(15, s.SkillXp(Ids.Scribe));
			Assert.AreEqual(10, s.GlobalXp);
			Assert.AreEqual(JobStatus.Completed, s.StatusOf("SCR-01"));
			Assert.AreEqual(JobStatus.Available, s.StatusOf("SCR-02"));
			Assert.IsNull(s.ActiveJobId);
		}

		[Test]
		public void ScribeQuality90Pays18()
		{
			var s = Scribe01(67);
			var r = s.Submit();
			Assert.AreEqual(90, r.Quality);
			Assert.AreEqual(18, r.Coins);
			Assert.AreEqual(10, r.GlobalXp);
			Assert.AreEqual(15, r.SkillXp);
		}

		[Test]
		public void ReplayPaysLess()
		{
			var s = Scribe01(67);
			s.Submit();
			s.Accept("SCR-01");
			FillScribe(s, Ids.Accept, 67, Ids.Academy, new Vector2(0.72f, 0.30f));
			var r = s.Submit();
			Assert.IsFalse(r.FirstTime);
			Assert.AreEqual(6, r.Coins);
			Assert.AreEqual(2, r.GlobalXp);
			Assert.AreEqual(5, r.SkillXp);
		}

		[Test]
		public void ReplayOfAPerfectResultPaysExactlySevenCoins()
		{
			var s = Scribe01();
			s.Submit();
			s.Accept("SCR-01");
			FillScribe(s, Ids.Accept, 100, Ids.Academy, new Vector2(0.72f, 0.30f));
			Assert.AreEqual(7, s.Submit().Coins);
		}

		[Test]
		public void WrongAnswerRejectedWithoutPay()
		{
			var s = new MasterSession();
			s.Accept("SCR-01");
			FillScribe(s, Ids.Flag, 100, Ids.Academy, new Vector2(0.72f, 0.30f));
			Assert.IsNull(s.Submit());
			Assert.AreEqual(100, s.Coins);
			Assert.IsFalse(s.LastOutcome.Success);
			Assert.Contains("why.inspect", s.LastOutcome.Reasons);
			Assert.AreEqual("SCR-01", s.ActiveJobId);
		}

		[Test]
		public void WrongSealOrPlaceRejected()
		{
			var s = new MasterSession();
			s.Accept("SCR-01");
			FillScribe(s, Ids.Accept, 100, Ids.Village, new Vector2(0.72f, 0.30f));
			Assert.IsNull(s.Submit());
			s.Retry();
			FillScribe(s, Ids.Accept, 100, Ids.Academy, new Vector2(0.10f, 0.90f));
			Assert.IsNull(s.Submit());
			Assert.Contains("why.seal", s.LastOutcome.Reasons);
		}

		[Test]
		public void BadTraceRejected()
		{
			var s = Scribe01(40);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.trace", s.LastOutcome.Reasons);
		}

		[Test]
		public void IncompleteTaskNotSubmittable()
		{
			var s = new MasterSession();
			s.Accept("SCR-01");
			s.Data.Set("choice", Ids.Accept);
			Assert.IsFalse(s.ReadyToSubmit);
			Assert.IsNull(s.Submit());
		}

		[Test]
		public void OneActiveCommission()
		{
			var s = new MasterSession();
			Assert.IsTrue(s.Accept("SCR-01"));
			Assert.IsFalse(s.Accept("FOR-01"));
		}

		[Test]
		public void AbandonKeepsWalletAndProgress()
		{
			var s = Scribe01();
			s.Submit();
			s.Buy(Ids.Guide);
			var coins = s.Coins;
			s.Accept("SCR-02");
			s.Data.Set("choice", Ids.Flag);
			s.Abandon();
			Assert.AreEqual(coins, s.Coins);
			Assert.IsNull(s.ActiveJobId);
			Assert.AreEqual(0, s.Data.Count);
			Assert.IsTrue(s.Has(Ids.Guide));
			Assert.AreEqual(JobStatus.Completed, s.StatusOf("SCR-01"));
		}

		[Test]
		public void SubmitTwicePaysOnce()
		{
			var s = Scribe01();
			Assert.IsNotNull(s.Submit());
			Assert.IsNull(s.Submit());
			Assert.AreEqual(120, s.Coins);
		}

		[Test]
		public void ForgePerfectAndWrongFinish()
		{
			var s = new MasterSession();
			s.Accept("FOR-01");
			var job = MasterContent.FindJob("FOR-01");
			s.Data.Set("heat", 0.45f);
			for (var i = 0; i < 3; i++) s.Data.Set("h" + i, job.Hammer[i]);
			s.Data.Set("finish", Ids.Quick);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.finish", s.LastOutcome.Reasons);
			s.Retry();
			s.Data.Set("heat", 0.45f);
			for (var i = 0; i < 3; i++) s.Data.Set("h" + i, job.Hammer[i]);
			s.Data.Set("finish", Ids.Gentle);
			var r = s.Submit();
			Assert.AreEqual(100, r.Quality);
			Assert.AreEqual(24, r.Coins);
		}

		[Test]
		public void ForgeLowQualityRejected()
		{
			var s = new MasterSession();
			s.Accept("FOR-01");
			s.Data.Set("heat", 0.95f);
			for (var i = 0; i < 3; i++) s.Data.Set("h" + i, new Vector2(0.9f, 0.9f));
			s.Data.Set("finish", Ids.Gentle);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.low", s.LastOutcome.Reasons);
		}

		[Test]
		public void WorkshopOrderMatters()
		{
			var s = new MasterSession();
			s.Accept("WRK-01");
			s.Data.Set("s0", "LEVER");
			s.Data.Set("s1", "PIN");
			s.Data.Set("s2", "SPRING");
			s.Data.Set("test", 0.5f);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.parts", s.LastOutcome.Reasons);
			s.Retry();
			s.Data.Set("s0", "PIN");
			s.Data.Set("s1", "LEVER");
			s.Data.Set("s2", "SPRING");
			s.Data.Set("test", 0.5f);
			var r = s.Submit();
			Assert.AreEqual(100, r.Quality);
		}

		[Test]
		public void CookingAnyOrderButExactSet()
		{
			var s = new MasterSession();
			s.Accept("CK-01");
			s.Data.SetList("ing", new[] { "ONION", "WATER", "APPLE" });
			s.Data.Set("heat", 0.5f);
			s.Data.Set("stir", 3);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.set", s.LastOutcome.Reasons);
			s.Retry();
			s.Data.SetList("ing", new[] { "ONION", "WATER", "CARROT" });
			s.Data.Set("heat", 0.5f);
			s.Data.Set("stir", 3);
			Assert.AreEqual(100, s.Submit().Quality);
		}

		[Test]
		public void TailorFabricAndCut()
		{
			var s = new MasterSession();
			s.Accept("TLR-01");
			s.Data.Set("fabric", Ids.Linen);
			s.Data.Set("cut", 100);
			s.Data.Set("stitch", 3);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.fabric", s.LastOutcome.Reasons);
			s.Retry();
			s.Data.Set("fabric", Ids.Wool);
			s.Data.Set("cut", 30);
			s.Data.Set("stitch", 3);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.cut", s.LastOutcome.Reasons);
			s.Retry();
			s.Data.Set("fabric", Ids.Wool);
			s.Data.Set("cut", 100);
			s.Data.Set("stitch", 3);
			Assert.AreEqual(100, s.Submit().Quality);
		}

		[Test]
		public void ApothecaryHerbsAndLabel()
		{
			var s = new MasterSession();
			s.Accept("APT-01");
			s.Data.SetList("herbs", new[] { "MINT", "THYME" });
			s.Data.Set("grind", 0.5f);
			s.Data.Set("label", Ids.Leaf);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.herbs", s.LastOutcome.Reasons);
			s.Retry();
			s.Data.SetList("herbs", new[] { "CHAMOMILE", "MINT" });
			s.Data.Set("grind", 0.5f);
			s.Data.Set("label", Ids.Moon);
			Assert.IsNull(s.Submit());
			Assert.Contains("why.label", s.LastOutcome.Reasons);
			s.Retry();
			s.Data.SetList("herbs", new[] { "CHAMOMILE", "MINT" });
			s.Data.Set("grind", 0.5f);
			s.Data.Set("label", Ids.Leaf);
			Assert.AreEqual(100, s.Submit().Quality);
		}

		[Test]
		public void BenchAddsOnlyAfterPassing()
		{
			var s = new MasterSession();
			s.Buy(Ids.Bench);
			s.Accept("CK-01");
			s.Data.SetList("ing", new[] { "WATER", "CARROT", "ONION" });
			s.Data.Set("heat", 0.95f);
			s.Data.Set("stir", 1);
			var r = s.Submit();
			var heat = TaskScoring.CookHeatScore(0.95f, 0.5f);
			var stir = TaskScoring.StirScore(1);
			Assert.AreEqual(Mathf.RoundToInt(60f + 0.2f * heat + 0.2f * stir) + 5, r.Quality);
			var t = new MasterSession();
			t.Buy(Ids.Bench);
			t.Accept("CK-01");
			t.Data.SetList("ing", new[] { "WATER", "CARROT", "APPLE" });
			t.Data.Set("heat", 0.5f);
			t.Data.Set("stir", 3);
			Assert.IsNull(t.Submit());
			Assert.AreEqual(0, t.LastOutcome.Quality);
		}

		[Test]
		public void GuideBonusIsCappedAndNeverFixesAnswers()
		{
			Assert.AreEqual(100, TaskScoring.WithGuide(95, true));
			Assert.AreEqual(60, TaskScoring.WithGuide(50, true));
			var s = new MasterSession();
			s.Buy(Ids.Guide);
			s.Accept("SCR-01");
			FillScribe(s, Ids.Flag, 100, Ids.Academy, new Vector2(0.72f, 0.30f));
			Assert.IsNull(s.Submit());
		}

		[Test]
		public void UpgradeBoughtOnceAndNeverNegative()
		{
			var s = new MasterSession();
			Assert.IsTrue(s.Buy(Ids.Brush));
			Assert.AreEqual(40, s.Coins);
			Assert.IsFalse(s.Buy(Ids.Brush));
			Assert.IsFalse(s.Buy(Ids.Bench));
			Assert.AreEqual(40, s.Coins);
		}

		[Test]
		public void LevelsAndRanks()
		{
			Assert.AreEqual(1, MasterSession.LevelFor(74));
			Assert.AreEqual(2, MasterSession.LevelFor(75));
			Assert.AreEqual(5, MasterSession.LevelFor(5000));
			Assert.AreEqual(0, MasterSession.RankFor(14));
			Assert.AreEqual(1, MasterSession.RankFor(15));
			Assert.AreEqual(3, MasterSession.RankFor(70));
		}

		[Test]
		public void TraceScoring()
		{
			var pts = MasterContent.Path("stroke_arc");
			Assert.AreEqual(100, TraceMath.Score(pts, pts));
			Assert.AreEqual(100, TraceMath.Score(pts.Reverse().ToArray(), pts));
			var off = pts.Select(p => p + new Vector2(0.3f, 0f)).ToArray();
			Assert.AreEqual(0, TraceMath.Score(off, pts));
		}

		[Test]
		public void TimingMarkerStaysInRange()
		{
			for (var t = 0f; t < 20f; t += 0.07f)
			{
				var m = TimingMath.Marker(t, 0.8f);
				Assert.GreaterOrEqual(m, 0f);
				Assert.LessOrEqual(m, 1f);
			}
			Assert.IsTrue(TimingMath.Hit(0.35f, 0.35f, 0.1f));
			Assert.IsFalse(TimingMath.Hit(0.5f, 0.35f, 0.1f));
		}

		// ---- the stable ----

		[Test]
		public void ObstacleRewards()
		{
			var s = new MasterSession();
			var run = s.StartTraining(Ids.Obstacles);
			Assert.AreEqual(60, s.Horse.Energy);
			var r = s.CompleteTraining(run.RunId, 8);
			Assert.AreEqual(26, r.Coins);
			Assert.AreEqual(34, r.GlobalXp);
			Assert.AreEqual(15, r.SkillXp);
			Assert.AreEqual(34, s.Horse.Xp);
			Assert.AreEqual(126, s.Coins);
		}

		[Test]
		public void CommandRewardsAndSkillXpDrops()
		{
			var s = new MasterSession();
			var run = s.StartTraining(Ids.Commands);
			var r = s.CompleteTraining(run.RunId, 5);
			Assert.AreEqual(23, r.Coins);
			Assert.AreEqual(28, r.GlobalXp);
			Assert.AreEqual(15, r.SkillXp);
			var run2 = s.StartTraining(Ids.Commands);
			Assert.AreEqual(5, s.CompleteTraining(run2.RunId, 5).SkillXp);
		}

		[Test]
		public void TrainingPaysOnce()
		{
			var s = new MasterSession();
			var run = s.StartTraining(Ids.Obstacles);
			Assert.IsNotNull(s.CompleteTraining(run.RunId, 4));
			Assert.IsNull(s.CompleteTraining(run.RunId, 4));
		}

		[Test]
		public void DirtyHorseEarnsEightyPercent()
		{
			var s = new MasterSession();
			s.Horse.SetNeeds(70, 10, 80, 40);
			var run = s.StartTraining(Ids.Obstacles);
			Assert.IsTrue(run.Dirty);
			Assert.AreEqual(20, s.CompleteTraining(run.RunId, 8).Coins);
		}

		[Test]
		public void TrainingBlocked()
		{
			var s = new MasterSession();
			s.Horse.SetNeeds(70, 70, 15, 40);
			Assert.AreEqual("train.energy", s.TrainingBlock(Ids.Obstacles));
			Assert.IsNull(s.TrainingBlock(Ids.Commands));
			s.Horse.SetNeeds(10, 70, 80, 40);
			Assert.AreEqual("train.satiety", s.TrainingBlock(Ids.Obstacles));
			s.Horse.SetNeeds(70, 70, 80, 40);
			s.Accept("SCR-01");
			Assert.AreEqual("train.job", s.TrainingBlock(Ids.Obstacles));
			s.Abandon();
			s.StartTraining(Ids.Obstacles);
			Assert.AreEqual("train.busy", s.TrainingBlock(Ids.Commands));
			Assert.IsFalse(s.Accept("SCR-01"));
			Assert.AreEqual("care.training", s.FeedRation().Message);
		}

		[Test]
		public void CareEffects()
		{
			var s = new MasterSession();
			s.Horse.SetNeeds(50, 50, 50, 40);
			Assert.IsTrue(s.FeedHay().Ok);
			Assert.AreEqual(90, s.Coins);
			Assert.AreEqual(80, s.Horse.Satiety);
			Assert.AreEqual(42, s.Horse.Trust);
			s.FeedRation();
			Assert.AreEqual(95, s.Horse.Satiety);
			s.Groom(T0);
			Assert.AreEqual(75, s.Horse.Cleanliness);
			Assert.AreEqual(45, s.Horse.Trust);
			s.Rest();
			Assert.AreEqual(90, s.Horse.Energy);
		}

		[Test]
		public void BrushImprovesGrooming()
		{
			var s = new MasterSession();
			s.Buy(Ids.Brush);
			s.Horse.SetNeeds(50, 50, 50, 40);
			s.Groom(T0);
			Assert.AreEqual(85, s.Horse.Cleanliness);
		}

		[Test]
		public void TrustCooldown()
		{
			var s = new MasterSession();
			s.Horse.SetNeeds(50, 50, 50, 10);
			Assert.AreEqual("care.pet.trust", s.Pet(T0).Message);
			Assert.AreEqual(15, s.Horse.Trust);
			Assert.AreEqual("care.pet", s.Pet(T0.AddSeconds(5)).Message);
			Assert.AreEqual(15, s.Horse.Trust);
			Assert.AreEqual("care.pet.trust", s.Pet(T0.AddSeconds(31)).Message);
			Assert.AreEqual(20, s.Horse.Trust);
		}

		[Test]
		public void NeedsClampAndHayNeedsCoins()
		{
			var s = new MasterSession();
			s.Horse.SetNeeds(95, 95, 95, 99);
			s.FeedRation();
			s.Rest();
			s.Pet(T0);
			Assert.AreEqual(100, s.Horse.Satiety);
			Assert.AreEqual(100, s.Horse.Energy);
			Assert.AreEqual(100, s.Horse.Trust);
			for (var i = 0; i < 10; i++) s.FeedHay();
			Assert.AreEqual(0, s.Coins);
			Assert.AreEqual("care.coins", s.FeedHay().Message);
		}

		[Test]
		public void OfflineDecayAppliedOnceAndCapped()
		{
			var s = new MasterSession();
			var save = s.ToSave(T0);
			save.lastUpdatedUtcTicks = T0.Ticks;
			s.Load(save);
			s.Horse.SetNeeds(70, 70, 40, 40);
			s.ApplyElapsed(T0.AddHours(100));
			Assert.AreEqual(54, s.Horse.Satiety);
			Assert.AreEqual(62, s.Horse.Cleanliness);
			Assert.AreEqual(80, s.Horse.Energy);
			s.ApplyElapsed(T0.AddHours(100));
			Assert.AreEqual(54, s.Horse.Satiety);
		}

		[Test]
		public void ClockSetBackChangesNothing()
		{
			var s = new MasterSession();
			var save = s.ToSave(T0);
			save.lastUpdatedUtcTicks = T0.Ticks;
			s.Load(save);
			s.Horse.SetNeeds(70, 70, 50, 40);
			s.ApplyElapsed(T0.AddHours(-5));
			Assert.AreEqual(70, s.Horse.Satiety);
			Assert.AreEqual(50, s.Horse.Energy);
		}

		[Test]
		public void HorseLevels()
		{
			Assert.AreEqual(1, HorseProgress.LevelFor(99));
			Assert.AreEqual(2, HorseProgress.LevelFor(100));
			Assert.AreEqual(5, HorseProgress.LevelFor(700));
			Assert.AreEqual(HorseStage.Adult, HorseProgress.StageFor(5));
		}

		// ---- saving ----

		[Test]
		public void SaveRoundTripKeepsEverything()
		{
			var s = Scribe01(67);
			s.Submit();
			s.Buy(Ids.Guide);
			var run = s.StartTraining(Ids.Commands);
			s.CompleteTraining(run.RunId, 3);
			s.Accept("SCR-02");
			s.Data.Set("choice", Ids.Flag);
			s.MarkTutorial(Ids.Scribe);
			s.Horse.Name = "Грім";
			var json = JsonUtility.ToJson(s.ToSave(T0));
			var t = new MasterSession();
			t.Load(JsonUtility.FromJson<MasterSave>(json));
			Assert.AreEqual(s.Coins, t.Coins);
			Assert.AreEqual(s.GlobalXp, t.GlobalXp);
			Assert.AreEqual(s.SkillXp(Ids.Scribe), t.SkillXp(Ids.Scribe));
			Assert.AreEqual(s.SkillXp(Ids.Stable), t.SkillXp(Ids.Stable));
			Assert.AreEqual(JobStatus.Completed, t.StatusOf("SCR-01"));
			Assert.AreEqual(90, t.BestScore("SCR-01"));
			Assert.IsTrue(t.Has(Ids.Guide));
			Assert.AreEqual("SCR-02", t.ActiveJobId);
			Assert.AreEqual(Ids.Flag, t.Data.Get("choice"));
			Assert.AreEqual(s.AttemptId, t.AttemptId);
			Assert.IsTrue(t.TutorialShown(Ids.Scribe));
			Assert.AreEqual("Грім", t.Horse.Name);
			Assert.AreEqual(s.Horse.Xp, t.Horse.Xp);
			Assert.AreEqual(s.History.Count, t.History.Count);
			Assert.IsTrue(t.FirstTrainings.Contains(Ids.Commands));
		}

		[Test]
		public void LoadedProcessedAttemptCannotPayAgain()
		{
			var s = Scribe01();
			var attempt = s.AttemptId;
			s.Submit();
			var save = s.ToSave(T0);
			save.activeCommissionId = "SCR-01";
			save.attemptId = attempt;
			var t = new MasterSession();
			t.Load(save);
			Assert.IsNull(t.ActiveJobId);
		}

		[Test]
		public void LoadSanitisesBadData()
		{
			var save = new MasterSession().ToSave(T0);
			save.coins = -50;
			save.globalXp = -1;
			save.satiety = 900;
			save.energy = -4;
			save.completedCommissionIds = new[] { "SCR-01", "NOPE", "SCR-01" };
			save.purchasedUpgradeIds = new[] { "UP_BRUSH", "UP_FAKE" };
			save.selectedProfessionId = "bogus";
			save.horseName = "  ";
			var t = new MasterSession();
			t.Load(save);
			Assert.AreEqual(0, t.Coins);
			Assert.AreEqual(0, t.GlobalXp);
			Assert.AreEqual(100, t.Horse.Satiety);
			Assert.AreEqual(0, t.Horse.Energy);
			Assert.AreEqual(1, t.Completed.Count);
			Assert.AreEqual(1, t.Upgrades.Count);
			Assert.AreEqual(Ids.Stable, t.SelectedProfession);
			Assert.AreEqual(MasterConfig.StartHorseName, t.Horse.Name);
		}

		[Test]
		public void HistoryKeepsLastTwentyFive()
		{
			var s = new MasterSession();
			for (var i = 0; i < 30; i++)
			{
				s.Horse.SetNeeds(70, 70, 80, 40);
				s.CompleteTraining(s.StartTraining(Ids.Commands).RunId, 1);
			}
			Assert.AreEqual(25, s.History.Count);
		}

		[Test]
		public void SaveStoreWritesBacksUpAndRecovers()
		{
			var dir = Path.Combine(Path.GetTempPath(), "master_test_" + Guid.NewGuid().ToString("N"));
			try
			{
				var store = new MasterSaveStore(dir);
				Assert.IsNull(store.Load(out var st));
				Assert.AreEqual(SaveStatus.NotFound, st);
				var s = new MasterSession();
				Assert.IsTrue(store.Save(s.ToSave(T0)));
				s.FeedHay();
				Assert.IsTrue(store.Save(s.ToSave(T0)));
				Assert.IsTrue(store.BackupExists);
				Assert.AreEqual(90, store.Load(out st).coins);
				Assert.AreEqual(100, store.LoadBackup(out st).coins);
				File.WriteAllText(store.Path_, "{ not json");
				Assert.IsNull(store.Load(out st));
				Assert.AreEqual(SaveStatus.Corrupt, st);
				Assert.IsNotNull(store.LoadBackup(out st));
				new SaveStore("master", dir).Save("default", s.ToSave(T0), 99);
				store.Load(out st);
				Assert.AreEqual(SaveStatus.UnsupportedVersion, st);
				store.Delete();
				Assert.IsFalse(store.Exists);
				Assert.IsFalse(store.BackupExists);
			}
			finally
			{
				if (Directory.Exists(dir)) Directory.Delete(dir, true);
			}
		}

		[Test]
		public void SettingsRoundTrip()
		{
			var dir = Path.Combine(Path.GetTempPath(), "master_test_" + Guid.NewGuid().ToString("N"));
			try
			{
				var store = new MasterSaveStore(dir);
				store.SaveSettings(new MasterSettings { masterVolume = 0.3f, sfxVolume = 0.2f, fullscreen = false });
				var back = store.LoadSettings();
				Assert.AreEqual(0.3f, back.masterVolume, 0.001f);
				Assert.IsFalse(back.fullscreen);
			}
			finally
			{
				if (Directory.Exists(dir)) Directory.Delete(dir, true);
			}
		}

		// ---- content and text ----

		[Test]
		public void EveryJobAndKeyHasText()
		{
			foreach (var j in MasterContent.Jobs)
			{
				Assert.IsTrue(Loc.HasJob(j.Id), j.Id);
				Assert.IsNotEmpty(Loc.Title(j.Id));
				Assert.IsNotEmpty(Loc.Order(j.Id));
				Assert.IsNotEmpty(Loc.Why(j.Id));
			}
			foreach (var p in MasterContent.Professions)
			{
				Assert.IsTrue(Loc.Has("prof." + p.Id), p.Id);
				Assert.IsTrue(Loc.Has("prof." + p.Id + ".tag"), p.Id);
				Assert.IsTrue(Loc.Has("tip." + p.Id), p.Id);
			}
			foreach (var u in MasterContent.Upgrades)
			{
				Assert.IsTrue(Loc.Has(u.Id), u.Id);
				Assert.IsTrue(Loc.Has(u.Id + ".d"), u.Id);
			}
			foreach (var j in MasterContent.Jobs.Where(x => x.Options != null))
				foreach (var o in j.Options) Assert.IsTrue(Loc.Has("item." + o), o);
		}

		[Test]
		public void AuthoredPathsExist()
		{
			foreach (var id in MasterContent.PathIds) Assert.GreaterOrEqual(MasterContent.Path(id).Length, 4, id);
			foreach (var j in MasterContent.Jobs.Where(x => x.PathId != null)) Assert.IsNotNull(MasterContent.Path(j.PathId));
		}
	}
}
