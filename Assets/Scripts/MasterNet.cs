using System;
using System.Collections.Generic;
using System.Globalization;
using ITKamianets.Engine.Net;
using ITKamianets.Engine.Shell;
using UnityEngine;

namespace Temnorid.Master
{
	/// <summary>What a guest sends when it has finished an order on its own screen.</summary>
	[Serializable]
	public sealed class SubmitMsg
	{
		public string job;
		public string attempt;
		public string[] keys;
		public string[] values;
	}

	/// <summary>What the host answers a guest whose order it paid.</summary>
	[Serializable]
	public sealed class ReceiptMsg
	{
		public string attempt;
		public string profession;
		public string id;
		public int quality;
		public int coins;
		public int globalXp;
		public int skillXp;
		public bool first;
		public int levelBefore;
		public int levelAfter;
		public int rankBefore;
		public int rankAfter;

		public static ReceiptMsg From(Receipt r) => new ReceiptMsg
		{
			attempt = r.AttemptId, profession = r.Profession, id = r.Id, quality = r.Quality, coins = r.Coins, globalXp = r.GlobalXp, skillXp = r.SkillXp,
			first = r.FirstTime, levelBefore = r.LevelBefore, levelAfter = r.LevelAfter, rankBefore = r.RankBefore, rankAfter = r.RankAfter,
		};

		public Receipt ToReceipt() => new Receipt
		{
			AttemptId = attempt, Profession = profession, Id = id, Quality = quality, Coins = coins, GlobalXp = globalXp, SkillXp = skillXp,
			FirstTime = first, LevelBefore = levelBefore, LevelAfter = levelAfter, RankBefore = rankBefore, RankAfter = rankAfter,
		};
	}

	/// <summary>Who else is in the workshop and what they are doing.</summary>
	public struct Presence
	{
		public ulong Id;
		public string Name;
		public int Colour;
		/// <summary>The trade they are looking at (a <see cref="Ids"/> value), or null when they are in the hub.</summary>
		public string Profession;
		/// <summary>The order they are working on, or null.</summary>
		public string Job;
	}

	/// <summary>
	/// A shared workshop for up to four masters. The host owns the order board and the shared wallet, level, ranks, upgrades and horse
	/// (the host's <see cref="MasterSession"/>); every guest works on its own task locally (the trade mini-games are local and deterministic)
	/// and sends the result, which the host judges again with <see cref="TaskScoring"/>, pays from the shared wallet and republishes.
	/// <list type="bullet">
	/// <item><c>ms.state</c>: the host's state as a <see cref="MasterSave"/> JSON without the host's own active order, about once a second and after every change; a guest
	/// keeps its own order and answers when it takes it over with <see cref="MasterSession.LoadShared"/>. <c>ms.hello</c> asks for it at once.</item>
	/// <item>Orders: the host publishes 12 entities (ids <see cref="OrderBase"/> and up, in the order of <see cref="MasterContent.Jobs"/>; State is the
	/// <see cref="JobStatus"/>, Value is the id of the player working on it plus one, 0 when free). A guest asks with <c>ms.claim</c>; the host grants
	/// (<c>ms.claimok</c>) only when nobody else has it, so two players never hold one order. <c>ms.release</c> gives it back.</item>
	/// <item><c>ms.submit</c> (order, attempt id, answers): the host checks that the sender holds the order, that the answers only use the order's own keys and that some
	/// time passed, evaluates them, pays once under an id of its own and answers <c>ms.paid</c> (the receipt) or <c>ms.reject</c>.</item>
	/// <item><c>ms.care</c> and <c>ms.buy</c>: the shared horse and wallet are changed only by the host, which answers with <c>ms.careres</c> and <c>ms.buyres</c>.</item>
	/// <item>Poses carry what each player is doing: State is 1 plus the index of the trade (0 in the hub), A is 1 plus the index of the order in work.</item>
	/// </list>
	/// A guest never writes the host's save, and its own offline save is never touched while it is online. Trainings are only for the host.
	/// </summary>
	public sealed class MasterNet : MonoBehaviour
	{
		public const string HelloEvent = "ms.hello";
		public const string StateEvent = "ms.state";
		public const string ClaimEvent = "ms.claim";
		public const string ClaimOkEvent = "ms.claimok";
		public const string ClaimNoEvent = "ms.claimno";
		public const string ReleaseEvent = "ms.release";
		public const string SubmitEvent = "ms.submit";
		public const string PaidEvent = "ms.paid";
		public const string RejectEvent = "ms.reject";
		public const string CareEvent = "ms.care";
		public const string CareResultEvent = "ms.careres";
		public const string BuyEvent = "ms.buy";
		public const string BuyResultEvent = "ms.buyres";

