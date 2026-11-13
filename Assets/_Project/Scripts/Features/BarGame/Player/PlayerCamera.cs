using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Startup.Features.BarGame
{
    // First-person view from the owner's eyes (default) or third-person behind them, toggled with V.
    // Mouse looks while the cursor is locked (Tab frees it for the UI); on touch, the right half of the screen looks.
    public class PlayerCamera : MonoBehaviour
    {
        private const float EyeHeight = 1.5f;
        private const float SitEyeHeight = 1.15f;
        private const float PivotHeight = 1.6f;
        private const float ThirdPersonDistance = 3.2f;
        private const float CollisionRadius = 0.2f;
        private const float CollisionPadding = 0.1f;
        private const float MouseDegreesPerPixel = 0.12f;
        private const float TouchDegreesPerPixel = 0.25f;
        private const float MinPitch = -80f;
        private const float MaxPitch = 80f;
        private const float FieldOfView = 65f;
        private const float NearClip = 0.05f;
        // Customers sit on the Ignore Raycast layer, so they never push the camera in.
        private const int IgnoreRaycastLayer = 2;
        // Furniture pick volumes (TransparentFX) should not push the camera either.
        private const int FurniturePickLayer = 1;

        private Camera _camera;
        private Transform _target;
        private Renderer[] _body;
        private TouchControls _touch;
        private Func<Vector2, bool> _isOverUi;
        private int _collisionMask;
        private float _pitch;
        private bool _thirdPerson;

        public float Yaw { get; private set; }

        public bool IsFirstPerson => !_thirdPerson;

        // Lowers the eyes while the owner sits.
        public bool Sitting { get; set; }

        public void SetYaw(float yaw)
        {
            Yaw = yaw;
        }

        public void Setup(Camera cam, Transform target, Renderer[] body, TouchControls touch,
            Func<Vector2, bool> isOverUi, float startYaw)
        {
            _camera = cam;
            _target = target;
            _body = body;
            _touch = touch;
            _isOverUi = isOverUi;
            Yaw = startYaw;
            _collisionMask = ~((1 << IgnoreRaycastLayer) | (1 << FurniturePickLayer));

            cam.orthographic = false;
            cam.fieldOfView = FieldOfView;
            cam.nearClipPlane = NearClip;
            cam.clearFlags = CameraClearFlags.SolidColor;
            ApplyView();
            Place();
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            UpdateCursor();
            if (Time.timeScale == 0f)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                _thirdPerson = !_thirdPerson;
                ApplyView();
            }

            Look(ReadLook());
        }

        private void LateUpdate()
        {
            if (_camera != null)
            {
                Place();
            }
        }

        private Vector2 ReadLook()
        {
            var look = Vector2.zero;
            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                look += mouse.delta.ReadValue() * MouseDegreesPerPixel;
            }

            if (_touch != null)
            {
                look += _touch.LookDelta * TouchDegreesPerPixel;
            }

            return look;
        }

        private void Look(Vector2 degrees)
        {
            Yaw = Mathf.Repeat(Yaw + degrees.x, 360f);
            _pitch = Mathf.Clamp(_pitch - degrees.y, MinPitch, MaxPitch);
        }

        // PC only: lock the cursor for mouse look; Tab frees it, clicking the game (not a button) locks it again.
        // Menus that pause the game always free it.
        private void UpdateCursor()
        {
            if (TouchControls.IsTouchPlatform || Mouse.current == null)
            {
                return;
            }

            if (Time.timeScale == 0f)
            {
                SetCursorLocked(false);
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
            {
                SetCursorLocked(Cursor.lockState != CursorLockMode.Locked);
                return;
            }

            var mouse = Mouse.current;
            if (Cursor.lockState != CursorLockMode.Locked && mouse.leftButton.wasPressedThisFrame
                && !_isOverUi(mouse.position.ReadValue()))
            {
                SetCursorLocked(true);
            }
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Place()
        {
            var rotation = Quaternion.Euler(_pitch, Yaw, 0f);
            if (!_thirdPerson)
            {
                _camera.transform.SetPositionAndRotation(_target.position + Vector3.up * (Sitting ? SitEyeHeight : EyeHeight), rotation);
                return;
            }

            // Pull the camera in when a wall or ceiling is between it and the owner.
            var pivot = _target.position + Vector3.up * PivotHeight;
            var back = rotation * Vector3.back;
            var distance = ThirdPersonDistance;
            if (Physics.SphereCast(pivot, CollisionRadius, back, out var hit, ThirdPersonDistance,
                    _collisionMask, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Max(0f, hit.distance - CollisionPadding);
            }

            _camera.transform.SetPositionAndRotation(pivot + back * distance, rotation);
        }

        // The owner's own body would block the view in first person, so hide it there.
        private void ApplyView()
        {
            foreach (var part in _body)
            {
                if (part != null)
                {
                    part.enabled = _thirdPerson;
                }
            }
        }
    }
}
