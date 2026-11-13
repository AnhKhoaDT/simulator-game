using System;
using Startup.Core.Localization;
using UnityEngine.UIElements;

namespace Startup.Core.UI
{
    // Base for full-screen dimmed popups built in code: a centered card with a title and a close button.
    // Styles come from UI/Shared/Panels.uss, which every screen's USS imports.
    public abstract class OverlayPanel : IDisposable
    {
        public const string OverlayClass = "overlay";

        protected readonly LocalizedBindings Bindings = new LocalizedBindings();

        private readonly VisualElement _overlay;
        private readonly Button _close;

        protected OverlayPanel(VisualElement root, string titleKey, string extraPanelClass = null)
        {
            _overlay = new VisualElement();
            _overlay.AddToClassList(OverlayClass);
            _overlay.style.display = DisplayStyle.None;

            Card = new VisualElement();
            Card.AddToClassList("panel");
            if (!string.IsNullOrEmpty(extraPanelClass))
            {
                Card.AddToClassList(extraPanelClass);
            }

            var title = new Label();
            title.AddToClassList("panel-title");
            Bindings.Bind(title, titleKey);
            Card.Add(title);

            Body = new VisualElement();
            Body.AddToClassList("panel-body");
            Card.Add(Body);

            _close = AddButton(Card, LocKeys.CommonClose, Close);
            _close.AddToClassList("panel-btn--secondary");

            _overlay.Add(Card);
            root.Add(_overlay);
        }

        public bool IsOpen { get; private set; }

        public event Action Closed;

        protected VisualElement Card { get; }

        protected VisualElement Body { get; }

        public virtual void Open()
        {
            IsOpen = true;
            _overlay.style.display = DisplayStyle.Flex;
            _overlay.BringToFront();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            _overlay.style.display = DisplayStyle.None;
            Closed?.Invoke();
        }

        public virtual void Dispose()
        {
            Bindings.Dispose();
        }

        protected Button AddButton(VisualElement parent, string key, Action onClick)
        {
            var button = new Button(onClick);
            button.AddToClassList("panel-btn");
            Bindings.Bind(button, key);
            parent.Add(button);
            return button;
        }

        protected Label AddLabel(VisualElement parent, string key, string className)
        {
            var label = new Label();
            label.AddToClassList(className);
            Bindings.Bind(label, key);
            parent.Add(label);
            return label;
        }
    }
}
