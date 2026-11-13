using UnityEngine;

namespace Startup.Features.BarGame
{
    // Builds a table and, separately, chairs, from imported models when present, otherwise from primitives.
    internal static class TableBuilder
    {
        private const string TableModelPath = "Models/Furniture/table";
        private const string ChairModelPath = "Models/Furniture/chair";

        private const float TopHeight = 0.75f;
        private const float TopSize = 1.4f;
        private const float TopThickness = 0.1f;
        private const float LegSize = 0.1f;
        private const float LegInset = 0.6f;
        private const float SeatSize = 0.5f;
        private const float SeatThickness = 0.08f;
        private const float ChairHeight = 0.9f;

        // Rotates the chair model if its front does not face +Z.
        public const float ChairYawOffset = 180f;
        public const float TopHalfSize = TopSize * 0.5f;
        public const float ChairHalfSize = SeatSize * 0.5f;
        public const float SeatHeight = 0.45f;
        // Where a new table's four chairs go, measured from the table center.
        public const float DefaultSeatDistance = 1.1f;

        private static readonly Color WoodColor = new Color(0.55f, 0.35f, 0.2f);
        private static readonly Color SeatColor = new Color(0.25f, 0.2f, 0.18f);

        public static readonly Vector3[] DefaultSeatDirections =
        {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back
        };

        public static BarTable Build(Transform parent, Vector3 position)
        {
            var root = new GameObject("Table");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            var model = ModelLoader.Instantiate(TableModelPath, root.transform, "TableModel");
            if (model != null)
            {
                ModelLoader.FitToBox(model, new Vector3(TopSize, TopHeight, TopSize));
            }
            else
            {
                BuildTop(root.transform);
                BuildLegs(root.transform);
            }

            return root.AddComponent<BarTable>();
        }

        // A chair whose anchor (at sitting height) stands above 'floorPosition'. The chair model hangs under it,
        // so moving or turning the anchor moves the whole chair.
        public static BarChair BuildChair(Transform parent, Vector3 floorPosition, float yaw)
        {
            var anchor = new GameObject("Chair").transform;
            anchor.SetParent(parent, false);
            anchor.position = floorPosition + Vector3.up * SeatHeight;

            var useModel = false;
            var model = ModelLoader.Instantiate(ChairModelPath, parent, "ChairModel");
            if (model != null)
            {
                // Fit while unrotated (fitting measures world-aligned bounds), then hang it under the anchor.
                model.transform.position = floorPosition;
                ModelLoader.FitToBox(model, new Vector3(SeatSize, ChairHeight, SeatSize));
                model.transform.SetParent(anchor, true);
                useModel = true;
            }
            else
            {
                PrimitiveFactory.Box("SeatBox", anchor, Vector3.zero, new Vector3(SeatSize, SeatThickness, SeatSize), SeatColor);
            }

            anchor.rotation = Quaternion.Euler(0f, yaw, 0f);
            AddPickVolume(anchor);

            var chair = anchor.gameObject.AddComponent<BarChair>();
            // Model anchors sit on the seat surface; the primitive seat is a thin box centered on the anchor.
            chair.Initialize(useModel ? 0f : SeatThickness * 0.5f);
            return chair;
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

        // A box the owner can aim at to pick the chair up. Furniture pick volumes live on their own layer.
        private static void AddPickVolume(Transform anchor)
        {
            var pick = new GameObject("PickVolume") { layer = BarFurniture.FurnitureLayer };
            pick.transform.SetParent(anchor, false);
            var box = pick.AddComponent<BoxCollider>();
            box.size = new Vector3(SeatSize, ChairHeight, SeatSize);
            box.center = new Vector3(0f, ChairHeight * 0.5f - SeatHeight, 0f);
        }
    }
}
