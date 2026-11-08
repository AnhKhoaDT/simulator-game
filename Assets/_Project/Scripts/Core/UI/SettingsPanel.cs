using System;
using Startup.Core.Localization;
using Startup.Shared;
using UnityEngine.UIElements;

namespace Startup.Core.UI
{
    // Settings: owner name, language and a way into the tutorial. Used on the main menu and in the pause menu.
    public sealed class SettingsPanel : OverlayPanel
    {
        public const int MaxNameLength = 20;

        private readonly IPlayerProfile _profile;
        private readonly TutorialPanel _tutorial;
        private readonly TextField _nameField;

        public SettingsPanel(VisualElement root, IPlayerProfile profile, TutorialPanel tutorial)
            : base(root, LocKeys.SettingsTitle)
        {
            _profile = profile;
            _tutorial = tutorial;

            var nameRow = new VisualElement();
            nameRow.AddToClassList("panel-row");
            AddLabel(nameRow, LocKeys.SettingsOwnerName, "panel-field-label");
            _nameField = new TextField { maxLength = MaxNameLength };
            _nameField.AddToClassList("panel-field");
            _nameField.RegisterCallback<FocusOutEvent>(_ => SaveName());
            nameRow.Add(_nameField);
            Body.Add(nameRow);

            AddButton(Body, LocKeys.MenuBtnLanguage, LocalizationService.ToggleLanguage);
            var tutorialButton = AddButton(Body, LocKeys.SettingsTutorial, OpenTutorial);
            tutorialButton.AddToClassList("panel-btn--primary");

            Closed += SaveName;
        }

        // Fired after the owner name is saved, so the in-game name tag can update right away.
        public event Action<string> OwnerNameChanged;

        public override void Open()
        {
            _nameField.SetValueWithoutNotify(_profile.LoadOwnerName());
            base.Open();
        }

        private void SaveName()
        {
            var name = (_nameField.value ?? string.Empty).Trim();
            if (name == _profile.LoadOwnerName())
            {
                return;
            }

            _profile.SaveOwnerName(name);
            OwnerNameChanged?.Invoke(name);
        }

        private void OpenTutorial()
        {
            _tutorial.Open();
        }
    }
}
