using System;
using ITKamianets.Engine.Save;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Temnorid.Master.Tests
{
	public class MasterRunTests
	{
		string _saveFolder;

		[SetUp]
		public void SetUp()
		{
			_saveFolder = Path.Combine(Path.GetTempPath(), "master-playmode-" + Guid.NewGuid());
			MasterGame.SaveRootOverride = _saveFolder;
		}

		[TearDown]
		public void TearDown()
		{
			MasterGame.SaveRootOverride = null;
			Time.timeScale = 1f;
			if (Directory.Exists(_saveFolder)) Directory.Delete(_saveFolder, true);
		}

		static IEnumerator Load()
		{
			yield return SceneManager.LoadSceneAsync("Master");
			yield return null;
			yield return null;
		}

		static IEnumerator Frames(int count)
		{
			for (var i = 0; i < count; i++) yield return null;
		}

		static IEnumerator Start(MasterGame g)
		{
			g.RequestNewGame();
			yield return Frames(2);
			Assert.AreEqual(Mode.Hub, g.Mode);
		}

		/// <summary>Goes to a profession's order list from wherever the game is.</summary>
		static void ToOrders(MasterGame g, string profession)
		{
			if (g.Mode != Mode.Hub) g.ToHub();
			Assert.AreEqual(Mode.Hub, g.Mode);
			g.OpenProfession(profession);
			Assert.AreEqual(Mode.Orders, g.Mode);
		}

		/// <summary>Does the open step of the active commission with the right answer, exactly.</summary>
		static void DoStep(MasterGame g, JobDef job)
		{
			var step = g.CurrentStep;
			switch (step.Kind)
			{
				case StepKind.Inspect:
					Assert.IsTrue(g.Pick(job.Answer));
					break;
				case StepKind.Trace:
					Assert.GreaterOrEqual(g.SubmitTrace(TraceMath.Resample(MasterContent.Path(job.PathId), 80)), 90);
					break;
				case StepKind.Seal:
					g.SelectSeal(job.SealId);
					Assert.IsTrue(g.PlaceSeal(job.SealTarget.x, job.SealTarget.y));
					break;
				case StepKind.Bar:
					g.BarTime = job.Target / TimingMath.BarSpeed;
					Assert.IsTrue(g.LockBar());
					break;
				case StepKind.Hammer:
					for (var i = 0; i < 3; i++) Assert.AreEqual(100, g.Strike(job.Hammer[i]));
					break;
				case StepKind.Choose:
					Assert.IsTrue(g.Pick(job.Choice));
					break;
				case StepKind.Multi:
					foreach (var item in job.Correct) Assert.IsTrue(g.Toggle(item));
					break;
				case StepKind.Parts:
					for (var i = 0; i < 3; i++)
					{
						g.SelectPart(job.Correct[i]);
						Assert.IsTrue(g.PlaceInSlot(i));
					}
					break;
				case StepKind.Stir:
					for (var r = 0; r < 3; r++)
					{
						g.BarTime = g.StirCentre / g.StirSpeed;
						Assert.IsTrue(g.StirPress(), "round " + r);
					}
					break;
			}
		}

		/// <summary>Plays the whole commission in the open task, step by step, and files it.</summary>
		static void Finish(MasterGame g, JobDef job)
		{
			var steps = g.Steps.Length;
			for (var i = 0; i < steps; i++)
			{
				Assert.AreEqual(i, g.Step);
				DoStep(g, job);
				Assert.IsTrue(g.StepDone(i), "step " + i + " of " + job.Id);
				if (i < steps - 1) Assert.IsTrue(g.NextStep());
			}
			Assert.IsTrue(g.Session.ReadyToSubmit);
			Assert.IsTrue(g.Submit());
		}

		static void Work(MasterGame g, string id)
		{
			var job = MasterContent.FindJob(id);
			if (g.Session.ActiveJobId != id)
			{
				ToOrders(g, job.Profession);
				Assert.IsTrue(g.Take(id), "take " + id);
			}
			Assert.AreEqual(Mode.Task, g.Mode);
			Finish(g, job);
			Assert.AreEqual(Mode.Result, g.Mode);
		}

		// ---- the scene ----

		[UnityTest]
		public IEnumerator The_game_opens_on_the_title_with_six_stations_and_every_camera_view()
		{
			yield return Load();
			var g = MasterGame.Instance;
			Assert.IsNotNull(g);
			Assert.AreEqual(Mode.Title, g.Mode);
			Assert.IsFalse(g.HasSave);
			var ids = Station.All.Select(s => s.Id).ToList();
			foreach (var id in new[] { Ids.Scribe, Ids.Forge, Ids.Workshop, Ids.Cooking, Ids.Tailor, Ids.Apothecary })
			{
				Assert.AreEqual(1, ids.Count(x => x == id), "station " + id);
				Assert.IsTrue(g.Cam.Has(id + ".overview"), id + ".overview");
				Assert.IsTrue(g.Cam.Has(id + ".focus"), id + ".focus");
			}
			Assert.IsTrue(g.Cam.Has("hub") && g.Cam.Has("stable") && g.Cam.Has("train"));
			Assert.AreEqual(HorseStage.Foal, g.Session.Horse.Stage);
			Assert.AreEqual(8, g.Track.Obstacles.Length);
		}

		[UnityTest]
		public IEnumerator Every_workstation_can_be_clicked_from_the_hub_and_opens_its_orders()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			yield return Frames(30);
			var camera = g.Cam.GetComponent<Camera>();
			camera.aspect = 16f / 9f;
			foreach (var station in Station.All)
			{
				var colliders = station.GetComponentsInChildren<Collider>();
				var bounds = colliders[0].bounds;
				foreach (var c in colliders) bounds.Encapsulate(c.bounds);
				var view = camera.WorldToViewportPoint(bounds.center);
				Assert.IsTrue(view.z > 0f && view.x > 0.02f && view.x < 0.98f && view.y > 0.47f && view.y < 0.85f, station.Id + " is in view above the cards at " + view);
				Assert.IsTrue(Physics.Raycast(camera.ViewportPointToRay(view), out var hit, 80f), station.Id + " is hit");
				Assert.AreEqual(station.Id, hit.collider.GetComponentInParent<Station>()?.Id, "what the click at " + station.Id + " would open");
			}
			camera.ResetAspect();
			g.OpenProfession(Ids.Forge);
			Assert.AreEqual(Mode.Orders, g.Mode);
			Assert.AreEqual("forge.overview", g.Cam.Key);
		}

		// ---- the whole game ----

		[UnityTest]
		public IEnumerator A_player_can_finish_all_twelve_commissions_and_both_trainings_and_buy_the_upgrades()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			var s = g.Session;
			Assert.AreEqual(100, s.Coins);

			var expectedCoins = 100;
			var expectedXp = 0;
			foreach (var job in MasterContent.Jobs)
			{
				ToOrders(g, job.Profession);
				if (job.Id.EndsWith("02")) Assert.AreEqual(JobStatus.Available, s.StatusOf(job.Id));
				Work(g, job.Id);
				var r = s.LastReceipt;
				Assert.IsTrue(g.ResultSuccess, job.Id);
				Assert.IsTrue(r.FirstTime);
				Assert.GreaterOrEqual(r.Quality, 95, job.Id);
				Assert.AreEqual(Mathf.FloorToInt(job.BaseCoins * r.Quality / 100f), r.Coins, job.Id);
				expectedCoins += r.Coins;
				expectedXp += job.GlobalXp;
				Assert.AreEqual(expectedCoins, s.Coins);
				g.LeaveResult();
				yield return null;
			}
			Assert.AreEqual(12, s.CompletedCount);
			Assert.AreEqual(expectedXp, s.GlobalXp);
			Assert.AreEqual(2, s.Level);
			foreach (var p in MasterContent.Professions.Where(x => x.HasJobs)) Assert.AreEqual(35, s.SkillXp(p.Id), p.Id);

			// Every upgrade, once each.
			g.ToHub();
			g.OpenUpgrades();
			foreach (var u in MasterContent.Upgrades)
			{
				var coins = s.Coins;
				Assert.IsTrue(g.Buy(u.Id));
				Assert.IsFalse(g.Buy(u.Id), "not twice");
				Assert.AreEqual(coins - u.Price, s.Coins);
			}
			g.Back();
			Assert.AreEqual(Mode.Hub, g.Mode);

			// A repeat pays the smaller reward.
			var before = s.Coins;
			Work(g, "SCR-01");
			Assert.IsFalse(s.LastReceipt.FirstTime);
			Assert.AreEqual(7, s.Coins - before);
		}

		[UnityTest]
		public IEnumerator Wrong_answers_are_judged_only_when_confirmed_cost_nothing_and_can_be_retried()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			var s = g.Session;
			var coins = s.Coins;

			// Scribe: the wrong verdict.
			ToOrders(g, Ids.Scribe);
			g.Take("SCR-01");
			var job = MasterContent.FindJob("SCR-01");
			Assert.IsTrue(g.Pick(Ids.Flag), "the wrong choice is accepted while working");
			Assert.IsTrue(g.NextStep());
			DoStep(g, job);
			g.NextStep();
			DoStep(g, job);
			Assert.IsTrue(g.Submit());
			Assert.AreEqual(Mode.Result, g.Mode);
			Assert.IsFalse(g.ResultSuccess);
			Assert.Contains("why.inspect", s.LastOutcome.Reasons);
			Assert.AreEqual(coins, s.Coins);
			Assert.AreEqual(JobStatus.Available, s.StatusOf("SCR-01"));
			g.RetryAfterReject();
			Assert.AreEqual(Mode.Task, g.Mode);
			Assert.AreEqual(0, g.Session.Data.Count, "a clean attempt");
			Finish(g, job);
			Assert.IsTrue(g.ResultSuccess);
			g.LeaveResult();

			// Workshop: the parts in the wrong order explain which part is out of place.
			ToOrders(g, Ids.Workshop);
			g.Take("WRK-01");
			var wrk = MasterContent.FindJob("WRK-01");
			var wrong = new[] { "LEVER", "PIN", "SPRING" };
			for (var i = 0; i < 3; i++)
			{
				g.SelectPart(wrong[i]);
				g.PlaceInSlot(i);
			}
			g.NextStep();
			DoStep(g, wrk);
			var coinsBefore = s.Coins;
			Assert.IsTrue(g.Submit());
			Assert.IsFalse(g.ResultSuccess);
			Assert.Contains("why.parts", s.LastOutcome.Reasons);
			Assert.AreEqual(coinsBefore, s.Coins);
			g.RetryAfterReject();
			Finish(g, wrk);
			Assert.IsTrue(g.ResultSuccess);
			g.LeaveResult();

			// Cooking: a wrong ingredient; apothecary: a wrong label; tailor: the wrong fabric; forge: the wrong finish.
			ToOrders(g, Ids.Cooking);
			g.Take("CK-01");
			g.Toggle("WATER");
			g.Toggle("CARROT");
			g.Toggle("APPLE");
			g.NextStep();
			DoStep(g, MasterContent.FindJob("CK-01"));
			g.NextStep();
			DoStep(g, MasterContent.FindJob("CK-01"));
			Assert.IsTrue(g.Submit());
			Assert.Contains("why.set", s.LastOutcome.Reasons);
			g.RetryAfterReject();
			g.Back();
			g.AbandonActive();

			ToOrders(g, Ids.Tailor);
			g.Take("TLR-01");
			g.Pick(Ids.Linen);
			g.NextStep();
			DoStep(g, MasterContent.FindJob("TLR-01"));
			g.NextStep();
			DoStep(g, MasterContent.FindJob("TLR-01"));
			var coinsTailor = s.Coins;
			Assert.IsTrue(g.Submit());
			Assert.Contains("why.fabric", s.LastOutcome.Reasons);
			Assert.AreEqual(coinsTailor, s.Coins);
		}

		[UnityTest]
		public IEnumerator Walking_away_keeps_the_task_and_only_one_commission_is_open_at_a_time()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			var s = g.Session;
			ToOrders(g, Ids.Forge);
			g.Take("FOR-01");
			g.BarTime = 0.45f / TimingMath.BarSpeed;
			g.LockBar();
			g.Back();
			Assert.AreEqual(Mode.Orders, g.Mode);
			Assert.AreEqual("FOR-01", s.ActiveJobId);
			Assert.AreEqual("forge.overview", g.Cam.Key);
			g.Back();
			Assert.AreEqual(Mode.Hub, g.Mode);
			Assert.AreEqual("hub", g.Cam.Key);

			// Another profession's order cannot be taken, and the stable's training waits.
			g.OpenProfession(Ids.Scribe);
			Assert.IsFalse(g.Take("SCR-01"));
			Assert.IsNotNull(g.TakeBlock("SCR-01"));
			g.Back();
			g.OpenProfession(Ids.Stable);
			Assert.AreEqual("train.job", s.TrainingBlock(Ids.Commands));
			Assert.IsFalse(g.StartTraining(Ids.Commands));
			Assert.AreEqual(Mode.Stable, g.Mode);

			// Back at the forge the work is as it was left.
			g.Back();
			g.OpenProfession(Ids.Forge);
			Assert.IsTrue(g.Take("FOR-01"));
			Assert.AreEqual(1, g.Step, "the heat is done, the next step is open");
			Assert.IsTrue(s.Data.Has("heat"));

			// Giving it up keeps the wallet and the rest.
			var coins = s.Coins;
			g.Back();
			g.AbandonActive();
			Assert.IsNull(s.ActiveJobId);
			Assert.AreEqual(coins, s.Coins);
			g.Back();
			g.OpenProfession(Ids.Scribe);
			Assert.IsTrue(g.Take("SCR-01"));
		}

		// ---- the stable ----

		[UnityTest]
		public IEnumerator The_horse_can_be_cared_for_and_the_buttons_have_costs_and_cooldowns()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			var s = g.Session;
			g.OpenProfession(Ids.Stable);
			Assert.AreEqual("stable", g.Cam.Key);
			var coins = s.Coins;
			g.FeedHay();
			Assert.AreEqual(coins - 10, s.Coins);
			Assert.AreEqual(100, s.Horse.Satiety);
			g.FeedRation();
			g.Groom();
			g.PetIt();
			var trust = s.Horse.Trust;
			g.PetIt();
			Assert.AreEqual(trust, s.Horse.Trust, "no more trust until the cooldown is over");
			g.Rest();
			Assert.AreEqual(HorseAnim.Sleep, g.Horse.Anim);
			s.Horse.SetNeeds(5, 5, 5, 40);
			Assert.AreEqual("train.energy", s.TrainingBlock(Ids.Obstacles));
			Assert.IsFalse(g.StartTraining(Ids.Obstacles));
			Assert.IsTrue(g.ToastActive);
			yield return null;
		}

		[UnityTest]
		public IEnumerator A_bot_can_run_the_obstacle_course_and_the_command_timing_and_is_paid_once()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			var s = g.Session;
			g.OpenProfession(Ids.Stable);
			Time.timeScale = 6f;

			// Obstacles: jump just before each one reaches the horse.
			Assert.IsTrue(g.StartTraining(Ids.Obstacles));
			Assert.AreEqual(Mode.Obstacle, g.Mode);
			Assert.AreEqual("train", g.Cam.Key);
			Assert.IsTrue(s.TrainingActive);
			Assert.IsFalse(s.Rest().Ok, "care is blocked while training");
			var coins = s.Coins;
			var frames = 0;
			var limit = Time.realtimeSinceStartup + 90f;
			while (g.Mode == Mode.Obstacle && Time.realtimeSinceStartup < limit)
			{
				var run = g.Run;
				if (run != null)
				{
					for (var i = 0; i < ObstacleRunLogic.ObstacleCount; i++)
					{
						var t = ObstacleRunLogic.ObstacleTime(i);
						if (run.Time >= t - 0.55f && run.Time < t - 0.15f) g.Jump();
					}
				}
				yield return null;
			}
			Time.timeScale = 1f;
			Assert.AreEqual(Mode.Result, g.Mode, "the run finished");
			Assert.IsTrue(g.ResultTraining);
			var r = s.LastReceipt;
			Assert.GreaterOrEqual(r.Quality, 75, "most obstacles cleared");
			Assert.AreEqual(coins + r.Coins, s.Coins);
			Assert.AreEqual(1, s.History.Count);
			Assert.IsFalse(s.TrainingActive);
			g.LeaveResult();
			Assert.AreEqual(Mode.Stable, g.Mode);
			Assert.AreEqual(0, g.Track.Obstacles.Count(o => o.gameObject.activeSelf), "the course is put away");

			// Commands: press when the marker is in the zone.
			s.Horse.SetNeeds(70, 70, 80, 40);
			Time.timeScale = 6f;
			Assert.IsTrue(g.StartTraining(Ids.Commands));
			Assert.AreEqual(Mode.Command, g.Mode);
			frames = 0;
			var lastRound = -1;
			limit = Time.realtimeSinceStartup + 90f;
			while (g.Mode == Mode.Command && Time.realtimeSinceStartup < limit)
			{
				var timing = g.Timing;
				if (timing != null && !timing.ShowingResult && timing.Round != lastRound && timing.InZone)
				{
					lastRound = timing.Round;
					g.Command();
				}
				yield return null;
			}
			Time.timeScale = 1f;
			Assert.AreEqual(Mode.Result, g.Mode);
			Assert.AreEqual(5, s.LastReceipt.Quality / 20, "all five hit");
			Assert.AreEqual(2, s.History.Count);
			Assert.AreEqual(2, s.FirstTrainings.Count);
		}

		[UnityTest]
		public IEnumerator Leaving_a_training_pays_nothing_and_keeps_the_energy_spent()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			var s = g.Session;
			g.OpenProfession(Ids.Stable);
			var coins = s.Coins;
			Assert.IsTrue(g.StartTraining(Ids.Commands));
			Assert.AreEqual(70, s.Horse.Energy);
			yield return Frames(5);
			g.Back();
			Assert.AreEqual(Mode.Stable, g.Mode);
			Assert.IsFalse(s.TrainingActive);
			Assert.AreEqual(coins, s.Coins);
			Assert.AreEqual(70, s.Horse.Energy);
			Assert.AreEqual(0, s.History.Count);
		}

		// ---- a real mouse through the screens ----

		static InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
		static InputSettings.EditorInputBehaviorInPlayMode _editorBehaviour;