		public const int OrderBase = 1;
		public const int KindOrder = 300;

		/// <summary>The least time between taking an order and handing it in that the host accepts (a guest cannot answer faster than a person works). Tests lower it.</summary>
		public static float MinWorkSeconds = 3f;

		const float PoseEvery = 0.25f;
		const float EntitiesEvery = 0.1f;
		const float StateEvery = 1f;
		const float HelloEvery = 1f;
		const float PendingSeconds = 10f;

		MasterGame _game;
		NetSession _net;
		bool _leaving;
		float _nextPose;
		float _nextEntities;
		float _nextState;
		float _nextHello;
		float _pendingAt;
		readonly Dictionary<string, ulong> _claims = new Dictionary<string, ulong>();
		readonly Dictionary<string, float> _claimedAt = new Dictionary<string, float>();
		readonly List<NetEntity> _entities = new List<NetEntity>();

		public NetSession Session => _net;
		public bool IsHost => _net != null && _net.IsHost;
		public bool IsClient => _net != null && _net.IsClient;
		/// <summary>A guest has the host's state at least once.</summary>
		public bool Synced { get; private set; }
		/// <summary>A guest has handed in an order and waits for the host's answer.</summary>
		public bool Pending { get; private set; }
		public IReadOnlyDictionary<string, ulong> Claims => _claims;

		public MasterNet Init(MasterGame game, NetSession session = null)
		{
			_game = game;
			_net = session != null ? session : NetSession.Instance;
			Synced = !_net.IsClient;
			_net.On(HelloEvent, OnHello);
			_net.On(StateEvent, OnState);
			_net.On(ClaimEvent, OnClaim);
			_net.On(ClaimOkEvent, OnClaimOk);
			_net.On(ClaimNoEvent, OnClaimNo);
			_net.On(ReleaseEvent, OnRelease);
			_net.On(SubmitEvent, OnSubmit);
			_net.On(PaidEvent, OnPaid);
			_net.On(RejectEvent, OnReject);
			_net.On(CareEvent, OnCare);
			_net.On(CareResultEvent, OnCareResult);
			_net.On(BuyEvent, OnBuy);
			_net.On(BuyResultEvent, OnBuyResult);
			_net.Disconnected += OnDisconnected;
			_net.PlayerLeft += OnPlayerLeft;
			return this;
		}

		void OnDestroy()
		{
			if (_net == null) return;
			_net.Off(HelloEvent, OnHello);
			_net.Off(StateEvent, OnState);
			_net.Off(ClaimEvent, OnClaim);
			_net.Off(ClaimOkEvent, OnClaimOk);
			_net.Off(ClaimNoEvent, OnClaimNo);
			_net.Off(ReleaseEvent, OnRelease);
			_net.Off(SubmitEvent, OnSubmit);
			_net.Off(PaidEvent, OnPaid);
			_net.Off(RejectEvent, OnReject);
			_net.Off(CareEvent, OnCare);
			_net.Off(CareResultEvent, OnCareResult);
			_net.Off(BuyEvent, OnBuy);
			_net.Off(BuyResultEvent, OnBuyResult);
			_net.Disconnected -= OnDisconnected;
			_net.PlayerLeft -= OnPlayerLeft;
		}

		void Update()
		{
			if (_net == null || !_net.IsOnline) return;
			var now = Time.unscaledTime;

			if (now >= _nextPose)
			{
				_nextPose = now + PoseEvery;
				_net.PublishPose(MyPose());
			}

			if (_net.IsHost)
			{
				if (now >= _nextEntities)
				{
					_nextEntities = now + EntitiesEvery;
					PublishOrders();
				}
				if (now >= _nextState)
				{
					_nextState = now + StateEvery;
					SendState(null);
				}
			}
			else
			{
				if (!Synced && now >= _nextHello)
				{
					_nextHello = now + HelloEvery;
					_net.Send(HelloEvent, "", true);
				}
				if (Pending && now > _pendingAt + PendingSeconds) Pending = false;
			}
		}

