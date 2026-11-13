using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Startup.Features.BarGame
{
    // The owner the player walks around with WASD or the touch joystick, relative to where the camera looks.
    // Walking up to an ordering customer serves them; standing behind the counter collects payment.
    public class PlayerController : MonoBehaviour
    {
        private const float Radius = 0.3f;
        private const float ServeRadius = 1.6f;
        private const float TurnSpeedDegrees = 720f;
        private const float MinInput = 0.1f;
        private const float StandDistance = 0.8f;
        private const float MarkerSize = 0.8f;
        private const float MarkerHeight = 0.01f;

        private static readonly Color MarkerColor = new Color(1f, 0.75f, 0.2f);

        private BarLayout _layout;
        private IReadOnlyList<Rect> _furniture;
        private IReadOnlyList<Rect> _props;
        private CustomerSpawner _spawner;
        private PlayerCamera _view;
        private TouchControls _touch;
        private CharacterAnimator _animator;
        private ISeat _seat;
        private int _standFrame = -1;
        private float _speed;
        private bool _moving;

        public bool IsSitting => _seat != null;

        // Everything the camera hides in first person.
        public Renderer[] BodyRenderers { get; private set; }

        // furniture grows as tables are bought; props are the fixed street trees and lamps.
        public void Setup(BarLayout layout, IReadOnlyList<Rect> furniture, IReadOnlyList<Rect> props,
            CustomerSpawner spawner, PlayerCamera view, TouchControls touch, float speed)
        {
            _layout = layout;
            _furniture = furniture;
            _props = props;
            _spawner = spawner;
            _view = view;
            _touch = touch;
            _speed = speed;

            transform.position = layout.OwnerStart;
            _animator = CustomerFactory.BuildCharacter(gameObject, CustomerFactory.OwnerLetter, out _);
            BuildMarker();
            BodyRenderers = GetComponentsInChildren<Renderer>();
        }

        private void Update()
        {
            if (_view == null || Time.timeScale == 0f)
            {
                return;
            }

            var input = ReadInput();
            var moving = input.sqrMagnitude > MinInput * MinInput;
            if (IsSitting)
            {
                var keyboard = Keyboard.current;
                if (moving || (keyboard != null && keyboard.fKey.wasPressedThisFrame))
                {
                    StandUp();
                }

                return;
            }

            if (moving)
            {
                Move(input);
            }
            else if (_view.IsFirstPerson)
            {
                transform.rotation = Quaternion.Euler(0f, _view.Yaw, 0f);
            }

            SetMoving(moving);
            _spawner.ServeNear(transform.position, ServeRadius);
        }

        // Sits on a free seat: the body snaps onto it, the view drops to seated eye level.
        public bool TrySit(ISeat seat)
        {
            if (IsSitting || !seat.IsFree || _standFrame == Time.frameCount)
            {
                return false;
            }

            _seat = seat;
            seat.Occupy();
            transform.SetPositionAndRotation(SeatPose.BodyPosition(seat), seat.SitRotation);
            _animator?.PlaySit();
            _moving = false;
            _view.Sitting = true;
            _view.SetYaw(seat.SitRotation.eulerAngles.y);
            return true;
        }

        private void StandUp()
        {
            var facing = _seat.SitRotation;
            var sitPoint = _seat.SitPoint;
            _seat.Vacate();
            _seat = null;
            _standFrame = Time.frameCount;
            _view.Sitting = false;
            transform.position = FindStandPoint(sitPoint, facing);
            _animator?.PlayIdle();
        }

        // In front of the seat if free, otherwise to a side or behind it.
        private Vector3 FindStandPoint(Vector3 sitPoint, Quaternion facing)
        {
            var directions = new[] { Vector3.forward, Vector3.right, Vector3.left, Vector3.back };
            foreach (var direction in directions)
            {
                var candidate = sitPoint + facing * direction * StandDistance;
                candidate.y = 0f;
                if (_layout.IsWalkable(candidate, Radius, _furniture) && _layout.IsWalkable(candidate, Radius, _props))
                {
                    return candidate;
                }
            }

            var fallback = sitPoint + facing * Vector3.forward * StandDistance;
            fallback.y = 0f;
            return fallback;
        }

        // Joystick and keyboard share one direction: x = right, y = forward.
        private Vector2 ReadInput()
        {
            var input = _touch != null ? _touch.Direction : Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                input.x += Axis(keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed,
                    keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed);
                input.y += Axis(keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed,
                    keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed);
            }

            return Vector2.ClampMagnitude(input, 1f);
        }

        private void Move(Vector2 input)
        {
            var heading = Quaternion.Euler(0f, _view.Yaw, 0f);
            var direction = heading * new Vector3(input.x, 0f, input.y);
            var step = direction * (_speed * Time.deltaTime);

            // Slide along walls and furniture: try the full step, then each axis on its own.
            var position = transform.position;
            if (!TryStep(position + step) && !TryStep(position + new Vector3(step.x, 0f, 0f)))
            {
                TryStep(position + new Vector3(0f, 0f, step.z));
            }

            // First person: the body follows the view. Third person: the body turns toward where it walks.
            var facing = _view.IsFirstPerson ? heading : Quaternion.LookRotation(direction);
            transform.rotation = _view.IsFirstPerson
                ? facing
                : Quaternion.RotateTowards(transform.rotation, facing, TurnSpeedDegrees * Time.deltaTime);
        }

        private bool TryStep(Vector3 candidate)
        {
            if (!_layout.IsWalkable(candidate, Radius, _furniture) || !_layout.IsWalkable(candidate, Radius, _props))
            {
                return false;
            }

            transform.position = candidate;
            return true;
        }

        private void SetMoving(bool moving)
        {
            if (moving == _moving || _animator == null)
            {
                _moving = moving;
                return;
            }

            _moving = moving;
            if (moving)
            {
                _animator.PlayWalk();
            }
            else
            {
                _animator.PlayIdle();
            }
        }

        // A ring under the owner's feet, visible in third person.
        private void BuildMarker()
        {
            var marker = PrimitiveFactory.Shape(PrimitiveType.Cylinder, "OwnerMarker", transform,
                Vector3.up * MarkerHeight, new Vector3(MarkerSize, MarkerHeight, MarkerSize), MarkerColor);
            Destroy(marker.GetComponent<Collider>());
        }

        private static float Axis(bool positive, bool negative)
        {
            return (positive ? 1f : 0f) - (negative ? 1f : 0f);
        }
    }
}
