using NUnit.Framework;
using UnityEngine;

namespace Temnorid.Master.Tests
{
	/// <summary>The rules of the shared workshop that need no network: the host paying another player's order and a guest mirroring the host.</summary>
	public sealed class MasterSharedTests
	{
		static AttemptData PerfectForge(string id)
		{
			var job = MasterContent.FindJob(id);
			var data = new AttemptData();
			data.Set("heat", job.Target);
			for (var i = 0; i < 3; i++) data.Set("h" + i, job.Hammer[i]);
			data.Set("finish", job.Choice);
			return data;
		}

		[Test]
		public void The_host_pays_another_players_order_once_and_leaves_its_own_order_alone()
		{
			var host = new MasterSession();
			Assert.IsTrue(host.Accept("SCR-01"));
			var coins = host.Coins;

			var receipt = host.CommitRemote("FOR-01", PerfectForge("FOR-01"), "n1-A-1", out var outcome);
			Assert.IsNotNull(receipt);
			Assert.IsTrue(outcome.Success);
			Assert.AreEqual(coins + receipt.Coins, host.Coins);
			Assert.AreEqual(JobStatus.Completed, host.StatusOf("FOR-01"));
			Assert.AreEqual("SCR-01", host.ActiveJobId, "the host's own order is untouched");
			Assert.IsNull(host.LastReceipt, "the host's result screen is not changed by another player's pay");

			Assert.IsNull(host.CommitRemote("FOR-01", PerfectForge("FOR-01"), "n1-A-1", out _), "the same attempt pays once");
			Assert.AreEqual(coins + receipt.Coins, host.Coins);
			var again = host.CommitRemote("FOR-01", PerfectForge("FOR-01"), "n1-A-2", out _);
			Assert.IsNotNull(again, "a second try is a repeat, paid less");
			Assert.Less(again.Coins, receipt.Coins);
		}

		[Test]
		public void The_host_rejudges_and_rejects_wrong_or_locked_work()
		{
			var host = new MasterSession();
			var bad = PerfectForge("FOR-01");
			bad.Set("finish", Ids.Quick);
			Assert.IsNull(host.CommitRemote("FOR-01", bad, "n1-B-1", out var outcome));
			Assert.IsFalse(outcome.Success);
			Assert.AreEqual(100, host.Coins);

			Assert.IsNull(host.CommitRemote("FOR-02", PerfectForge("FOR-02"), "n1-B-2", out _), "a locked order cannot be paid");
			Assert.IsNull(host.CommitRemote("NOPE", PerfectForge("FOR-01"), "n1-B-3", out _));
			Assert.IsNull(host.CommitRemote("FOR-01", new AttemptData(), "n1-B-4", out _), "nothing done is not complete");
		}

		[Test]
		public void A_guest_takes_the_hosts_state_but_keeps_its_own_order_and_ids()
		{
			var host = new MasterSession();
			host.CommitRemote("FOR-01", PerfectForge("FOR-01"), "n1-C-1", out _);
			host.MarkTutorial(Ids.Forge);
			var saved = host.ToSave(System.DateTime.UtcNow);
			saved.activeCommissionId = "";
			saved.attemptId = "";
			saved.processedAttemptIds = new string[0];

			var guest = new MasterSession();
			Assert.IsTrue(guest.Accept("SCR-01"));
			var attempt = guest.AttemptId;
			guest.Data.Set("choice", Ids.Accept);
			guest.SelectedProfession = Ids.Scribe;
			guest.MarkTutorial(Ids.Scribe);

			guest.LoadShared(JsonUtility.FromJson<MasterSave>(JsonUtility.ToJson(saved)));
			Assert.AreEqual(host.Coins, guest.Coins);
			Assert.AreEqual(host.GlobalXp, guest.GlobalXp);
			Assert.AreEqual(JobStatus.Completed, guest.StatusOf("FOR-01"));
			Assert.AreEqual("SCR-01", guest.ActiveJobId);
			Assert.AreEqual(attempt, guest.AttemptId);
			Assert.AreEqual(Ids.Accept, guest.Data.Get("choice"));
			Assert.AreEqual(Ids.Scribe, guest.SelectedProfession);
			Assert.IsTrue(guest.TutorialShown(Ids.Scribe), "the guest's own help state stays");
			Assert.IsFalse(guest.TutorialShown(Ids.Forge), "the host's help state is not the guest's");
			Assert.AreEqual(0, guest.Processed.Count);
		}

		[Test]
		public void A_guests_mirror_of_a_receipt_matches_what_the_host_applied()
		{
			var host = new MasterSession();
			var guest = new MasterSession();
			var receipt = host.CommitRemote("FOR-01", PerfectForge("FOR-01"), "n1-D-1", out _);
			Assert.IsTrue(guest.Accept("FOR-01"));
			guest.ApplyRemoteReceipt(receipt);
			Assert.AreEqual(host.Coins, guest.Coins);
			Assert.AreEqual(host.GlobalXp, guest.GlobalXp);
			Assert.AreEqual(host.SkillXp(Ids.Forge), guest.SkillXp(Ids.Forge));
			Assert.AreEqual(host.BestScore("FOR-01"), guest.BestScore("FOR-01"));
			Assert.AreEqual(JobStatus.Completed, guest.StatusOf("FOR-01"));
			Assert.IsNull(guest.ActiveJobId);
			Assert.AreSame(receipt, guest.LastReceipt);
		}

		[Test]
		public void A_guest_can_judge_its_own_answers_without_being_paid()
		{
			var guest = new MasterSession();
			Assert.IsTrue(guest.Accept("FOR-01"));
			foreach (var pair in PerfectForge("FOR-01").All) guest.Data.Set(pair.Key, pair.Value);
			var outcome = guest.Judge();
			Assert.IsTrue(outcome.Success);
			Assert.AreEqual(100, guest.Coins);
			Assert.AreEqual("FOR-01", guest.ActiveJobId);
		}
	}
}
