namespace Startup.Shared
{
    // Player-facing profile (owner name, tutorial seen) so Core UI can read and save it without the Data layer.
    public interface IPlayerProfile
    {
        // Empty string when the player has not chosen a name yet.
        string LoadOwnerName();
        void SaveOwnerName(string name);
        bool LoadTutorialSeen();
        void SaveTutorialSeen();
    }
}