#endif

		/// <summary>Headless runs have no focused window, and a virtual mouse added then would be switched off; let it work anyway.</summary>
		static void AllowBackgroundInput()
		{
			_background = InputSystem.settings.backgroundBehavior;
			InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
			_editorBehaviour = InputSystem.settings.editorInputBehaviorInPlayMode;
			InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
		}

		static void RestoreInput()
		{
			InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
			InputSystem.settings.editorInputBehaviorInPlayMode = _editorBehaviour;
#endif
		}

		static IEnumerator MoveMouse(Mouse mouse, Vector2 position, bool down)
		{
			InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left, down));
			yield return null;
		}

		[UnityTest]
		public IEnumerator Drawing_with_a_mouse_is_scored_and_a_poor_drag_changes_nothing()
		{
			yield return Load();
			var g = MasterGame.Instance;
			var hud = UnityEngine.Object.FindFirstObjectByType<MasterHud>();
			yield return Start(g);
			ToOrders(g, Ids.Scribe);
			g.Take("SCR-01");
			g.Pick(Ids.Accept);
			g.NextStep();
			yield return Frames(4);
			AllowBackgroundInput();
			var mouse = InputSystem.AddDevice<Mouse>();
			try
			{
				var panel = hud.TracePanelRect;
				Assert.Greater(panel.width, 100f);
				var reference = MasterContent.Path("stroke_arc");
				Vector2 Screen(Vector2 p) => new Vector2(panel.xMin + p.x * panel.width, panel.yMin + p.y * panel.height);
				var points = TraceMath.Resample(reference, 80);

				yield return MoveMouse(mouse, Screen(points[0]), false);
				yield return MoveMouse(mouse, Screen(points[0]), true);
				foreach (var p in points.Skip(1)) yield return MoveMouse(mouse, Screen(p), true);
				yield return MoveMouse(mouse, Screen(points[points.Length - 1]), false);
				yield return Frames(3);
				Assert.IsTrue(g.Session.Data.Has("trace"), g.StepMessage);
				Assert.GreaterOrEqual(g.Session.Data.Int("trace"), 90);
				var kept = g.Session.Data.Int("trace");

				// A drag that goes off the paper is clamped; the saved trace is not replaced by a poor one.
				yield return MoveMouse(mouse, Screen(points[0]), true);
				for (var i = 0; i < 20; i++) yield return MoveMouse(mouse, Screen(points[0]) + new Vector2(i * 120f, i * 90f), true);
				yield return MoveMouse(mouse, new Vector2(-500f, -500f), false);
				yield return Frames(3);
				Assert.AreEqual(Mode.Task, g.Mode);
				Assert.AreEqual(kept, g.Session.Data.Int("trace"));
			}
			finally
			{
				InputSystem.RemoveDevice(mouse);
				RestoreInput();
			}
		}

		[UnityTest]
		public IEnumerator Dragging_a_seal_places_it_and_letting_go_outside_places_nothing()
		{
			yield return Load();
			var g = MasterGame.Instance;
			var hud = UnityEngine.Object.FindFirstObjectByType<MasterHud>();
			yield return Start(g);
			ToOrders(g, Ids.Scribe);
			g.Take("SCR-01");
			var job = MasterContent.FindJob("SCR-01");
			g.Pick(Ids.Accept);
			g.NextStep();
			DoStep(g, job);
			g.NextStep();
			yield return Frames(4);
			AllowBackgroundInput();
			var mouse = InputSystem.AddDevice<Mouse>();
			try
			{
				var panel = hud.SealPanelRect;
				Assert.Greater(panel.width, 100f);
				Vector2 Screen(Vector2 p) => new Vector2(panel.xMin + p.x * panel.width, panel.yMin + p.y * panel.height);
				var target = job.SealTarget;

				// With no seal chosen a press on the paper does nothing.
				yield return MoveMouse(mouse, Screen(target), true);
				yield return MoveMouse(mouse, Screen(target), false);
				yield return Frames(2);
				Assert.IsFalse(g.Session.Data.Has("seal"));

				g.SelectSeal(Ids.Academy);
				yield return MoveMouse(mouse, Screen(new Vector2(0.5f, 0.6f)), true);
				yield return MoveMouse(mouse, Screen(new Vector2(0.6f, 0.45f)), true);
				yield return MoveMouse(mouse, Screen(target + new Vector2(0.02f, 0f)), true);
				yield return MoveMouse(mouse, Screen(target + new Vector2(0.02f, 0f)), false);
				yield return Frames(3);
				Assert.AreEqual(Ids.Academy, g.Session.Data.Get("seal"));
				Assert.AreEqual(0f, Vector2.Distance(target + new Vector2(0.02f, 0f), g.Session.Data.Vector("sealpos")), 0.02f);

				var placed = g.Session.Data.Vector("sealpos");
				yield return MoveMouse(mouse, Screen(new Vector2(0.3f, 0.5f)), true);
				yield return MoveMouse(mouse, new Vector2(-300f, -300f), true);
				yield return MoveMouse(mouse, new Vector2(-300f, -300f), false);
				yield return Frames(3);
				Assert.AreEqual(placed.x, g.Session.Data.Vector("sealpos").x, 1e-4f);
				Assert.AreEqual(Mode.Task, g.Mode);
			}
			finally
			{
				InputSystem.RemoveDevice(mouse);
				RestoreInput();
			}
		}

		[UnityTest]
		public IEnumerator Clicking_the_anvil_strikes_the_circles_in_turn()
		{
			yield return Load();
			var g = MasterGame.Instance;
			var hud = UnityEngine.Object.FindFirstObjectByType<MasterHud>();
			yield return Start(g);
			ToOrders(g, Ids.Forge);
			g.Take("FOR-01");
			var job = MasterContent.FindJob("FOR-01");
			DoStep(g, job);
			g.NextStep();
			yield return Frames(4);
			AllowBackgroundInput();
			var mouse = InputSystem.AddDevice<Mouse>();
			try
			{
				var panel = hud.AnvilRect;
				Assert.Greater(panel.width, 100f);
				Vector2 Screen(Vector2 p) => new Vector2(panel.xMin + p.x * panel.width, panel.yMin + p.y * panel.height);
				for (var i = 0; i < 3; i++)
				{
					yield return MoveMouse(mouse, Screen(job.Hammer[i]), false);
					yield return MoveMouse(mouse, Screen(job.Hammer[i]), true);
					yield return MoveMouse(mouse, Screen(job.Hammer[i]), false);
					yield return Frames(2);
					Assert.IsTrue(g.Session.Data.Has("h" + i), "strike " + i);
				}
				Assert.IsTrue(g.StepDone(g.Step));
				Assert.AreEqual(3, Enumerable.Range(0, 3).Count(i => g.Session.Data.Has("h" + i)));
			}
			finally
			{
				InputSystem.RemoveDevice(mouse);
				RestoreInput();
			}
		}

		[UnityTest]
		public IEnumerator Space_stops_the_sweeping_bar_and_the_screen_buttons_work()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			ToOrders(g, Ids.Forge);
			g.Take("FOR-01");
			Assert.AreEqual(StepKind.Bar, g.CurrentStep.Kind);
			var before = g.BarTime;
			yield return Frames(10);
			Assert.Greater(g.BarTime, before, "the marker sweeps");
			Assert.IsTrue(g.LockBar());
			var locked = g.Session.Data.Float("heat");
			yield return Frames(5);
			Assert.AreEqual(locked, g.Session.Data.Float("heat"), 1e-6f, "the lock holds");
			Assert.IsFalse(g.LockBar(), "a second press changes nothing");
			Assert.IsFalse(g.GoToStep(2), "the third step is closed until the second is done");
			g.ResetStep();
			Assert.IsFalse(g.Session.Data.Has("heat"));
		}

		// ---- saving ----

		[UnityTest]
		public IEnumerator Quitting_mid_task_and_continuing_keeps_everything_and_does_not_pay_twice()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			Work(g, "SCR-01");
			g.LeaveResult();
			g.ToHub();
			g.OpenUpgrades();
			g.Buy(Ids.Guide);
			g.Back();
			ToOrders(g, Ids.Forge);
			g.Take("FOR-01");
			var job = MasterContent.FindJob("FOR-01");
			DoStep(g, job);
			g.NextStep();
			var coins = g.Session.Coins;
			var attempt = g.Session.AttemptId;
			g.SaveNow();
			Assert.IsTrue(g.Store.Exists);

			yield return Load();
			g = MasterGame.Instance;
			Assert.AreEqual(Mode.Title, g.Mode);
			Assert.IsTrue(g.HasSave);
			Assert.IsTrue(g.ContinueGame());
			Assert.AreEqual(Mode.Hub, g.Mode);
			var s = g.Session;
			Assert.AreEqual(coins, s.Coins);
			Assert.IsTrue(s.Has(Ids.Guide));
			Assert.AreEqual(JobStatus.Completed, s.StatusOf("SCR-01"));
			Assert.AreEqual("FOR-01", s.ActiveJobId);
			Assert.AreEqual(attempt, s.AttemptId);
			Assert.IsTrue(s.Data.Has("heat"));
			g.OpenProfession(Ids.Forge);
			Assert.IsTrue(g.Take("FOR-01"));
			Assert.AreEqual(1, g.Step);
			Finish2(g, job);
			Assert.IsTrue(g.ResultSuccess);
			var after = s.Coins;
			Assert.IsFalse(g.Submit(), "the result is already filed");
			Assert.AreEqual(after, s.Coins);
		}

		static void Finish2(MasterGame g, JobDef job)
		{
			// The first step is already done; carry on from the open one.
			var steps = g.Steps.Length;
			for (var i = g.Step; i < steps; i++)
			{
				DoStep(g, job);
				if (i < steps - 1) Assert.IsTrue(g.NextStep());
			}
			Assert.IsTrue(g.Submit());
		}

		[UnityTest]
		public IEnumerator Time_away_changes_the_horse_once_and_never_more_than_eight_hours()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			g.Session.Horse.SetNeeds(70, 70, 40, 40);
			var save = g.Session.ToSave(DateTime.UtcNow);
			save.lastUpdatedUtcTicks = DateTime.UtcNow.AddHours(-30).Ticks;
			Assert.IsTrue(g.Store.Save(save));

			yield return Load();
			g = MasterGame.Instance;
			Assert.IsTrue(g.ContinueGame());
			Assert.AreEqual(54, g.Session.Horse.Satiety, 1, "eight hours at two a hour");
			Assert.AreEqual(80, g.Session.Horse.Energy, 1);
			var satiety = g.Session.Horse.SatietyExact;
			g.Session.ApplyElapsed(DateTime.UtcNow);
			Assert.AreEqual(satiety, g.Session.Horse.SatietyExact, 0.05, "not applied again");
		}

		[UnityTest]
		public IEnumerator A_corrupt_save_opens_the_recovery_menu_with_the_backup_new_game_and_quit()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			g.OpenProfession(Ids.Stable);
			g.FeedHay();
			g.SaveNow();
			g.FeedHay();
			g.SaveNow();
			Assert.IsTrue(g.Store.BackupExists);
			File.WriteAllText(g.Store.Path_, "{ definitely not a save");

			yield return Load();
			g = MasterGame.Instance;
			Assert.IsFalse(g.HasSave);
			Assert.IsTrue(g.SaveProblem);
			Assert.IsFalse(g.ContinueGame());
			Assert.AreEqual(Mode.Recovery, g.Mode);
			Assert.AreEqual("recovery.corrupt", g.RecoveryReason);
			Assert.IsTrue(g.TryBackup());
			Assert.AreEqual(Mode.Hub, g.Mode);
			Assert.Less(g.Session.Coins, 100, "the backup is one of the later saves");

			// With no backup at all the menu says so and starting fresh still works.
			g.Store.Delete();
			File.WriteAllText(g.Store.Path_, "garbage");
			yield return Load();
			g = MasterGame.Instance;
			g.ContinueGame();
			Assert.AreEqual(Mode.Recovery, g.Mode);
			Assert.IsFalse(g.TryBackup());
			Assert.AreEqual("recovery.nobackup", g.RecoveryNote);
			g.StartFresh();
			Assert.AreEqual(Mode.Hub, g.Mode);
			Assert.AreEqual(100, g.Session.Coins);
			Assert.IsTrue(g.Store.Exists);
		}

		[UnityTest]
		public IEnumerator A_save_from_a_newer_version_is_not_opened()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			new SaveStore("master", _saveFolder).Save("default", g.Session.ToSave(DateTime.UtcNow), 99);
			yield return Load();
			g = MasterGame.Instance;
			g.ContinueGame();
			Assert.AreEqual(Mode.Recovery, g.Mode);
			Assert.AreEqual("recovery.newer", g.RecoveryReason);
		}

		// ---- the shell ----

		[UnityTest]
		public IEnumerator Esc_and_the_back_buttons_always_lead_somewhere_valid()
		{
			yield return Load();
			var g = MasterGame.Instance;
			Assert.AreEqual(Mode.Title, g.Mode);
			g.OpenSettings();
			Assert.AreEqual(Mode.Settings, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Title, g.Mode);
			yield return Start(g);
			g.OpenJournal();
			g.Back();
			g.OpenUpgrades();
			g.Back();
			Assert.AreEqual(Mode.Hub, g.Mode);
			g.OpenHelp();
			Assert.AreEqual(Mode.Help, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Hub, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Pause, g.Mode);
			g.OpenSettings();
			g.Back();
			Assert.AreEqual(Mode.Pause, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Hub, g.Mode);
			g.OpenProfession(Ids.Scribe);
			g.Take("SCR-01");
			g.OpenHelp();
			Assert.AreEqual(Mode.Help, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Task, g.Mode, "back from the help returns to the task");
			g.Back();
			Assert.AreEqual(Mode.Orders, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Hub, g.Mode);

			// A new game asks first when there is progress, and a reset needs its confirmation.
			g.ToTitle();
			Assert.AreEqual(Mode.Title, g.Mode);
			Assert.IsTrue(g.HasSave);
			g.RequestNewGame();
			Assert.AreEqual(Mode.ConfirmNew, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Title, g.Mode);
			g.OpenSettings();
			g.RequestReset();
			Assert.AreEqual(Mode.ConfirmReset, g.Mode);
			g.Back();
			Assert.AreEqual(Mode.Settings, g.Mode);
			g.RequestReset();
			g.ConfirmReset();
			Assert.AreEqual(Mode.Title, g.Mode);
			Assert.IsFalse(g.HasSave);
		}

		[UnityTest]
		public IEnumerator Every_screen_shows_without_errors_in_every_mode()
		{
			yield return Load();
			var g = MasterGame.Instance;
			yield return Start(g);
			yield return Frames(3);
			foreach (var p in MasterContent.Professions)
			{
				g.OpenProfession(p.Id);
				yield return Frames(2);
				g.Back();
			}
			foreach (var job in MasterContent.Jobs)
			{
				ToOrders(g, job.Profession);
				if (job.Id.EndsWith("02")) continue;
				g.Take(job.Id);
				var steps = g.Steps.Length;
				for (var i = 0; i < steps; i++)
				{
					yield return Frames(2);
					DoStep(g, job);
					if (i < steps - 1) g.NextStep();
				}
				yield return Frames(2);
				g.Submit();
				yield return Frames(2);
				g.LeaveResult();
			}
			g.OpenProfession(Ids.Stable);
			yield return Frames(2);
			LogAssert.NoUnexpectedReceived();
		}
	}
}
