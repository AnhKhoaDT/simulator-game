using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace Startup.Core.Localization
{
    // Keeps a UI Toolkit text element in sync with a String Table key; Dispose unsubscribes all.
    public sealed class LocalizedBindings : IDisposable
    {
        private readonly List<Action> _unbinders = new List<Action>();

        public void Bind(TextElement element, string key)
        {
            if (element == null)
            {
                return;
            }

            var localized = new LocalizedString(LocKeys.Table, key);
            LocalizedString.ChangeHandler handler = value => element.text = value;
            // Subscribing triggers an immediate refresh, and again on every locale change.
            localized.StringChanged += handler;
            _unbinders.Add(() => localized.StringChanged -= handler);
        }

        // For Smart String keys with placeholders. Returns a setter that re-renders with new arguments.
        public Action<object[]> BindWithArgs(TextElement element, string key, object[] initialArgs)
        {
            if (element == null)
            {
                return _ => { };
            }

            var localized = new LocalizedString(LocKeys.Table, key) { Arguments = initialArgs };
            LocalizedString.ChangeHandler handler = value => element.text = value;
            localized.StringChanged += handler;
            _unbinders.Add(() => localized.StringChanged -= handler);

            return args =>
            {
                localized.Arguments = args;
                localized.RefreshString();
            };
        }

        public void Dispose()
        {
            foreach (var unbind in _unbinders)
            {
                unbind();
            }

            _unbinders.Clear();
        }
    }
}
