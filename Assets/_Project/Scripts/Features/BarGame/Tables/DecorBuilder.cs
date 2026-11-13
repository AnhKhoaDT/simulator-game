using UnityEngine;

namespace Startup.Features.BarGame
{
    // Builds a PlacedDecor from its catalog entry: the Kenney model fitted to size, or a plain box if it is missing.
    internal static class DecorBuilder
    {
        private const float FlatThickness = 0.03f;
        private const float MinPickHeight = 0.2f;
        private const float FallbackWidth = 0.6f;
        private const float SeatSurface = 0.42f;

        private static readonly Color FallbackColor = new Color(0.55f, 0.5f, 0.45f);

        public static PlacedDecor Build(Transform parent, FurnitureDef def, Vector3 floorPosition, float yaw)
        {
            var root = new GameObject(def.Id);
            root.transform.SetParent(parent, false);
            root.transform.position = floorPosition;

            var size = BuildVisual(root.transform, def);
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            AddPickVolume(root.transform, size);

            var decor = root.AddComponent<PlacedDecor>();
            decor.Initialize(def, new Vector2(size.x, size.z));
            AddSeats(decor, def, size);
            return decor;
        }

        // Measured while unrotated, so the size is along the world axes.
        private static Vector3 BuildVisual(Transform root, FurnitureDef def)
        {
            var model = ModelLoader.Instantiate(def.ModelPath, root, "Model");
            if (model != null)
            {
                if (def.IsFlat)
                {
                    ModelLoader.FitToBox(model, new Vector3(def.FlatSize.x, FlatThickness, def.FlatSize.y));
                }
                else
                {
                    ModelLoader.FitToHeight(model, def.Height);
                }

                return ModelLoader.MeasureSize(model);
            }

            var size = def.IsFlat
                ? new Vector3(def.FlatSize.x, FlatThickness, def.FlatSize.y)
                : new Vector3(FallbackWidth, def.Height, FallbackWidth);
            var box = PrimitiveFactory.Box("Fallback", root, Vector3.up * (size.y * 0.5f), size, FallbackColor);
            Object.Destroy(box.GetComponent<Collider>());
            return size;
        }

        // Benches and sofas get one seat per person, spread along their width, facing the model's front (-Z).
        private static void AddSeats(PlacedDecor decor, FurnitureDef def, Vector3 size)
        {
            for (var i = 0; i < def.SeatSlots; i++)
            {
                var x = def.SeatSlots == 1 ? 0f : (i - (def.SeatSlots - 1) * 0.5f) * (size.x / def.SeatSlots);
                var seat = new GameObject("Seat_" + i);
                seat.transform.SetParent(decor.transform, false);
                seat.transform.localPosition = new Vector3(x, SeatSurface, 0f);
                seat.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                decor.AddSeat(seat.AddComponent<StaffSeat>());
            }
        }

        // Models have no colliders; this box on the furniture layer is what the owner aims at.
        private static void AddPickVolume(Transform root, Vector3 size)
        {
            var pick = new GameObject("PickVolume") { layer = BarFurniture.FurnitureLayer };
            pick.transform.SetParent(root, false);
            var height = Mathf.Max(size.y, MinPickHeight);
            var box = pick.AddComponent<BoxCollider>();
            box.size = new Vector3(size.x, height, size.z);
            box.center = new Vector3(0f, height * 0.5f, 0f);
        }
    }
}
