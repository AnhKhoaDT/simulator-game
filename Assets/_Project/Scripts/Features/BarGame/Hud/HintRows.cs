namespace Startup.Features.BarGame
{
    // The key list shown by the H guide: what to press and the String Table key for what it does.
    internal static class HintRows
    {
        internal readonly struct Row
        {
            public readonly string Keys;
            public readonly string LabelKey;

            public Row(string keys, string labelKey)
            {
                Keys = keys;
                LabelKey = labelKey;
            }
        }

        public static readonly Row[] All =
        {
            new Row("WASD", "hint_move"),
            new Row("E", "hint_interact"),
            new Row("F", "hint_sit"),
            new Row("X", "hint_store"),
            new Row("R  Q", "hint_rotate"),
            new Row("I", "hint_inventory"),
            new Row("B", "hint_shop"),
            new Row("O", "hint_open"),
            new Row("V", "hint_view"),
            new Row("Tab", "hint_cursor"),
            new Row("Esc", "hint_pause")
        };
    }
}
