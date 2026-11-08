using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Startup.Features.BarGame
{
    // Twin-stick touch input: drag on the left half to walk (a joystick appears under the finger),
    // drag on the right half to look around. Several fingers at once are supported.
    public sealed class TouchControls
    {
        private const float RadiusPixels = 70f;
        private const float BaseSize = 140f;
        private const float KnobSize = 60f;
        private const string OverlayClass = "pause-overlay";

        private readonly VisualElement _root;
        private readonly VisualElement _base;
        private readonly VisualElement _knob;
        private Vector2 _moveStart;
        private int _moveTouchId;
        private int _lookTouchId;

        public TouchControls(VisualElement root)
        {
            _root = root;
            _base = CreateElement("joystick-base");
            _knob = CreateElement("joystick-knob");
            SetJoystickVisible(false);
        }

        // Walk direction: x = right, y = forward, magnitude 0..1.
        public Vector2 Direction { get; private set; }

        // Look movement this frame, in screen pixels (x = right, y = up).
        public Vector2 LookDelta { get; private set; }

        // Phones and tablets play with touch; desktops (even with a touch screen) use keyboard and mouse.
        public static bool IsTouchPlatform => Application.isMobilePlatform;

        public void Tick()
        {
            LookDelta = Vector2.zero;
            var screen = Touchscreen.current;
            var panel = _root.panel;
            if (screen == null || panel == null || Time.timeScale == 0f)
            {
                ReleaseMove();
                _lookTouchId = 0;
                return;
            }

            foreach (var touch in screen.touches)
            {
                var id = touch.touchId.ReadValue();
                if (id == 0)
                {
                    continue;
                }

                var phase = touch.phase.ReadValue();
                var position = touch.position.ReadValue();

                if (phase == InputTouchPhase.Began)
                {
                    Begin(panel, id, position);
                }
                else if (phase == InputTouchPhase.Moved || phase == InputTouchPhase.Stationary)
                {
                    Continue(panel, id, position, touch.delta.ReadValue());
                }
                else if (phase == InputTouchPhase.Ended || phase == InputTouchPhase.Canceled)
                {
                    End(id);
                }
            }
        }

        private void Begin(IPanel panel, int id, Vector2 screenPosition)
        {
            var panelPosition = ToPanel(panel, screenPosition);
            if (IsOverUi(panel, panelPosition))
            {
                return;
            }

            if (screenPosition.x < Screen.width * 0.5f)
            {
                if (_moveTouchId == 0)
                {
                    _moveTouchId = id;
                    _moveStart = panelPosition;
                    Place(_base, _moveStart, BaseSize);
                    Place(_knob, _moveStart, KnobSize);
                    SetJoystickVisible(true);
                }
            }
            else if (_lookTouchId == 0)
            {
                _lookTouchId = id;
            }
        }

        private void Continue(IPanel panel, int id, Vector2 screenPosition, Vector2 delta)
        {
            if (id == _moveTouchId)
            {
                var offset = Vector2.ClampMagnitude(ToPanel(panel, screenPosition) - _moveStart, RadiusPixels);
                Place(_knob, _moveStart + offset, KnobSize);
                // Panel space grows downward, so flip y for "forward".
                Direction = new Vector2(offset.x, -offset.y) / RadiusPixels;
            }
            else if (id == _lookTouchId)
            {
                LookDelta += delta;
            }
        }

        private void End(int id)
        {
            if (id == _moveTouchId)
            {
                ReleaseMove();
            }
            else if (id == _lookTouchId)
            {
                _lookTouchId = 0;
            }
        }

        private void ReleaseMove()
        {
            _moveTouchId = 0;
            Direction = Vector2.zero;
            SetJoystickVisible(false);
        }

        private VisualElement CreateElement(string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            _root.Add(element);
            return element;
        }

        private void SetJoystickVisible(bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _base.style.display = display;
            _knob.style.display = display;
        }

        private static void Place(VisualElement element, Vector2 center, float size)
        {
            element.style.left = center.x - size * 0.5f;
            element.style.top = center.y - size * 0.5f;
        }

        // Screen positions start bottom-left; panel positions start top-left.
        public static Vector2 ToPanel(IPanel panel, Vector2 screen)
        {
            return RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
        }

        // Buttons and open overlays catch the press. Containers may still be pickable, so look up the hierarchy.
        public static bool IsOverUi(IPanel panel, Vector2 panelPosition)
        {
            for (var element = panel.Pick(panelPosition); element != null; element = element.parent)
            {
                if (element is Button || element is TextField
                    || element.ClassListContains(OverlayClass)
                    || element.ClassListContains(Core.UI.OverlayPanel.OverlayClass))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
