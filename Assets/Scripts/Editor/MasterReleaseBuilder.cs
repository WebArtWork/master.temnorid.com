using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Temnorid.Master.EditorTools
{
	/// <summary>
	/// Makes the Windows player (<c>Builds/Windows/TemnoridMaster</c>) and a portable zip next to it.
	/// The installer is made from that folder by <c>installer/Master.iss</c> (Inno Setup).
	/// Command line: <c>-executeMethod Temnorid.Master.EditorTools.MasterReleaseBuilder.BuildWindows</c>.
	/// </summary>
	public static class MasterReleaseBuilder
	{
		const string ScenePath = "Assets/Scenes/Master.unity";
		const string PlayerFolder = "Builds/Windows/TemnoridMaster";
		const string ExeName = "TemnoridMaster.exe";

		[MenuItem("Temnorid/Master/Build Windows Player")]
		public static void BuildWindows()
		{
			if (!File.Exists(ScenePath)) throw new InvalidOperationException("Build the scene first: Temnorid > Master > Build Scene.");

			if (Directory.Exists(PlayerFolder)) Directory.Delete(PlayerFolder, true);
			Directory.CreateDirectory(PlayerFolder);

			var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
			{
				scenes = new[] { ScenePath },
				locationPathName = PlayerFolder + "/" + ExeName,
				target = BuildTarget.StandaloneWindows64,
				options = BuildOptions.None,
			});
			if (report.summary.result != BuildResult.Succeeded)
				throw new Exception("Windows build failed: " + report.summary.result + ", " + report.summary.totalErrors + " errors.");

			foreach (var folder in Directory.GetDirectories(PlayerFolder))
			{
				var name = Path.GetFileName(folder);
				if (name.Contains("DoNotShip") || name.Contains("BackUpThisFolder")) Directory.Delete(folder, true);
			}

			var zip = "Builds/Windows/Temnorid-Master-v" + PlayerSettings.bundleVersion + "-win-x64.zip";
			if (File.Exists(zip)) File.Delete(zip);
			ZipFile.CreateFromDirectory(PlayerFolder, zip);

			Debug.Log("[Master] Windows build ready: " + PlayerFolder + "/" + ExeName + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB), zip: " + zip);
		}
	}
}
