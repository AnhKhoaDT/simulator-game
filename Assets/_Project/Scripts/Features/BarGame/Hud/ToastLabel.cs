using System;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // A label that shows localized text for a moment, then hides itself.
    internal sealed class ToastLabel
    {
        private readonly Label _label;
        private readonly Action<object[]> _setArgs;
        private IVisualElementScheduledItem _hide;

        public ToastLabel(Label label, Action<object[]> setArgs)
        {
            _label = label;
            _setArgs = setArgs;
            Hide();
        }

        public void Show(object[] args, long milliseconds)
        {
            if (_label == null)
            {
                return;
            }

            _setArgs(args);
            _label.style.display = DisplayStyle.Flex;

            // Pause the previous timer so an older toast never hides a newer one early.
            _hide?.Pause();
            _hide = _label.schedule.Execute(Hide).StartingIn(milliseconds);
        }

        private void Hide()
        {
            if (_label != null)
            {
                _label.style.display = DisplayStyle.None;
            }
        }
    }
}
