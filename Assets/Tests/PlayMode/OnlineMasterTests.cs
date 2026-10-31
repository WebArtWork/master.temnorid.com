using System;
using System.Collections;
using System.IO;
using ITKamianets.Engine.Net;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Temnorid.Master.Tests
{
	/// <summary>
	/// The real workshop with this machine as the host and a second player (a detached session in the same process) as the guest: the guest's
	/// presence shows on the host, the host's order board reaches the guest as entities, and an order goes through claim, hand-in, re-scoring and pay.
	/// </summary>
	public class OnlineMasterTests
	{
		NetSession _guest;
		string _saveFolder;

		static IEnumerator Until(Func<bool> condition, string what, float seconds = 10f)
		{
			var end = Time.realtimeSinceStartup + seconds;
			while (!condition())
			{
				if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out waiting for " + what);
				yield return null;
			}
		}

		[SetUp]
		public void SetUp()
		{
			_saveFolder = Path.Combine(Path.GetTempPath(), "master-online-" + Guid.NewGuid());
			MasterGame.SaveRootOverride = _saveFolder;
		}

		[TearDown]
		public void TearDown()
		{
			Time.timeScale = 1f;
			MasterNet.MinWorkSeconds = 3f;
			MasterGame.SaveRootOverride = null;
			if (_guest != null) { _guest.Leave(); UnityEngine.Object.Destroy(_guest.gameObject); }
			NetSession.Instance.Leave();
			if (Directory.Exists(_saveFolder)) Directory.Delete(_saveFolder, true);
		}

		static SubmitMsg Perfect(string id, string attempt)
		{
			var job = MasterContent.FindJob(id);
			var data = new AttemptData();
			data.Set("heat", job.Target);
			for (var i = 0; i < 3; i++) data.Set("h" + i, job.Hammer[i]);
			data.Set("finish", job.Choice);
			var keys = new System.Collections.Generic.List<string>();
			var values = new System.Collections.Generic.List<string>();
			foreach (var pair in data.All) { keys.Add(pair.Key); values.Add(pair.Value); }
			return new SubmitMsg { job = id, attempt = attempt, keys = keys.ToArray(), values = values.ToArray() };
		}

		[UnityTest]
		[Timeout(120000)]
		public IEnumerator A_guest_works_an_order_in_the_hosts_workshop_and_the_host_pays_the_shared_wallet()
		{
			MasterNet.MinWorkSeconds = 0.3f;
			var host = NetSession.Instance;
			host.Leave();
			host.GameId = "master";
			host.GameVersion = Application.version;
			host.LocalName = "Hosty";
			host.MaxPlayers = 4;
			Assert.IsTrue(host.HostDirect(7809), host.LastError);

			_guest = NetSession.CreateDetached();
			_guest.GameId = "master";
			_guest.GameVersion = Application.version;
			_guest.LocalName = "Guesty";
			_guest.LocalColour = 3;
			Assert.IsTrue(_guest.JoinDirect("127.0.0.1:7809"), _guest.LastError);
			yield return Until(() => host.Players.Count == 2 && _guest.Players.Count == 2 && !_guest.Connecting, "the guest to join");

			string state = null, claimOk = null, paid = null, reject = null, care = null;
			_guest.On(MasterNet.StateEvent, (who, text) => state = text);
			_guest.On(MasterNet.ClaimOkEvent, (who, text) => claimOk = text);
			_guest.On(MasterNet.PaidEvent, (who, text) => paid = text);
			_guest.On(MasterNet.RejectEvent, (who, text) => reject = text);
			_guest.On(MasterNet.CareResultEvent, (who, text) => care = text);

			yield return SceneManager.LoadSceneAsync("Master");
			yield return null;

			var game = MasterGame.Instance;
			Assert.IsNotNull(game.Net, "an online workshop has the link to other players");
			Assert.IsTrue(game.Net.IsHost);
			Assert.IsFalse(game.IsGuest);
			Assert.AreEqual(Mode.Hub, game.Mode, "online skips the title");
			var hostSession = game.Session;

			// The host's state reaches the guest, who mirrors it.
			_guest.Send(MasterNet.HelloEvent, "", true);
			yield return Until(() => state != null, "the host's state");
			var mirror = new MasterSession();
			mirror.LoadShared(JsonUtility.FromJson<MasterSave>(state));
			Assert.AreEqual(MasterConfig.StartCoins, mirror.Coins);

			// The guest's presence: which trade it looks at and which order it works on, from its pose.
			var forge = Array.FindIndex(MasterContent.Professions, p => p.Id == Ids.Forge);
			var job = Array.FindIndex(MasterContent.Jobs, j => j.Id == "FOR-01");
			for (var i = 0; i < 4; i++)
			{
				_guest.PublishPose(new NetPose { State = 1 + forge, A = 1 + job });
				yield return new WaitForSeconds(0.1f);
			}
			yield return Until(() => game.Net.Others().Count == 1 && game.Net.Others()[0].Job == "FOR-01", "the guest's presence");
			Assert.AreEqual(Ids.Forge, game.Net.Others()[0].Profession);
			Assert.AreEqual("Guesty", game.Net.Others()[0].Name);

			// A claim: the host grants it, publishes who holds the order, and the host itself cannot take it.
			_guest.Send(MasterNet.ClaimEvent, "FOR-01", true);
			yield return Until(() => claimOk == "FOR-01", "the claim to be granted");
			yield return Until(() => _guest.Entities.TryGetValue(MasterNet.OrderBase + job, out var e) && Mathf.RoundToInt(e.Value) == (int)_guest.LocalId + 1, "the order board to show the claim");
			Assert.IsTrue(game.Net.ClaimedByOther("FOR-01"));
			Assert.AreEqual("Guesty", game.Net.ClaimerName("FOR-01"));
			game.OpenProfession(Ids.Forge);
			Assert.IsFalse(game.Take("FOR-01"), "two players never hold the same order");
			Assert.AreEqual(Mode.Orders, game.Mode);
			Assert.IsNull(hostSession.ActiveJobId);
			Assert.AreEqual("net.taken", game.TakeBlock("FOR-01"));

			// Answers that are not the order's own are refused; then the real result is re-scored and paid from the shared wallet.
			yield return new WaitForSeconds(0.4f);
			var bad = Perfect("FOR-01", "G-1");
			bad.keys[0] = "evil";
			_guest.Send(MasterNet.SubmitEvent, JsonUtility.ToJson(bad), true);
			yield return Until(() => reject == "FOR-01|data", "the bad answers to be rejected");
			Assert.AreEqual(MasterConfig.StartCoins, hostSession.Coins);

			_guest.Send(MasterNet.SubmitEvent, JsonUtility.ToJson(Perfect("FOR-01", "G-1")), true);
			yield return Until(() => paid != null, "the order to be paid");
			var receipt = JsonUtility.FromJson<ReceiptMsg>(paid);
			Assert.AreEqual("FOR-01", receipt.id);
			Assert.Greater(receipt.coins, 0);
			Assert.AreEqual(MasterConfig.StartCoins + receipt.coins, hostSession.Coins, "the shared wallet was paid");
			Assert.AreEqual(JobStatus.Completed, hostSession.StatusOf("FOR-01"));
			Assert.IsNull(hostSession.ActiveJobId, "the host's own order is not touched");
			Assert.AreEqual(-1, game.Net.ClaimerOf("FOR-01"), "the claim ended with the pay");

			// Everybody sees the new state; a repeat of the same hand-in is not paid.
			state = null;
			yield return Until(() => state != null, "the new state");
			mirror.LoadShared(JsonUtility.FromJson<MasterSave>(state));
			Assert.AreEqual(hostSession.Coins, mirror.Coins);
			Assert.AreEqual(JobStatus.Completed, mirror.StatusOf("FOR-01"));
			yield return Until(() => _guest.Entities.TryGetValue(MasterNet.OrderBase + job, out var e) && e.State == (int)JobStatus.Completed, "the board to show the order done");
			reject = null;
			_guest.Send(MasterNet.SubmitEvent, JsonUtility.ToJson(Perfect("FOR-01", "G-1")), true);
			yield return Until(() => reject == "FOR-01|claim", "the repeat to be rejected");
			Assert.AreEqual(MasterConfig.StartCoins + receipt.coins, hostSession.Coins);

			// Care of the shared horse is done by the host on the shared wallet.
			var before = hostSession.Coins;
			_guest.Send(MasterNet.CareEvent, "hay", true);
			yield return Until(() => care != null, "the care to be answered");
			Assert.IsTrue(care.StartsWith("1|"));
			Assert.AreEqual(before - MasterConfig.HayCost, hostSession.Coins);
		}
	}
}
