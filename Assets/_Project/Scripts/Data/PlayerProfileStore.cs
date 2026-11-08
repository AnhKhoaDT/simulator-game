using Startup.Shared;

namespace Startup.Data
{
    // Persists the player profile through any IDataStore.
    public class PlayerProfileStore : IPlayerProfile
    {
        public const string OwnerNameKey = "profile_owner_name";
        public const string TutorialSeenKey = "profile_tutorial_seen";

        private readonly IDataStore _store;

        public PlayerProfileStore(IDataStore store)
        {
            _store = store;
        }

        public string LoadOwnerName()
        {
            return _store.Load(OwnerNameKey, string.Empty);
        }

        public void SaveOwnerName(string name)
        {
            _store.Save(OwnerNameKey, name ?? string.Empty);
        }

        public bool LoadTutorialSeen()
        {
            return _store.Load(TutorialSeenKey, false);
        }

        public void SaveTutorialSeen()
        {
            _store.Save(TutorialSeenKey, true);
        }
    }
}
