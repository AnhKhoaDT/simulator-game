using UnityEngine;

namespace Startup.Features.BarGame
{
    // Builds characters from the Blocky models (customers, passers-by, staff), or primitives if a model is missing.
    internal static class CustomerFactory
    {
        public const string ModelFolder = "Models/Characters/character-";
        // Real-world scale, because the player now looks through the owner's eyes.
        public const float ModelHeight = 1.65f;

        // Letters a..p are guests and passers-by; q is the waiter and r is the owner.
        public const string GuestLetters = "abcdefghijklmnop";
        public const char WaiterLetter = 'q';
        public const char OwnerLetter = 'r';

        private const float ColliderRadius = 0.3f;
        private const float BodyHeight = 0.45f;
        private const float HeadHeight = 1.05f;
        private const float BubbleHeight = 2.2f;
        // Ignore Raycast, so the third-person camera never collides with customers.
        private const int CustomerLayer = 2;

        private static readonly Vector3 BodyScale = new Vector3(0.5f, 0.45f, 0.5f);
        private static readonly Vector3 HeadScale = new Vector3(0.35f, 0.35f, 0.35f);
        private static readonly Color SkinColor = new Color(0.95f, 0.8f, 0.65f);

        private static int _created;

        public static Customer Create(Transform parent)
        {
            var root = new GameObject("Customer") { layer = CustomerLayer };
            root.transform.SetParent(parent, false);

            var letter = GuestLetters[_created++ % GuestLetters.Length];
            var animator = BuildCharacter(root, letter, out var body);
            AddCollider(root);
            var bubble = CustomerBubble.Create(root.transform, BubbleHeight);

            var customer = root.AddComponent<Customer>();
            customer.Initialize(body, bubble, animator);
            return customer;
        }

        public static char RandomGuestLetter()
        {
            return GuestLetters[Random.Range(0, GuestLetters.Length)];
        }

        // Adds the character model under root. Returns its animator, or null (with a primitive body) if missing.
        public static CharacterAnimator BuildCharacter(GameObject root, char letter, out Renderer primitiveBody)
        {
            var path = ModelFolder + letter;
            var visual = ModelLoader.Instantiate(path, root.transform, "Model");
            if (visual == null)
            {
                primitiveBody = BuildPrimitiveBody(root.transform);
                return null;
            }

            primitiveBody = null;
            var animator = root.AddComponent<CharacterAnimator>();
            animator.Initialize(ModelLoader.EnsureAnimator(visual), path);
            animator.PlayIdle();

            // Measure after the clip is applied: clips may move or scale bones compared to the import pose.
            animator.ApplyPoseNow();
            ModelLoader.FitToHeight(visual, ModelHeight);
            return animator;
        }

        private static Renderer BuildPrimitiveBody(Transform parent)
        {
            var body = PrimitiveFactory.Shape(PrimitiveType.Capsule, "Body", parent,
                new Vector3(0f, BodyHeight, 0f), BodyScale, Color.white);
            PrimitiveFactory.Shape(PrimitiveType.Sphere, "Head", parent,
                new Vector3(0f, HeadHeight, 0f), HeadScale, SkinColor);
            return body.GetComponent<Renderer>();
        }

        private static void AddCollider(GameObject root)
        {
            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = ColliderRadius;
            collider.height = ModelHeight;
            collider.center = new Vector3(0f, ModelHeight * 0.5f, 0f);
        }
    }
}
