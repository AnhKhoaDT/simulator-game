using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Waypoint path on the floor plane: go straight, or detour around the first table in the way.
    internal static class PathPlanner
    {
        private const float Inset = 0.01f;
        private const float ParallelEpsilon = 0.00001f;

        // Appends waypoints to result (excluding 'from', including 'to').
        public static void Plan(Vector3 from, Vector3 to, IReadOnlyList<Rect> obstacles, List<Vector3> result)
        {
            var start = new Vector2(from.x, from.z);
            var end = new Vector2(to.x, to.z);
            var blocker = FindBlocker(start, end, obstacles);

            if (blocker >= 0 && TryDetour(start, end, obstacles, obstacles[blocker], out var corner))
            {
                result.Add(new Vector3(corner.x, 0f, corner.y));
            }

            result.Add(to);
        }

        private static bool TryDetour(Vector2 start, Vector2 end, IReadOnlyList<Rect> obstacles, Rect rect, out Vector2 best)
        {
            best = Vector2.zero;
            var bestLength = float.MaxValue;

            for (var i = 0; i < 4; i++)
            {
                var corner = Corner(rect, i);
                if (FindBlocker(start, corner, obstacles) >= 0 || FindBlocker(corner, end, obstacles) >= 0)
                {
                    continue;
                }

                var length = (corner - start).magnitude + (end - corner).magnitude;
                if (length < bestLength)
                {
                    bestLength = length;
                    best = corner;
                }
            }

            return bestLength < float.MaxValue;
        }

        private static int FindBlocker(Vector2 a, Vector2 b, IReadOnlyList<Rect> obstacles)
        {
            for (var i = 0; i < obstacles.Count; i++)
            {
                if (Intersects(a, b, obstacles[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static Vector2 Corner(Rect rect, int index)
        {
            switch (index)
            {
                case 0: return new Vector2(rect.xMin, rect.yMin);
                case 1: return new Vector2(rect.xMax, rect.yMin);
                case 2: return new Vector2(rect.xMax, rect.yMax);
                default: return new Vector2(rect.xMin, rect.yMax);
            }
        }

        // Slab test against the rect shrunk by Inset, so touching an edge or corner counts as clear.
        private static bool Intersects(Vector2 a, Vector2 b, Rect rect)
        {
            var min = new Vector2(rect.xMin + Inset, rect.yMin + Inset);
            var max = new Vector2(rect.xMax - Inset, rect.yMax - Inset);
            var direction = b - a;
            var t0 = 0f;
            var t1 = 1f;

            for (var axis = 0; axis < 2; axis++)
            {
                if (Mathf.Abs(direction[axis]) < ParallelEpsilon)
                {
                    if (a[axis] < min[axis] || a[axis] > max[axis])
                    {
                        return false;
                    }

                    continue;
                }

                var near = (min[axis] - a[axis]) / direction[axis];
                var far = (max[axis] - a[axis]) / direction[axis];
                if (near > far)
                {
                    (near, far) = (far, near);
                }

                t0 = Mathf.Max(t0, near);
                t1 = Mathf.Min(t1, far);
                if (t0 > t1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
