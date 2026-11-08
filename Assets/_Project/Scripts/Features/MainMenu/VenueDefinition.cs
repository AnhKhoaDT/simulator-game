using Startup.Core.Localization;
using Startup.Core.SceneFlow;
using UnityEngine;

namespace Startup.Features.MainMenu
{
    // Data-driven venue entry for the main menu. Add a new venue = create a new asset, no code change.
    [CreateAssetMenu(fileName = "Venue", menuName = "Startup/Venue Definition")]
    public class VenueDefinition : ScriptableObject
    {
        [SerializeField] private string _nameKey = LocKeys.MenuVenueBar;
        [SerializeField] private string _sceneName = SceneNames.BarGame;
        [SerializeField] private bool _unlocked = true;

        public string NameKey => _nameKey;
        public string SceneName => _sceneName;
        public bool Unlocked => _unlocked;
    }
}
