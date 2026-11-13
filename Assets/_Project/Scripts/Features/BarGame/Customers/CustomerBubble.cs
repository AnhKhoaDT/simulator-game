using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Bubble over a customer's head: what they want (item or coin) and a patience bar. Always faces the camera.
    public class CustomerBubble : MonoBehaviour
    {
        private const float CardSize = 0.62f;
        private const float CardDepth = 0.1f;
        private const float IconSize = 0.36f;
        private const float IconZ = -0.12f;
        private const float IconLift = -0.12f;
        private const float BarWidth = 0.62f;
        private const float BarHeight = 0.07f;
        private const float BarDepth = 0.04f;
        private const float BarGap = 0.07f;
        private const float WarnRatio = 0.5f;
        private const float BadRatio = 0.25f;
        private const float RedrawThreshold = 0.01f;

        private static readonly Color CardColor = Color.white;
        private static readonly Color BarBackColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color GoodColor = new Color(0.3f, 0.8f, 0.35f);
        private static readonly Color WarnColor = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color BadColor = new Color(0.9f, 0.2f, 0.2f);

        private readonly Dictionary<ItemShape, GameObject> _orderIcons = new Dictionary<ItemShape, GameObject>();

        private Transform _icons;
        private GameObject _payIcon;
        private GameObject _shownIcon;
        private Transform _barFill;
        private Renderer _barFillRenderer;
        private float _shownRatio = -1f;

        public static CustomerBubble Create(Transform parent, float height)
        {
            var root = new GameObject("Bubble");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, height, 0f);

            var bubble = root.AddComponent<CustomerBubble>();
            bubble.Build();
            root.SetActive(false);
            return bubble;
        }

        public void ShowOrder(ItemShape shape)
        {
            if (!_orderIcons.TryGetValue(shape, out var icon))
            {
                icon = CreateIcon(ItemVisuals.Build(shape, _icons, IconSize));
                _orderIcons[shape] = icon;
            }

            Show(icon);
        }

        public void ShowPay()
        {
            Show(_payIcon);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ratio 1 = full patience, 0 = about to give up.
        public void SetPatience(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            if (Mathf.Abs(ratio - _shownRatio) < RedrawThreshold)
            {
                return;
            }

            _shownRatio = ratio;
            _barFill.localScale = new Vector3(BarWidth * ratio, BarHeight, BarDepth);
            _barFill.localPosition = new Vector3(-BarWidth * (1f - ratio) * 0.5f, BarY, -BarDepth);

            var color = ratio > WarnRatio ? GoodColor : ratio > BadRatio ? WarnColor : BadColor;
            _barFillRenderer.sharedMaterial = PrimitiveFactory.GetMaterial(color);
        }

        private static float BarY => -CardSize * 0.5f - BarGap;

        private void LateUpdate()
        {
            FaceCamera();
        }

        // Billboard: match the camera's rotation so the icon always reads face-on, from any angle.
        private void FaceCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;
            }
        }

        private void Show(GameObject icon)
        {
            if (_shownIcon != null)
            {
                _shownIcon.SetActive(false);
            }

            _shownIcon = icon;
            _shownIcon.SetActive(true);
            _shownRatio = -1f;
            SetPatience(1f);
            FaceCamera();
            gameObject.SetActive(true);
        }

        private void Build()
        {
            var card = PrimitiveFactory.Shape(PrimitiveType.Sphere, "Card", transform,
                Vector3.zero, new Vector3(CardSize, CardSize, CardDepth), CardColor);
            Destroy(card.GetComponent<Collider>());

            _icons = new GameObject("Icons").transform;
            _icons.SetParent(transform, false);
            _icons.localPosition = new Vector3(0f, IconLift, IconZ);

            _payIcon = CreateIcon(ItemVisuals.BuildCoin(_icons, IconSize * 1.4f));
            _payIcon.transform.localPosition = new Vector3(0f, -IconLift, 0f);

            var back = PrimitiveFactory.Box("PatienceBack", transform, new Vector3(0f, BarY, 0f),
                new Vector3(BarWidth, BarHeight, BarDepth), BarBackColor);
            Destroy(back.GetComponent<Collider>());

            var fill = PrimitiveFactory.Box("PatienceFill", transform, new Vector3(0f, BarY, -BarDepth),
                new Vector3(BarWidth, BarHeight, BarDepth), GoodColor);
            Destroy(fill.GetComponent<Collider>());
            _barFill = fill.transform;
            _barFillRenderer = fill.GetComponent<Renderer>();
        }

        private static GameObject CreateIcon(GameObject icon)
        {
            icon.SetActive(false);
            return icon;
        }
    }
}
