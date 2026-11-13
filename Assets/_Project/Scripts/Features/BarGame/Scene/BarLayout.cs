using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // All fixed positions of the bar and its street, derived from the room size, plus the walkable-area rule.
    // Room is centered on the origin; the street runs along the front (-Z) side.
    public sealed class BarLayout
    {
        public const float WallThickness = 0.2f;
        public const float WallHeight = 3.2f;
        public const float DoorHalfWidth = 0.8f;
        public const float DoorHeight = 2.2f;
        public const float SidewalkWidth = 3f;
        public const float RoadWidth = 6f;
        public const float StreetHalfLength = 30f;

        private const float DoorOffsetRatio = 0.375f;
        private const float DoorStepDistance = 1f;
        private const float CounterWallGap = 1.2f;
        private const float CounterFrontOffset = 1.8f;
        private const float CashierWallGap = 0.5f;
        private const float QueueGap = 0.5f;
        private const float CornerInset = 1.2f;
        private const float OwnerStartInset = 0.8f;
        private const float OwnerStartOffsetX = 3f;
        private const float Epsilon = 0.01f;

        public BarLayout(Vector2 roomSize)
        {
            HalfX = roomSize.x * 0.5f;
            HalfZ = roomSize.y * 0.5f;
            FrontZ = -HalfZ;
            DoorX = -roomSize.x * DoorOffsetRatio;

            DoorInside = new Vector3(DoorX, 0f, FrontZ + DoorStepDistance);
            DoorOutside = new Vector3(DoorX, 0f, FrontZ - DoorStepDistance);
            SidewalkZ = FrontZ - SidewalkWidth * 0.5f;
            RoadZ = FrontZ - SidewalkWidth - RoadWidth * 0.5f;
            OppositeSidewalkZ = FrontZ - SidewalkWidth * 1.5f - RoadWidth;
            OppositeFrontZ = FrontZ - SidewalkWidth * 2f - RoadWidth;

            CounterSize = new Vector3(0.8f, 1f, 2f);
            CounterCenter = new Vector3(HalfX - CounterWallGap, 0f, FrontZ + CounterFrontOffset);
            CashierSpot = new Vector3(HalfX - CashierWallGap, 0f, CounterCenter.z);
            QueueFirstSpot = new Vector3(CounterCenter.x - CounterSize.x * 0.5f - QueueGap, 0f, CounterCenter.z);
            QueueDirection = Vector3.forward;

            WaiterStation = new Vector3(-HalfX + CornerInset, 0f, HalfZ - CornerInset);
            OwnerStart = new Vector3(DoorX + OwnerStartOffsetX, 0f, FrontZ - SidewalkWidth + OwnerStartInset);
        }

        public float HalfX { get; }
        public float HalfZ { get; }
        public float FrontZ { get; }
        public float DoorX { get; }
        public Vector3 DoorInside { get; }
        public Vector3 DoorOutside { get; }
        public float SidewalkZ { get; }
        public float RoadZ { get; }
        public float OppositeSidewalkZ { get; }
        // Where the houses across the road begin (their fronts face the bar).
        public float OppositeFrontZ { get; }
        // The owner starts on the sidewalk, facing the bar's front.
        public Vector3 OwnerStart { get; }
        public Vector3 CounterCenter { get; }
        public Vector3 CounterSize { get; }
        public Vector3 CashierSpot { get; }
        public Vector3 QueueFirstSpot { get; }
        public Vector3 QueueDirection { get; }
        public Vector3 WaiterStation { get; }

        public Rect CounterFootprint => new Rect(
            CounterCenter.x - CounterSize.x * 0.5f, CounterCenter.z - CounterSize.z * 0.5f,
            CounterSize.x, CounterSize.z);

        public Vector3 SidewalkPoint(float x, float z)
        {
            return new Vector3(x, 0f, z);
        }

        // Inside the room, out on the sidewalk or road, or passing through the door; never inside an obstacle.
        public bool IsWalkable(Vector3 p, float radius, IReadOnlyList<Rect> obstacles)
        {
            var wall = WallThickness * 0.5f + radius;
            var inRoom = Within(p.x, -HalfX + wall, HalfX - wall) && Within(p.z, FrontZ + wall, HalfZ - wall);
            var inOutdoor = Within(p.x, -StreetHalfLength, StreetHalfLength)
                            && Within(p.z, OppositeFrontZ + radius, FrontZ - wall);
            var inDoor = Within(p.x, DoorX - DoorHalfWidth + radius, DoorX + DoorHalfWidth - radius)
                         && Within(p.z, FrontZ - wall - Epsilon, FrontZ + wall + Epsilon);

            if (!inRoom && !inOutdoor && !inDoor)
            {
                return false;
            }

            for (var i = 0; i < obstacles.Count; i++)
            {
                var r = obstacles[i];
                if (p.x > r.xMin - radius && p.x < r.xMax + radius && p.z > r.yMin - radius && p.z < r.yMax + radius)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Within(float value, float min, float max)
        {
            return value >= min && value <= max;
        }
    }
}
