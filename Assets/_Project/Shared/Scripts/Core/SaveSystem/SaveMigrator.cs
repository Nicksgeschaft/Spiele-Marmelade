namespace GameJamUniverse.Core.SaveSystem
{
    /// <summary>
    /// Upgrades older save files in place. Add one branch per version bump - never delete old
    /// branches, since a player could be migrating from any past version.
    /// </summary>
    public static class SaveMigrator
    {
        public static SaveData Migrate(SaveData data)
        {
            if (data == null) return new SaveData();

            // Example for future versions:
            // if (data.version < 2) { ... upgrade fields ...; data.version = 2; }

            data.version = SaveData.CurrentVersion;
            return data;
        }
    }
}
