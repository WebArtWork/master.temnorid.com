using ITKamianets.Engine.Save;

namespace Temnorid.Master
{
	/// <summary>
	/// Reads and writes the <see cref="MasterSave"/> in the single save slot, with the previous good save kept as a backup. The write is atomic
	/// and a damaged file is set aside, never deleted (both done by the save package).
	/// </summary>
	public sealed class MasterSaveStore
	{
		const string Slot = "default";
		const string SettingsSlot = "settings";

		readonly SaveStore _store;

		public MasterSaveStore(string rootDirectory = null)
		{
			_store = new SaveStore("master", rootDirectory, keepBackup: true);
		}

		public string Path_ => _store.PathFor(Slot);
		public string BackupPath => _store.BackupPathFor(Slot);
		public bool Exists => _store.Exists(Slot);
		public bool BackupExists => _store.BackupExists(Slot);

		/// <summary>The saved game, or null; <paramref name="status"/> says why it is null (no save, damaged, a newer version).</summary>
		public MasterSave Load(out SaveStatus status)
		{
			var result = _store.Load<MasterSave>(Slot, MasterConfig.SaveVersion);
			status = result.Status;
			return result.Ok ? result.Value : null;
		}

		/// <summary>The last good save, kept when a newer one replaced it.</summary>
		public MasterSave LoadBackup(out SaveStatus status)
		{
			var result = _store.LoadBackup<MasterSave>(Slot, MasterConfig.SaveVersion);
			status = result.Status;
			return result.Ok ? result.Value : null;
		}

		public bool Save(MasterSave save) => _store.Save(Slot, save, MasterConfig.SaveVersion) == SaveStatus.Ok;

		/// <summary>Removes the save and its backup (a reset or a new game the player confirmed).</summary>
		public void Delete() => _store.Delete(Slot);

		public MasterSettings LoadSettings()
		{
			var result = _store.Load<MasterSettings>(SettingsSlot);
			return result.Ok && result.Value != null ? result.Value : new MasterSettings();
		}

		public void SaveSettings(MasterSettings settings) => _store.Save(SettingsSlot, settings);
	}
}
