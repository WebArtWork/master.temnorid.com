using System;
using ITKamianets.Engine.Save;
using ITKamianets.Engine.UI;
using System.Collections.Generic;
using System.Linq;
using ITKamianets.Engine.Input;
using ITKamianets.Engine.Net;
using ITKamianets.Engine.Shell;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Temnorid.Master
{
	public enum Mode
	{
		Title,
		ConfirmNew,
		Recovery,
		Hub,
		Orders,
		Task,
		Result,
		Stable,
		Obstacle,
		Command,
		Upgrades,
		Journal,
		Help,
		Pause,
		Settings,
		ConfirmReset,
		/// <summary>The wallet, level, horse and trades at a glance, and who is in the workshop.</summary>
		Overview,
	}

	public enum StepKind
	{
		Inspect,
		Trace,
		Seal,
		/// <summary>A marker sweeps a bar; one press locks its place (heat, test, grinding).</summary>
		Bar,
		Hammer,
		/// <summary>One choice from a few (the finish, the fabric, the label).</summary>
		Choose,
		/// <summary>Several choices from a list (ingredients, herbs).</summary>
		Multi,
		Parts,
		/// <summary>Three presses, each in its own green zone (stirring, stitching).</summary>
		Stir,
	}

	/// <summary>One numbered step of a commission: what it asks and which answers it owns.</summary>
	public sealed class StepDef
	{
		public StepKind Kind;
		public string Title;
		public string Key;
		public string[] Keys;
		public int Count;
		public string[] Options;
		public bool Stitch;

		public static StepDef[] For(JobDef job)
		{
			switch (job.Profession)
			{
				case Ids.Scribe:
					return new[]
					{
						new StepDef { Kind = StepKind.Inspect, Title = "step.inspect", Key = "choice", Keys = new[] { "choice" } },
						new StepDef { Kind = StepKind.Trace, Title = "step.trace", Key = "trace", Keys = new[] { "trace" } },
						new StepDef { Kind = StepKind.Seal, Title = "step.seal", Key = "seal", Keys = new[] { "seal", "sealpos" } },
					};
				case Ids.Forge:
					return new[]
					{
						new StepDef { Kind = StepKind.Bar, Title = "step.heat", Key = "heat", Keys = new[] { "heat" } },
						new StepDef { Kind = StepKind.Hammer, Title = "step.hammer", Key = "h0", Keys = new[] { "h0", "h1", "h2" }, Count = 3 },
						new StepDef { Kind = StepKind.Choose, Title = "step.finish", Key = "finish", Keys = new[] { "finish" }, Options = new[] { Ids.Gentle, Ids.Quick } },
					};
				case Ids.Workshop:
					return new[]
					{
						new StepDef { Kind = StepKind.Parts, Title = "step.parts", Key = "s0", Keys = new[] { "s0", "s1", "s2" }, Count = 3, Options = job.Options },
						new StepDef { Kind = StepKind.Bar, Title = "step.test", Key = "test", Keys = new[] { "test" } },
					};
				case Ids.Cooking:
					return new[]
					{
						new StepDef { Kind = StepKind.Multi, Title = "step.ingredients", Key = "ing", Keys = new[] { "ing" }, Count = 3, Options = job.Options },
						new StepDef { Kind = StepKind.Bar, Title = "step.heat", Key = "heat", Keys = new[] { "heat" } },
						new StepDef { Kind = StepKind.Stir, Title = "step.stir", Key = "stir", Keys = new[] { "stir" }, Count = 3 },
					};
				case Ids.Tailor:
					return new[]
					{
						new StepDef { Kind = StepKind.Choose, Title = "step.fabric", Key = "fabric", Keys = new[] { "fabric" }, Options = new[] { Ids.Wool, Ids.Linen } },
						new StepDef { Kind = StepKind.Trace, Title = "step.cut", Key = "cut", Keys = new[] { "cut" } },
						new StepDef { Kind = StepKind.Stir, Title = "step.stitch", Key = "stitch", Keys = new[] { "stitch" }, Count = 3, Stitch = true },
					};
				default:
					return new[]
					{
						new StepDef { Kind = StepKind.Multi, Title = "step.herbs", Key = "herbs", Keys = new[] { "herbs" }, Count = 2, Options = job.Options },
						new StepDef { Kind = StepKind.Bar, Title = "step.grind", Key = "grind", Keys = new[] { "grind" } },
						new StepDef { Kind = StepKind.Choose, Title = "step.label", Key = "label", Keys = new[] { "label" }, Options = new[] { Ids.Sun, Ids.Moon, Ids.Leaf } },
					};
			}
		}
	}

	/// <summary>
	/// The game as played: it owns the <see cref="MasterSession"/>, moves between the hub, the workstations and the stable, runs the interactions of every
	/// task, saves after every committed step and plays the two trainings. Screens only show what it says and call it; a test (or a bot) can play the whole game through it.
	/// </summary>
	[DefaultExecutionOrder(-100)]
	public sealed class MasterGame : MonoBehaviour
	{
		public static MasterGame Instance { get; private set; }
		/// <summary>Tests point the save somewhere else.</summary>
		public static string SaveRootOverride;

		[Header("Scene")]
		public ViewCamera Cam;
		public MasterAudio Audio;
		public HorseView Horse;
		public YardTrack Track;
		public Renderer Billet;

		public MasterSession Session { get; private set; } = new MasterSession();
		public MasterSettings Settings { get; private set; } = new MasterSettings();
		public MasterSaveStore Store { get; private set; }
		/// <summary>The link to the other players of a shared workshop, or null when playing alone.</summary>
		public MasterNet Net { get; private set; }
		/// <summary>False for a guest (its own offline save is never touched online) and after an unreadable save the host could not use.</summary>
		public bool SaveEnabled { get; set; } = true;
		public bool IsGuest => Net != null && Net.IsClient;

		public Mode Mode { get; private set; } = Mode.Title;
		public Mode Return { get; private set; } = Mode.Hub;
		public bool HasSave { get; private set; }
		public bool SaveProblem { get; private set; }
		public string RecoveryReason { get; private set; } = "";
		public string RecoveryNote { get; private set; } = "";
		public string ToastText { get; private set; } = "";
		public float ToastUntil { get; private set; }
		public int Saves { get; private set; }
		public string HoverStation { get; private set; }
		public bool ToastActive => Time.unscaledTime < ToastUntil;

		// The task in progress.
		public int Step { get; private set; }
		public string StepMessage { get; private set; } = "";
		public string SelectedSeal { get; private set; }
		public string SelectedPart { get; private set; }
		public int StirRound { get; private set; }
		public int StirHits { get; private set; }
		public int LastStrike { get; private set; } = -1;
		/// <summary>The time on the sweeping marker. Tests set it to put the marker where they want it.</summary>
		public float BarTime { get; set; }

		// The result on show.
		public bool ResultTraining { get; private set; }
		public bool ResultSuccess { get; private set; }
		public bool LeveledUp { get; private set; }
		public bool RankedUp { get; private set; }
		public bool HorseLeveled { get; private set; }

		// The trainings.
		public ObstacleRunLogic Run { get; private set; }
		public CommandTimingLogic Timing { get; private set; }

		float _nextSave;
		float _nextHoof;
		bool _tipDismissedNow;
		string _tipFor;

		public StepDef[] Steps => Session.ActiveJobId == null ? new StepDef[0] : StepDef.For(MasterContent.FindJob(Session.ActiveJobId));
		public StepDef CurrentStep => Steps.Length > Step ? Steps[Step] : null;
		public JobDef ActiveJob => Session.ActiveJobId == null ? null : MasterContent.FindJob(Session.ActiveJobId);

		void Awake()
		{
			Instance = this;
			Time.timeScale = 1f;
			EngineInput.Default.PlayerEnabled = true;
			Store = new MasterSaveStore(SaveRootOverride);
			Settings = Store.LoadSettings();
			ApplySettings();
			RefreshSave();
			ShellFlow.BeforeQuit += OnBeforeQuit;
		}

		void OnBeforeQuit()
		{
			if (Live) SaveNow();
		}

		void Start()
		{
			SetMode(Mode.Title, true);
			Horse.Apply(Session.Horse, true);
			Track.Show(false);
			if (NetSession.Instance.IsOnline) BeginOnline();
		}

		void OnDestroy()
		{
			ShellFlow.BeforeQuit -= OnBeforeQuit;
			if (Instance == this) Instance = null;
		}

		// ---- the shared workshop ----

		/// <summary>An online game skips the title: the host opens its own save (or a new game) and a guest waits for the host's state.</summary>
		void BeginOnline()
		{
			Net = gameObject.AddComponent<MasterNet>();
			Net.Init(this);
			if (Net.IsClient)
			{
				SaveEnabled = false;
				StartSession(new MasterSession());
				Toast(Loc.T("net.syncing"), 4f);
				return;
			}
			var saved = Store.Load(out var status);
			if (status == SaveStatus.Ok && saved != null)
			{
				var session = new MasterSession();
				session.Load(saved);
				session.ApplyElapsed(DateTime.UtcNow);
				StartSession(session);
				return;
			}
			// A save that cannot be read is left alone for a later solo game.
			if (status == SaveStatus.Corrupt || status == SaveStatus.UnsupportedVersion) SaveEnabled = false;
			StartSession(new MasterSession());
		}

		/// <summary>A guest has a new state from the host: the horse's size follows its level.</summary>
		public void OnSharedState()
		{
			Horse.Apply(Session.Horse);
		}

		public void Save() => SaveNow();

		public void ShowCare(CareResult result) => Report(result);

		public void ShowBought(string id, bool ok)
		{
			if (ok)
			{
				Audio.Play(Sfx.Coin);
				Toast(Loc.T("up.bought", Loc.T(id)));
			}
			else Toast(Loc.T("up.short"));
		}

		public void OnClaimGranted(string jobId)
		{
			if (Mode == Mode.Orders && Session.ActiveJobId == null && Session.Accept(jobId)) BeginAccepted();
			else Net.Release(jobId);
		}

		public void OnClaimDenied(string reasonKey)
		{
			Toast(Loc.T(reasonKey));
			Audio.Play(Sfx.Bad, 0.4f);
		}

		/// <summary>The host paid this guest's order: the result screen shows what the host paid.</summary>
		public void OnRemotePaid(Receipt receipt)
		{
			Session.ApplyRemoteReceipt(receipt);
			ResultTraining = false;
			ResultSuccess = true;
			LeveledUp = receipt.LevelAfter > receipt.LevelBefore;
			RankedUp = receipt.RankAfter > receipt.RankBefore;
			HorseLeveled = false;
			Audio.Play(Sfx.Good);
			Audio.Play(Sfx.Coin, 0.7f);
			if (LeveledUp || RankedUp) Audio.Play(Sfx.LevelUp, 0.6f);
			SelectedPartClear();
			SetMode(Mode.Result);
		}

		public void OnRemoteRejected(string reason)
		{
			Toast(Loc.T("net.rejected"));
			Audio.Play(Sfx.Bad, 0.5f);
			if (reason != "claim") return;
			// The host does not know this order as ours (it restarted or gave it away): drop it and let the guest take it again.
			Session.Abandon();
			SelectedPartClear();
			Step = 0;
			if (Mode == Mode.Task) SetMode(Mode.Orders);
		}

		/// <summary>The host left or the connection dropped.</summary>
		public void LeaveOnline() => ShellFlow.QuitToMenu();

		// ---- starting, saving ----

		public void RefreshSave()
		{
			var saved = Store.Load(out var status);
			HasSave = status == SaveStatus.Ok && saved != null;
			SaveProblem = status == SaveStatus.Corrupt || status == SaveStatus.UnsupportedVersion;
			RecoveryReason = status == SaveStatus.UnsupportedVersion ? "recovery.newer" : "recovery.corrupt";
		}

		public void RequestNewGame()
		{
			if (Mode != Mode.Title) return;
			if (HasSave || SaveProblem) SetMode(Mode.ConfirmNew);
			else NewGame();
		}

		public void NewGame()
		{
			Store.Delete();
			StartSession(new MasterSession());
			SaveNow();
		}

		void StartSession(MasterSession session)
		{
			Session = session;
			_nextSave = Time.unscaledTime + 60f;
			ResetScreens();
			Horse.Stop();
			Horse.Apply(Session.Horse, true);
			SetMode(Mode.Hub, true);
		}

		/// <summary>The title button: loads the save, or opens the recovery menu when it cannot be read.</summary>
		public bool ContinueGame()
		{
			var saved = Store.Load(out var status);
			if (status != SaveStatus.Ok || saved == null)
			{
				RecoveryReason = status == SaveStatus.UnsupportedVersion ? "recovery.newer" : "recovery.corrupt";
				RecoveryNote = "";
				SetMode(Mode.Recovery);
				return false;
			}
			var session = new MasterSession();
			session.Load(saved);
			session.ApplyElapsed(DateTime.UtcNow);
			StartSession(session);
			return true;
		}

		/// <summary>From the recovery menu: the last good save, written back as the current one.</summary>
		public bool TryBackup()
		{
			var saved = Store.LoadBackup(out var status);
			if (status != SaveStatus.Ok || saved == null)
			{
				RecoveryNote = "recovery.nobackup";
				return false;
			}
			var session = new MasterSession();
			session.Load(saved);
			session.ApplyElapsed(DateTime.UtcNow);
			StartSession(session);
			SaveNow();
			return true;
		}

		public void StartFresh()
		{
			if (Mode != Mode.Recovery) return;
			NewGame();
		}

		void ResetScreens()
		{
			Step = 0;
			StepMessage = "";
			SelectedSeal = null;
			SelectedPart = null;
			StirRound = StirHits = 0;
			LastStrike = -1;
			ToastUntil = 0f;
			LeveledUp = RankedUp = HorseLeveled = false;
			Run = null;
			Timing = null;
			RecoveryNote = "";
		}

		public void SaveNow()
		{
			if (!SaveEnabled) return;
			if (Store.Save(Session.ToSave(DateTime.UtcNow))) Saves++;
			RefreshSave();
		}

		void ApplySettings()
		{
			if (Audio != null) Audio.SetVolumes(Settings.masterVolume, Settings.sfxVolume);
			if (!Application.isEditor && Screen.fullScreen != Settings.fullscreen) Screen.fullScreen = Settings.fullscreen;
		}

		public void SetVolumes(float master, float sfx)
		{
			Settings.masterVolume = Mathf.Clamp01(master);
			Settings.sfxVolume = Mathf.Clamp01(sfx);
			ApplySettings();
			Store.SaveSettings(Settings);
		}

		public void SetFullscreen(bool on)
		{
			Settings.fullscreen = on;
			ApplySettings();
			Store.SaveSettings(Settings);
		}

		// ---- modes and cameras ----

		void SetMode(Mode mode, bool snap = false)
		{
			Mode = mode;
			var key = CameraKey(mode);
			if (key != null && Cam != null) Cam.Go(key, snap);
			if (mode != Mode.Task) _tipDismissedNow = false;
		}

		string CameraKey(Mode mode)
		{
			switch (mode)
			{
				case Mode.Title:
				case Mode.ConfirmNew:
				case Mode.Recovery:
				case Mode.Hub:
				case Mode.Upgrades:
				case Mode.Journal:
				case Mode.Overview:
					return "hub";
				case Mode.Orders:
					return Session.SelectedProfession + ".overview";
				case Mode.Task:
					return Session.SelectedProfession + ".focus";
				case Mode.Stable:
				case Mode.Command:
					return "stable";
				case Mode.Obstacle:
					return "train";
				default:
					return null;
			}
		}

		public bool Live => Mode != Mode.Title && Mode != Mode.ConfirmNew && Mode != Mode.Recovery;

		public void Toast(string text, float seconds = 3.2f)
		{
			ToastText = text;
			ToastUntil = Time.unscaledTime + seconds;
		}

		// ---- the frame ----

		void Update()
		{
			var input = EngineInput.Default;
			if (input.UI.Cancel.WasPressedThisFrame()) Back();

			if (Live)
			{
				var dt = Time.unscaledDeltaTime;
				if (!IsGuest) Session.Tick(dt);
				Session.Touch(DateTime.UtcNow);
				if (Time.unscaledTime >= _nextSave)
				{
					_nextSave = Time.unscaledTime + 60f;
					SaveNow();
				}
			}

			if (Mode == Mode.Hub) PickStation(input);
			else ClearHover();

			switch (Mode)
			{
				case Mode.Task:
					UpdateTask(input);
					break;
				case Mode.Obstacle:
					UpdateObstacle(input);
					break;
				case Mode.Command:
					UpdateCommand(input);
					break;
			}
		}

		void PickStation(EngineInput input)
		{
			var screen = input.UI.Point.ReadValue<Vector2>();
			var overUi = (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) || (ShellChrome.Current != null && ShellChrome.Current.ProfileOpen);
			string hit = null;
			if (!overUi && Cam != null && !Cam.Blending && screen != Vector2.zero)
			{
				var camera = Cam.GetComponent<Camera>();
				if (Physics.Raycast(camera.ScreenPointToRay(screen), out var info, 80f))
				{
					var station = info.collider.GetComponentInParent<Station>();
					if (station != null) hit = station.Id;
				}
			}
			SetHover(hit);
			if (hit != null && input.UI.Click.WasPressedThisFrame()) OpenProfession(hit);
		}

		void SetHover(string id)
		{
			if (id == HoverStation) return;
			HoverStation = id;
			foreach (var station in Station.All) station.Hover = station.Id == id;
		}

		void ClearHover() => SetHover(null);

		// ---- moving around ----

		public void OpenProfession(string id)
		{
			if (Mode != Mode.Hub || MasterContent.FindProfession(id) == null) return;
			Session.SelectedProfession = id;
			Audio.Play(Sfx.Page, 0.6f);
			if (id == Ids.Stable)
			{
				Horse.Apply(Session.Horse);
				SetMode(Mode.Stable);
			}
			else SetMode(Mode.Orders);
		}

		public void OpenUpgrades()
		{
			if (Mode == Mode.Hub) SetMode(Mode.Upgrades);
		}

		public void OpenJournal()
		{
			if (Mode == Mode.Hub) SetMode(Mode.Journal);
		}

		/// <summary>The overview place: from the hub, the order lists, the stable, the journal, the upgrades or a task (which stays as it is).</summary>
		public void OpenOverview()
		{
			if (Mode == Mode.Hub || Mode == Mode.Orders || Mode == Mode.Stable || Mode == Mode.Journal || Mode == Mode.Upgrades || Mode == Mode.Task)
				SetMode(Mode.Overview);
		}

		public void OpenHelp()
		{
			if (Mode == Mode.Title || Mode == Mode.Help) return;
			Return = Mode;
			SetMode(Mode.Help);
		}

		public void OpenSettings()
		{
			Return = Mode;
			SetMode(Mode.Settings);
		}

		public void Pause()
		{
			if (Mode != Mode.Hub) return;
			SaveNow();
			Return = Mode.Hub;
			SetMode(Mode.Pause);
		}

		public void Resume()
		{
			if (Mode == Mode.Pause) SetMode(Mode.Hub);
		}

		public void ToTitle()
		{
			if (Net != null)
			{
				ShellFlow.QuitToMenu();
				return;
			}
			if (Mode == Mode.Pause || Mode == Mode.Hub) SaveNow();
			RefreshSave();
			SetMode(Mode.Title);
		}

		public void ToHub()
		{
			if (Mode == Mode.Obstacle || Mode == Mode.Command) AbortTraining();
			SelectedPartClear();
			SetMode(Mode.Hub);
		}

		void SelectedPartClear()
		{
			SelectedPart = null;
			SelectedSeal = null;
		}

		/// <summary>Back or Esc: closes the screen and returns to a valid view. Nothing is lost; a task in progress stays as it is.</summary>
		public void Back()
		{
			switch (Mode)
			{
				case Mode.Title:
				case Mode.Recovery:
					return;
				case Mode.ConfirmNew:
					SetMode(Mode.Title);
					return;
				case Mode.Hub:
					Pause();
					return;
				case Mode.Orders:
					SetMode(Mode.Hub);
					return;
				case Mode.Task:
					SelectedPartClear();
					SetMode(Mode.Orders);
					return;
				case Mode.Result:
					LeaveResult();
					return;
				case Mode.Stable:
					SetMode(Mode.Hub);
					return;
				case Mode.Obstacle:
				case Mode.Command:
					AbortTraining();
					return;
				case Mode.Settings:
					SetMode(Return == Mode.Pause ? Mode.Pause : Return == Mode.Hub ? Mode.Hub : Mode.Title);
					return;
				case Mode.ConfirmReset:
					SetMode(Mode.Settings);
					return;
				case Mode.Help:
					SetMode(Return);
					return;
				case Mode.Pause:
					Resume();
					return;
				default:
					SetMode(Mode.Hub);
					return;
			}
		}

		public void RequestReset()
		{
			if (Net != null)
			{
				Toast(Loc.T("net.noreset"));
				return;
			}
			if (Mode == Mode.Settings) SetMode(Mode.ConfirmReset);
		}

		public void ConfirmReset()
		{
			if (Mode != Mode.ConfirmReset) return;
			Store.Delete();
			Session = new MasterSession();
			ResetScreens();
			Horse.Stop();
			Horse.Apply(Session.Horse, true);
			SetMode(Mode.Title, true);
			RefreshSave();
		}

		// ---- tips ----

		public string TipKey
		{
			get
			{
				if (Mode != Mode.Orders && Mode != Mode.Task && Mode != Mode.Stable) return null;
				var id = Session.SelectedProfession;
				return Session.TutorialShown(id) ? null : "tip." + id;
			}
		}

		public void DismissTip()
		{
			var id = Session.SelectedProfession;
			Session.MarkTutorial(id);
			Audio.Play(Sfx.Click, 0.5f);
			SaveNow();
		}

		// ---- commissions ----

		/// <summary>Takes a commission (or goes back into the one already taken) and opens its close view.</summary>
		public bool Take(string jobId)
		{
			if (Mode != Mode.Orders) return false;
			var job = MasterContent.FindJob(jobId);
			if (job == null) return false;
			if (Session.ActiveJobId == jobId)
			{
				Step = FirstOpenStep();
				BeginStep();
				SetMode(Mode.Task);
				return true;
			}
			if (Session.ActiveJobId != null)
			{
				Toast(Loc.T("train.job"));
				return false;
			}
			if (Net != null && Net.ClaimedByOther(jobId))
			{
				Toast(Loc.T("net.taken"));
				return false;
			}
			if (IsGuest)
			{
				// The host decides who gets an order: the task opens when it says yes.
				if (!Net.Synced) { Toast(Loc.T("net.syncing")); return false; }
				if (!Session.CanAccept(jobId)) return false;
				Net.RequestClaim(jobId);
				Toast(Loc.T("net.claiming"));
				return true;
			}
			if (!Session.Accept(jobId)) return false;
			BeginAccepted();
			return true;
		}

		void BeginAccepted()
		{
			Step = 0;
			BeginStep();
			SelectedPartClear();
			Audio.Play(Sfx.Page);
			SaveNow();
			SetMode(Mode.Task);
		}

		/// <summary>Why a commission cannot be taken right now (a key of <see cref="Loc"/>), or null.</summary>
		public string TakeBlock(string jobId)
		{
			if (Session.StatusOf(jobId) == JobStatus.Locked) return "orders.locked";
			if (Session.ActiveJobId != null && Session.ActiveJobId != jobId) return "train.job";
			if (Session.TrainingActive) return "train.busy";
			if (Net != null && Net.ClaimedByOther(jobId)) return "net.taken";
			return null;
		}

		public void AbandonActive()
		{
			if (Session.ActiveJobId == null) return;
			if (IsGuest) Net.Release(Session.ActiveJobId);
			Session.Abandon();
			SelectedPartClear();
			Step = 0;
			SaveNow();
		}

		int FirstOpenStep()
		{
			var steps = Steps;
			for (var i = 0; i < steps.Length; i++) if (!StepDone(i)) return i;
			return Math.Max(0, steps.Length - 1);
		}

		public bool StepDone(int index)
		{
			var steps = Steps;
			if (index < 0 || index >= steps.Length) return false;
			var step = steps[index];
			var data = Session.Data;
			switch (step.Kind)
			{
				case StepKind.Multi:
					return data.List(step.Key).Length == step.Count;
				default:
					return step.Keys.All(data.Has);
			}
		}

		public bool StepOpen(int index) => Enumerable.Range(0, index).All(StepDone);

		public bool GoToStep(int index)
		{
			if (Mode != Mode.Task || index < 0 || index >= Steps.Length) return false;
			if (!StepOpen(index))
			{
				StepMessage = Loc.T("need.before");
				return false;
			}
			Step = index;
			BeginStep();
			Audio.Play(Sfx.Click, 0.5f);
			return true;
		}

		public bool NextStep() => GoToStep(Step + 1);

		void BeginStep()
		{
			StepMessage = "";
			BarTime = 0f;
			StirRound = StirHits = 0;
			LastStrike = -1;
			SelectedPart = null;
			if (CurrentStep != null && CurrentStep.Kind == StepKind.Hammer) LastStrike = Session.Data.Has("h2") ? 2 : Session.Data.Has("h1") ? 1 : Session.Data.Has("h0") ? 0 : -1;
			var job = ActiveJob;
			if (job != null && CurrentStep != null && CurrentStep.Kind == StepKind.Seal) SelectedSeal = Session.Data.Get("seal");
		}

		/// <summary>Clears what the current step holds, so it can be done again (free).</summary>
		public void ResetStep()
		{
			if (Mode != Mode.Task || CurrentStep == null) return;
			foreach (var key in CurrentStep.Keys) Session.Data.Remove(key);
			BeginStep();
			SelectedSeal = null;
			StepMessage = "";
			Audio.Play(Sfx.Click, 0.5f);
			SaveNow();
		}

		void UpdateTask(EngineInput input)
		{
			var step = CurrentStep;
			if (step == null) return;
			if (step.Kind == StepKind.Bar && !Session.Data.Has(step.Key)) BarTime += Time.unscaledDeltaTime;
			else if (step.Kind == StepKind.Stir && StirRound < 3 && !Session.Data.Has(step.Key)) BarTime += Time.unscaledDeltaTime;
			if (input.Player.Jump.WasPressedThisFrame())
			{
				if (step.Kind == StepKind.Bar) LockBar();
				else if (step.Kind == StepKind.Stir) StirPress();
			}
			if (Billet != null) PaintBillet();
		}

		void PaintBillet()
		{
			var block = new MaterialPropertyBlock();
			var heat = 0f;
			var job = ActiveJob;
			if (job != null && job.Profession == Ids.Forge)
			{
				if (Session.Data.Has("heat")) heat = Session.Data.Float("heat");
				else if (CurrentStep != null && CurrentStep.Kind == StepKind.Bar) heat = Marker;
			}
			Billet.GetPropertyBlock(block);
			block.SetColor("_BaseColor", Color.Lerp(new Color(0.35f, 0.35f, 0.38f), new Color(1f, 0.45f, 0.1f), heat));
			Billet.SetPropertyBlock(block);
		}

		// ---- the interactions of the steps ----

		/// <summary>The marker of the bar: 0 to 1 and back.</summary>
		public float Marker => TimingMath.Marker(BarTime, TimingMath.BarSpeed);

		/// <summary>The sweeping marker of the current stirring or stitching round, and the zone it has to be in.</summary>
		public float StirMarker => TimingMath.Marker(BarTime, StirSpeed);
		public float StirSpeed => Stitching ? TimingMath.StitchSpeeds[Math.Min(StirRound, 2)] : TimingMath.StirSpeeds[Math.Min(StirRound, 2)];
		public float StirCentre => Stitching ? TimingMath.StitchCentres[Math.Min(StirRound, 2)] : TimingMath.StirCentres[Math.Min(StirRound, 2)];
		bool Stitching => CurrentStep != null && CurrentStep.Stitch;

		bool Active(StepKind kind) => Mode == Mode.Task && CurrentStep != null && CurrentStep.Kind == kind;

		/// <summary>The one answer of a Choose step or the inspection: the verdict, the finish, the fabric, the label.</summary>
		public bool Pick(string value)
		{
			var step = CurrentStep;
			if (Mode != Mode.Task || step == null || (step.Kind != StepKind.Choose && step.Kind != StepKind.Inspect)) return false;
			Session.Data.Set(step.Key, value);
			Audio.Play(Sfx.Click, 0.6f);
			SaveNow();
			return true;
		}

		/// <summary>Adds or removes one item of a multiple-choice step. At most the needed number can be held.</summary>
		public bool Toggle(string item)
		{
			if (!Active(StepKind.Multi)) return false;
			var step = CurrentStep;
			var list = Session.Data.List(step.Key).ToList();
			if (list.Contains(item)) list.Remove(item);
			else if (list.Count < step.Count) list.Add(item);
			else
			{
				StepMessage = Loc.T("chosen.of", step.Count, step.Count);
				return false;
			}
			if (list.Count == 0) Session.Data.Remove(step.Key);
			else Session.Data.SetList(step.Key, list);
			Audio.Play(Sfx.Click, 0.6f);
			SaveNow();
			return true;
		}

		public string[] Chosen => CurrentStep == null ? new string[0] : Session.Data.List(CurrentStep.Key);

		public void SelectPart(string item)
		{
			if (!Active(StepKind.Parts)) return;
			SelectedPart = SelectedPart == item ? null : item;
			Audio.Play(Sfx.Click, 0.5f);
		}

		public string PartIn(int slot) => Session.Data.Get("s" + slot);

		public bool PartUsed(string item) => Enumerable.Range(0, 3).Any(i => PartIn(i) == item);

		/// <summary>Puts the selected part in a slot (moving it if it was in another one); with nothing selected, an occupied slot is emptied.</summary>
		public bool PlaceInSlot(int slot)
		{
			if (!Active(StepKind.Parts) || slot < 0 || slot > 2) return false;
			var data = Session.Data;
			if (SelectedPart == null)
			{
				if (!data.Has("s" + slot)) return false;
				data.Remove("s" + slot);
			}
			else
			{
				for (var i = 0; i < 3; i++) if (PartIn(i) == SelectedPart) data.Remove("s" + i);
				data.Set("s" + slot, SelectedPart);
				SelectedPart = null;
			}
			Audio.Play(Sfx.Click, 0.7f);
			SaveNow();
			return true;
		}

		/// <summary>Locks the sweeping marker of a bar step where it is.</summary>
		public bool LockBar()
		{
			if (!Active(StepKind.Bar) || Session.Data.Has(CurrentStep.Key)) return false;
			var value = Mathf.Clamp01(Marker);
			Session.Data.Set(CurrentStep.Key, value);
			StepMessage = Loc.T("locked.at", Mathf.RoundToInt(value * 100f));
			PlayStepSound();
			SaveNow();
			return true;
		}

		/// <summary>One press of a stirring or stitching round. Three rounds in all; the number of hits is kept when the third is done.</summary>
		public bool StirPress()
		{
			if (!Active(StepKind.Stir) || StirRound >= 3 || Session.Data.Has(CurrentStep.Key)) return false;
			var hit = TimingMath.Hit(StirMarker, StirCentre, TimingMath.ZoneHalfWidth);
			if (hit) StirHits++;
			StirRound++;
			BarTime = 0f;
			PlayStepSound();
			Audio.Play(hit ? Sfx.Good : Sfx.Bad, 0.4f);
			if (StirRound >= 3)
			{
				Session.Data.Set(CurrentStep.Key, StirHits);
				StepMessage = Loc.T("chosen.of", StirHits, 3);
				SaveNow();
			}
			return hit;
		}

		void PlayStepSound()
		{
			var job = ActiveJob;
			if (job == null) return;
			switch (job.Profession)
			{
				case Ids.Forge: Audio.Play(Sfx.Hammer); break;
				case Ids.Cooking: Audio.Play(Sfx.Bubble); break;
				case Ids.Tailor: Audio.Play(Sfx.Cloth); break;
				default: Audio.Play(Sfx.Tick); break;
			}
		}

		/// <summary>One hammer strike at a point of the anvil (0 to 1 on both axes, y up).</summary>
		public int Strike(Vector2 point)
		{
			if (!Active(StepKind.Hammer)) return -1;
			var job = ActiveJob;
			var index = LastStrike + 1;
			if (index > 2) return -1;
			var score = TaskScoring.StrikeScore(point, job.Hammer[index]);
			Session.Data.Set("h" + index, point);
			LastStrike = index;
			StepMessage = Loc.T("trace.good", score);
			Audio.Play(Sfx.Hammer);
			SaveNow();
			return score;
		}

		/// <summary>The drawn path, in panel units. Returns the accuracy that counts (with the guide), or -1 when it was too short; a good one is kept.</summary>
		public int SubmitTrace(IList<Vector2> points)
		{
			if (!Active(StepKind.Trace)) return -1;
			var job = ActiveJob;
			if (points == null || points.Count < MasterConfig.MinTracePoints)
			{
				StepMessage = Loc.T("trace.short");
				return -1;
			}
			var raw = TraceMath.Score(points, MasterContent.Path(job.PathId));
			var effective = TaskScoring.WithGuide(raw, Session.Has(Ids.Guide));
			var need = job.Profession == Ids.Scribe ? MasterConfig.ScribeMinTrace : MasterConfig.TailorMinCut;
			if (effective < need)
			{
				StepMessage = Loc.T("trace.bad", effective);
				Audio.Play(Sfx.Bad, 0.4f);
				return effective;
			}
			Session.Data.Set(CurrentStep.Key, raw);
			StepMessage = Loc.T("trace.good", effective);
			Audio.Play(job.Profession == Ids.Scribe ? Sfx.Quill : Sfx.Cloth);
			SaveNow();
			return effective;
		}

		public void SelectSeal(string id)
		{
			if (Active(StepKind.Seal)) SelectedSeal = id;
		}

		/// <summary>Puts the chosen seal on the parchment (0 to 1 on both axes, y up). It can be moved or removed.</summary>
		public bool PlaceSeal(float x, float y)
		{
			if (!Active(StepKind.Seal) || SelectedSeal == null) return false;
			Session.Data.Set("seal", SelectedSeal);
			Session.Data.Set("sealpos", new Vector2(x, y));
			StepMessage = Loc.T("seal.placed");
			Audio.Play(Sfx.Stamp);
			SaveNow();
			return true;
		}

		public void RemoveSeal()
		{
			if (!Active(StepKind.Seal)) return;
			Session.Data.Remove("seal");
			Session.Data.Remove("sealpos");
			SaveNow();
		}

		/// <summary>Judges the active commission. A rejection explains itself and pays nothing; a success pays once.</summary>
		public bool Submit()
		{
			if (Mode != Mode.Task || Session.ActiveJobId == null) return false;
			if (!Session.ReadyToSubmit)
			{
				StepMessage = Loc.T("need.before");
				Toast(Loc.T("need.before"));
				return false;
			}
			if (IsGuest) return SubmitAsGuest();
			var level = Session.Level;
			var rank = Session.Rank(Session.SelectedProfession);
			var receipt = Session.Submit();
			ResultTraining = false;
			LeveledUp = RankedUp = HorseLeveled = false;
			if (receipt == null)
			{
				ResultSuccess = false;
				Audio.Play(Sfx.Bad);
			}
			else
			{
				ResultSuccess = true;
				LeveledUp = Session.Level > level;
				RankedUp = Session.Rank(receipt.Profession) > rank;
				Audio.Play(Sfx.Good);
				Audio.Play(Sfx.Coin, 0.7f);
				if (LeveledUp || RankedUp) Audio.Play(Sfx.LevelUp, 0.6f);
			}
			SaveNow();
			SetMode(Mode.Result);
			return true;
		}

		/// <summary>A guest judges its own answers for the screen; a pass is sent to the host, which judges again, pays and answers.</summary>
		bool SubmitAsGuest()
		{
			if (Net.Pending) return false;
			var outcome = Session.Judge();
			if (outcome == null || !outcome.Success)
			{
				ResultTraining = false;
				ResultSuccess = false;
				LeveledUp = RankedUp = HorseLeveled = false;
				Audio.Play(Sfx.Bad);
				SetMode(Mode.Result);
				return true;
			}
			Net.RequestSubmit();
			Toast(Loc.T("net.waiting"));
			return true;
		}

		/// <summary>After a rejection: a clean attempt of the same commission, free.</summary>
		public void RetryAfterReject()
		{
			if (Mode != Mode.Result || ResultTraining || ResultSuccess || Session.ActiveJobId == null) return;
			Session.Retry();
			Step = 0;
			SelectedPartClear();
			BeginStep();
			SaveNow();
			SetMode(Mode.Task);
		}

		/// <summary>After a success: the same commission again for the smaller replay reward.</summary>
		public bool Again()
		{
			if (Mode != Mode.Result || ResultTraining || !ResultSuccess) return false;
			var id = Session.LastReceipt.Id;
			SetMode(Mode.Orders);
			return Take(id);
		}

		public void LeaveResult()
		{
			if (Mode != Mode.Result) return;
			if (ResultTraining) SetMode(Mode.Stable);
			else SetMode(Mode.Orders);
		}

		// ---- the stable ----

		void Report(CareResult result)
		{
			if (Mode != Mode.Stable) return;
			if (!result.Ok)
			{
				Toast(Loc.T(result.Message));
				Audio.Play(Sfx.Bad);
				return;
			}
			Toast(Loc.T(result.Message + ".done"));
			Horse.Play(result.Anim, (float)Math.Min(result.Seconds, 4.0));
			switch (result.Anim)
			{
				case HorseAnim.Eat: Audio.Play(Sfx.Munch); break;
				case HorseAnim.Groom: Audio.Play(Sfx.Brush); break;
				case HorseAnim.Pet: Audio.Play(Sfx.Neigh, 0.7f); break;
				default: Audio.Play(Sfx.Click); break;
			}
			SaveNow();
		}

		public void FeedHay() { if (IsGuest) Net.RequestCare("hay"); else Report(Session.FeedHay()); }
		public void FeedRation() { if (IsGuest) Net.RequestCare("ration"); else Report(Session.FeedRation()); }
		public void Groom() { if (IsGuest) Net.RequestCare("groom"); else Report(Session.Groom(DateTime.UtcNow)); }
		public void PetIt() { if (IsGuest) Net.RequestCare("pet"); else Report(Session.Pet(DateTime.UtcNow)); }
		public void Rest() { if (IsGuest) Net.RequestCare("rest"); else Report(Session.Rest()); }

		/// <summary>Starts a training. Returns false (with a reason on screen) when it may not start.</summary>
		public bool StartTraining(string kind)
		{
			if (Mode != Mode.Stable) return false;
			if (IsGuest)
			{
				Toast(Loc.T("net.trainhost"));
				Audio.Play(Sfx.Bad);
				return false;
			}
			var block = Session.TrainingBlock(kind);
			if (block != null)
			{
				Toast(Loc.T(block));
				Audio.Play(Sfx.Bad);
				return false;
			}
			var run = Session.StartTraining(kind);
			if (run == null) return false;
			Horse.Stop();
			Audio.Play(Sfx.Click);
			if (kind == Ids.Obstacles)
			{
				Run = new ObstacleRunLogic();
				Run.Resolved += OnObstacleResolved;
				Track.ResetAll();
				Horse.Play(HorseAnim.Run, 9999f);
				SetMode(Mode.Obstacle, true);
			}
			else
			{
				Timing = new CommandTimingLogic();
				Timing.RoundEnded += OnRoundEnded;
				SetMode(Mode.Command);
			}
			SaveNow();
			return true;
		}

		void UpdateObstacle(EngineInput input)
		{
			if (Run == null) return;
			if (input.Player.Jump.WasPressedThisFrame()) Jump();
			Run.Update(Time.deltaTime);
			Track.Place(Run.Time);
			Horse.JumpOffset = Run.JumpHeight;
			if (Run.Jumping) { if (Horse.Anim != HorseAnim.Jump) Horse.Play(HorseAnim.Jump, 9999f); }
			else if (Horse.Anim == HorseAnim.Jump || Horse.Anim == HorseAnim.Idle) Horse.Play(HorseAnim.Run, 9999f);
			if (!Run.Jumping && Time.time >= _nextHoof)
			{
				_nextHoof = Time.time + 0.3f;
				Audio.Play(Sfx.Hoof, 0.6f);
			}
			if (Run.Finished) FinishTraining(Run.Cleared);
		}

		public void Jump()
		{
			if (Mode != Mode.Obstacle || Run == null) return;
			if (Run.PressJump()) Audio.Play(Sfx.Whoosh, 0.7f);
		}

		void OnObstacleResolved(int index, bool cleared)
		{
			if (cleared)
			{
				Audio.Play(Sfx.Good, 0.5f);
				return;
			}
			Track.Knock(index);
			Horse.Play(HorseAnim.Stumble, 0.5f);
			Audio.Play(Sfx.Bad, 0.5f);
		}

		void UpdateCommand(EngineInput input)
		{
			if (Timing == null) return;
			if (input.Player.Jump.WasPressedThisFrame()) Command();
			Timing.Update(Time.deltaTime);
			if (Timing.Finished) FinishTraining(Timing.Hits);
		}

		public void Command()
		{
			if (Mode != Mode.Command || Timing == null) return;
			Timing.Press();
		}

		void OnRoundEnded(bool hit)
		{
			if (hit)
			{
				Horse.Play(HorseAnim.Nod, 0.8f);
				Audio.Play(Sfx.Good, 0.6f);
			}
			else
			{
				Horse.Play(HorseAnim.Stumble, 0.5f);
				Audio.Play(Sfx.Bad, 0.5f);
			}
		}

		void FinishTraining(int result)
		{
			var run = Session.ActiveTraining;
			if (run == null) return;
			var level = Session.Level;
			var rank = Session.Rank(Ids.Stable);
			var receipt = Session.CompleteTraining(run.RunId, result);
			Run = null;
			Timing = null;
			Horse.JumpOffset = 0f;
			Track.Show(false);
			ResultTraining = true;
			ResultSuccess = receipt != null;
			LeveledUp = Session.Level > level;
			RankedUp = Session.Rank(Ids.Stable) > rank;
			HorseLeveled = receipt != null && receipt.HorseLevelAfter > receipt.HorseLevelBefore;
			Horse.Apply(Session.Horse);
			Horse.Play(HorseAnim.Celebrate, 2.5f);
			Audio.Play(Sfx.LevelUp, 0.7f);
			Audio.Play(Sfx.Coin, 0.7f);
			SaveNow();
			SetMode(Mode.Result);
			Cam.Go("stable");
		}

		/// <summary>Leaves a training early: nothing is paid and the energy already spent is not refunded.</summary>
		public void AbortTraining()
		{
			if (Mode != Mode.Obstacle && Mode != Mode.Command) return;
			Session.AbortTraining();
			Run = null;
			Timing = null;
			Horse.Stop();
			Horse.JumpOffset = 0f;
			Track.Show(false);
			Toast(Loc.T("train.aborted"));
			SaveNow();
			SetMode(Mode.Stable);
		}

		// ---- upgrades ----

		public bool Buy(string id)
		{
			if (Mode != Mode.Upgrades) return false;
			if (IsGuest)
			{
				Net.RequestBuy(id);
				return true;
			}
			if (!Session.Buy(id))
			{
				if (!Session.Has(id)) Toast(Loc.T("up.short"));
				return false;
			}
			Audio.Play(Sfx.Coin);
			Toast(Loc.T("up.bought", Loc.T(id)));
			SaveNow();
			return true;
		}

		public void Rename(string name)
		{
			name = string.IsNullOrWhiteSpace(name) ? MasterConfig.StartHorseName : name.Trim();
			if (name.Length > 16) name = name.Substring(0, 16);
			Session.Horse.Name = name;
			SaveNow();
		}

		/// <summary>Back to the shell's landing screen (the shell has the Quit button); the progress is saved first.</summary>
		public void Quit() => ShellFlow.QuitToMenu();
	}
}
