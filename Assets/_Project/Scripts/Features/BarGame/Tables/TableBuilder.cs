using UnityEngine;

namespace Startup.Features.BarGame
{
    // Table with 4 seats, built from imported models when present, otherwise from primitives.
    internal static class TableBuilder
    {
        private const string TableModelPath = "Models/Furniture/table";
        private const string ChairModelPath = "Models/Furniture/chair";

        private const float TopHeight = 0.75f;
        private const float TopSize = 1.4f;
        private const float TopThickness = 0.1f;
        private const float LegSize = 0.1f;
        private const float LegInset = 0.6f;
        private const float SeatOffset = 1.1f;
        private const float SeatSize = 0.5f;
        private const float SeatThickness = 0.08f;
        private const float SeatHeight = 0.45f;
        private const float ChairHeight = 0.9f;
        // Rotates the chair model if its front does not face +Z.
        private const float ChairYawOffset = 180f;

        private static readonly Color WoodColor = new Color(0.55f, 0.35f, 0.2f);
        private static readonly Color SeatColor = new Color(0.25f, 0.2f, 0.18f);
        private static readonly Vector3[] SeatDirections =
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back
        };

        public static BarTable Build(Transform parent, Vector3 position)
        {
            var root = new GameObject("Table");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            BuildSurface(root.transform);

            var table = root.AddComponent<BarTable>();
            var seats = BuildSeats(root.transform, out var surfaceOffset);
            table.SetSeats(seats, surfaceOffset);
            return table;
        }

        private static void BuildSurface(Transform parent)
        {
            var model = ModelLoader.Instantiate(TableModelPath, parent, "TableModel");
            if (model != null)
            {
                ModelLoader.FitToBox(model, new Vector3(TopSize, TopHeight, TopSize));
                return;
            }

            BuildTop(parent);
            BuildLegs(parent);
        }

        private static void BuildTop(Transform parent)
        {
            var size = new Vector3(TopSize, TopThickness, TopSize);
            PrimitiveFactory.Box("Top", parent, new Vector3(0f, TopHeight - TopThickness * 0.5f, 0f), size, WoodColor);
        }

        private static void BuildLegs(Transform parent)
        {
            var legHeight = TopHeight - TopThickness;
            var size = new Vector3(LegSize, legHeight, LegSize);
            var y = legHeight * 0.5f;

            for (var i = 0; i < 4; i++)
            {
                var x = (i % 2 == 0 ? 1f : -1f) * LegInset;
                var z = (i < 2 ? 1f : -1f) * LegInset;
                PrimitiveFactory.Box($"Leg_{i}", parent, new Vector3(x, y, z), size, WoodColor);
            }
        }

        private static Transform[] BuildSeats(Transform parent, out float surfaceOffset)
        {
            var useModel = Resources.Load<GameObject>(ChairModelPath) != null;
            // Model anchors sit on the seat surface; primitive anchors are the box center.
            surfaceOffset = useModel ? 0f : SeatThickness * 0.5f;

            var seats = new Transform[SeatDirections.Length];
            for (var i = 0; i < seats.Length; i++)
            {
                seats[i] = useModel ? BuildChair(parent, i) : BuildPrimitiveSeat(parent, i);
            }

            return seats;
        }

        private static Transform BuildPrimitiveSeat(Transform parent, int index)
        {
            var position = SeatDirections[index] * SeatOffset + Vector3.up * SeatHeight;
            var size = new Vector3(SeatSize, SeatThickness, SeatSize);
            return PrimitiveFactory.Box($"Seat_{index}", parent, position, size, SeatColor).transform;
        }

        // The anchor marks where a customer sits; the chair model is its child, facing the table.
        private static Transform BuildChair(Transform parent, int index)
        {
            var direction = SeatDirections[index];
            var anchor = new GameObject($"Seat_{index}").transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = direction * SeatOffset + Vector3.up * SeatHeight;

            var chair = ModelLoader.Instantiate(ChairModelPath, parent, $"Chair_{index}");
            chair.transform.localPosition = direction * SeatOffset;
            ModelLoader.FitToBox(chair, new Vector3(SeatSize, ChairHeight, SeatSize));
            chair.transform.rotation = Quaternion.LookRotation(-direction) * Quaternion.Euler(0f, ChairYawOffset, 0f);
            return anchor;
        }
    }
}
