using ITKamianets.Engine.Graybox;
using ITKamianets.Engine.Graybox.EditorTools;
using ITKamianets.Engine.Shell.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Temnorid.Master.EditorTools
{
	/// <summary>
	/// Builds the whole game from Unity primitives: the common workshop with its six workstations, the stable with the horse and the side
	/// of the obstacle course, the cameras, the light, the sound and the screens. Run it from the menu (Temnorid > Master > Build Scene) or the
	/// command line (<c>-executeMethod Temnorid.Master.EditorTools.MasterSceneBuilder.Build</c>); it rebuilds <c>Assets/Scenes/Master.unity</c>
	/// from scratch each time, so do not edit that scene by hand.
	/// </summary>
	public static class MasterSceneBuilder
	{
		public const string ScenePath = "Assets/Scenes/Master.unity";
		const string MaterialDir = "Assets/Materials";
		static readonly Vector3 StablePos = new Vector3(100f, 0f, 0f);
		static readonly string[] StationOrder = { Ids.Scribe, Ids.Forge, Ids.Workshop, Ids.Cooking, Ids.Tailor, Ids.Apothecary };
		const float StationZ = 1.5f;

		static Material _floor, _wall, _wood, _woodDark, _stone, _iron, _ironDark, _parchment, _ink, _wax, _brass, _gold, _red, _white, _cloth, _blue, _green, _herb, _clay, _straw, _hay,
			_coat, _mane, _dark, _grass, _dirt, _billet, _fire, _orange, _tan, _leather;

		[MenuItem("Temnorid/Master/Build Scene")]
		public static void Build()
		{
			ShellSceneKit.ApplyPlatformSettings("Temnorid", "Temnorid Master", "com.itkamianets.master", "0.2.0");
			PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.itkamianets.temnoridmaster");
			PlayerSettings.runInBackground = true;
			PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;

			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
			RenderSettings.ambientLight = new Color(0.5f, 0.46f, 0.4f);
			MakeMaterials();

			var room = new GameObject("Buildings").transform;
			BuildSun(room);
			var stations = new GameObject("Stations").transform;
			Renderer billet = null;
			for (var i = 0; i < StationOrder.Length; i++)
			{
				// Every profession has a building of its own, far from the others, so only the one on show is ever in view.
				BuildBuilding(room, i);
				var root = MakeStation(stations, StationOrder[i], BuildingOrigin(i) + new Vector3(0f, 0f, StationZ));
				switch (StationOrder[i])
				{
					case Ids.Scribe: BuildScribe(root); break;
					case Ids.Forge: billet = BuildForge(root); break;
					case Ids.Workshop: BuildWorkshop(root); break;
					case Ids.Cooking: BuildCooking(root); break;
					case Ids.Tailor: BuildTailor(root); break;
					default: BuildApothecary(root); break;
				}
				Finish(root);
			}

			var stableRoot = new GameObject("Stable").transform;
			stableRoot.gameObject.AddComponent<AssetIdTag>().Id = "buildings/stable";
			BuildStable(stableRoot);
			var track = BuildTrack();
			var horse = BuildHorse();

			var anchors = new GameObject("CameraAnchors").transform;
			var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
			var camera = cameraObject.AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.45f, 0.62f, 0.8f);
			camera.fieldOfView = 52f;
			camera.nearClipPlane = 0.2f;
			camera.farClipPlane = 300f;
			cameraObject.AddComponent<AudioListener>();
			var view = cameraObject.AddComponent<ViewCamera>();
			var poses = new System.Collections.Generic.List<CameraPose>
			{
				Pose(anchors, "hub", new Vector3(0f, 7.2f, -19f), new Vector3(0f, 0.6f, 3f), 50f),
				Pose(anchors, "stable", StablePos + new Vector3(3.6f, 3.3f, -7.6f), StablePos + new Vector3(0.6f, 1.1f, 0f), 42f),
				Pose(anchors, "train", StablePos + new Vector3(6.5f, 3.2f, -10.5f), StablePos + new Vector3(6.5f, 0.9f, 0f), 58f),
			};
			for (var i = 0; i < StationOrder.Length; i++)
			{
				var o = BuildingOrigin(i);
				poses.Add(Pose(anchors, StationOrder[i] + ".overview", o + new Vector3(-2.6f, 3.6f, -7.0f), o + new Vector3(-2.6f, 1.5f, StationZ), 52f));
				poses.Add(Pose(anchors, StationOrder[i] + ".focus", o + new Vector3(0f, 5.0f, -1.2f), o + new Vector3(0f, 0.9f, StationZ), 45f));
			}
			view.Poses = poses.ToArray();
			cameraObject.transform.position = poses[0].Anchor.position;

			var gameObject = new GameObject("Game");
			var audio = gameObject.AddComponent<MasterAudio>();
			var game = gameObject.AddComponent<MasterGame>();
			game.Cam = view;
			game.Audio = audio;
			game.Horse = horse;
			game.Track = track;
			game.Billet = billet;
			gameObject.AddComponent<MasterHud>().Game = game;

			EditorSceneManager.SaveScene(scene, ScenePath);
			// The shell scenes open new scenes: the game scene is saved first and nothing from it is needed after.
			ShellSceneKit.BuildBoot();
			ShellSceneKit.BuildMenu();
			ShellSceneKit.SetBuildOrder(ScenePath);
			AssetDatabase.SaveAssets();
			Debug.Log("[Master] Scene built: " + ScenePath);
		}

		static void MakeMaterials()
		{
			_floor = Mat("Floor", new Color(0.4f, 0.28f, 0.18f));
			_wall = Mat("Wall", new Color(0.66f, 0.58f, 0.46f));
			_wood = Mat("Wood", new Color(0.52f, 0.36f, 0.22f));
			_woodDark = Mat("WoodDark", new Color(0.32f, 0.21f, 0.12f));
			_stone = Mat("Stone", new Color(0.5f, 0.48f, 0.46f));
			_iron = Mat("Iron", new Color(0.42f, 0.43f, 0.48f));
			_ironDark = Mat("IronDark", new Color(0.2f, 0.2f, 0.24f));
			_parchment = Mat("Parchment", new Color(0.9f, 0.83f, 0.66f));
			_ink = Mat("Ink", new Color(0.08f, 0.07f, 0.1f));
			_wax = Mat("Wax", new Color(0.62f, 0.1f, 0.1f));
			_brass = Mat("Brass", new Color(0.8f, 0.6f, 0.2f));
			_gold = Mat("Gold", new Color(0.95f, 0.78f, 0.3f));
			_red = Mat("Red", new Color(0.78f, 0.2f, 0.18f));
			_white = Mat("White", new Color(0.95f, 0.93f, 0.88f));
			_cloth = Mat("Cloth", new Color(0.32f, 0.4f, 0.62f));
			_blue = Mat("Blue", new Color(0.35f, 0.5f, 0.65f));
			_green = Mat("Green", new Color(0.3f, 0.55f, 0.3f));
			_herb = Mat("Herb", new Color(0.4f, 0.6f, 0.3f));
			_clay = Mat("Clay", new Color(0.65f, 0.4f, 0.28f));
			_straw = Mat("Straw", new Color(0.78f, 0.65f, 0.35f));
			_hay = Mat("Hay", new Color(0.88f, 0.75f, 0.3f));
			_coat = Mat("HorseCoat", new Color(0.64f, 0.31f, 0.14f));
			_mane = Mat("HorseMane", new Color(0.2f, 0.12f, 0.08f));
			_dark = Mat("Dark", new Color(0.1f, 0.08f, 0.08f));
			_grass = Mat("Grass", new Color(0.36f, 0.55f, 0.28f));
			_dirt = Mat("Dirt", new Color(0.58f, 0.46f, 0.3f));
			_billet = Mat("Billet", new Color(0.35f, 0.35f, 0.38f));
			_fire = Mat("Fire", new Color(1f, 0.55f, 0.15f));
			_orange = Mat("Carrot", new Color(0.9f, 0.5f, 0.12f));
			_tan = Mat("Onion", new Color(0.82f, 0.68f, 0.4f));
			_leather = Mat("Leather", new Color(0.4f, 0.22f, 0.12f));
		}

		// ---- the workshop ----

		/// <summary>Where a profession's building stands: 120 units apart on a line, far enough that no other building is ever in view.</summary>
		static Vector3 BuildingOrigin(int index) => new Vector3(-200f, 0f, -120f * (index + 1));

		static void BuildSun(Transform room)
		{
			var sun = new GameObject("Sun");
			sun.transform.SetParent(room, false);
			sun.transform.rotation = Quaternion.Euler(48f, -20f, 0f);
			var d = sun.AddComponent<Light>();
			d.type = LightType.Directional;
			d.color = new Color(1f, 0.93f, 0.8f);
			d.intensity = 1.0f;
			d.shadows = LightShadows.Soft;
			d.shadowStrength = 0.6f;
		}

		/// <summary>One building: a grass yard, a floor, three walls (the front is open towards the camera), beams and a lamp.</summary>
		static void BuildBuilding(Transform parent, int index)
		{
			var o = BuildingOrigin(index);
			var walls = new[]
			{
				new Color(0.72f, 0.64f, 0.5f), new Color(0.6f, 0.62f, 0.7f), new Color(0.5f, 0.46f, 0.44f),
				new Color(0.66f, 0.56f, 0.4f), new Color(0.8f, 0.74f, 0.66f), new Color(0.54f, 0.64f, 0.5f),
			};
			var wall = Mat("Wall" + StationOrder[index], walls[index % walls.Length]);
			var room = new GameObject("Building_" + StationOrder[index]).transform;
			room.SetParent(parent, false);
			Box(room, "Yard", o + new Vector3(0f, -0.35f, 0f), new Vector3(100f, 0.2f, 100f), _grass);
			Box(room, "Floor", o + new Vector3(0f, -0.1f, -1.5f), new Vector3(16f, 0.2f, 18f), _floor);
			Box(room, "BackWall", o + new Vector3(0f, 4f, 6.2f), new Vector3(16f, 8f, 0.3f), wall);
			Box(room, "LeftWall", o + new Vector3(-8f, 4f, -1.5f), new Vector3(0.3f, 8f, 18f), wall);
			Box(room, "RightWall", o + new Vector3(8f, 4f, -1.5f), new Vector3(0.3f, 8f, 18f), wall);
			Prim(PrimitiveType.Cube, "Plinth", room, o + new Vector3(0f, 0.3f, 6f), new Vector3(16f, 0.6f, 0.15f), _stone);
			for (var i = 0; i < 4; i++) Prim(PrimitiveType.Cube, "Beam" + i, room, o + new Vector3(-6f + i * 4f, 7.6f, -1.5f), new Vector3(0.3f, 0.3f, 18f), _woodDark);
			Prim(PrimitiveType.Cube, "BeamLong", room, o + new Vector3(0f, 7.5f, 5.8f), new Vector3(16f, 0.3f, 0.3f), _woodDark);
			Prim(PrimitiveType.Cube, "Rug", room, o + new Vector3(0f, 0.01f, -2.2f), new Vector3(10f, 0.02f, 3f), Mat("Rug", new Color(0.36f, 0.16f, 0.14f)));

			var lamp = new GameObject("Lamp");
			lamp.transform.SetParent(room, false);
			lamp.transform.position = o + new Vector3(0f, 4.4f, -0.5f);
			var l = lamp.AddComponent<Light>();
			l.type = LightType.Point;
			l.color = new Color(1f, 0.82f, 0.58f);
			l.intensity = 2.2f;
			l.range = 14f;
			l.shadows = LightShadows.None;
		}

		static Transform MakeStation(Transform parent, string id, Vector3 position)
		{
			var root = new GameObject("Station_" + id).transform;
			root.SetParent(parent, false);
			root.position = position;
			var station = root.gameObject.AddComponent<Station>();
			station.Id = id;
			root.gameObject.AddComponent<AssetIdTag>().Id = "buildings/station-" + id;
			var hit = new GameObject("Hit");
			hit.transform.SetParent(root, false);
			hit.transform.localPosition = new Vector3(0f, 1.6f, 0f);
			hit.AddComponent<BoxCollider>().size = new Vector3(4.6f, 3.4f, 2.6f);
			Text(root, "Sign", Loc.T("prof." + id), new Vector3(0f, 3.7f, 1.9f), Quaternion.identity, 0.12f, _gold.color);
			Prim(PrimitiveType.Cube, "SignBoard", root, new Vector3(0f, 3.7f, 2.05f), new Vector3(3.4f, 0.8f, 0.1f), _woodDark);
			return root;
		}

		static void Finish(Transform root) => root.GetComponent<Station>().Renderers = root.GetComponentsInChildren<MeshRenderer>();

		static void Table(Transform root, float width = 4.2f)
		{
			Box(root, "Top", new Vector3(0f, 0.8f, 0f), new Vector3(width, 0.14f, 2f), _wood);
			foreach (var x in new[] { -width * 0.5f + 0.2f, width * 0.5f - 0.2f })
				foreach (var z in new[] { -0.8f, 0.8f })
					Box(root, "Leg", new Vector3(x, 0.37f, z), new Vector3(0.16f, 0.74f, 0.16f), _woodDark);
		}

		static void BuildScribe(Transform root)
		{
			Table(root);
			Prim(PrimitiveType.Cube, "Parchment", root, new Vector3(-0.2f, 0.88f, -0.2f), new Vector3(1.4f, 0.02f, 1.0f), _parchment);
			for (var i = 0; i < 4; i++) Prim(PrimitiveType.Cube, "Line", root, new Vector3(-0.2f, 0.9f, -0.5f + i * 0.2f), new Vector3(1.0f - (i == 3 ? 0.4f : 0f), 0.01f, 0.025f), _ink);
			Prim(PrimitiveType.Cylinder, "Inkwell", root, new Vector3(1.0f, 0.93f, -0.4f), new Vector3(0.22f, 0.07f, 0.22f), _ink);
			Prim(PrimitiveType.Cylinder, "Quill", root, new Vector3(1.15f, 1.15f, -0.35f), new Vector3(0.02f, 0.28f, 0.02f), _white).transform.rotation = Quaternion.Euler(0f, 0f, -28f);
			Prim(PrimitiveType.Cylinder, "WaxPad", root, new Vector3(1.3f, 0.9f, 0.3f), new Vector3(0.3f, 0.03f, 0.3f), _wax);
			Prim(PrimitiveType.Cylinder, "StampHandle", root, new Vector3(1.7f, 1.0f, 0.1f), new Vector3(0.1f, 0.14f, 0.1f), _woodDark);
			Prim(PrimitiveType.Cylinder, "StampHead", root, new Vector3(1.7f, 0.9f, 0.1f), new Vector3(0.2f, 0.03f, 0.2f), _brass);
			// Shelf of records on the wall behind.
			Box(root, "Shelf", new Vector3(0f, 2.0f, 2.1f), new Vector3(3.8f, 0.08f, 0.5f), _woodDark);
			var colours = new[] { _red, _blue, _green, _parchment };
			for (var i = 0; i < 11; i++) Prim(PrimitiveType.Cube, "Book", root, new Vector3(-1.6f + i * 0.32f, 2.3f, 2.0f), new Vector3(0.22f, 0.5f + (i % 3) * 0.05f, 0.34f), colours[i % 4]);
		}

		static Renderer BuildForge(Transform root)
		{
			Prim(PrimitiveType.Cylinder, "Stump", root, new Vector3(0f, 0.45f, 0f), new Vector3(1.3f, 0.45f, 1.3f), _wood);
			Box(root, "AnvilBase", new Vector3(0f, 1.0f, 0f), new Vector3(0.9f, 0.2f, 0.6f), _ironDark);
			Box(root, "AnvilWaist", new Vector3(0f, 1.2f, 0f), new Vector3(0.5f, 0.25f, 0.4f), _ironDark);
			Box(root, "AnvilTop", new Vector3(0f, 1.42f, 0f), new Vector3(1.5f, 0.22f, 0.55f), _iron);
			Box(root, "AnvilHorn", new Vector3(0.95f, 1.42f, 0f), new Vector3(0.5f, 0.14f, 0.3f), _iron);
			var billet = Box(root, "Billet", new Vector3(-0.2f, 1.6f, 0f), new Vector3(0.7f, 0.1f, 0.2f), _billet);
			Prim(PrimitiveType.Cylinder, "HammerHandle", root, new Vector3(-1.4f, 0.95f, -0.5f), new Vector3(0.07f, 0.45f, 0.07f), _woodDark).transform.rotation = Quaternion.Euler(0f, 0f, 70f);
			Box(root, "HammerHead", new Vector3(-1.0f, 1.2f, -0.5f), new Vector3(0.22f, 0.22f, 0.4f), _ironDark);
			// A furnace with a glow.
			Box(root, "Furnace", new Vector3(1.9f, 0.7f, 0.9f), new Vector3(1.3f, 1.4f, 1.0f), _stone);
			Box(root, "Mouth", new Vector3(1.9f, 0.75f, 0.38f), new Vector3(0.7f, 0.5f, 0.1f), _fire);
			Prim(PrimitiveType.Cube, "Chimney", root, new Vector3(1.9f, 2.0f, 1.2f), new Vector3(0.5f, 1.4f, 0.5f), _stone);
			var glow = new GameObject("FurnaceLight");
			glow.transform.SetParent(root, false);
			glow.transform.localPosition = new Vector3(1.9f, 0.9f, 0f);
			var l = glow.AddComponent<Light>();
			l.type = LightType.Point;
			l.color = new Color(1f, 0.55f, 0.25f);
			l.intensity = 1.6f;
			l.range = 4f;
			l.shadows = LightShadows.None;
			Prim(PrimitiveType.Cylinder, "Bucket", root, new Vector3(-1.7f, 0.3f, 0.4f), new Vector3(0.5f, 0.3f, 0.5f), _blue);
			return billet.GetComponent<Renderer>();
		}

		static void BuildWorkshop(Transform root)
		{
			Table(root);
			for (var i = 0; i < 3; i++)
			{
				Prim(PrimitiveType.Cube, "Slot" + (i + 1), root, new Vector3(-1.1f + i * 0.65f, 0.89f, 0.45f), new Vector3(0.5f, 0.03f, 0.5f), _woodDark);
				Text(root, "SlotNo" + (i + 1), (i + 1).ToString(), new Vector3(-1.1f + i * 0.65f, 0.915f, 0.45f), Quaternion.Euler(90f, 0f, 0f), 0.05f, _parchment.color);
			}
			Prim(PrimitiveType.Cylinder, "Gear", root, new Vector3(0.7f, 0.95f, -0.2f), new Vector3(0.6f, 0.05f, 0.6f), _iron);
			for (var i = 0; i < 6; i++)
			{
				var tooth = Prim(PrimitiveType.Cube, "Tooth", root, new Vector3(0.7f, 0.95f, -0.2f), new Vector3(0.14f, 0.07f, 0.8f), _iron);
				tooth.transform.localRotation = Quaternion.Euler(0f, i * 30f, 0f);
			}
			Prim(PrimitiveType.Cylinder, "Axle", root, new Vector3(1.5f, 0.95f, -0.3f), new Vector3(0.08f, 0.5f, 0.08f), _ironDark).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
			Box(root, "Lever", new Vector3(-0.9f, 0.95f, -0.3f), new Vector3(0.8f, 0.05f, 0.1f), _woodDark);
			Prim(PrimitiveType.Sphere, "Spring", root, new Vector3(-0.2f, 0.95f, -0.45f), new Vector3(0.2f, 0.2f, 0.2f), _brass);
			Prim(PrimitiveType.Cylinder, "Pin", root, new Vector3(0.1f, 0.93f, -0.45f), new Vector3(0.06f, 0.1f, 0.06f), _ironDark);
			Box(root, "Gate", new Vector3(0f, 2.1f, 2.0f), new Vector3(2.6f, 1.6f, 0.2f), _woodDark);
			Box(root, "GateBar", new Vector3(0f, 2.1f, 1.88f), new Vector3(2.8f, 0.14f, 0.1f), _ironDark);
		}

		static void BuildCooking(Transform root)
		{
			Table(root);
			Prim(PrimitiveType.Cylinder, "Pot", root, new Vector3(-0.6f, 1.15f, 0f), new Vector3(0.9f, 0.3f, 0.9f), _ironDark);
			Prim(PrimitiveType.Cylinder, "PotRim", root, new Vector3(-0.6f, 1.45f, 0f), new Vector3(0.96f, 0.03f, 0.96f), _iron);
			Prim(PrimitiveType.Cylinder, "Soup", root, new Vector3(-0.6f, 1.42f, 0f), new Vector3(0.78f, 0.02f, 0.78f), _orange);
			Prim(PrimitiveType.Cylinder, "Spoon", root, new Vector3(-0.3f, 1.6f, 0.1f), new Vector3(0.04f, 0.4f, 0.04f), _woodDark).transform.rotation = Quaternion.Euler(0f, 0f, 25f);
			Prim(PrimitiveType.Cylinder, "Bowl", root, new Vector3(0.7f, 0.92f, -0.3f), new Vector3(0.5f, 0.06f, 0.5f), _clay);
			Prim(PrimitiveType.Cube, "Carrot", root, new Vector3(1.4f, 0.9f, 0.3f), new Vector3(0.1f, 0.05f, 0.4f), _orange);
			Prim(PrimitiveType.Sphere, "Onion", root, new Vector3(1.0f, 0.97f, 0.5f), new Vector3(0.25f, 0.22f, 0.25f), _tan);
			Prim(PrimitiveType.Sphere, "Apple", root, new Vector3(1.6f, 0.96f, -0.3f), new Vector3(0.22f, 0.22f, 0.22f), _red);
			Box(root, "Hood", new Vector3(-0.6f, 2.6f, 1.6f), new Vector3(1.6f, 0.9f, 0.5f), _stone);
		}

		static void BuildTailor(Transform root)
		{
			Table(root);
			Prim(PrimitiveType.Cube, "Fabric", root, new Vector3(-0.3f, 0.9f, 0f), new Vector3(1.6f, 0.04f, 1.1f), _cloth);
			Prim(PrimitiveType.Cube, "FabricB", root, new Vector3(0.9f, 0.93f, 0.4f), new Vector3(0.7f, 0.05f, 0.5f), _parchment);
			Prim(PrimitiveType.Cube, "BladeA", root, new Vector3(1.3f, 0.93f, -0.4f), new Vector3(0.06f, 0.02f, 0.5f), _iron).transform.rotation = Quaternion.Euler(0f, 20f, 0f);
			Prim(PrimitiveType.Cube, "BladeB", root, new Vector3(1.3f, 0.95f, -0.4f), new Vector3(0.06f, 0.02f, 0.5f), _iron).transform.rotation = Quaternion.Euler(0f, -20f, 0f);
			Prim(PrimitiveType.Cylinder, "Spool", root, new Vector3(1.7f, 0.98f, 0.2f), new Vector3(0.2f, 0.12f, 0.2f), _red);
			Prim(PrimitiveType.Cube, "Dummy", root, new Vector3(-1.6f, 1.5f, 1.4f), new Vector3(0.7f, 1.4f, 0.4f), _leather);
			Prim(PrimitiveType.Cylinder, "DummyPole", root, new Vector3(-1.6f, 0.6f, 1.4f), new Vector3(0.1f, 0.6f, 0.1f), _woodDark);
		}

		static void BuildApothecary(Transform root)
		{
			Table(root);
			Prim(PrimitiveType.Cylinder, "Mortar", root, new Vector3(-0.8f, 1.0f, 0f), new Vector3(0.55f, 0.18f, 0.55f), _stone);
			Prim(PrimitiveType.Cylinder, "Pestle", root, new Vector3(-0.6f, 1.35f, 0f), new Vector3(0.1f, 0.3f, 0.1f), _white).transform.rotation = Quaternion.Euler(0f, 0f, -20f);
			var caps = new[] { _red, _blue, _gold };
			for (var i = 0; i < 3; i++)
			{
				Prim(PrimitiveType.Cylinder, "Jar", root, new Vector3(0.3f + i * 0.5f, 1.02f, 0.5f), new Vector3(0.36f, 0.2f, 0.36f), _clay);
				Prim(PrimitiveType.Cylinder, "Cap", root, new Vector3(0.3f + i * 0.5f, 1.25f, 0.5f), new Vector3(0.3f, 0.04f, 0.3f), caps[i]);
			}
			for (var i = 0; i < 3; i++) Prim(PrimitiveType.Cube, "Herb", root, new Vector3(0.5f + i * 0.4f, 0.92f, -0.5f), new Vector3(0.2f, 0.06f, 0.4f), _herb).transform.rotation = Quaternion.Euler(0f, 15f * i, 0f);
			Box(root, "HerbShelf", new Vector3(0f, 2.0f, 2.1f), new Vector3(3.6f, 0.08f, 0.5f), _woodDark);
			for (var i = 0; i < 6; i++) Prim(PrimitiveType.Cylinder, "Bottle", root, new Vector3(-1.4f + i * 0.56f, 2.25f, 2.0f), new Vector3(0.28f, 0.2f, 0.28f), i % 2 == 0 ? _green : _clay);
		}

		// ---- the stable and the course ----

		static void BuildStable(Transform stable)
		{
			var o = StablePos;
			Box(stable, "Floor", o + new Vector3(8f, -0.2f, 0f), new Vector3(60f, 0.4f, 24f), _grass);
			Box(stable, "StableFloor", o + new Vector3(0f, -0.15f, 1.5f), new Vector3(14f, 0.3f, 12f), Mat("StableFloor", new Color(0.8f, 0.7f, 0.46f)));
			Box(stable, "BackWall", o + new Vector3(0f, 2.25f, 7.7f), new Vector3(14.4f, 4.5f, 0.4f), _wood);
			Box(stable, "LeftWall", o + new Vector3(-7f, 2.25f, 1.5f), new Vector3(0.4f, 4.5f, 12f), _wood);
			for (var i = 0; i < 4; i++) Prim(PrimitiveType.Cube, "Plank" + i, stable, o + new Vector3(0f, 0.9f + i * 1.1f, 7.48f), new Vector3(14.2f, 0.06f, 0.06f), _woodDark);
			Box(stable, "Roof", o + new Vector3(-1.5f, 4.6f, 1.5f), new Vector3(11.6f, 0.3f, 12.4f), _woodDark);
			Prim(PrimitiveType.Cube, "PostA", stable, o + new Vector3(-6.8f, 2.25f, -4.4f), new Vector3(0.4f, 4.5f, 0.4f), _woodDark);
			Prim(PrimitiveType.Cube, "PostB", stable, o + new Vector3(4.2f, 2.25f, 7.3f), new Vector3(0.4f, 4.5f, 0.4f), _woodDark);

			var trough = new GameObject("HayTrough").transform;
			trough.SetParent(stable, false);
			trough.position = o + new Vector3(3.2f, 0f, 6.4f);
			trough.gameObject.AddComponent<AssetIdTag>().Id = "props/hay-trough";
			Prim(PrimitiveType.Cube, "Box", trough, new Vector3(0f, 0.35f, 0f), new Vector3(2.6f, 0.7f, 0.9f), _wood);
			Prim(PrimitiveType.Cube, "Hay", trough, new Vector3(0f, 0.75f, 0f), new Vector3(2.3f, 0.25f, 0.65f), _hay);

			var post = new GameObject("GroomingPost").transform;
			post.SetParent(stable, false);
			post.position = o + new Vector3(-3.5f, 0f, 5.4f);
			post.gameObject.AddComponent<AssetIdTag>().Id = "props/grooming-post";
			Prim(PrimitiveType.Cube, "Post", post, new Vector3(0f, 1f, 0f), new Vector3(0.22f, 2f, 0.22f), _woodDark);
			Prim(PrimitiveType.Cube, "Bar", post, new Vector3(0f, 1.8f, 0f), new Vector3(1.2f, 0.12f, 0.12f), _woodDark);
			Prim(PrimitiveType.Cube, "Brush", post, new Vector3(0.3f, 1.2f, 0.16f), new Vector3(0.5f, 0.12f, 0.16f), _leather);
			Prim(PrimitiveType.Cylinder, "Bucket", post, new Vector3(0.9f, 0.25f, 0.2f), new Vector3(0.55f, 0.25f, 0.55f), _blue);

			var bed = new GameObject("Bedding").transform;
			bed.SetParent(stable, false);
			bed.position = o + new Vector3(-4.8f, 0f, 1.0f);
			bed.gameObject.AddComponent<AssetIdTag>().Id = "props/bedding";
			Prim(PrimitiveType.Cylinder, "Base", bed, new Vector3(0f, 0.08f, 0f), new Vector3(3.6f, 0.08f, 2.8f), _straw);
			Prim(PrimitiveType.Sphere, "MoundA", bed, new Vector3(-0.5f, 0.2f, 0.1f), new Vector3(1.6f, 0.5f, 1.2f), _hay);
			Prim(PrimitiveType.Sphere, "MoundB", bed, new Vector3(0.7f, 0.17f, -0.3f), new Vector3(1.3f, 0.4f, 1.0f), _straw);

			Prim(PrimitiveType.Cube, "Bale1", stable, o + new Vector3(-6.0f, 0.4f, 6.4f), new Vector3(1.4f, 0.8f, 0.8f), _hay);
			Prim(PrimitiveType.Cube, "Bale2", stable, o + new Vector3(-6.0f, 0.4f, 5.4f), new Vector3(1.4f, 0.8f, 0.8f), _hay);
			Prim(PrimitiveType.Cube, "Bale3", stable, o + new Vector3(-6.0f, 1.2f, 5.9f), new Vector3(1.4f, 0.8f, 0.8f), _straw);

			var lantern = new GameObject("Lantern").transform;
			lantern.SetParent(stable, false);
			lantern.position = o + new Vector3(0f, 3.6f, 3f);
			Prim(PrimitiveType.Cube, "Body", lantern, Vector3.zero, new Vector3(0.3f, 0.4f, 0.3f), _gold);
			var light = lantern.gameObject.AddComponent<Light>();
			light.type = LightType.Point;
			light.range = 16f;
			light.intensity = 1.8f;
			light.color = new Color(1f, 0.84f, 0.58f);
			light.shadows = LightShadows.None;
		}

		static YardTrack BuildTrack()
		{
			var yard = new GameObject("Yard").transform;
			yard.gameObject.AddComponent<AssetIdTag>().Id = "buildings/training-yard";
			Box(yard, "Dirt", StablePos + new Vector3(62f, 0.01f, 0f), new Vector3(124f, 0.04f, 7f), _dirt);
			foreach (var z in new[] { 3.8f })
			{
				Prim(PrimitiveType.Cube, "RailLow", yard, StablePos + new Vector3(65f, 0.55f, z), new Vector3(120f, 0.12f, 0.12f), _wood);
				Prim(PrimitiveType.Cube, "RailHigh", yard, StablePos + new Vector3(65f, 1.05f, z), new Vector3(120f, 0.12f, 0.12f), _wood);
				for (var x = 8f; x <= 124f; x += 8f) Prim(PrimitiveType.Cube, "Post", yard, StablePos + new Vector3(x, 0.6f, z), new Vector3(0.22f, 1.3f, 0.22f), _woodDark);
			}

			var track = new GameObject("Track").AddComponent<YardTrack>();
			track.transform.SetParent(yard, false);
			track.transform.SetPositionAndRotation(StablePos, Quaternion.Euler(0f, 90f, 0f));
			track.Obstacles = new Transform[ObstacleRunLogic.ObstacleCount];
			for (var i = 0; i < ObstacleRunLogic.ObstacleCount; i++)
			{
				var jump = new GameObject("Jump" + (i + 1)).transform;
				jump.SetParent(track.transform, false);
				jump.gameObject.AddComponent<AssetIdTag>().Id = "props/obstacle-jump";
				Prim(PrimitiveType.Cube, "PostL", jump, new Vector3(-1.9f, 0.45f, 0f), new Vector3(0.18f, 0.9f, 0.18f), _woodDark);
				Prim(PrimitiveType.Cube, "PostR", jump, new Vector3(1.9f, 0.45f, 0f), new Vector3(0.18f, 0.9f, 0.18f), _woodDark);
				Prim(PrimitiveType.Cube, "Bar", jump, new Vector3(0f, 0.5f, 0f), new Vector3(3.8f, 0.14f, 0.14f), i % 2 == 0 ? _red : _white);
				Prim(PrimitiveType.Cube, "BarStripe", jump, new Vector3(0f, 0.5f, 0f), new Vector3(1.2f, 0.16f, 0.16f), i % 2 == 0 ? _white : _red);
				track.Obstacles[i] = jump;
			}
			track.Marks = new Transform[12];
			for (var i = 0; i < 12; i++)
			{
				var mark = Prim(PrimitiveType.Cube, "Mark" + i, track.transform, new Vector3(0f, 0.04f, i * 6f), new Vector3(5f, 0.02f, 0.3f), _white);
				track.Marks[i] = mark.transform;
			}
			return track;
		}

		static HorseView BuildHorse()
		{
			var root = new GameObject("Horse");
			root.AddComponent<AssetIdTag>().Id = "horses/basic";
			root.transform.SetPositionAndRotation(StablePos, Quaternion.Euler(0f, 90f, 0f));
			var view = root.AddComponent<HorseView>();

			var visual = new GameObject("Visual").transform;
			visual.SetParent(root.transform, false);
			view.Visual = visual;

			view.Body = Prim(PrimitiveType.Cube, "Body", visual, new Vector3(0f, 1.25f, 0f), new Vector3(0.75f, 0.8f, 1.9f), _coat).transform;
			Prim(PrimitiveType.Sphere, "Chest", visual, new Vector3(0f, 1.25f, 0.9f), new Vector3(0.78f, 0.82f, 0.6f), _coat);
			Prim(PrimitiveType.Sphere, "Rump", visual, new Vector3(0f, 1.27f, -0.9f), new Vector3(0.78f, 0.82f, 0.6f), _coat);

			var neck = new GameObject("Neck").transform;
			neck.SetParent(visual, false);
			neck.localPosition = new Vector3(0f, 1.45f, 0.85f);
			view.Neck = neck;
			Prim(PrimitiveType.Cube, "NeckMesh", neck, new Vector3(0f, 0.33f, 0.23f), new Vector3(0.32f, 0.85f, 0.36f), _coat).transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
			Prim(PrimitiveType.Cube, "Mane", neck, new Vector3(0f, 0.36f, 0.04f), new Vector3(0.1f, 0.85f, 0.14f), _mane).transform.localRotation = Quaternion.Euler(35f, 0f, 0f);

			var head = new GameObject("Head").transform;
			head.SetParent(neck, false);
			head.localPosition = new Vector3(0f, 0.68f, 0.47f);
			head.localRotation = Quaternion.Euler(40f, 0f, 0f);
			view.Head = head;
			Prim(PrimitiveType.Cube, "HeadMesh", head, new Vector3(0f, 0f, 0.22f), new Vector3(0.3f, 0.3f, 0.64f), _coat);
			Prim(PrimitiveType.Cube, "Muzzle", head, new Vector3(0f, -0.03f, 0.52f), new Vector3(0.26f, 0.24f, 0.18f), _mane);
			Prim(PrimitiveType.Cube, "Blaze", head, new Vector3(0f, 0.151f, 0.2f), new Vector3(0.08f, 0.01f, 0.5f), _white);
			Prim(PrimitiveType.Sphere, "EyeL", head, new Vector3(-0.155f, 0.06f, 0.1f), new Vector3(0.07f, 0.07f, 0.07f), _dark);
			Prim(PrimitiveType.Sphere, "EyeR", head, new Vector3(0.155f, 0.06f, 0.1f), new Vector3(0.07f, 0.07f, 0.07f), _dark);
			Prim(PrimitiveType.Cube, "EarL", head, new Vector3(-0.09f, 0.2f, -0.06f), new Vector3(0.08f, 0.2f, 0.06f), _coat);
			Prim(PrimitiveType.Cube, "EarR", head, new Vector3(0.09f, 0.2f, -0.06f), new Vector3(0.08f, 0.2f, 0.06f), _coat);

			var legs = new Transform[4];
			var hips = new[] { new Vector3(-0.27f, 0.9f, 0.68f), new Vector3(0.27f, 0.9f, 0.68f), new Vector3(-0.27f, 0.9f, -0.68f), new Vector3(0.27f, 0.9f, -0.68f) };
			for (var i = 0; i < 4; i++)
			{
				var leg = new GameObject("Leg" + i).transform;
				leg.SetParent(visual, false);
				leg.localPosition = hips[i];
				Prim(PrimitiveType.Cube, "Upper", leg, new Vector3(0f, -0.4f, 0f), new Vector3(0.2f, 0.8f, 0.2f), _coat);
				Prim(PrimitiveType.Cube, "Hoof", leg, new Vector3(0f, -0.86f, 0.02f), new Vector3(0.22f, 0.12f, 0.26f), _dark);
				legs[i] = leg;
			}
			view.Legs = legs;

			var tail = new GameObject("Tail").transform;
			tail.SetParent(visual, false);
			tail.localPosition = new Vector3(0f, 1.5f, -1.12f);
			Prim(PrimitiveType.Cube, "TailMesh", tail, new Vector3(0f, -0.3f, -0.05f), new Vector3(0.14f, 0.75f, 0.14f), _mane);
			view.Tail = tail;

			var zzz = Text(visual, "Zzz", "Zzz", new Vector3(0f, 2.3f, 1.1f), Quaternion.identity, 0.12f, new Color(0.9f, 0.95f, 1f));
			view.SleepMark = zzz.gameObject;
			return view;
		}

		// ---- helpers ----

		static CameraPose Pose(Transform anchors, string key, Vector3 position, Vector3 look, float fov)
		{
			var anchor = new GameObject(key).transform;
			anchor.SetParent(anchors, false);
			anchor.position = position;
			var target = new GameObject(key + ".look").transform;
			target.SetParent(anchors, false);
			target.position = look;
			return new CameraPose { Key = key, Anchor = anchor, Look = target, Fov = fov };
		}

		static TextMesh Text(Transform parent, string name, string text, Vector3 localPosition, Quaternion rotation, float size, Color colour) => GrayboxBuilder.Text(parent, name, text, localPosition, rotation, size, colour, MaterialDir);

		static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material) => GrayboxBuilder.Box(parent, name, position, scale, material);

		static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider = false) => GrayboxBuilder.Prim(type, name, parent, position, scale, material, keepCollider);

		static Material Mat(string name, Color colour) => GrayboxBuilder.Mat(name, colour, MaterialDir);
	}
}