		NetPose MyPose()
		{
			var state = 0;
			var job = 0;
			var mode = _game.Mode;
			if (mode == Mode.Orders || mode == Mode.Task || mode == Mode.Stable || mode == Mode.Obstacle || mode == Mode.Command)
				state = 1 + ProfessionIndex(_game.Session.SelectedProfession);
			if (mode == Mode.Task && _game.Session.ActiveJobId != null) job = 1 + JobIndex(_game.Session.ActiveJobId);
			return new NetPose { State = state, A = job, B = _game.Step };
		}

		static int ProfessionIndex(string id)
		{
			for (var i = 0; i < MasterContent.Professions.Length; i++) if (MasterContent.Professions[i].Id == id) return i;
			return 0;
		}

		static int JobIndex(string id)
		{
			for (var i = 0; i < MasterContent.Jobs.Length; i++) if (MasterContent.Jobs[i].Id == id) return i;
			return 0;
		}

		/// <summary>The other players and what each is doing, from their published poses.</summary>
		public List<Presence> Others()
		{
			var list = new List<Presence>();
			foreach (var player in _net.Players)
			{
				if (player.Id == _net.LocalId) continue;
				var p = new Presence { Id = player.Id, Name = player.Name, Colour = player.Colour };
				if (_net.TryGetPose(player.Id, out var pose))
				{
					if (pose.State > 0 && pose.State <= MasterContent.Professions.Length) p.Profession = MasterContent.Professions[pose.State - 1].Id;
					var job = Mathf.RoundToInt(pose.A);
					if (job > 0 && job <= MasterContent.Jobs.Length) p.Job = MasterContent.Jobs[job - 1].Id;
				}
				list.Add(p);
			}
			return list;
		}

		// ---- the order board ----

		/// <summary>Who works on an order: a player id, or -1 when it is free. The host counts its own active order.</summary>
		public long ClaimerOf(string jobId)
		{
			if (_net.IsHost)
			{
				if (_game.Session.ActiveJobId == jobId) return 0;
				return _claims.TryGetValue(jobId, out var id) ? (long)id : -1;
			}
			var index = JobIndex(jobId);
			if (MasterContent.Jobs[index].Id != jobId) return -1;
			if (_net.Entities.TryGetValue(OrderBase + index, out var entity) && entity.Kind == KindOrder) return Mathf.RoundToInt(entity.Value) - 1;
			return -1;
		}

		public bool ClaimedByOther(string jobId)
		{
			var by = ClaimerOf(jobId);
			return by >= 0 && (ulong)by != _net.LocalId;
		}

		public string ClaimerName(string jobId)
		{
			var by = ClaimerOf(jobId);
			if (by < 0) return null;
			var player = _net.Find((ulong)by);
			return player != null ? player.Name : "?";
		}

		void PublishOrders()
		{
			_entities.Clear();
			for (var i = 0; i < MasterContent.Jobs.Length; i++)
			{
				var job = MasterContent.Jobs[i];
				_entities.Add(new NetEntity
				{
					Id = OrderBase + i,
					Kind = KindOrder,
					State = (int)_game.Session.StatusOf(job.Id),
					Value = ClaimerOf(job.Id) + 1,
				});
			}
			_net.PublishEntities(_entities);
		}

		// ---- state ----

		/// <summary>The shared state as a guest needs it: everything but the host's own order in work and the attempt ids already paid.</summary>
		public MasterSave Snapshot()
		{
			var save = _game.Session.ToSave(DateTime.UtcNow);
			save.activeCommissionId = "";
			save.attemptId = "";
			save.dataKeys = new string[0];
			save.dataValues = new string[0];
			save.processedAttemptIds = new string[0];
			return save;
		}

		/// <summary>Host: sends the state to one guest (or to all) now.</summary>
		void SendState(ulong? to)
		{
			var json = JsonUtility.ToJson(Snapshot());
			if (to.HasValue) _net.SendTo(to.Value, StateEvent, json);
			else _net.Send(StateEvent, json);
		}

		/// <summary>Host: something shared changed, so the guests hear of it at the next frame.</summary>
		public void Changed() => _nextState = 0f;

		void OnHello(ulong sender, string payload)
		{
			if (_net.IsHost && _net.Find(sender) != null) SendState(sender);
		}

		void OnState(ulong sender, string json)
		{
			if (!_net.IsClient) return;
			MasterSave save;
			try { save = JsonUtility.FromJson<MasterSave>(json); }
			catch (ArgumentException) { return; }
			if (save == null) return;
			_game.Session.LoadShared(save);
			Synced = true;
			_game.OnSharedState();
		}

