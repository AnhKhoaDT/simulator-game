using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // A rectangular hole in a wall (door or window), measured along the wall and up from its base.
    public readonly struct WallOpening
    {
        public readonly float Start;
        public readonly float End;
        public readonly float Bottom;
        public readonly float Top;

        public WallOpening(float start, float end, float bottom, float top)
        {
            Start = start;
            End = end;
            Bottom = bottom;
            Top = top;
        }
    }

    // Builds a straight wall from boxes, leaving holes for doors and windows (solid columns, sills and headers).
    internal static class WallBuilder
    {
        private static readonly List<WallOpening> Sorted = new List<WallOpening>();

        // alongX: the wall runs along X at fixed Z = 'line'; otherwise along Z at fixed X = 'line'.
        public static void Build(string name, Transform parent, bool alongX, float line, float from, float to,
            float height, float thickness, Color color, params WallOpening[] openings)
        {
            Sorted.Clear();
            Sorted.AddRange(openings);
            Sorted.Sort((a, b) => a.Start.CompareTo(b.Start));

            var cursor = from;
            foreach (var opening in Sorted)
            {
                Piece(name, parent, alongX, line, cursor, opening.Start, 0f, height, thickness, color);
                Piece(name, parent, alongX, line, opening.Start, opening.End, 0f, opening.Bottom, thickness, color);
                Piece(name, parent, alongX, line, opening.Start, opening.End, opening.Top, height, thickness, color);
                cursor = opening.End;
            }

            Piece(name, parent, alongX, line, cursor, to, 0f, height, thickness, color);
        }

        private static void Piece(string name, Transform parent, bool alongX, float line,
            float from, float to, float bottom, float top, float thickness, Color color)
        {
            var length = to - from;
            var tall = top - bottom;
            if (length <= 0.001f || tall <= 0.001f)
            {
                return;
            }

            var middle = (from + to) * 0.5f;
            var y = (bottom + top) * 0.5f;
            var position = alongX ? new Vector3(middle, y, line) : new Vector3(line, y, middle);
            var size = alongX ? new Vector3(length, tall, thickness) : new Vector3(thickness, tall, length);
            PrimitiveFactory.Box(name, parent, position, size, color);
        }
    }
}
