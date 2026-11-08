using UnityEngine;

namespace Startup.Features.BarGame
{
    // Builds a customer from the character model with a random skin, or primitives if the model is missing.
    internal static class CustomerFactory
    {
        public const string ModelFolder = "Models/Characters/character-";
        public const float ModelHeight = 1.25f;

        private const string ModelLetters = "abcde";
        private const float ColliderRadius = 0.3f;
        private const float BodyHeight = 0.45f;
        private const float HeadHeight = 1.05f;
        private const float BubbleHeight = 1.75f;
        private const float BubbleSize = 0.55f;
        private const float MarkZ = -0.28f;

        private static readonly Vector3 BodyScale = new Vector3(0.5f, 0.45f, 0.5f);
        private static readonly Vector3 HeadScale = new Vector3(0.35f, 0.35f, 0.35f);
        private static readonly Color SkinColor = new Color(0.95f, 0.8f, 0.65f);
        private static readonly Color MarkColor = new Color(0.15f, 0.12f, 0.1f);

        private static int _created;

        public static Customer Create(Transform parent)
        {
            var root = new GameObject("Customer");
            root.transform.SetParent(parent, false);

            var animator = BuildModel(root, out var body);
            var bubble = BuildBubble(root.transform, out var bubbleRenderer);

            var customer = root.AddComponent<Customer>();
            customer.Initialize(body, bubble, bubbleRenderer, animator);
            return customer;
        }

        // Shared with the waiter so both use the same model setup. Each model has its own baked-in texture.
        public static CharacterAnimator SetupCharacter(GameObject root, GameObject visual, string modelPath)
        {
            var characterAnimator = root.AddComponent<CharacterAnimator>();
            characterAnimator.Initialize(ModelLoader.EnsureAnimator(visual), modelPath);
            characterAnimator.PlayIdle();

            // Measure after the clip is applied: clips may move or scale bones compared to the import pose.
            characterAnimator.ApplyPoseNow();
            ModelLoader.FitToHeight(visual, ModelHeight);
            return characterAnimator;
        }

        // Returns the animator when a model was used; otherwise builds primitives and returns null.
        private static CharacterAnimator BuildModel(GameObject root, out Renderer body)
        {
            var path = ModelFolder + ModelLetters[_created++ % ModelLetters.Length];
            var visual = ModelLoader.Instantiate(path, root.transform, "Model");
            if (visual == null)
            {
                body = BuildPrimitiveBody(root.transform);
                return null;
            }

            AddClickCollider(root);
            body = null;
            return SetupCharacter(root, visual, path);
        }

        private static Renderer BuildPrimitiveBody(Transform parent)
        {
            var body = PrimitiveFactory.Shape(PrimitiveType.Capsule, "Body", parent,
                new Vector3(0f, BodyHeight, 0f), BodyScale, Color.white);
            PrimitiveFactory.Shape(PrimitiveType.Sphere, "Head", parent,
                new Vector3(0f, HeadHeight, 0f), HeadScale, SkinColor);
            return body.GetComponent<Renderer>();
        }

        // Models have no collider, so add one for click and tap picking.
        private static void AddClickCollider(GameObject root)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = ColliderRadius;
            collider.height = ModelHeight;
            collider.center = new Vector3(0f, ModelHeight * 0.5f, 0f);
        }

        // Round bubble with a "!" on the camera-facing side; the sphere keeps its collider so it is clickable.
        private static Transform BuildBubble(Transform parent, out Renderer bubbleRenderer)
        {
            var root = new GameObject("OrderBubble").transform;
            root.SetParent(parent, false);
            root.localPosition = new Vector3(0f, BubbleHeight, 0f);

            var sphere = PrimitiveFactory.Shape(PrimitiveType.Sphere, "BubbleBody", root,
                Vector3.zero, Vector3.one * BubbleSize, Color.white);
            bubbleRenderer = sphere.GetComponent<Renderer>();

            AddMark(root, "MarkBar", new Vector3(0f, 0.06f, MarkZ), new Vector3(0.08f, 0.22f, 0.06f));
            AddMark(root, "MarkDot", new Vector3(0f, -0.14f, MarkZ), new Vector3(0.08f, 0.08f, 0.06f));

            root.gameObject.SetActive(false);
            return root;
        }

        private static void AddMark(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var mark = PrimitiveFactory.Box(name, parent, position, scale, MarkColor);
            Object.Destroy(mark.GetComponent<Collider>());
        }
    }
}
