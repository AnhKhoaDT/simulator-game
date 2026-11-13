using Startup.Core.Localization;
using Startup.Core.UI;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // The card for one waiter: rename, send to rest on a free sofa or back to work, or dismiss.
    public sealed class StaffPanel : OverlayPanel
    {
        public const int MaxNameLength = 20;

        private readonly BarUpgrades _upgrades;
        private readonly TextField _nameField;
        private readonly Label _working;
        private readonly Label _resting;
        private readonly Label _restHint;
        private readonly Button _restButton;
        private readonly Button _workButton;

        private WaiterRecord _record;

        public StaffPanel(VisualElement root, BarUpgrades upgrades) : base(root, LocKeys.StaffTitle)
        {
            _upgrades = upgrades;

            var nameRow = new VisualElement();
            nameRow.AddToClassList("panel-row");
            AddLabel(nameRow, LocKeys.StaffName, "panel-field-label");
            _nameField = new TextField { maxLength = MaxNameLength };
            _nameField.AddToClassList("panel-field");
            _nameField.RegisterCallback<FocusOutEvent>(_ => SaveName());
            nameRow.Add(_nameField);
            Body.Add(nameRow);

            _working = AddLabel(Body, LocKeys.StaffStatusWorking, "staff-status staff-status--working");
            _resting = AddLabel(Body, LocKeys.StaffStatusResting, "staff-status staff-status--resting");
            _restHint = AddLabel(Body, LocKeys.StaffRestHint, "staff-hint");
            _restButton = AddButton(Body, LocKeys.StaffBtnRest, SendToRest);
            _restButton.AddToClassList("panel-btn--primary");
            _workButton = AddButton(Body, LocKeys.StaffBtnWork, BackToWork);
            _workButton.AddToClassList("panel-btn--primary");
            var dismiss = AddButton(Body, LocKeys.StaffBtnDismiss, Dismiss);
            dismiss.AddToClassList("panel-btn--danger");

            Closed += SaveName;
        }

        public void Open(WaiterRecord record)
        {
            _record = record;
            _nameField.SetValueWithoutNotify(record.Name);
            Refresh();
            Open();
        }

        private void SaveName()
        {
            if (_record == null)
            {
                return;
            }

            var name = (_nameField.value ?? string.Empty).Trim();
            if (name != _record.Name)
            {
                _record.Name = name;
                _upgrades.NotifyWaiterEdited();
            }
        }

        private void SendToRest()
        {
            _record.Resting = true;
            _upgrades.NotifyWaiterEdited();
            Refresh();
        }

        private void BackToWork()
        {
            _record.Resting = false;
            _upgrades.NotifyWaiterEdited();
            Refresh();
        }

        private void Dismiss()
        {
            var record = _record;
            _record = null;
            _upgrades.Dismiss(record);
            Close();
        }

        private void Refresh()
        {
            var resting = _record != null && _record.Resting;
            Show(_working, !resting);
            Show(_resting, resting);
            Show(_restHint, !resting);
            Show(_restButton, !resting);
            Show(_workButton, resting);
        }

        private static void Show(VisualElement element, bool shown)
        {
            element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
