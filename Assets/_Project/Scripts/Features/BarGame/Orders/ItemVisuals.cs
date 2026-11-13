using UnityEngine;

namespace Startup.Features.BarGame
{
    // Small primitive models for food and drinks, used on order bubbles, tables and the sign.
    // Built at unit size around the parent's origin (resting on it), then scaled by 'size'.
    internal static class ItemVisuals
    {
        private static readonly Color Amber = new Color(0.55f, 0.32f, 0.08f);
        private static readonly Color Label = new Color(0.95f, 0.85f, 0.4f);
        private static readonly Color CanBlue = new Color(0.2f, 0.5f, 0.9f);
        private static readonly Color Silver = new Color(0.8f, 0.82f, 0.85f);
        private static readonly Color PlateWhite = new Color(0.95f, 0.95f, 0.92f);
        private static readonly Color Peanut = new Color(0.8f, 0.55f, 0.3f);
        private static readonly Color Chili = new Color(0.85f, 0.2f, 0.15f);
        private static readonly Color Herb = new Color(0.35f, 0.7f, 0.3f);
        private static readonly Color Grilled = new Color(0.45f, 0.22f, 0.1f);
        private static readonly Color Gold = new Color(1f, 0.8f, 0.2f);
        private static readonly Color Beer = new Color(1f, 0.75f, 0.15f);
        private static readonly Color Foam = Color.white;

        public static GameObject Build(ItemShape shape, Transform parent, float size)
        {
            var model = TryBuildModel(shape, parent, size);
            if (model != null)
            {
                return model;
            }

            var root = CreateRoot(shape.ToString(), parent, size);
            switch (shape)
            {
                case ItemShape.Bottle:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.3f, 0f), new Vector3(0.32f, 0.3f, 0.32f), Amber);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.3f, 0f), new Vector3(0.33f, 0.08f, 0.33f), Label);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.75f, 0f), new Vector3(0.13f, 0.17f, 0.13f), Amber);
                    break;
                case ItemShape.Can:
                    Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.35f, 0f), new Vector3(0.42f, 0.35f, 0.42f), CanBlue);
                    Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.71f, 0f), new Vector3(0.4f, 0.02f, 0.4f), Silver);
                    break;
                case ItemShape.Plate:
                    BuildPlate(root);
                    Part(PrimitiveType.Sphere, root, new Vector3(-0.15f, 0.12f, 0.05f), Vector3.one * 0.22f, Peanut);
                    Part(PrimitiveType.Sphere, root, new Vector3(0.12f, 0.12f, 0.1f), Vector3.one * 0.2f, Chili);
                    Part(PrimitiveType.Sphere, root, new Vector3(0.02f, 0.12f, -0.15f), Vector3.one * 0.2f, Herb);
                    break;
                case ItemShape.Skewers:
                    BuildPlate(root);
                    for (var i = 0; i < 3; i++)
                    {
                        var skewer = Part(PrimitiveType.Capsule, root, new Vector3(0f, 0.1f, (i - 1) * 0.16f),
                            new Vector3(0.1f, 0.32f, 0.1f), Grilled);
                        skewer.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }

                    break;
            }

            return root.gameObject;
        }

        // A coin standing upright and facing local -Z (the camera, on a bubble).
        public static GameObject BuildCoin(Transform parent, float size)
        {
            var root = CreateRoot("Coin", parent, size);
            var coin = Part(PrimitiveType.Cylinder, root, Vector3.zero, new Vector3(0.75f, 0.05f, 0.75f), Gold);
            coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var mark = Part(PrimitiveType.Cube, root, new Vector3(0f, 0f, -0.06f), new Vector3(0.12f, 0.4f, 0.02f), Amber);
            mark.transform.localRotation = Quaternion.identity;
            return root.gameObject;
        }

        // Beer mug for the shop sign.
        public static GameObject BuildMug(Transform parent, float size)
        {
            var root = CreateRoot("Mug", parent, size);
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.6f, 0.5f), Beer);
            Part(PrimitiveType.Cube, root, new Vector3(0f, 0.66f, 0f), new Vector3(0.56f, 0.14f, 0.56f), Foam);
            Part(PrimitiveType.Cube, root, new Vector3(0.33f, 0.3f, 0f), new Vector3(0.14f, 0.36f, 0.12f), Beer);
            return root.gameObject;
        }

        // Food Kit model for the item, fitted to about 'size'; null if the pack is missing.
        private static GameObject TryBuildModel(ItemShape shape, Transform parent, float size)
        {
            string name;
            bool byWidth;
            float fit;
            switch (shape)
            {
                case ItemShape.Bottle:
                    name = "soda-bottle"; byWidth = false; fit = size;
                    break;
                case ItemShape.Can:
                    name = "soda-can"; byWidth = false; fit = size * 0.7f;
                    break;
                case ItemShape.Plate:
                    name = "fries"; byWidth = true; fit = size * 1.1f;
                    break;
                default:
                    name = "skewer"; byWidth = true; fit = size * 1.2f;
                    break;
            }

            // Wrap it so the caller can scale and move the item freely, as with the primitive version.
            var root = new GameObject(shape.ToString()).transform;
            root.SetParent(parent, false);
            var model = ModelLoader.Instantiate(ModelCatalog.Food + name, root, name);
            if (model == null)
            {
                Object.Destroy(root.gameObject);
                return null;
            }

            if (byWidth)
            {
                ModelLoader.FitToWidth(model, fit);
            }
            else
            {
                ModelLoader.FitToHeight(model, fit);
            }

            return root.gameObject;
        }

        private static void BuildPlate(Transform root)
        {
            Part(PrimitiveType.Cylinder, root, new Vector3(0f, 0.02f, 0f), new Vector3(0.8f, 0.02f, 0.8f), PlateWhite);
        }

        private static Transform CreateRoot(string name, Transform parent, float size)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * size;
            return root;
        }

        // Decorative only: colliders are removed so items never block movement or picking.
        private static GameObject Part(PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Color color)
        {
            var part = PrimitiveFactory.Shape(type, type.ToString(), parent, position, scale, color);
            Object.Destroy(part.GetComponent<Collider>());
            return part;
        }
    }
}
