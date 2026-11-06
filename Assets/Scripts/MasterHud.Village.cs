using System;
using System.Linq;
using ITKamianets.Engine.Shell;
using ITKamianets.Engine.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Temnorid.Master
{
	/// <summary>
	/// The first screens after sign-in: the village, where the elder asks which profession the player wants to learn, and the teachers,
	/// one profession at a time, full screen, with arrows to the next. Drawn from rectangles like the profession icons.
	/// </summary>
	public sealed partial class MasterHud
	{
		static readonly Color Skin = new Color(0.93f, 0.76f, 0.6f);

		// One teacher per profession: robe, hat and the thing they hold.
		static readonly Color[] Robes =
		{
			new Color(0.42f, 0.3f, 0.18f), new Color(0.25f, 0.3f, 0.55f), new Color(0.35f, 0.2f, 0.18f), new Color(0.3f, 0.42f, 0.4f),
			new Color(0.8f, 0.78f, 0.7f), new Color(0.55f, 0.25f, 0.45f), new Color(0.3f, 0.5f, 0.28f),
		};

		static readonly Color[] Hats =
		{
			new Color(0.6f, 0.45f, 0.2f), new Color(0.15f, 0.18f, 0.35f), new Color(0.2f, 0.15f, 0.14f), new Color(0.45f, 0.4f, 0.2f),
			new Color(0.95f, 0.95f, 0.92f), new Color(0.3f, 0.12f, 0.25f), new Color(0.22f, 0.36f, 0.2f),
		};

		GameObject[] _people;
		Image[] _dots;
		Text _mentorName, _mentorTag, _mentorCount, _mentorTitle, _mentorSay, _villageAsk;
		GameObject _villageYes;
		string _tabsKey = "";

		// ---- drawing helpers ----

		/// <summary>A teacher, drawn in a box (0..1 both ways): legs, robe, arms, head, a hat and what they hold.</summary>
		void BuildPerson(Transform host, Color robe, Color hat, string profession, bool elder)
		{
			var dark = new Color(robe.r * 0.55f, robe.g * 0.55f, robe.b * 0.55f);
			Part(host, 0.25f, 0.0f, 0.45f, 0.22f, new Color(0.18f, 0.12f, 0.08f));
			Part(host, 0.55f, 0.0f, 0.75f, 0.22f, new Color(0.18f, 0.12f, 0.08f));
			Part(host, 0.14f, 0.2f, 0.86f, 0.64f, robe);
			Part(host, 0.14f, 0.38f, 0.86f, 0.43f, dark);
			Part(host, 0.0f, 0.3f, 0.16f, 0.6f, robe);
			Part(host, 0.84f, 0.3f, 1.0f, 0.6f, robe);
			Part(host, 0.0f, 0.26f, 0.16f, 0.32f, Skin);
			Part(host, 0.84f, 0.26f, 1.0f, 0.32f, Skin);
			Part(host, 0.3f, 0.64f, 0.7f, 0.88f, Skin);
			Part(host, 0.38f, 0.74f, 0.44f, 0.79f, new Color(0.1f, 0.08f, 0.06f));
			Part(host, 0.56f, 0.74f, 0.62f, 0.79f, new Color(0.1f, 0.08f, 0.06f));
			Part(host, 0.42f, 0.67f, 0.58f, 0.69f, new Color(0.6f, 0.3f, 0.25f));
			Part(host, 0.24f, 0.86f, 0.76f, 0.94f, hat);
			Part(host, 0.34f, 0.93f, 0.66f, 1.0f, hat);

			if (elder)
			{
				Part(host, 0.3f, 0.5f, 0.7f, 0.68f, new Color(0.92f, 0.92f, 0.9f));
				Part(host, 0.9f, 0.0f, 0.95f, 0.98f, new Color(0.4f, 0.27f, 0.14f));
				return;
			}

			// What each one holds in the right hand: a shape that differs, so the colour is never the only sign.
			switch (profession)
			{
				case Ids.Stable:
					Part(host, 0.9f, 0.2f, 0.94f, 0.7f, new Color(0.45f, 0.3f, 0.15f));
					Part(host, 0.84f, 0.66f, 1.0f, 0.74f, new Color(0.85f, 0.8f, 0.4f));
					break;
				case Ids.Scribe:
					Part(host, 0.92f, 0.3f, 0.95f, 0.85f, new Color(0.95f, 0.95f, 0.9f));
					Part(host, 0.9f, 0.82f, 0.97f, 0.92f, new Color(0.95f, 0.95f, 0.9f));
					break;
				case Ids.Forge:
					Part(host, 0.9f, 0.2f, 0.94f, 0.7f, new Color(0.4f, 0.3f, 0.2f));
					Part(host, 0.8f, 0.68f, 1.0f, 0.82f, new Color(0.5f, 0.52f, 0.56f));
					break;
				case Ids.Workshop:
					Part(host, 0.88f, 0.28f, 1.0f, 0.4f, new Color(0.7f, 0.72f, 0.75f));
					Part(host, 0.92f, 0.22f, 0.96f, 0.6f, new Color(0.7f, 0.72f, 0.75f));
					break;
				case Ids.Cooking:
					Part(host, 0.9f, 0.25f, 0.94f, 0.7f, new Color(0.5f, 0.35f, 0.2f));
					Part(host, 0.82f, 0.62f, 1.0f, 0.74f, new Color(0.7f, 0.7f, 0.72f));
					break;
				case Ids.Tailor:
					Part(host, 0.86f, 0.32f, 0.9f, 0.7f, new Color(0.8f, 0.8f, 0.85f));
					Part(host, 0.94f, 0.32f, 0.98f, 0.7f, new Color(0.8f, 0.8f, 0.85f));
					break;
				default:
					Part(host, 0.88f, 0.3f, 1.0f, 0.58f, new Color(0.35f, 0.65f, 0.3f));
					Part(host, 0.92f, 0.56f, 0.96f, 0.7f, new Color(0.35f, 0.65f, 0.3f));
					break;
			}
		}

		static void House(Transform host, float x, float y, float w, float h, Color wall, Color roof)
		{
			Part(host, x, y, x + w, y + h, wall);
			Part(host, x - 0.012f, y + h, x + w + 0.012f, y + h + 0.035f, roof);
			Part(host, x + 0.005f, y + h + 0.035f, x + w - 0.005f, y + h + 0.07f, roof);
			Part(host, x + 0.02f, y + h + 0.07f, x + w - 0.02f, y + h + 0.1f, roof);
			Part(host, x + w * 0.42f, y, x + w * 0.62f, y + h * 0.55f, new Color(0.3f, 0.2f, 0.12f));
			Part(host, x + w * 0.12f, y + h * 0.5f, x + w * 0.3f, y + h * 0.8f, new Color(0.75f, 0.88f, 0.95f));
		}

		static void Tree(Transform host, float x, float y, float s)
		{
			Part(host, x + 0.012f * s, y, x + 0.026f * s, y + 0.1f * s, new Color(0.38f, 0.26f, 0.14f));
			Part(host, x - 0.012f * s, y + 0.08f * s, x + 0.05f * s, y + 0.2f * s, new Color(0.2f, 0.45f, 0.22f));
			Part(host, x, y + 0.18f * s, x + 0.038f * s, y + 0.26f * s, new Color(0.24f, 0.52f, 0.25f));
		}

		// ---- the village ----

		void BuildVillage(Transform root)
		{
			// The title is only a dark backdrop for the moments before the game opens in the village.
			_screens[Mode.Title] = Box("Title", root, 0f, 0f, 1f, 1f, new Color(0.06f, 0.04f, 0.03f, 1f));

			var screen = Box("Village", root, 0f, 0f, 1f, 1f, new Color(0.6f, 0.78f, 0.92f));
			_screens[Mode.Village] = screen;
			var t = screen.transform;

			Part(t, 0.8f, 0.8f, 0.9f, 0.95f, new Color(1f, 0.92f, 0.5f));
			Part(t, 0.1f, 0.78f, 0.24f, 0.84f, new Color(1f, 1f, 1f, 0.8f));
			Part(t, 0.14f, 0.82f, 0.3f, 0.88f, new Color(1f, 1f, 1f, 0.8f));
			Part(t, 0.55f, 0.84f, 0.68f, 0.9f, new Color(1f, 1f, 1f, 0.7f));
			Part(t, 0f, 0.32f, 1f, 0.5f, new Color(0.45f, 0.62f, 0.38f));
			Part(t, 0f, 0f, 1f, 0.34f, new Color(0.36f, 0.52f, 0.26f));
			Part(t, 0.4f, 0f, 0.62f, 0.34f, new Color(0.7f, 0.6f, 0.4f));

			House(t, 0.03f, 0.34f, 0.13f, 0.17f, new Color(0.85f, 0.78f, 0.62f), new Color(0.65f, 0.25f, 0.2f));
			House(t, 0.2f, 0.36f, 0.1f, 0.14f, new Color(0.78f, 0.7f, 0.55f), new Color(0.5f, 0.3f, 0.18f));
			House(t, 0.64f, 0.36f, 0.12f, 0.16f, new Color(0.88f, 0.82f, 0.68f), new Color(0.7f, 0.3f, 0.2f));
			Tree(t, 0.36f, 0.34f, 1.3f);
			Tree(t, 0.55f, 0.34f, 1.1f);
			Tree(t, 0.94f, 0.34f, 1.4f);

			// The elder, with his question.
			var host = NewHost("Elder", t, 100f);
			host.anchorMin = new Vector2(0.8f, 0.1f);
			host.anchorMax = new Vector2(0.92f, 0.56f);
			host.offsetMin = host.offsetMax = Vector2.zero;
			BuildPerson(host, new Color(0.3f, 0.4f, 0.6f), new Color(0.45f, 0.3f, 0.18f), "", true);

			Part(t, 0.755f, 0.52f, 0.8f, 0.58f, Parchment);
			var bubble = Box("Bubble", t, 0.28f, 0.5f, 0.76f, 0.8f, Parchment);
			bubble.GetComponent<Image>().raycastTarget = false;
			Lbl(bubble.transform, Loc.T("village.elder"), 30, TextAnchor.UpperLeft, 0.04f, 0.74f, 0.96f, 0.96f, new Color(0.5f, 0.3f, 0.1f));
			_villageAsk = Lbl(bubble.transform, "", 40, TextAnchor.MiddleCenter, 0.04f, 0.08f, 0.96f, 0.76f, Dark, true);

			_villageYes = Btn(t, Loc.T("village.yes"), 0.28f, 0.34f, 0.76f, 0.46f, () => Game.StartLearning(), 38).gameObject;

			Btn(t, Loc.T("quit"), 0.78f, 0.03f, 0.98f, 0.12f, () => Game.Quit(), 28);
		}

		void RefreshVillage()
		{
			var learned = Game.Session.Learned.Count;
			var more = Game.Candidates.Count > 0;
			UiBuilder.SetText(_villageAsk, Loc.T(!more ? "village.done" : learned == 0 ? "village.ask" : "village.more"));
			_villageYes.SetActive(more);
		}

		// ---- the teachers: the professions not learned yet, one at a time ----

		void BuildMentor(Transform root)
		{
			var t = Clear(Mode.Mentor, root);

			var header = Box("Header", t, 0.2f, 0.78f, 0.8f, 0.97f, new Color(Panel.r, Panel.g, Panel.b, 0.85f));
			header.GetComponent<Image>().raycastTarget = false;
			_mentorTitle = Lbl(header.transform, "", 52, TextAnchor.MiddleCenter, 0.02f, 0.4f, 0.78f, 1f, Gold);
			_mentorTag = Lbl(header.transform, "", 24, TextAnchor.MiddleCenter, 0.02f, 0f, 0.98f, 0.42f, Ink);
			_mentorCount = Lbl(header.transform, "", 28, TextAnchor.MiddleRight, 0.78f, 0.45f, 0.98f, 1f, Muted);

			var count = MasterContent.Professions.Length;
			_dots = new Image[count];
			for (var i = 0; i < count; i++)
			{
				var x = 0.5f - count * 0.0125f + i * 0.025f;
				var dot = Box("Dot", t, x, 0.745f, x + 0.016f, 0.765f, Muted);
				dot.GetComponent<Image>().raycastTarget = false;
				_dots[i] = dot.GetComponent<Image>();
			}

			Btn(t, "<", 0.01f, 0.4f, 0.07f, 0.62f, () => Game.MentorStep(-1), 72);
			Btn(t, ">", 0.93f, 0.4f, 0.99f, 0.62f, () => Game.MentorStep(1), 72);

			_people = new GameObject[count];
			for (var i = 0; i < count; i++)
			{
				var host = NewHost("Teacher", t, 100f);
				host.anchorMin = new Vector2(0.09f, 0.1f);
				host.anchorMax = new Vector2(0.22f, 0.58f);
				host.offsetMin = host.offsetMax = Vector2.zero;
				BuildPerson(host, Robes[i], Hats[i], MasterContent.Professions[i].Id, false);
				_people[i] = host.gameObject;
			}

			Part(t, 0.225f, 0.3f, 0.265f, 0.36f, Parchment);
			var bubble = Box("Bubble", t, 0.26f, 0.05f, 0.76f, 0.34f, Parchment);
			bubble.GetComponent<Image>().raycastTarget = false;
			_mentorName = Lbl(bubble.transform, "", 32, TextAnchor.UpperLeft, 0.04f, 0.72f, 0.96f, 0.96f, new Color(0.5f, 0.3f, 0.1f));
			_mentorSay = Lbl(bubble.transform, "", 34, TextAnchor.MiddleLeft, 0.04f, 0.06f, 0.96f, 0.74f, Dark, true);

			Btn(t, Loc.T("mentor.learn"), 0.79f, 0.17f, 0.97f, 0.32f, () => Game.MentorLearn(), 36);
			Btn(t, Loc.T("back"), 0.79f, 0.04f, 0.97f, 0.14f, () => Game.Back(), 28);
		}

		void RefreshMentor()
		{
			var candidates = Game.Candidates;
			var id = Game.MentorId;
			var index = candidates.Count == 0 ? 0 : Mathf.Clamp(Game.MentorIndex, 0, candidates.Count - 1);
			UiBuilder.SetText(_mentorTitle, Name(id));
			UiBuilder.SetText(_mentorTag, Loc.T("prof." + id + ".tag"));
			UiBuilder.SetText(_mentorCount, Loc.T("mentor.of", index + 1, candidates.Count));
			UiBuilder.SetText(_mentorName, Loc.T("mentor." + id + ".name"));
			UiBuilder.SetText(_mentorSay, Loc.T("mentor." + id + ".say"));
			var person = Array.FindIndex(MasterContent.Professions, p => p.Id == id);
			for (var i = 0; i < _people.Length; i++)
			{
				if (_people[i].activeSelf != (i == person)) _people[i].SetActive(i == person);
				_dots[i].gameObject.SetActive(i < candidates.Count);
				_dots[i].color = i == index ? Gold : Muted;
			}
		}

		// ---- the tabs: the village and the learned professions ----

		void BuildTabArrows()
		{
			// The arrows sit in the shell's top bar, on each side of the one tab on show.
			_chrome.EnableNavigation();
			_chrome.NavigatePrevious += () => Game.TabStep(-1);
			_chrome.NavigateNext += () => Game.TabStep(1);
		}

		/// <summary>The top bar lists the village and the learned professions, and the arrows beside the screen step through them.</summary>
		void RefreshTabs(Mode mode)
		{
			var tabs = Game.Tabs;
			var current = Game.CurrentTab;
			if (current != _tabsKey)
			{
				// Only the place the player is in is shown; the arrows go to the others.
				_tabsKey = current;
				_chrome.SetPlaces(new[] { new ShellPlace(current, current == "village" ? Loc.T("village.tab") : Name(current), "") });
				_chrome.Select(current, false);
			}
			_chrome.SetNavigationVisible(tabs.Length > 1 && (mode == Mode.Village || mode == Mode.Orders || mode == Mode.Stable));
		}
	}
}
