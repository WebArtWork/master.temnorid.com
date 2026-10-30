using System.Collections.Generic;
using System.Linq;
using ITKamianets.Engine.Input;
using ITKamianets.Engine.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Temnorid.Master
{
	/// <summary>The task screen: a numbered step bar, one central panel for the step in hand and a bottom row (back, reset the step, next, confirm).</summary>
	public sealed partial class MasterHud
	{
		const int MaxSteps = 3;

		GameObject _taskCard;
		Button[] _stepButtons;
		Text[] _stepTexts;
		Text _order, _message;
		Button _nextStepButton, _submitButton;
		readonly Dictionary<StepKind, GameObject> _panels = new Dictionary<StepKind, GameObject>();

		// Inspect.
		Text _inspectSource, _inspectReference, _inspectPrompt, _inspectChosen;
		Button _acceptButton, _flagButton;

		// Trace.
		RectTransform _tracePanel;
		LineDrawer _guide, _stroke;
		readonly List<Vector2> _drawn = new List<Vector2>();
		bool _drawing;
		Vector2 _traceSize;
		string _guideKey;

		// Seal.
		RectTransform _sealPanel, _sealTarget, _sealPlaced, _dragPreview;
		Button[] _sealButtons;
		Text _sealNeed;
		bool _dragging;
		string _placedIcon;

		// Bar and stirring.
		RectTransform _barRect, _barZone, _barMarker, _barLock;
		Text _barInfo;
		Button _barButton;
		Text _barButtonText;

		// Hammer.
		RectTransform _anvil;
		RectTransform _hammerTarget;
		RectTransform[] _hammerDots;
		Text _hammerInfo;

		// Choose, multi, parts.
		Button[] _chooseButtons;
		Button[] _multiButtons;
		Text _multiInfo;
		Button[] _partButtons;
		Button[] _slotButtons;

		void BuildTask(Transform root)
		{
			var screen = Box("Task", root, 0f, 0f, 1f, 1f, Color.clear);
			screen.GetComponent<Image>().raycastTarget = false;
			_screens[Mode.Task] = screen;
			var t = screen.transform;

			var bar = Box("Steps", t, 0.04f, 0.775f, 0.96f, 0.835f, new Color(0f, 0f, 0f, 0.5f)).transform;
			_stepButtons = new Button[MaxSteps];
			_stepTexts = new Text[MaxSteps];
			for (var i = 0; i < MaxSteps; i++)
			{
				var index = i;
				var x0 = 0.005f + i * 0.332f;
				_stepButtons[i] = Btn(bar, "", x0, 0.08f, x0 + 0.325f, 0.92f, () => Game.GoToStep(index), 24);
				_stepTexts[i] = _stepButtons[i].GetComponentInChildren<Text>();
			}

			_taskCard = Box("Card", t, 0.04f, 0.125f, 0.96f, 0.765f, Panel);
			var card = _taskCard.transform;
			_order = Lbl(card, "", 26, TextAnchor.MiddleLeft, 0.03f, 0.87f, 0.97f, 0.99f, Gold, true);
			_message = Lbl(card, "", 28, TextAnchor.MiddleCenter, 0.03f, 0.01f, 0.97f, 0.12f, Ink, true);
			var region = Box("Region", card, 0.03f, 0.13f, 0.97f, 0.86f, Color.clear);
			region.GetComponent<Image>().raycastTarget = false;
			var r = region.transform;
			BuildInspectPanel(r);
			BuildTracePanel(r);
			BuildSealPanel(r);
			BuildBarPanel(r);
			BuildHammerPanel(r);
			BuildChoosePanel(r);
			BuildMultiPanel(r);
			BuildPartsPanel(r);

			Btn(t, Loc.T("back"), 0.04f, 0.02f, 0.2f, 0.11f, () => Game.Back(), 28);
			Btn(t, Loc.T("reset.step"), 0.22f, 0.02f, 0.46f, 0.11f, () => Game.ResetStep(), 24);
			Btn(t, Loc.T("help"), 0.48f, 0.02f, 0.54f, 0.11f, () => Game.OpenHelp(), 28);
			_nextStepButton = Btn(t, Loc.T("next") + " →", 0.56f, 0.02f, 0.76f, 0.11f, () => Game.NextStep(), 28);
			_submitButton = Btn(t, Loc.T("submit"), 0.78f, 0.02f, 0.96f, 0.11f, () => Game.Submit(), 28);
		}

		GameObject Panel_(StepKind kind, Transform region)
		{
			var panel = Box(kind.ToString(), region, 0f, 0f, 1f, 1f, Color.clear);
			panel.GetComponent<Image>().raycastTarget = false;
			_panels[kind] = panel;
			return panel;
		}

		// ---- inspect ----

		void BuildInspectPanel(Transform region)
		{
			var p = Panel_(StepKind.Inspect, region).transform;
			var source = Box("Source", p, 0.02f, 0.38f, 0.49f, 1f, Parchment).transform;
			Lbl(source, Loc.T("inspect.source"), 22, TextAnchor.UpperLeft, 0.04f, 0.8f, 0.96f, 0.98f, new Color(0.4f, 0.3f, 0.2f));
			_inspectSource = Lbl(source, "", 34, TextAnchor.MiddleCenter, 0.05f, 0.05f, 0.95f, 0.8f, Dark, true);
			var reference = Box("Reference", p, 0.51f, 0.38f, 0.98f, 1f, new Color(0.82f, 0.88f, 0.8f)).transform;
			Lbl(reference, Loc.T("inspect.reference"), 22, TextAnchor.UpperLeft, 0.04f, 0.8f, 0.96f, 0.98f, new Color(0.25f, 0.35f, 0.25f));
			_inspectReference = Lbl(reference, "", 34, TextAnchor.MiddleCenter, 0.05f, 0.05f, 0.95f, 0.8f, Dark, true);
			_inspectPrompt = Lbl(p, "", 32, TextAnchor.MiddleCenter, 0.02f, 0.24f, 0.98f, 0.36f, Gold, true);
			_acceptButton = Btn(p, Loc.T("verdict.accept"), 0.1f, 0.02f, 0.46f, 0.22f, () => Game.Pick(Ids.Accept), 32);
			_flagButton = Btn(p, Loc.T("verdict.flag"), 0.54f, 0.02f, 0.9f, 0.22f, () => Game.Pick(Ids.Flag), 32);
		}

		void RefreshInspect()
		{
			var job = Game.ActiveJob;
			UiBuilder.SetText(_inspectSource, job.Source);
			UiBuilder.SetText(_inspectReference, job.Reference);
			UiBuilder.SetText(_inspectPrompt, Loc.Question(job.Id));
			var choice = Game.Session.Data.Get("choice");
			Tint(_acceptButton, choice == Ids.Accept ? Chosen : Plain);
			Tint(_flagButton, choice == Ids.Flag ? Chosen : Plain);
		}

		// ---- trace ----

		void BuildTracePanel(Transform region)
		{
			var p = Panel_(StepKind.Trace, region).transform;
			var paper = Box("Paper", p, 0.3f, 0.0f, 0.7f, 1f, Parchment);
			_tracePanel = (RectTransform)paper.transform;
			_guide = new LineDrawer(_tracePanel, new Color(0.75f, 0.5f, 0.15f, 0.8f), 14f, _disc);
			_stroke = new LineDrawer(_tracePanel, new Color(0.1f, 0.08f, 0.14f, 1f), 7f, _disc);
			Lbl(p, Loc.T("trace.hint"), 24, TextAnchor.MiddleLeft, 0.01f, 0.3f, 0.28f, 0.7f, Muted, true);
		}

		/// <summary>Where the tracing paper is on the screen, in pixels. Tests use it to drive a real mouse across it.</summary>
		public Rect TracePanelRect => UiShapes.ScreenRect(_tracePanel);

		/// <summary>Where the parchment of the seal step is on the screen, in pixels.</summary>
		public Rect SealPanelRect => UiShapes.ScreenRect(_sealPanel);

		/// <summary>Where the anvil is on the screen, in pixels.</summary>
		public Rect AnvilRect => UiShapes.ScreenRect(_anvil);

		void ClearDrawing()
		{
			_drawn.Clear();
			_drawing = false;
			_stroke.Clear();
		}

		void UpdateTrace()
		{
			var job = Game.ActiveJob;
			if (_tracePanel.rect.size != _traceSize || _guideKey != job.PathId)
			{
				_traceSize = _tracePanel.rect.size;
				_guideKey = job.PathId;
				_guide.Set(MasterContent.Path(job.PathId));
				if (_drawn.Count > 0) _stroke.Set(_drawn);
			}
			var input = EngineInput.Default.UI;
			var pointer = input.Point.ReadValue<Vector2>();
			var panel = UiShapes.ScreenRect(_tracePanel);
			if (!_drawing && input.Click.WasPressedThisFrame() && panel.Contains(pointer))
			{
				_drawing = true;
				_drawn.Clear();
				_stroke.Clear();
			}
			if (!_drawing) return;
			var point = TraceMath.Normalize(pointer, panel);
			if (_drawn.Count == 0 || Vector2.Distance(_drawn[_drawn.Count - 1], point) > 0.004f)
			{
				_drawn.Add(point);
				_stroke.Set(_drawn);
			}
			if (!input.Click.IsPressed())
			{
				_drawing = false;
				Game.SubmitTrace(_drawn.ToList());
			}
		}

		// ---- seal ----

		void BuildSealPanel(Transform region)
		{
			var p = Panel_(StepKind.Seal, region).transform;
			var paper = Box("Paper", p, 0.3f, 0f, 0.99f, 1f, Parchment);
			_sealPanel = (RectTransform)paper.transform;
			for (var i = 0; i < 5; i++) Box("Line", paper.transform, 0.1f, 0.78f - i * 0.05f, 0.9f - (i == 4 ? 0.4f : 0f), 0.79f - i * 0.05f, new Color(0.3f, 0.22f, 0.15f, 0.5f));
			_sealTarget = NewImage("Target", paper.transform, _ring, new Color(0.7f, 0.2f, 0.15f, 0.9f), 120f);
			_sealPlaced = NewHost("Placed", paper.transform, 110f);
			_dragPreview = NewHost("Preview", paper.transform, 110f);
			_sealNeed = Lbl(p, "", 26, TextAnchor.UpperLeft, 0.0f, 0.62f, 0.28f, 1f, Ink, true);
			_sealButtons = new Button[3];
			var seals = new[] { Ids.Academy, Ids.Village, Ids.Merchant };
			for (var i = 0; i < 3; i++)
			{
				var id = seals[i];
				var y1 = 0.62f - i * 0.17f;
				_sealButtons[i] = Btn(p, "", 0.0f, y1 - 0.155f, 0.28f, y1, () => Game.SelectSeal(id), 20);
				var label = _sealButtons[i].GetComponentInChildren<Text>();
				label.alignment = TextAnchor.MiddleLeft;
				label.rectTransform.anchorMin = new Vector2(0.36f, 0f);
				label.rectTransform.anchorMax = new Vector2(0.98f, 1f);
				label.text = Loc.T("item." + id) + "\n" + Loc.T("shape." + id);
				var icon = NewHost("Icon", _sealButtons[i].transform, 60f);
				icon.anchorMin = icon.anchorMax = new Vector2(0.17f, 0.5f);
				icon.pivot = new Vector2(0.5f, 0.5f);
				icon.anchoredPosition = Vector2.zero;
				BuildSealIcon(icon, id, Dark);
			}
			Btn(p, Loc.T("seal.remove"), 0.0f, 0.0f, 0.28f, 0.1f, () => Game.RemoveSeal(), 22);
		}

		/// <summary>A seal's picture from plain rectangles: an open book, a guard tower, a pair of scales.</summary>
		static void BuildSealIcon(RectTransform host, string id, Color c)
		{
			foreach (Transform child in host) Destroy(child.gameObject);
			switch (id)
			{
				case Ids.Academy:
					Part(host, 0.04f, 0.2f, 0.48f, 0.78f, c);
					Part(host, 0.52f, 0.2f, 0.96f, 0.78f, c);
					Part(host, 0.48f, 0.16f, 0.52f, 0.82f, new Color(1f, 1f, 1f, 0.55f));
					for (var i = 0; i < 3; i++)
					{
						Part(host, 0.1f, 0.62f - i * 0.15f, 0.42f, 0.66f - i * 0.15f, new Color(1f, 1f, 1f, 0.5f));
						Part(host, 0.58f, 0.62f - i * 0.15f, 0.9f, 0.66f - i * 0.15f, new Color(1f, 1f, 1f, 0.5f));
					}
					break;
				case Ids.Village:
					Part(host, 0.3f, 0.08f, 0.7f, 0.72f, c);
					Part(host, 0.26f, 0.72f, 0.38f, 0.92f, c);
					Part(host, 0.44f, 0.72f, 0.56f, 0.92f, c);
					Part(host, 0.62f, 0.72f, 0.74f, 0.92f, c);
					Part(host, 0.26f, 0.68f, 0.74f, 0.74f, c);
					Part(host, 0.44f, 0.08f, 0.56f, 0.32f, new Color(1f, 1f, 1f, 0.55f));
					Part(host, 0.46f, 0.5f, 0.54f, 0.62f, new Color(1f, 1f, 1f, 0.55f));
					break;
				default:
					Part(host, 0.08f, 0.8f, 0.92f, 0.86f, c);
					Part(host, 0.47f, 0.1f, 0.53f, 0.86f, c);
					Part(host, 0.28f, 0.06f, 0.72f, 0.14f, c);
					Part(host, 0.09f, 0.52f, 0.12f, 0.8f, c);
					Part(host, 0.31f, 0.52f, 0.34f, 0.8f, c);
					Part(host, 0.66f, 0.52f, 0.69f, 0.8f, c);
					Part(host, 0.88f, 0.52f, 0.91f, 0.8f, c);
					Part(host, 0.05f, 0.44f, 0.38f, 0.54f, c);
					Part(host, 0.62f, 0.44f, 0.95f, 0.54f, c);
					break;
			}
		}

		void UpdateSeal()
		{
			var input = EngineInput.Default.UI;
			var pointer = input.Point.ReadValue<Vector2>();
			var panel = UiShapes.ScreenRect(_sealPanel);
			if (!_dragging && Game.SelectedSeal != null && input.Click.WasPressedThisFrame() && panel.Contains(pointer))
			{
				_dragging = true;
				BuildSealIcon(_dragPreview, Game.SelectedSeal, new Color(0.55f, 0.1f, 0.1f, 0.85f));
			}
			if (!_dragging) return;
			var point = TraceMath.Normalize(pointer, panel);
			Place(_dragPreview, point);
			if (!input.Click.IsPressed())
			{
				_dragging = false;
				Hide(_dragPreview);
				// Released on the paper: place the seal. Released outside: nothing is placed or changed.
				if (panel.Contains(pointer)) Game.PlaceSeal(point.x, point.y);
			}
		}

		void RefreshSeal()
		{
			var s = Game.Session;
			var job = Game.ActiveJob;
			UiBuilder.SetText(_sealNeed, Loc.T("seal.hint") + "\n\n" + Loc.Title(job.Id));
			var seals = new[] { Ids.Academy, Ids.Village, Ids.Merchant };
			for (var i = 0; i < 3; i++) Tint(_sealButtons[i], Game.SelectedSeal == seals[i] ? Chosen : Plain);
			// The target mark is the place the order asks for; which seal belongs there is for the player to read from the order.
			Place(_sealTarget, job.SealTarget);
			if (s.Data.Has("seal"))
			{
				var id = s.Data.Get("seal");
				if (_placedIcon != id)
				{
					_placedIcon = id;
					BuildSealIcon(_sealPlaced, id, new Color(0.6f, 0.1f, 0.1f, 0.9f));
				}
				Place(_sealPlaced, s.Data.Vector("sealpos"));
			}
			else Hide(_sealPlaced);
		}

		// ---- bar and stirring ----

		void BuildBarPanel(Transform region)
		{
			var p = Panel_(StepKind.Bar, region).transform;
			_barInfo = Lbl(p, "", 30, TextAnchor.MiddleCenter, 0.05f, 0.62f, 0.95f, 0.8f, Ink, true);
			var bar = Box("Bar", p, 0.08f, 0.38f, 0.92f, 0.56f, new Color(0f, 0f, 0f, 0.7f));
			_barRect = (RectTransform)bar.transform;
			_barZone = (RectTransform)Box("Zone", bar.transform, 0.4f, 0f, 0.6f, 1f, new Color(0.35f, 0.7f, 0.3f, 0.9f)).transform;
			_barLock = (RectTransform)Box("Lock", bar.transform, 0.5f, -0.15f, 0.51f, 1.15f, new Color(0.95f, 0.75f, 0.25f)).transform;
			_barMarker = (RectTransform)Box("Marker", bar.transform, 0.5f, -0.15f, 0.51f, 1.15f, Color.white).transform;
			_barButton = Btn(p, "", 0.32f, 0.08f, 0.68f, 0.28f, () => PressBar(), 34);
			_barButtonText = _barButton.GetComponentInChildren<Text>();
		}

		void PressBar()
		{
			var step = Game.CurrentStep;
			if (step == null) return;
			if (step.Kind == StepKind.Stir) Game.StirPress();
			else Game.LockBar();
		}

		void RefreshBar(StepDef step)
		{
			var job = Game.ActiveJob;
			var data = Game.Session.Data;
			var stir = step.Kind == StepKind.Stir;
			if (stir)
			{
				var done = data.Has(step.Key);
				var round = Mathf.Min(Game.StirRound + 1, 3);
				UiBuilder.SetText(_barInfo, done ? Loc.T("chosen.of", data.Int(step.Key), 3) : Loc.T(step.Stitch ? "stitch.hint" : "stir.hint", round));
				var c = done ? 0.5f : Game.StirCentre;
				_barZone.anchorMin = new Vector2(c - TimingMath.ZoneHalfWidth, 0f);
				_barZone.anchorMax = new Vector2(c + TimingMath.ZoneHalfWidth, 1f);
				_barZone.gameObject.SetActive(!done);
				var m = done ? 0.5f : Game.StirMarker;
				SetMarker(_barMarker, m, !done);
				Hide(_barLock);
				UiBuilder.SetText(_barButtonText, Loc.T(step.Stitch ? "press.stitch" : "press.stir"));
				Tint(_barButton, done ? Disabled : Plain);
			}
			else
			{
				var locked = data.Has(step.Key);
				var target = job.Target;
				UiBuilder.SetText(_barInfo, locked ? Loc.T("locked.at", Mathf.RoundToInt(data.Float(step.Key) * 100f)) : Loc.T("bar.target", Mathf.RoundToInt(target * 100f)));
				_barZone.anchorMin = new Vector2(target - 0.012f, 0f);
				_barZone.anchorMax = new Vector2(target + 0.012f, 1f);
				_barZone.gameObject.SetActive(true);
				SetMarker(_barMarker, Game.Marker, !locked);
				if (locked)
				{
					var v = data.Float(step.Key);
					_barLock.gameObject.SetActive(true);
					_barLock.anchorMin = new Vector2(v - 0.005f, -0.15f);
					_barLock.anchorMax = new Vector2(v + 0.005f, 1.15f);
				}
				else Hide(_barLock);
				UiBuilder.SetText(_barButtonText, Loc.T("press"));
				Tint(_barButton, locked ? Disabled : Plain);
			}
		}

		static void SetMarker(RectTransform marker, float value, bool visible)
		{
			marker.gameObject.SetActive(visible);
			marker.anchorMin = new Vector2(value - 0.005f, -0.15f);
			marker.anchorMax = new Vector2(value + 0.005f, 1.15f);
		}

		// ---- hammer ----

		void BuildHammerPanel(Transform region)
		{
			var p = Panel_(StepKind.Hammer, region).transform;
			var anvil = Box("Anvil", p, 0.3f, 0f, 0.7f, 1f, new Color(0.22f, 0.22f, 0.26f, 1f));
			_anvil = (RectTransform)anvil.transform;
			Part(anvil.transform, 0.05f, 0.05f, 0.95f, 0.2f, new Color(0.3f, 0.3f, 0.35f, 1f));
			Part(anvil.transform, 0.1f, 0.25f, 0.9f, 0.85f, new Color(0.36f, 0.36f, 0.42f, 1f));
			_hammerTarget = NewImage("Target", anvil.transform, _ring, new Color(1f, 0.6f, 0.15f, 1f), 120f);
			_hammerDots = new RectTransform[3];
			for (var i = 0; i < 3; i++) _hammerDots[i] = NewImage("Hit" + i, anvil.transform, _disc, new Color(0.95f, 0.8f, 0.3f, 0.9f), 26f);
			_hammerInfo = Lbl(p, "", 28, TextAnchor.MiddleLeft, 0.01f, 0.3f, 0.28f, 0.7f, Ink, true);
		}

		void UpdateHammer()
		{
			var input = EngineInput.Default.UI;
			if (!input.Click.WasPressedThisFrame()) return;
			var pointer = input.Point.ReadValue<Vector2>();
			var rect = UiShapes.ScreenRect(_anvil);
			if (rect.Contains(pointer)) Game.Strike(TraceMath.Normalize(pointer, rect));
		}

		void RefreshHammer()
		{
			var job = Game.ActiveJob;
			var data = Game.Session.Data;
			var next = Game.LastStrike + 1;
			if (next <= 2) Place(_hammerTarget, job.Hammer[next]);
			else Hide(_hammerTarget);
			for (var i = 0; i < 3; i++)
			{
				if (data.Has("h" + i)) Place(_hammerDots[i], data.Vector("h" + i));
				else Hide(_hammerDots[i]);
			}
			UiBuilder.SetText(_hammerInfo, next <= 2 ? Loc.T("hammer.hint", next + 1) : Loc.T("done"));
		}

		// ---- choose, multi, parts ----

		void BuildChoosePanel(Transform region)
		{
			var p = Panel_(StepKind.Choose, region).transform;
			_chooseButtons = new Button[3];
			for (var i = 0; i < 3; i++)
			{
				var index = i;
				_chooseButtons[i] = Btn(p, "", 0f, 0f, 0.3f, 0.5f, () => ChooseOption(index), 34);
			}
		}

		void ChooseOption(int index)
		{
			var step = Game.CurrentStep;
			if (step != null && index < step.Options.Length) Game.Pick(step.Options[index]);
		}

		void RefreshChoose(StepDef step)
		{
			var n = step.Options.Length;
			var width = 0.3f;
			var gap = 0.04f;
			var start = 0.5f - (n * width + (n - 1) * gap) * 0.5f;
			var chosen = Game.Session.Data.Get(step.Key);
			for (var i = 0; i < 3; i++)
			{
				_chooseButtons[i].gameObject.SetActive(i < n);
				if (i >= n) continue;
				var rect = (RectTransform)_chooseButtons[i].transform;
				rect.anchorMin = new Vector2(start + i * (width + gap), 0.3f);
				rect.anchorMax = new Vector2(start + i * (width + gap) + width, 0.7f);
				UiBuilder.SetText(_chooseButtons[i].GetComponentInChildren<Text>(), Loc.T("item." + step.Options[i]));
				Tint(_chooseButtons[i], chosen == step.Options[i] ? Chosen : Plain);
			}
		}

		void BuildMultiPanel(Transform region)
		{
			var p = Panel_(StepKind.Multi, region).transform;
			_multiButtons = new Button[4];
			for (var i = 0; i < 4; i++)
			{
				var index = i;
				var col = i % 2;
				var row = i / 2;
				_multiButtons[i] = Btn(p, "", 0.1f + col * 0.42f, 0.5f - row * 0.32f, 0.48f + col * 0.42f, 0.8f - row * 0.32f, () => ToggleOption(index), 34);
			}
			_multiInfo = Lbl(p, "", 28, TextAnchor.MiddleCenter, 0.1f, 0.0f, 0.9f, 0.16f, Ink);
		}

		void ToggleOption(int index)
		{
			var step = Game.CurrentStep;
			if (step != null && index < step.Options.Length) Game.Toggle(step.Options[index]);
		}

		void RefreshMulti(StepDef step)
		{
			var chosen = Game.Chosen;
			for (var i = 0; i < 4; i++)
			{
				_multiButtons[i].gameObject.SetActive(i < step.Options.Length);
				if (i >= step.Options.Length) continue;
				UiBuilder.SetText(_multiButtons[i].GetComponentInChildren<Text>(), Loc.T("item." + step.Options[i]));
				Tint(_multiButtons[i], chosen.Contains(step.Options[i]) ? Chosen : Plain);
			}
			UiBuilder.SetText(_multiInfo, Loc.T("chosen.of", chosen.Length, step.Count));
		}

		void BuildPartsPanel(Transform region)
		{
			var p = Panel_(StepKind.Parts, region).transform;
			_partButtons = new Button[4];
			for (var i = 0; i < 4; i++)
			{
				var index = i;
				_partButtons[i] = Btn(p, "", 0f, 0.77f - i * 0.25f, 0.27f, 0.99f - i * 0.25f, () => SelectPartOption(index), 28);
			}
			_slotButtons = new Button[3];
			for (var i = 0; i < 3; i++)
			{
				var slot = i;
				_slotButtons[i] = Btn(p, "", 0.32f + i * 0.226f, 0.3f, 0.32f + i * 0.226f + 0.21f, 0.75f, () => Game.PlaceInSlot(slot), 28);
			}
			Lbl(p, Loc.T("parts.hint"), 26, TextAnchor.MiddleLeft, 0.32f, 0.0f, 0.99f, 0.24f, Muted, true);
		}

		void SelectPartOption(int index)
		{
			var step = Game.CurrentStep;
			if (step != null && index < step.Options.Length) Game.SelectPart(step.Options[index]);
		}

		void RefreshParts(StepDef step)
		{
			for (var i = 0; i < 4; i++)
			{
				var item = step.Options[i];
				UiBuilder.SetText(_partButtons[i].GetComponentInChildren<Text>(), Loc.T("item." + item));
				Tint(_partButtons[i], Game.SelectedPart == item ? Chosen : Game.PartUsed(item) ? Disabled : Plain);
			}
			for (var i = 0; i < 3; i++)
			{
				var part = Game.PartIn(i);
				UiBuilder.SetText(_slotButtons[i].GetComponentInChildren<Text>(), Loc.T("slot", i + 1) + "\n" + (part == null ? Loc.T("slot.empty") : Loc.T("item." + part)));
				Tint(_slotButtons[i], part != null ? new Color(0.3f, 0.4f, 0.25f) : Plain);
			}
		}

		// ---- the screen ----

		void OnShowTask(Mode mode)
		{
			ClearDrawing();
			_guideKey = null;
			_traceSize = Vector2.zero;
			_dragging = false;
			_placedIcon = null;
			if (_dragPreview != null) Hide(_dragPreview);
		}

		void RefreshTask()
		{
			var job = Game.ActiveJob;
			var step = Game.CurrentStep;
			if (job == null || step == null) return;
			var steps = Game.Steps;
			for (var i = 0; i < MaxSteps; i++)
			{
				_stepButtons[i].gameObject.SetActive(i < steps.Length);
				if (i >= steps.Length) continue;
				var done = Game.StepDone(i);
				UiBuilder.SetText(_stepTexts[i], (done ? "✓ " : "") + (i + 1) + ". " + Loc.T(steps[i].Title));
				Tint(_stepButtons[i], i == Game.Step ? Chosen : Game.StepOpen(i) ? (done ? new Color(0.3f, 0.4f, 0.25f) : Plain) : Disabled);
			}
			UiBuilder.SetText(_order, Loc.Title(job.Id) + ": " + Loc.Order(job.Id));
			foreach (var pair in _panels) pair.Value.SetActive(pair.Key == step.Kind || (pair.Key == StepKind.Bar && step.Kind == StepKind.Stir));
			switch (step.Kind)
			{
				case StepKind.Inspect: RefreshInspect(); break;
				case StepKind.Seal: RefreshSeal(); break;
				case StepKind.Bar:
				case StepKind.Stir: RefreshBar(step); break;
				case StepKind.Hammer: RefreshHammer(); break;
				case StepKind.Choose: RefreshChoose(step); break;
				case StepKind.Multi: RefreshMulti(step); break;
				case StepKind.Parts: RefreshParts(step); break;
			}
			UiBuilder.SetText(_message, Game.StepMessage);
			var last = Game.Step >= steps.Length - 1;
			_nextStepButton.gameObject.SetActive(!last);
			Tint(_nextStepButton, Game.StepDone(Game.Step) ? Plain : Disabled);
			_submitButton.gameObject.SetActive(last);
			Tint(_submitButton, Game.Session.ReadyToSubmit ? Good : Disabled);
		}

		void UpdateTaskInput(Mode mode)
		{
			if (mode != Mode.Task)
			{
				if (_drawing) ClearDrawing();
				_dragging = false;
				return;
			}
			var step = Game.CurrentStep;
			if (step == null) return;
			switch (step.Kind)
			{
				case StepKind.Trace: UpdateTrace(); break;
				case StepKind.Seal: UpdateSeal(); break;
				case StepKind.Hammer: UpdateHammer(); break;
			}
		}
	}
}
