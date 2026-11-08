using Startup.Core.Localization;
using UnityEngine.UIElements;

namespace Startup.Core.UI
{
    // "How to play": one scrollable page of short sections. Opened from Settings and on the first visit to the bar.
    public sealed class TutorialPanel : OverlayPanel
    {
        private static readonly string[,] Sections =
        {
            { LocKeys.TutControlsTitle, LocKeys.TutControlsBody },
            { LocKeys.TutShiftTitle, LocKeys.TutShiftBody },
            { LocKeys.TutServeTitle, LocKeys.TutServeBody },
            { LocKeys.TutCashierTitle, LocKeys.TutCashierBody },
            { LocKeys.TutShopTitle, LocKeys.TutShopBody },
            { LocKeys.TutViewTitle, LocKeys.TutViewBody }
        };

        public TutorialPanel(VisualElement root) : base(root, LocKeys.TutTitle, "panel--wide")
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("panel-scroll");
            Body.Add(scroll);

            for (var i = 0; i < Sections.GetLength(0); i++)
            {
                var section = new VisualElement();
                section.AddToClassList("panel-section");
                AddLabel(section, Sections[i, 0], "panel-heading");
                var text = AddLabel(section, Sections[i, 1], "panel-text");
                text.style.whiteSpace = WhiteSpace.Normal;
                scroll.Add(section);
            }
        }
    }
}
