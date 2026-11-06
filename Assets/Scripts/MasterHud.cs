using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ITKamianets.Engine.Input;
using ITKamianets.Engine.Net;
using ITKamianets.Engine.Shell;
using ITKamianets.Engine.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Temnorid.Master
{
	/// <summary>All the screens, drawn with plain uGUI built in code, in Ukrainian. It only shows what <see cref="MasterGame"/> says and calls it when a button is pressed or a drag ends.</summary>
	public sealed partial class MasterHud : MonoBehaviour
	{
		public MasterGame Game;

		static readonly Color Panel = new Color(0.17f, 0.12f, 0.08f, 0.94f);
		static readonly Color Ink = new Color(0.95f, 0.9f, 0.78f);
		static readonly Color Gold = new Color(0.93f, 0.76f, 0.36f);
		static readonly Color Muted = new Color(0.75f, 0.7f, 0.6f);
		static readonly Color Parchment = new Color(0.9f, 0.84f, 0.68f);
		static readonly Color Dark = new Color(0.2f, 0.13f, 0.08f);
		static readonly Color Chosen = new Color(0.6f, 0.42f, 0.14f, 1f);
		static readonly Color Plain = new Color(0.3f, 0.22f, 0.14f, 1f);
		static readonly Color Disabled = new Color(0.22f, 0.18f, 0.15f, 1f);
		static readonly Color Good = new Color(0.4f, 0.7f, 0.35f);

		UiBuilder _ui;
		Sprite _ring, _disc;
		readonly Dictionary<Mode, GameObject> _screens = new Dictionary<Mode, GameObject>();
		GameObject _topBar, _tipBox, _toastBox, _continue;
		Text _coins, _level, _xp, _activeJob, _tip, _toast, _titleNote;
		Mode _shown = (Mode)(-1);

		// The shell's bars and the shared workshop.
		ShellChrome _chrome;
		Text _presence, _overviewLeft, _overviewRight;
		float _netAt;

		static readonly string[] Order = { Ids.Stable, Ids.Scribe, Ids.Forge, Ids.Workshop, Ids.Cooking, Ids.Tailor, Ids.Apothecary };


		// Orders.
		Text _ordersHead, _ordersTag;
		Text[] _jobTexts;
		Button[] _jobButtons;
		Text[] _jobButtonTexts;
		GameObject _abandonButton;

		// Result, upgrades, journal, settings.
		Text _resultTitle, _resultBody;
		GameObject _retryButton, _againButton, _nextButton, _stableButton;
		Button[] _buyButtons;
		Text[] _upgradeTexts, _buyTexts;
		Text _journalJobs, _journalSkills, _journalHistory;
		Text _masterText, _sfxText, _fullscreenText, _helpText, _recoveryText, _recoveryNote;

		// Stable.
		Text _horseTitle;
		RectTransform[] _needFill;
		Text[] _needText;
		Button[] _careButtons;
		Button _obstacleButton, _commandButton;
		Text _obstacleText, _commandText;

		// Trainings.
		Text _runInfo, _runHits, _commandInfo;
		RectTransform _runProgress, _cmdBar, _cmdZone, _cmdMarker;
		Text _cmdRound;

		void Start()
		{
			_ui = new UiBuilder
			{
				ButtonColour = Plain,
				ButtonHighlight = new Color(1f, 0.92f, 0.65f),
				ButtonSelected = new Color(1f, 0.85f, 0.45f),
				ButtonPressed = new Color(0.8f, 0.65f, 0.3f),
			};
			UiBuilder.EnsureEventSystem(transform);
			_ring = UiShapes.Circle(false);
			_disc = UiShapes.Circle(true);
			var canvas = UiBuilder.CreateCanvas(transform);
			_chrome = ShellChrome.Create(transform);
			_chrome.PlaceSelected += OnPlace;

			// Every screen lives in the frame the shell's bars leave free.
			var frame = new GameObject("Frame", typeof(RectTransform));
			frame.transform.SetParent(canvas.transform, false);
			var frameRect = (RectTransform)frame.transform;
			frameRect.anchorMin = new Vector2(0f, _chrome.BottomFraction);
			frameRect.anchorMax = new Vector2(1f, 1f - _chrome.TopFraction);
			frameRect.offsetMin = Vector2.zero;
			frameRect.offsetMax = Vector2.zero;
			var root = frame.transform;
			BuildOrders(root);
			BuildTask(root);
			BuildStable(root);
			BuildObstacle(root);
			BuildCommand(root);
			BuildResult(root);
			BuildUpgrades(root);
			BuildJournal(root);
			BuildOverview(root);
			BuildHelp(root);
			BuildPause(root);
			BuildSettings(root);
			BuildConfirm(root, Mode.ConfirmNew, Loc.T("newconfirm"), () => Game.NewGame(), () => Game.Back());
			BuildConfirm(root, Mode.ConfirmReset, Loc.T("reset.confirm"), () => Game.ConfirmReset(), () => Game.Back());
			BuildRecovery(root);
			BuildMentor(root);
			BuildTitle(root);
			BuildTabArrows();
			BuildOverlays(root);
		}

		// ---- building blocks ----

		Button Btn(Transform parent, string text, float x0, float y0, float x1, float y1, Action onClick, int size = 30)
		{
			var button = _ui.Button(text, parent, new Vector2(x0, y0), new Vector2(x1, y1), () =>
			{
				if (Game != null) Game.Audio.Play(Sfx.Click, 0.5f);
				onClick();
			});
			var label = button.GetComponentInChildren<Text>();
			label.fontSize = size;
			label.horizontalOverflow = HorizontalWrapMode.Wrap;
			return button;
		}

		Text Lbl(Transform parent, string text, int size, TextAnchor anchor, float x0, float y0, float x1, float y1, Color colour, bool wrap = false)
			=> _ui.Label("Label", parent, text, size, anchor, new Vector2(x0, y0), new Vector2(x1, y1), colour, wrap);

		static GameObject Box(string name, Transform parent, float x0, float y0, float x1, float y1, Color colour) => UiBuilder.Box(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), colour);

		static void Tint(Button button, Color colour) => button.GetComponent<Image>().color = colour;

		GameObject Overlay(Mode mode, Transform root, float x0, float y0, float x1, float y1, float dim = 0.45f)
		{
			var screen = Box(mode.ToString(), root, 0f, 0f, 1f, 1f, new Color(0f, 0f, 0f, dim));
			_screens[mode] = screen;
			return Box("Card", screen.transform, x0, y0, x1, y1, Panel);
		}

		/// <summary>A screen that lets the 3D scene show through and does not block it.</summary>
		Transform Clear(Mode mode, Transform root)
		{
			var screen = Box(mode.ToString(), root, 0f, 0f, 1f, 1f, Color.clear);
			screen.GetComponent<Image>().raycastTarget = false;
			_screens[mode] = screen;
			return screen.transform;
		}

		static void Part(Transform parent, float x0, float y0, float x1, float y1, Color colour)
		{
			var box = UiBuilder.Box("Part", parent, new Vector2(x0, y0), new Vector2(x1, y1), colour);
			box.GetComponent<Image>().raycastTarget = false;
		}

		RectTransform NewHost(string name, Transform parent, float size)
		{
			var go = new GameObject(name, typeof(RectTransform));
			go.transform.SetParent(parent, false);
			var rect = (RectTransform)go.transform;
			rect.anchorMin = rect.anchorMax = Vector2.zero;
			rect.pivot = Vector2.zero;
			rect.sizeDelta = new Vector2(size, size);
			return rect;
		}

		RectTransform NewImage(string name, Transform parent, Sprite sprite, Color colour, float size)
		{
			var rect = NewHost(name, parent, size);
			var image = rect.gameObject.AddComponent<Image>();
			image.sprite = sprite;
			image.color = colour;
			image.raycastTarget = false;
			return rect;
		}

		void Place(RectTransform rect, Vector2 normalized)
		{
			var parent = (RectTransform)rect.parent;
			rect.gameObject.SetActive(true);
			rect.anchoredPosition = new Vector2(normalized.x * parent.rect.width - rect.sizeDelta.x * 0.5f, normalized.y * parent.rect.height - rect.sizeDelta.y * 0.5f);
		}

		static void Hide(RectTransform rect) => rect.gameObject.SetActive(false);

		static string Name(string profession) => Loc.T("prof." + profession);

		// ---- icons from rectangles: the shapes differ, so the colour is never what tells the cards apart ----

		static void BuildIcon(Transform host, string profession, Color c)
		{
			var w = new Color(1f, 1f, 1f, 0.55f);
			switch (profession)
			{
				case Ids.Stable:
					Part(host, 0.18f, 0.2f, 0.34f, 0.85f, c);
					Part(host, 0.66f, 0.2f, 0.82f, 0.85f, c);
					Part(host, 0.18f, 0.12f, 0.82f, 0.3f, c);
					Part(host, 0.22f, 0.5f, 0.3f, 0.56f, w);
					Part(host, 0.7f, 0.5f, 0.78f, 0.56f, w);
					break;
				case Ids.Scribe:
					Part(host, 0.25f, 0.08f, 0.75f, 0.92f, c);
					for (var i = 0; i < 4; i++) Part(host, 0.32f, 0.72f - i * 0.14f, 0.68f - (i == 3 ? 0.18f : 0f), 0.76f - i * 0.14f, w);
					Part(host, 0.5f, 0.12f, 0.64f, 0.26f, new Color(0.8f, 0.2f, 0.2f, 0.9f));
					break;
				case Ids.Forge:
					Part(host, 0.1f, 0.55f, 0.9f, 0.75f, c);
					Part(host, 0.0f, 0.62f, 0.14f, 0.7f, c);
					Part(host, 0.35f, 0.35f, 0.65f, 0.55f, c);
					Part(host, 0.2f, 0.15f, 0.8f, 0.35f, c);
					Part(host, 0.5f, 0.78f, 0.58f, 0.95f, new Color(1f, 0.55f, 0.15f, 0.9f));
					break;
				case Ids.Workshop:
					Part(host, 0.3f, 0.3f, 0.7f, 0.7f, c);
					Part(host, 0.42f, 0.14f, 0.58f, 0.3f, c);
					Part(host, 0.42f, 0.7f, 0.58f, 0.86f, c);
					Part(host, 0.14f, 0.42f, 0.3f, 0.58f, c);
					Part(host, 0.7f, 0.42f, 0.86f, 0.58f, c);
					Part(host, 0.43f, 0.43f, 0.57f, 0.57f, new Color(0.1f, 0.07f, 0.05f, 0.9f));
					break;
				case Ids.Cooking:
					Part(host, 0.2f, 0.2f, 0.8f, 0.55f, c);
					Part(host, 0.14f, 0.55f, 0.86f, 0.62f, c);
					Part(host, 0.04f, 0.4f, 0.2f, 0.47f, c);
					Part(host, 0.8f, 0.4f, 0.96f, 0.47f, c);
					Part(host, 0.34f, 0.68f, 0.4f, 0.92f, w);
					Part(host, 0.5f, 0.7f, 0.56f, 0.95f, w);
					Part(host, 0.66f, 0.68f, 0.72f, 0.9f, w);
					break;
				case Ids.Tailor:
					Part(host, 0.3f, 0.18f, 0.7f, 0.82f, c);
					Part(host, 0.24f, 0.1f, 0.76f, 0.2f, c);
					Part(host, 0.24f, 0.8f, 0.76f, 0.9f, c);
					for (var i = 0; i < 4; i++) Part(host, 0.3f, 0.26f + i * 0.14f, 0.7f, 0.31f + i * 0.14f, w);
					break;
				default:
					Part(host, 0.25f, 0.1f, 0.75f, 0.62f, c);
					Part(host, 0.35f, 0.62f, 0.65f, 0.76f, c);
					Part(host, 0.3f, 0.76f, 0.7f, 0.9f, c);
					Part(host, 0.35f, 0.26f, 0.65f, 0.46f, w);
					break;
			}
		}

		// ---- title ----

		void BuildTitle(Transform root)
		{
			BuildVillage(root);
		}

		void BuildRecovery(Transform root)
		{
			var card = Overlay(Mode.Recovery, root, 0.25f, 0.2f, 0.75f, 0.8f, 0.75f).transform;
			Lbl(card, Loc.T("recovery.head"), 44, TextAnchor.MiddleCenter, 0.05f, 0.82f, 0.95f, 0.97f, Gold);
			_recoveryText = Lbl(card, "", 32, TextAnchor.MiddleCenter, 0.05f, 0.64f, 0.95f, 0.82f, Ink, true);
			_recoveryNote = Lbl(card, "", 28, TextAnchor.MiddleCenter, 0.05f, 0.5f, 0.95f, 0.64f, new Color(1f, 0.6f, 0.5f), true);
			Btn(card, Loc.T("recovery.backup"), 0.1f, 0.34f, 0.9f, 0.48f, () => Game.TryBackup(), 32);
			Btn(card, Loc.T("recovery.new"), 0.1f, 0.18f, 0.9f, 0.32f, () => Game.StartFresh(), 32);
			Btn(card, Loc.T("recovery.quit"), 0.1f, 0.02f, 0.9f, 0.16f, () => Game.Quit(), 32);
		}

		// ---- the always-there pieces ----

		void BuildOverlays(Transform root)
		{
			_topBar = Box("TopBar", root, 0.01f, 0.915f, 0.99f, 0.99f, new Color(0f, 0f, 0f, 0.62f));
			_coins = Lbl(_topBar.transform, "", 30, TextAnchor.MiddleLeft, 0.02f, 0f, 0.19f, 1f, Gold);
			_level = Lbl(_topBar.transform, "", 30, TextAnchor.MiddleLeft, 0.2f, 0f, 0.4f, 1f, Ink);
			_xp = Lbl(_topBar.transform, "", 30, TextAnchor.MiddleLeft, 0.41f, 0f, 0.59f, 1f, Ink);
			_activeJob = Lbl(_topBar.transform, "", 26, TextAnchor.MiddleLeft, 0.6f, 0f, 0.99f, 1f, Muted);
			_topBar.GetComponent<Image>().raycastTarget = false;

			_presence = Lbl(root, "", 22, TextAnchor.UpperRight, 0.5f, 0.82f, 0.99f, 0.912f, Ink, true);
			_presence.supportRichText = true;
			_presence.raycastTarget = false;
			_tipBox = Box("Tip", root, 0.04f, 0.83f, 0.96f, 0.912f, new Color(0.1f, 0.18f, 0.28f, 0.94f));
			_tip = Lbl(_tipBox.transform, "", 26, TextAnchor.MiddleLeft, 0.015f, 0f, 0.85f, 1f, new Color(0.8f, 0.9f, 1f), true);
			Btn(_tipBox.transform, Loc.T("close"), 0.86f, 0.06f, 0.99f, 0.94f, () => Game.DismissTip(), 24);
			_toastBox = Box("Toast", root, 0.2f, 0.838f, 0.8f, 0.905f, new Color(0f, 0f, 0f, 0.85f));
			_toast = Lbl(_toastBox.transform, "", 28, TextAnchor.MiddleCenter, 0.02f, 0f, 0.98f, 1f, Ink, true);
			_toastBox.GetComponent<Image>().raycastTarget = false;
		}

		// ---- orders ----

		void BuildOrders(Transform root)
		{
			var card = Overlay(Mode.Orders, root, 0.03f, 0.1f, 0.5f, 0.76f, 0.0f).transform;
			_screens[Mode.Orders].GetComponent<Image>().raycastTarget = false;
			_ordersHead = Lbl(card, "", 44, TextAnchor.MiddleLeft, 0.04f, 0.88f, 0.96f, 0.99f, Gold);
			_ordersTag = Lbl(card, "", 24, TextAnchor.MiddleLeft, 0.04f, 0.8f, 0.96f, 0.88f, Muted, true);
			_jobTexts = new Text[2];
			_jobButtons = new Button[2];
			_jobButtonTexts = new Text[2];
			for (var i = 0; i < 2; i++)
			{
				var index = i;
				var y1 = 0.79f - i * 0.33f;
				_jobTexts[i] = Lbl(card, "", 24, TextAnchor.UpperLeft, 0.04f, y1 - 0.22f, 0.96f, y1, Ink, true);
				_jobButtons[i] = Btn(card, "", 0.04f, y1 - 0.32f, 0.8f, y1 - 0.24f, () => TakeJob(index), 26);
				_jobButtonTexts[i] = _jobButtons[i].GetComponentInChildren<Text>();
			}
			_abandonButton = Btn(card, Loc.T("task.abandon"), 0.5f, 0.015f, 0.96f, 0.09f, () => Game.AbandonActive(), 22).gameObject;
		}

		string CurrentJobId(int index) => MasterContent.JobsOf(Game.Session.SelectedProfession).ElementAt(index).Id;

		void TakeJob(int index)
		{
			var id = CurrentJobId(index);
			if (Game.TakeBlock(id) != null && Game.Session.ActiveJobId != id)
			{
				Game.Toast(Loc.T(Game.TakeBlock(id)));
				return;
			}
			Game.Take(id);
		}

		void RefreshOrders()
		{
			var s = Game.Session;
			var profession = s.SelectedProfession;
			if (profession == Ids.Stable) return;
			UiBuilder.SetText(_ordersHead, Loc.T("orders") + ": " + Name(profession));
			UiBuilder.SetText(_ordersTag, Loc.T("prof." + profession + ".tag") + " · " + Loc.T("rank." + s.Rank(profession)) + " (" + Loc.T("skill") + " " + s.SkillXp(profession) + ")");
			var jobs = MasterContent.JobsOf(profession).ToArray();
			for (var i = 0; i < 2; i++)
			{
				var job = jobs[i];
				var status = s.StatusOf(job.Id);
				var active = s.ActiveJobId == job.Id;
				var best = s.BestScore(job.Id);
				var state = active ? Loc.T("orders.active") : status == JobStatus.Completed ? Loc.T("orders.completed") + " · " + Loc.T("orders.best") + " " + best : status == JobStatus.Locked ? Loc.T("orders.locked") : Loc.T("orders.available");
				var by = Game.Net != null && Game.Net.ClaimedByOther(job.Id) ? Game.Net.ClaimerName(job.Id) : null;
				if (by != null) state += " · " + Loc.T("net.working", by);
				UiBuilder.SetText(_jobTexts[i], job.Id + " · " + Loc.Title(job.Id) + "\n<size=22>" + Loc.Order(job.Id) + "</size>\n<size=22>" + state + "</size>");
				var block = Game.TakeBlock(job.Id);
				var blocked = block != null && !active;
				UiBuilder.SetText(_jobButtonTexts[i], active ? Loc.T("task.continue") : blocked ? Loc.T(block) : status == JobStatus.Completed ? Loc.T("orders.repeat") : Loc.T("orders.take"));
				Tint(_jobButtons[i], blocked ? Disabled : Plain);
			}
			_abandonButton.SetActive(s.ActiveJobId != null);
			if (s.ActiveJobId != null) UiBuilder.SetText(_abandonButton.GetComponentInChildren<Text>(), Loc.T("task.abandon") + ": " + s.ActiveJobId);
		}

		// ---- upgrades, journal ----

		void BuildUpgrades(Transform root)
		{
			var card = Overlay(Mode.Upgrades, root, 0.1f, 0.15f, 0.9f, 0.85f).transform;
			Lbl(card, Loc.T("up.head"), 44, TextAnchor.MiddleCenter, 0.03f, 0.88f, 0.97f, 0.99f, Gold);
			_buyButtons = new Button[3];
			_upgradeTexts = new Text[3];
			_buyTexts = new Text[3];
			for (var i = 0; i < 3; i++)
			{
				var id = MasterContent.Upgrades[i].Id;
				var y1 = 0.86f - i * 0.27f;
				_upgradeTexts[i] = Lbl(card, "", 26, TextAnchor.MiddleLeft, 0.04f, y1 - 0.24f, 0.68f, y1, Ink, true);
				_buyButtons[i] = Btn(card, "", 0.7f, y1 - 0.2f, 0.96f, y1 - 0.04f, () => Game.Buy(id), 24);
				_buyTexts[i] = _buyButtons[i].GetComponentInChildren<Text>();
			}
			Btn(card, Loc.T("back"), 0.35f, 0.01f, 0.65f, 0.09f, () => Game.Back(), 26);
		}

		void RefreshUpgrades()
		{
			var s = Game.Session;
			for (var i = 0; i < 3; i++)
			{
				var u = MasterContent.Upgrades[i];
				UiBuilder.SetText(_upgradeTexts[i], Loc.T(u.Id) + " (" + u.Price + ")\n" + Loc.T(u.Id + ".d"));
				var owned = s.Has(u.Id);
				UiBuilder.SetText(_buyTexts[i], owned ? Loc.T("up.owned") : s.Coins >= u.Price ? Loc.T("up.buy") : Loc.T("up.short"));
				Tint(_buyButtons[i], owned ? new Color(0.25f, 0.4f, 0.22f) : s.Coins >= u.Price ? Plain : Disabled);
			}
		}

		void BuildJournal(Transform root)
		{
			var card = Overlay(Mode.Journal, root, 0.05f, 0.08f, 0.95f, 0.9f).transform;
			Lbl(card, Loc.T("journal.head"), 44, TextAnchor.MiddleCenter, 0.03f, 0.9f, 0.97f, 0.99f, Gold);
			Lbl(card, Loc.T("journal.jobs"), 28, TextAnchor.MiddleLeft, 0.03f, 0.82f, 0.4f, 0.89f, Gold);
			_journalJobs = Lbl(card, "", 22, TextAnchor.UpperLeft, 0.03f, 0.1f, 0.4f, 0.82f, Ink, true);
			Lbl(card, Loc.T("journal.skills"), 28, TextAnchor.MiddleLeft, 0.42f, 0.82f, 0.68f, 0.89f, Gold);
			_journalSkills = Lbl(card, "", 22, TextAnchor.UpperLeft, 0.42f, 0.1f, 0.68f, 0.82f, Ink, true);
			Lbl(card, Loc.T("journal.history"), 28, TextAnchor.MiddleLeft, 0.7f, 0.82f, 0.97f, 0.89f, Gold);
			_journalHistory = Lbl(card, "", 20, TextAnchor.UpperLeft, 0.7f, 0.1f, 0.97f, 0.82f, Muted, true);
			Btn(card, Loc.T("back"), 0.35f, 0.01f, 0.65f, 0.09f, () => Game.Back(), 26);
		}

		void RefreshJournal()
		{
			var s = Game.Session;
			var jobs = new StringBuilder();
			foreach (var j in MasterContent.Jobs)
				jobs.Append(j.Id).Append(" ").Append(Loc.Title(j.Id)).Append(" — ").Append(s.StatusOf(j.Id) == JobStatus.Completed ? Loc.T("journal.done") + " (" + s.BestScore(j.Id) + ")" : Loc.T("journal.notdone")).Append('\n');
			foreach (var k in MasterContent.Trainings)
				jobs.Append(k).Append(" ").Append(Loc.T("train." + k)).Append(" — ").Append(s.FirstTrainings.Contains(k) ? Loc.T("journal.done") + " (" + s.BestScore(k) + ")" : Loc.T("journal.notdone")).Append('\n');
			UiBuilder.SetText(_journalJobs, jobs.ToString());
			var skills = new StringBuilder();
			foreach (var id in Order) skills.Append(Name(id)).Append(": ").Append(Loc.T("rank." + s.Rank(id))).Append(" (").Append(s.SkillXp(id)).Append(")\n");
			skills.Append('\n').Append(Loc.T("level")).Append(": ").Append(s.Level).Append("\n").Append(Loc.T("xp")).Append(": ").Append(s.GlobalXp).Append("\n").Append(Loc.T("coins")).Append(": ").Append(s.Coins);
			UiBuilder.SetText(_journalSkills, skills.ToString());
			var history = new StringBuilder();
			foreach (var h in s.History.Reverse().Take(12)) history.Append(h.id).Append(" · ").Append(h.quality).Append("/100 · +").Append(h.coins).Append(" · +").Append(h.xp).Append('\n');
			if (s.History.Count == 0) history.Append(Loc.T("journal.none"));
			UiBuilder.SetText(_journalHistory, history.ToString());
		}

		// ---- the overview place and the shared workshop ----

		void BuildOverview(Transform root)
		{
			var card = Overlay(Mode.Overview, root, 0.04f, 0.04f, 0.96f, 0.9f).transform;
			Lbl(card, Loc.T("overview.head"), 44, TextAnchor.MiddleCenter, 0.03f, 0.9f, 0.97f, 0.99f, Gold);
			_overviewLeft = Lbl(card, "", 24, TextAnchor.UpperLeft, 0.03f, 0.12f, 0.48f, 0.9f, Ink, true);
			_overviewLeft.supportRichText = true;
			_overviewRight = Lbl(card, "", 22, TextAnchor.UpperLeft, 0.5f, 0.12f, 0.97f, 0.9f, Ink, true);
			_overviewRight.supportRichText = true;
			Btn(card, Loc.T("hub"), 0.3f, 0.01f, 0.7f, 0.11f, () => Game.Back(), 30);
		}

		static string Hex(int colour)
		{
			var c = ShellConfig.Current.ColourOf(colour);
			return "#" + ColorUtility.ToHtmlStringRGB(c);
		}

		static string DoingText(Presence p)
		{
			if (p.Job != null) return Name(p.Profession ?? Ids.Stable) + " · " + p.Job;
			return p.Profession != null ? Name(p.Profession) : Loc.T("net.hub");
		}

		void RefreshOverview()
		{
			var s = Game.Session;
			var h = s.Horse;
			var left = new StringBuilder();
			left.Append("<color=#EDC25C>").Append(Loc.T("overview.wallet")).Append("</color>\n");
			left.Append(Loc.T("coins")).Append(": ").Append(s.Coins).Append('\n');
			left.Append(Loc.T("level")).Append(": ").Append(s.Level).Append(" · ").Append(Loc.T("xp")).Append(": ").Append(s.GlobalXp);
			left.Append(s.Level < MasterConfig.MaxLevel ? " (" + Loc.T("overview.next", MasterConfig.LevelXp[s.Level] - s.GlobalXp) + ")" : " (" + Loc.T("overview.maxlevel") + ")").Append("\n\n");
			left.Append("<color=#EDC25C>").Append(Loc.T("overview.horse")).Append("</color>\n");
			left.Append(h.Name).Append(" · ").Append(Loc.T("horse.stage." + (int)h.Stage)).Append(" · ").Append(Loc.T("horse.level", h.Level)).Append('\n');
			left.Append(Loc.T(NeedKeys[0])).Append(' ').Append(h.Satiety).Append(" · ").Append(Loc.T(NeedKeys[1])).Append(' ').Append(h.Cleanliness).Append('\n');
			left.Append(Loc.T(NeedKeys[2])).Append(' ').Append(h.Energy).Append(" · ").Append(Loc.T(NeedKeys[3])).Append(' ').Append(h.Trust).Append("\n\n");
			left.Append("<color=#EDC25C>").Append(Loc.T("overview.trades")).Append("</color>\n");
			foreach (var id in Order) left.Append(Name(id)).Append(": ").Append(Loc.T("rank." + s.Rank(id))).Append(" (").Append(s.SkillXp(id)).Append(")\n");
			UiBuilder.SetText(_overviewLeft, left.ToString());

			var right = new StringBuilder();
			right.Append("<color=#EDC25C>").Append(Loc.T("net.who")).Append("</color>\n");
			if (Game.Net == null) right.Append(Loc.T("net.alone")).Append('\n');
			else
			{
				var me = NetSession.Instance.Local;
				right.Append("<color=").Append(Hex(me != null ? me.Colour : 0)).Append(">●</color> ").Append(ShellProfile.Current.DisplayName).Append(" (").Append(Game.IsGuest ? "" : Loc.T("net.host") + ", ").Append(DoingText(new Presence { Profession = Game.Mode == Mode.Overview ? null : s.SelectedProfession })).Append(")\n");
				foreach (var p in Game.Net.Others()) right.Append("<color=").Append(Hex(p.Colour)).Append(">●</color> ").Append(p.Name).Append(" (").Append(DoingText(p)).Append(")\n");
			}
			right.Append("\n<color=#EDC25C>").Append(Loc.T("overview.board")).Append("</color>\n");
			foreach (var j in MasterContent.Jobs)
			{
				var status = s.StatusOf(j.Id);
				var by = Game.Net != null ? Game.Net.ClaimerName(j.Id) : (s.ActiveJobId == j.Id ? ShellProfile.Current.DisplayName : null);
				var state = by != null ? Loc.T("net.working", by) : status == JobStatus.Completed ? Loc.T("orders.completed") : status == JobStatus.Locked ? Loc.T("orders.locked") : Loc.T("overview.free");
				right.Append(j.Id).Append(" · ").Append(Loc.Title(j.Id)).Append(" — ").Append(state).Append('\n');
			}
			UiBuilder.SetText(_overviewRight, right.ToString());
		}

		void OnPlace(string id)
		{
			if (Game != null) Game.GoTab(id);
		}

		/// <summary>The tab bar follows the screen the game is on, and the bar and the presence line show who plays along.</summary>
		void SyncChrome(Mode mode)
		{
			if (_chrome == null) return;
			RefreshTabs(mode);
			if (Time.unscaledTime < _netAt) return;
			_netAt = Time.unscaledTime + 0.5f;
			var net = Game.Net;
			if (net == null)
			{
				_chrome.SetStatus("");
				UiBuilder.SetText(_presence, "");
				return;
			}
			var others = net.Others();
			_chrome.SetStatus(others.Count > 0 ? Loc.T("net.with", others[0].Name) : Loc.T("net.alone"));
			var sb = new StringBuilder();
			foreach (var p in others)
			{
				if (sb.Length > 0) sb.Append("    ");
				sb.Append("<color=").Append(Hex(p.Colour)).Append(">●</color> ").Append(p.Name).Append(": ").Append(DoingText(p));
			}
			UiBuilder.SetText(_presence, Game.Mode == Mode.Title || !Game.Live ? "" : sb.ToString());
		}

		// ---- help, pause, settings, confirm ----

		void BuildHelp(Transform root)
		{
			var card = Overlay(Mode.Help, root, 0.12f, 0.12f, 0.88f, 0.88f).transform;
			Lbl(card, Loc.T("help.title"), 44, TextAnchor.MiddleCenter, 0.03f, 0.88f, 0.97f, 0.99f, Gold);
			_helpText = Lbl(card, "", 28, TextAnchor.UpperLeft, 0.05f, 0.14f, 0.95f, 0.88f, Ink, true);
			Btn(card, Loc.T("close"), 0.35f, 0.02f, 0.65f, 0.12f, () => Game.Back(), 28);
		}

		void BuildPause(Transform root)
		{
			var card = Overlay(Mode.Pause, root, 0.33f, 0.18f, 0.67f, 0.82f, 0.6f).transform;
			Lbl(card, Loc.T("pause"), 52, TextAnchor.MiddleCenter, 0.05f, 0.84f, 0.95f, 0.98f, Gold);
			Btn(card, Loc.T("resume"), 0.1f, 0.65f, 0.9f, 0.8f, () => Game.Resume(), 32);
			Btn(card, Loc.T("menu.settings"), 0.1f, 0.46f, 0.9f, 0.61f, () => Game.OpenSettings(), 32);
			Btn(card, Loc.T("help.title"), 0.1f, 0.27f, 0.9f, 0.42f, () => Game.OpenHelp(), 32);
			Btn(card, Loc.T("menu"), 0.1f, 0.06f, 0.9f, 0.21f, () => Game.ToTitle(), 32);
		}

		void BuildSettings(Transform root)
		{
			var screen = Box("Settings", root, 0f, 0f, 1f, 1f, new Color(0.06f, 0.04f, 0.03f, 0.97f));
			_screens[Mode.Settings] = screen;
			var t = screen.transform;
			Lbl(t, Loc.T("menu.settings"), 64, TextAnchor.MiddleCenter, 0.1f, 0.84f, 0.9f, 0.96f, Gold);
			_masterText = Lbl(t, "", 38, TextAnchor.MiddleRight, 0.1f, 0.7f, 0.5f, 0.8f, Ink);
			Btn(t, "−", 0.52f, 0.7f, 0.58f, 0.8f, () => Game.SetVolumes(Game.Settings.masterVolume - 0.1f, Game.Settings.sfxVolume), 40);
			Btn(t, "+", 0.6f, 0.7f, 0.66f, 0.8f, () => Game.SetVolumes(Game.Settings.masterVolume + 0.1f, Game.Settings.sfxVolume), 40);
			_sfxText = Lbl(t, "", 38, TextAnchor.MiddleRight, 0.1f, 0.57f, 0.5f, 0.67f, Ink);
			Btn(t, "−", 0.52f, 0.57f, 0.58f, 0.67f, () => Game.SetVolumes(Game.Settings.masterVolume, Game.Settings.sfxVolume - 0.1f), 40);
			Btn(t, "+", 0.6f, 0.57f, 0.66f, 0.67f, () => Game.SetVolumes(Game.Settings.masterVolume, Game.Settings.sfxVolume + 0.1f), 40);
			_fullscreenText = Btn(t, "", 0.3f, 0.44f, 0.7f, 0.54f, () => Game.SetFullscreen(!Game.Settings.fullscreen), 34).GetComponentInChildren<Text>();
			Btn(t, Loc.T("reset"), 0.3f, 0.31f, 0.7f, 0.41f, () => Game.RequestReset(), 30);
			Lbl(t, Loc.T("controls"), 28, TextAnchor.MiddleCenter, 0.12f, 0.17f, 0.88f, 0.29f, Muted, true);
			Btn(t, Loc.T("back"), 0.38f, 0.04f, 0.62f, 0.14f, () => Game.Back(), 32);
		}

		void BuildConfirm(Transform root, Mode mode, string text, Action yes, Action no)
		{
			var card = Overlay(mode, root, 0.25f, 0.3f, 0.75f, 0.7f, 0.7f).transform;
			Lbl(card, text, 36, TextAnchor.MiddleCenter, 0.05f, 0.4f, 0.95f, 0.95f, Ink, true);
			Btn(card, Loc.T("yes"), 0.1f, 0.08f, 0.45f, 0.35f, () => yes(), 34);
			Btn(card, Loc.T("no"), 0.55f, 0.08f, 0.9f, 0.35f, () => no(), 34);
		}

		// ---- the stable ----

		static readonly string[] NeedKeys = { "bar.satiety", "bar.cleanliness", "bar.energy", "bar.trust" };

		void BuildStable(Transform root)
		{
			var t = Clear(Mode.Stable, root);
			_horseTitle = Lbl(t, "", 36, TextAnchor.MiddleLeft, 0.02f, 0.75f, 0.6f, 0.84f, Gold);
			var panel = Box("Needs", t, 0.02f, 0.02f, 0.66f, 0.17f, new Color(0f, 0f, 0f, 0.55f)).transform;
			_needFill = new RectTransform[4];
			_needText = new Text[4];
			for (var i = 0; i < 4; i++)
			{
				var x0 = 0.015f + i * 0.245f;
				_needText[i] = Lbl(panel, "", 22, TextAnchor.MiddleLeft, x0, 0.5f, x0 + 0.23f, 0.95f, Ink);
				Box("Back", panel, x0, 0.12f, x0 + 0.23f, 0.45f, new Color(0.25f, 0.2f, 0.16f, 1f));
				var fill = Box("Fill", panel, x0, 0.12f, x0 + 0.23f, 0.45f, new Color(0.5f, 0.75f, 0.35f, 1f));
				_needFill[i] = (RectTransform)fill.transform;
			}
			var actions = new (string label, Action action)[]
			{
				(Loc.T("care.hay") + " (" + MasterConfig.HayCost + ")", () => Game.FeedHay()),
				(Loc.T("care.ration"), () => Game.FeedRation()),
				(Loc.T("care.groom"), () => Game.Groom()),
				(Loc.T("care.pet"), () => Game.PetIt()),
				(Loc.T("care.rest"), () => Game.Rest()),
			};
			_careButtons = new Button[5];
			for (var i = 0; i < 5; i++)
			{
				var action = actions[i].action;
				var x0 = 0.02f + i * 0.128f;
				_careButtons[i] = Btn(t, actions[i].label, x0, 0.185f, x0 + 0.122f, 0.3f, () => action(), 22);
			}
			var side = Box("Training", t, 0.72f, 0.3f, 0.985f, 0.8f, Panel).transform;
			Lbl(side, Loc.T("train.head"), 36, TextAnchor.MiddleCenter, 0.03f, 0.85f, 0.97f, 0.99f, Gold);
			_obstacleButton = Btn(side, "", 0.05f, 0.45f, 0.95f, 0.82f, () => Game.StartTraining(Ids.Obstacles), 26);
			_obstacleText = _obstacleButton.GetComponentInChildren<Text>();
			_commandButton = Btn(side, "", 0.05f, 0.05f, 0.95f, 0.42f, () => Game.StartTraining(Ids.Commands), 26);
			_commandText = _commandButton.GetComponentInChildren<Text>();
			Btn(t, Loc.T("help"), 0.72f, 0.02f, 0.78f, 0.1f, () => Game.OpenHelp(), 28);
		}

		void RefreshStable()
		{
			var s = Game.Session;
			var h = s.Horse;
			UiBuilder.SetText(_horseTitle, h.Name + " · " + Loc.T("horse.stage." + (int)h.Stage) + " · " + Loc.T("horse.level", h.Level));
			var values = new[] { h.Satiety, h.Cleanliness, h.Energy, h.Trust };
			for (var i = 0; i < 4; i++)
			{
				UiBuilder.SetText(_needText[i], Loc.T(NeedKeys[i]) + " " + values[i]);
				_needFill[i].anchorMax = new Vector2(_needFill[i].anchorMin.x + 0.23f * Mathf.Clamp01(values[i] / 100f), _needFill[i].anchorMax.y);
			}
			var busy = s.TrainingActive;
			Tint(_careButtons[0], s.Coins < MasterConfig.HayCost ? Disabled : Plain);
			SetTraining(_obstacleButton, _obstacleText, Ids.Obstacles);
			SetTraining(_commandButton, _commandText, Ids.Commands);
			if (busy) UiBuilder.SetText(_horseTitle, Loc.T("train.busy"));
		}

		void SetTraining(Button button, Text text, string kind)
		{
			var block = Game.IsGuest ? "net.trainhost" : Game.Session.TrainingBlock(kind);
			var dirty = Game.Session.Horse.Cleanliness < 20 ? "\n" + Loc.T("train.dirty") : "";
			UiBuilder.SetText(text, Loc.T("train." + kind) + "\n<size=20>" + Loc.T("train." + kind + ".tip") + "</size>" + (block != null ? "\n<size=20>" + Loc.T(block) + "</size>" : dirty));
			Tint(button, block != null ? Disabled : Plain);
		}

		void BuildObstacle(Transform root)
		{
			var t = Clear(Mode.Obstacle, root);
			_runInfo = Lbl(t, "", 40, TextAnchor.MiddleCenter, 0.2f, 0.82f, 0.8f, 0.9f, Gold);
			var bar = Box("Progress", t, 0.25f, 0.78f, 0.75f, 0.81f, new Color(0f, 0f, 0f, 0.6f)).transform;
			_runProgress = (RectTransform)Box("Fill", bar, 0f, 0f, 0f, 1f, Gold).transform;
			_runHits = Lbl(t, "", 30, TextAnchor.MiddleCenter, 0.3f, 0.7f, 0.7f, 0.77f, Ink);
			Btn(t, Loc.T("train.jump"), 0.35f, 0.04f, 0.65f, 0.17f, () => Game.Jump(), 40);
			Btn(t, Loc.T("train.leave"), 0.78f, 0.04f, 0.98f, 0.11f, () => Game.Back(), 24);
		}

		void RefreshObstacle()
		{
			var run = Game.Run;
			if (run == null) return;
			UiBuilder.SetText(_runInfo, Loc.T("train.cleared", run.Cleared));
			UiBuilder.SetText(_runHits, Loc.T("train.knocked", run.Hit));
			_runProgress.anchorMax = new Vector2(run.Progress, 1f);
		}

		void BuildCommand(Transform root)
		{
			var t = Clear(Mode.Command, root);
			_commandInfo = Lbl(t, "", 40, TextAnchor.MiddleCenter, 0.2f, 0.82f, 0.8f, 0.9f, Gold);
			_cmdRound = Lbl(t, "", 30, TextAnchor.MiddleCenter, 0.3f, 0.76f, 0.7f, 0.82f, Ink);
			var bar = Box("Bar", t, 0.2f, 0.6f, 0.8f, 0.7f, new Color(0f, 0f, 0f, 0.7f));
			_cmdBar = (RectTransform)bar.transform;
			_cmdZone = (RectTransform)Box("Zone", bar.transform, 0.4f, 0f, 0.6f, 1f, new Color(0.35f, 0.7f, 0.3f, 0.9f)).transform;
			_cmdMarker = (RectTransform)Box("Marker", bar.transform, 0.5f, -0.1f, 0.51f, 1.1f, Color.white).transform;
			Btn(t, Loc.T("train.command"), 0.35f, 0.04f, 0.65f, 0.17f, () => Game.Command(), 40);
			Btn(t, Loc.T("train.leave"), 0.78f, 0.04f, 0.98f, 0.11f, () => Game.Back(), 24);
		}

		void RefreshCommand()
		{
			var timing = Game.Timing;
			if (timing == null) return;
			UiBuilder.SetText(_commandInfo, Loc.T("train.hits", timing.Hits));
			UiBuilder.SetText(_cmdRound, Loc.T("train.round", Mathf.Min(timing.Round + 1, CommandTimingLogic.Rounds)));
			var c = timing.ZoneCentre;
			_cmdZone.anchorMin = new Vector2(c - CommandTimingLogic.ZoneWidth * 0.5f, 0f);
			_cmdZone.anchorMax = new Vector2(c + CommandTimingLogic.ZoneWidth * 0.5f, 1f);
			var m = timing.Marker;
			_cmdMarker.anchorMin = new Vector2(m - 0.004f, -0.1f);
			_cmdMarker.anchorMax = new Vector2(m + 0.004f, 1.1f);
		}

		// ---- result ----

		void BuildResult(Transform root)
		{
			var card = Overlay(Mode.Result, root, 0.14f, 0.1f, 0.86f, 0.9f, 0.55f).transform;
			_resultTitle = Lbl(card, "", 50, TextAnchor.MiddleCenter, 0.03f, 0.86f, 0.97f, 0.99f, Gold);
			_resultBody = Lbl(card, "", 26, TextAnchor.UpperLeft, 0.05f, 0.2f, 0.95f, 0.86f, Ink, true);
			_resultBody.supportRichText = true;
			_retryButton = Btn(card, Loc.T("retry"), 0.05f, 0.03f, 0.35f, 0.17f, () => Game.RetryAfterReject(), 28).gameObject;
			_againButton = Btn(card, Loc.T("result.again"), 0.05f, 0.03f, 0.35f, 0.17f, () => Game.Again(), 28).gameObject;
			_nextButton = Btn(card, Loc.T("orders"), 0.37f, 0.03f, 0.64f, 0.17f, () => Game.LeaveResult(), 28).gameObject;
			_stableButton = Btn(card, Loc.T("hub"), 0.66f, 0.03f, 0.95f, 0.17f, () => Game.ToHub(), 28).gameObject;
		}

		static string DetailText(string reason, string detail)
		{
			if (reason == "why.parts" || reason == "why.set" || reason == "why.herbs") return Loc.T(reason, detail != null ? Loc.T("item." + detail) : "");
			return Loc.T(reason);
		}

		void RefreshResult()
		{
			var s = Game.Session;
			var sb = new StringBuilder();
			string title;
			if (Game.ResultTraining)
			{
				var r = s.LastReceipt;
				title = Loc.T("train.done");
				if (r != null)
				{
					sb.Append(Loc.T("train." + r.Id)).Append("\n\n");
					sb.Append(Loc.T("result.score", r.Quality)).Append('\n');
					sb.Append(Loc.T("result.rewards", r.Coins, r.GlobalXp, r.SkillXp)).Append('\n');
					if (r.FirstTime) sb.Append(Loc.T("result.first")).Append('\n');
					if (Game.LeveledUp) sb.Append('\n').Append(Loc.T("result.levelup", s.Level));
					if (Game.RankedUp) sb.Append('\n').Append(Loc.T("result.rankup", Loc.T("rank." + s.Rank(Ids.Stable))));
					if (Game.HorseLeveled) sb.Append('\n').Append(Loc.T("result.horselevel", s.Horse.Level));
				}
			}
			else if (Game.ResultSuccess)
			{
				var r = s.LastReceipt;
				title = Loc.T("result.ok");
				sb.Append(r.Id).Append(" · ").Append(Loc.Title(r.Id)).Append("\n\n");
				sb.Append(Loc.T("result.score", r.Quality)).Append('\n');
				AppendParts(sb, s.LastOutcome);
				sb.Append(Loc.T("result.rewards", r.Coins, r.GlobalXp, r.SkillXp)).Append('\n');
				sb.Append(r.FirstTime ? Loc.T("result.first") : Loc.T("result.repeat")).Append("\n\n");
				sb.Append(Loc.Why(r.Id)).Append('\n');
				if (Game.LeveledUp) sb.Append('\n').Append(Loc.T("result.levelup", s.Level));
				if (Game.RankedUp) sb.Append('\n').Append(Loc.T("result.rankup", Loc.T("rank." + s.Rank(r.Profession))));
			}
			else
			{
				title = Loc.T("result.fail");
				var o = s.LastOutcome;
				var id = s.ActiveJobId;
				if (id != null) sb.Append(id).Append(" · ").Append(Loc.Title(id)).Append("\n\n");
				if (o != null)
				{
					AppendParts(sb, o);
					for (var i = 0; i < o.Reasons.Count; i++) sb.Append("• ").Append(DetailText(o.Reasons[i], i < o.Details.Count ? o.Details[i] : null)).Append('\n');
					if (id != null && Loc.Has("hint." + id) && o.Reasons.Contains("why.parts")) sb.Append('\n').Append(Loc.T("hint." + id)).Append('\n');
				}
				sb.Append('\n').Append(Loc.T("result.nopay"));
			}
			UiBuilder.SetText(_resultTitle, title);
			UiBuilder.SetText(_resultBody, sb.ToString());
			var fail = !Game.ResultTraining && !Game.ResultSuccess;
			_retryButton.SetActive(fail);
			_againButton.SetActive(!Game.ResultTraining && Game.ResultSuccess);
			_nextButton.SetActive(true);
			UiBuilder.SetText(_nextButton.GetComponentInChildren<Text>(), Game.ResultTraining ? Loc.T("prof.stable") : Loc.T("orders"));
		}

		static void AppendParts(StringBuilder sb, TaskOutcome o)
		{
			if (o == null || o.Parts.Count == 0) return;
			sb.Append(Loc.T("result.parts")).Append(' ');
			sb.Append(string.Join(", ", o.Parts.Select(p => Loc.T("part." + p.Key) + " " + p.Value))).Append('\n');
		}

		// ---- showing ----

		void Update()
		{
			if (Game == null || _screens.Count == 0) return;
			var mode = Game.Mode;
			if (mode != _shown)
			{
				Show(mode);
				_shown = mode;
			}
			Refresh(mode);
			SyncChrome(mode);
			UpdateTaskInput(mode);
		}

		void Show(Mode mode)
		{
			_tipBox.transform.SetAsLastSibling();
			_toastBox.transform.SetAsLastSibling();
			foreach (var pair in _screens) pair.Value.SetActive(pair.Key == mode);
			var inGame = Game.Live && mode != Mode.Settings && mode != Mode.ConfirmReset && mode != Mode.Pause && mode != Mode.Help;
			// The coins, level and experience are not shown: the top bar is the shell bar, with the professions as tabs.
			_topBar.SetActive(false);
			OnShowTask(mode);
			var first = _screens.TryGetValue(mode, out var s) ? s : null;
			if (first != null && mode != Mode.Mentor && mode != Mode.Village && mode != Mode.Task && mode != Mode.Stable && mode != Mode.Orders) UiBuilder.Select(first);
		}

		static bool IsConfirm(Mode mode) => mode == Mode.ConfirmNew || mode == Mode.ConfirmReset;

		void Refresh(Mode mode)
		{
			var s = Game.Session;
			if (_topBar.activeSelf)
			{
				UiBuilder.SetText(_coins, Loc.T("coins") + ": " + s.Coins);
				UiBuilder.SetText(_level, Loc.T("level") + ": " + s.Level);
				var next = s.Level < MasterConfig.MaxLevel ? "/" + MasterConfig.LevelXp[s.Level] : "";
				UiBuilder.SetText(_xp, Loc.T("xp") + ": " + s.GlobalXp + next);
				var job = s.ActiveJobId == null ? "" : s.ActiveJobId + " · " + Loc.Title(s.ActiveJobId);
				UiBuilder.SetText(_activeJob, job);
			}
			var tip = Game.TipKey;
			var showTip = tip != null && mode != Mode.Result;
			_tipBox.SetActive(showTip);
			if (showTip) UiBuilder.SetText(_tip, Loc.T(tip));
			var toast = Game.ToastActive && mode != Mode.Title;
			_toastBox.SetActive(toast);
			if (toast) UiBuilder.SetText(_toast, Game.ToastText);

			switch (mode)
			{
				case Mode.Title:
					break;
				case Mode.Recovery:
					UiBuilder.SetText(_recoveryText, Loc.T(Game.RecoveryReason));
					UiBuilder.SetText(_recoveryNote, Game.RecoveryNote == "" ? "" : Loc.T(Game.RecoveryNote));
					break;
				case Mode.Mentor:
					RefreshMentor();
					break;
				case Mode.Village:
					RefreshVillage();
					break;
				case Mode.Orders:
					RefreshOrders();
					break;
				case Mode.Task:
					RefreshTask();
					break;
				case Mode.Stable:
					RefreshStable();
					break;
				case Mode.Obstacle:
					RefreshObstacle();
					break;
				case Mode.Command:
					RefreshCommand();
					break;
				case Mode.Result:
					RefreshResult();
					break;
				case Mode.Upgrades:
					RefreshUpgrades();
					break;
				case Mode.Journal:
					RefreshJournal();
					break;
				case Mode.Overview:
					RefreshOverview();
					break;
				case Mode.Help:
					var id = s.SelectedProfession;
					UiBuilder.SetText(_helpText, Loc.T("prof." + id) + ": " + Loc.T("tip." + id) + "\n\n" + Loc.T("controls"));
					break;
				case Mode.Settings:
					UiBuilder.SetText(_masterText, Loc.T("volume.master") + ": " + Mathf.RoundToInt(Game.Settings.masterVolume * 100f) + "%");
					UiBuilder.SetText(_sfxText, Loc.T("volume.sfx") + ": " + Mathf.RoundToInt(Game.Settings.sfxVolume * 100f) + "%");
					UiBuilder.SetText(_fullscreenText, Loc.T("fullscreen") + ": " + (Game.Settings.fullscreen ? "✓" : "—"));
					break;
			}
		}
	}
}
