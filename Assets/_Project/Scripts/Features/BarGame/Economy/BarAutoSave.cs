using UnityEngine;

namespace Startup.Features.BarGame
{
    // Saves after every change, and again when the app pauses or quits (mobile may kill the app).
    public class BarAutoSave : MonoBehaviour
    {
        private EconomyManager _economy;
        private BarUpgrades _upgrades;
        private BarSaveStore _store;

        public void Setup(EconomyManager economy, BarUpgrades upgrades, BarSaveStore store)
        {
            _economy = economy;
            _upgrades = upgrades;
            _store = store;
            _economy.Changed += Save;
            _upgrades.Changed += Save;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        private void OnDestroy()
        {
            if (_economy == null)
            {
                return;
            }

            _economy.Changed -= Save;
            _upgrades.Changed -= Save;
            Save();
        }

        private void Save()
        {
            _store.Save(_economy, _upgrades);
        }
    }
}
