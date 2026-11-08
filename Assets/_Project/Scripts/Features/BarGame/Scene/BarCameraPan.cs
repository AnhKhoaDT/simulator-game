using UnityEngine;
using UnityEngine.InputSystem;

namespace Startup.Features.BarGame
{
    // Drag to pan, scroll to zoom. Pointer covers mouse and touch with one code path.
    public class BarCameraPan : MonoBehaviour
    {
        private const float ZoomStep = 1f;
        private const float MaxZoomIn = 6f;
        private const float MaxZoomOut = 3f;
        private const float MinPitchSin = 0.2f;

        private Camera _camera;
        private Vector2 _panLimit;
        private Vector3 _origin;
        private Vector2 _pan;
        private float _zoom;
        private float _pitchSin;
        private bool _ready;

        public void Setup(Camera cam, Vector2 panLimit)
        {
            _camera = cam;
            _panLimit = panLimit;
        }

        private void LateUpdate()
        {
            if (_camera == null || Time.timeScale == 0f)
            {
                return;
            }

            // Capture the origin after every Start has placed the camera.
            if (!_ready)
            {
                _origin = _camera.transform.position;
                _pitchSin = Mathf.Max(MinPitchSin, Mathf.Sin(_camera.transform.eulerAngles.x * Mathf.Deg2Rad));
                _ready = true;
            }

            ReadInput();
            _camera.transform.position = _origin
                                         + new Vector3(_pan.x, 0f, _pan.y)
                                         + _camera.transform.forward * _zoom;
        }

        private void ReadInput()
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.isPressed)
            {
                Pan(pointer.delta.ReadValue());
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                Zoom(mouse.scroll.ReadValue().y);
            }
        }

        private void Pan(Vector2 screenDelta)
        {
            // World units per screen pixel at the point the camera is looking at.
            var distance = _camera.transform.position.y / _pitchSin;
            var worldPerPixel = 2f * Mathf.Tan(_camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance / Screen.height;

            var right = _camera.transform.right;
            var forward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
            var move = (right * screenDelta.x + forward * (screenDelta.y / _pitchSin)) * worldPerPixel;

            _pan.x = Mathf.Clamp(_pan.x - move.x, -_panLimit.x, _panLimit.x);
            _pan.y = Mathf.Clamp(_pan.y - move.z, -_panLimit.y, _panLimit.y);
        }

        private void Zoom(float scroll)
        {
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            _zoom = Mathf.Clamp(_zoom + Mathf.Sign(scroll) * ZoomStep, -MaxZoomOut, MaxZoomIn);
        }
    }
}