		// ---- a guest takes an order ----

		public void RequestClaim(string jobId)
		{
			if (_net.IsClient) _net.Send(ClaimEvent, jobId, true);
		}

		public void Release(string jobId)
		{
			if (_net.IsClient) _net.Send(ReleaseEvent, jobId, true);
		}

		void OnClaim(ulong sender, string jobId)
		{
			if (!_net.IsHost || _net.Find(sender) == null) return;
			var job = MasterContent.FindJob(jobId);
			if (job == null) return;
			var session = _game.Session;
			if (session.StatusOf(jobId) == JobStatus.Locked)
			{
				_net.SendTo(sender, ClaimNoEvent, jobId + "|orders.locked");
				return;
			}
			if (session.ActiveJobId == jobId || (_claims.TryGetValue(jobId, out var other) && other != sender))
			{
				_net.SendTo(sender, ClaimNoEvent, jobId + "|net.taken");
				return;
			}
			// A player works on one order at a time: taking another gives the first back.
			var mine = new List<string>();
			foreach (var pair in _claims) if (pair.Value == sender && pair.Key != jobId) mine.Add(pair.Key);
			foreach (var id in mine) { _claims.Remove(id); _claimedAt.Remove(id); }
			_claims[jobId] = sender;
			_claimedAt[jobId] = Time.unscaledTime;
			_net.SendTo(sender, ClaimOkEvent, jobId);
			PublishOrders();
		}

		void OnRelease(ulong sender, string jobId)
		{
			if (!_net.IsHost) return;
			if (_claims.TryGetValue(jobId, out var by) && by == sender)
			{
				_claims.Remove(jobId);
				_claimedAt.Remove(jobId);
				PublishOrders();
			}
		}

		void OnClaimOk(ulong sender, string jobId)
		{
			if (_net.IsClient) _game.OnClaimGranted(jobId);
		}

		void OnClaimNo(ulong sender, string payload)
		{
			if (!_net.IsClient) return;
			var parts = payload.Split('|');
			_game.OnClaimDenied(parts.Length > 1 ? parts[1] : "net.taken");
		}

		// ---- a guest hands in an order ----

		/// <summary>Guest: sends the active order's answers to the host to be judged and paid.</summary>
		public bool RequestSubmit()
		{
			var session = _game.Session;
			if (!_net.IsClient || Pending || session.ActiveJobId == null) return false;
			var keys = new List<string>();
			var values = new List<string>();
			foreach (var pair in session.Data.All)
			{
				keys.Add(pair.Key);
				values.Add(pair.Value);
			}
			var msg = new SubmitMsg { job = session.ActiveJobId, attempt = session.AttemptId, keys = keys.ToArray(), values = values.ToArray() };
			Pending = true;
			_pendingAt = Time.unscaledTime;
			_net.Send(SubmitEvent, JsonUtility.ToJson(msg), true);
			return true;
		}

		static HashSet<string> AllowedKeys(JobDef job)
		{
			var keys = new HashSet<string>();
			foreach (var step in StepDef.For(job)) foreach (var key in step.Keys) keys.Add(key);
			return keys;
		}

		void OnSubmit(ulong sender, string json)
		{
			if (!_net.IsHost || _net.Find(sender) == null) return;
			SubmitMsg msg;
			try { msg = JsonUtility.FromJson<SubmitMsg>(json); }
			catch (ArgumentException) { return; }
			if (msg == null || string.IsNullOrEmpty(msg.job)) return;

			void Reject(string reason) => _net.SendTo(sender, RejectEvent, msg.job + "|" + reason);

			var job = MasterContent.FindJob(msg.job);
			if (job == null) return;
			if (!_claims.TryGetValue(job.Id, out var by) || by != sender) { Reject("claim"); return; }
			if (string.IsNullOrEmpty(msg.attempt) || msg.attempt.Length > 40 || msg.keys == null || msg.values == null || msg.keys.Length != msg.values.Length || msg.keys.Length > 16) { Reject("data"); return; }
			if (Time.unscaledTime - _claimedAt[job.Id] < MinWorkSeconds) { Reject("early"); return; }

			var allowed = AllowedKeys(job);
			var data = new AttemptData();
			for (var i = 0; i < msg.keys.Length; i++)
			{
				if (!allowed.Contains(msg.keys[i]) || msg.values[i] == null || msg.values[i].Length > 80) { Reject("data"); return; }
				data.Set(msg.keys[i], msg.values[i]);
			}

			var key = "n" + sender.ToString(CultureInfo.InvariantCulture) + "-" + msg.attempt;
			var receipt = _game.Session.CommitRemote(job.Id, data, key, out _);
			if (receipt == null) { Reject("score"); return; }

			_claims.Remove(job.Id);
			_claimedAt.Remove(job.Id);
			_net.SendTo(sender, PaidEvent, JsonUtility.ToJson(ReceiptMsg.From(receipt)));
			var player = _net.Find(sender);
			_game.Toast(Loc.T("net.done", player != null ? player.Name : "?", job.Id, receipt.Coins));
			_game.Save();
			SendState(null);
			PublishOrders();
		}

