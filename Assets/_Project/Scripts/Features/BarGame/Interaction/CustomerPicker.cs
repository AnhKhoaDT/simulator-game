using UnityEngine;
using UnityEngine.InputSystem;

namespace Startup.Features.BarGame
{
    // Click or tap a customer to serve them. A drag is ignored so it never conflicts with camera panning.
    public class CustomerPicker : MonoBehaviour
    {
        private const float MaxClickMovePixels = 12f;
        private const float MaxRayDistance = 100f;
        private const int HitBufferSize = 16;

        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        private Camera _camera;
        private Vector2 _pressPosition;
        private bool _pressing;

        public void Setup(Camera cam)
        {
            _camera = cam;
        }

        private void Update()
        {
            var pointer = Pointer.current;
            // Ignore the world while paused so clicks only reach the pause menu.
            if (_camera == null || pointer == null || Time.timeScale == 0f)
            {
                return;
            }

            if (pointer.press.wasPressedThisFrame)
            {
                _pressPosition = pointer.position.ReadValue();
                _pressing = true;
            }
            else if (_pressing && pointer.press.wasReleasedThisFrame)
            {
                _pressing = false;
                var position = pointer.position.ReadValue();
                if ((position - _pressPosition).sqrMagnitude <= MaxClickMovePixels * MaxClickMovePixels)
                {
                    ServeAt(position);
                }
            }
        }

        private void ServeAt(Vector2 screenPosition)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            var count = Physics.RaycastNonAlloc(ray, _hits, MaxRayDistance);

            Customer nearest = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var customer = _hits[i].collider.GetComponentInParent<Customer>();
                if (customer != null && _hits[i].distance < nearestDistance)
                {
                    nearest = customer;
                    nearestDistance = _hits[i].distance;
                }
            }

            if (nearest != null)
            {
                nearest.TryServe();
            }
        }
    }
}