		void OnPaid(ulong sender, string json)
		{
			if (!_net.IsClient) return;
			ReceiptMsg msg;
			try { msg = JsonUtility.FromJson<ReceiptMsg>(json); }
			catch (ArgumentException) { return; }
			if (msg == null) return;
			Pending = false;
			_game.OnRemotePaid(msg.ToReceipt());
		}

		void OnReject(ulong sender, string payload)
		{
			if (!_net.IsClient) return;
			Pending = false;
			var parts = payload.Split('|');
			_game.OnRemoteRejected(parts.Length > 1 ? parts[1] : "");
		}

		// ---- care and upgrades: the shared horse and wallet ----

		public void RequestCare(string kind)
		{
			if (_net.IsClient) _net.Send(CareEvent, kind, true);
		}

		public void RequestBuy(string upgradeId)
		{
			if (_net.IsClient) _net.Send(BuyEvent, upgradeId, true);
		}

		void OnCare(ulong sender, string kind)
		{
			if (!_net.IsHost || _net.Find(sender) == null) return;
			var session = _game.Session;
			CareResult result;
			switch (kind)
			{
				case "hay": result = session.FeedHay(); break;
				case "ration": result = session.FeedRation(); break;
				case "groom": result = session.Groom(DateTime.UtcNow); break;
				case "pet": result = session.Pet(DateTime.UtcNow); break;
				case "rest": result = session.Rest(); break;
				default: return;
			}
			_net.SendTo(sender, CareResultEvent, string.Join("|", result.Ok ? "1" : "0", result.Message, ((int)result.Anim).ToString(CultureInfo.InvariantCulture), result.Seconds.ToString(CultureInfo.InvariantCulture)));
			if (!result.Ok) return;
			var player = _net.Find(sender);
			_game.Toast(Loc.T("net.cared", player != null ? player.Name : "?"));
			if (_game.Mode == Mode.Stable) _game.Horse.Play(result.Anim, (float)Math.Min(result.Seconds, 4.0));
			_game.Save();
			Changed();
		}

		void OnCareResult(ulong sender, string payload)
		{
			if (!_net.IsClient) return;
			var parts = payload.Split('|');
			if (parts.Length != 4 || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var anim)
				|| !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)) return;
			if (anim < 0 || anim > (int)HorseAnim.Celebrate) return;
			_game.ShowCare(new CareResult { Ok = parts[0] == "1", Message = parts[1], Anim = (HorseAnim)anim, Seconds = seconds });
		}

		void OnBuy(ulong sender, string id)
		{
			if (!_net.IsHost || _net.Find(sender) == null) return;
			var ok = _game.Session.Buy(id);
			_net.SendTo(sender, BuyResultEvent, id + "|" + (ok ? "1" : "0"));
			if (!ok) return;
			var player = _net.Find(sender);
			_game.Toast(Loc.T("net.bought", player != null ? player.Name : "?"));
			_game.Save();
			Changed();
		}

		void OnBuyResult(ulong sender, string payload)
		{
			if (!_net.IsClient) return;
			var parts = payload.Split('|');
			if (parts.Length == 2) _game.ShowBought(parts[0], parts[1] == "1");
		}

		// ---- people come and go ----

		void OnDisconnected(string reason)
		{
			// The host left or the connection dropped: nothing to share any more.
			if (_leaving) return;
			_leaving = true;
			_game.LeaveOnline();
		}

		void OnPlayerLeft(NetPlayer player)
		{
			_game.Toast(Loc.T("net.left", player.Name));
			var gone = new List<string>();
			foreach (var pair in _claims) if (pair.Value == player.Id) gone.Add(pair.Key);
			foreach (var id in gone) { _claims.Remove(id); _claimedAt.Remove(id); }
		}
	}
}
