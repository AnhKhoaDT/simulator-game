using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // A hired waiter rests at a station, walks to a waiting customer, serves them, then walks back.
    public class WaiterService : MonoBehaviour
    {
        private const float BodyHeight = 0.45f;
        private const float HeadHeight = 1.05f;
        private const float ArriveDistance = 0.05f;
        private const float TurnSpeedDegrees = 540f;
        private const float MinLookSqrDistance = 0.0001f;


        private static readonly Vector3 BodyScale = new Vector3(0.5f, 0.45f, 0.5f);
        private static readonly Vector3 HeadScale = new Vector3(0.35f, 0.35f, 0.35f);
        private static readonly Color UniformColor = new Color(0.15f, 0.2f, 0.45f);
        private static readonly Color SkinColor = new Color(0.95f, 0.8f, 0.65f);

        private enum State
        {
            Resting,
            GoingToCustomer,
            Returning
        }

        private readonly List<Vector3> _path = new List<Vector3>();

        private CustomerSpawner _spawner;
        private BarSeating _seating;
        private BarUpgrades _upgrades;
        private Transform _figure;
        private CharacterAnimator _animator;

        // The visible waiter; inactive until one is hired.
        public Transform Figure => _figure;
        private Customer _target;
        private Vector3 _station;
        private float _walkSpeed;
        private float _timer;
        private int _pathIndex;
        private State _state;

        public void Setup(CustomerSpawner spawner, BarSeating seating, BarUpgrades upgrades,
            float walkSpeed, Vector3 stationPoint)
        {
            _spawner = spawner;
            _seating = seating;
            _upgrades = upgrades;
            _walkSpeed = walkSpeed;
            _station = stationPoint;
            _figure = BuildFigure(stationPoint).transform;
            _figure.gameObject.SetActive(upgrades.WaiterLevel > 0);
            upgrades.Changed += OnUpgradesChanged;
        }

        private void Update()
        {
            if (_upgrades == null || _upgrades.WaiterLevel == 0)
            {
                return;
            }

            switch (_state)
            {
                case State.Resting:
                    UpdateResting();
                    break;
                case State.GoingToCustomer:
                    UpdateGoing();
                    break;
                case State.Returning:
                    if (FollowPath())
                    {
                        _state = State.Resting;
                        _timer = 0f;
                        Rest();
                    }

                    break;
            }
        }

        private void OnDestroy()
        {
            if (_upgrades != null)
            {
                _upgrades.Changed -= OnUpgradesChanged;
            }
        }

        private void OnUpgradesChanged()
        {
            _figure.gameObject.SetActive(_upgrades.WaiterLevel > 0);
        }

        private bool TryBuildModel(GameObject root)
        {
            _animator = CustomerFactory.BuildCharacter(root, CustomerFactory.WaiterLetter, out var primitiveBody);
            if (primitiveBody == null)
            {
                return true;
            }

            // No model: drop the generic primitive and let the caller build the uniformed one.
            foreach (Transform child in root.transform)
            {
                Destroy(child.gameObject);
            }

            return false;
        }

        private void Walk()
        {
            if (_animator != null)
            {
                _animator.PlayWalk();
            }
        }

        private void Rest()
        {
            if (_animator != null)
            {
                _animator.PlayIdle();
            }
        }

        private void UpdateResting()
        {
            var interval = _upgrades.WaiterInterval;
            _timer += Time.deltaTime;
            if (_timer < interval)
            {
                return;
            }

            // Stay ready (timer full) until someone is waiting, then start a trip.
            _timer = interval;
            var customer = _spawner.FindWaitingCustomer();
            if (customer != null && customer.TryGetServicePoint(out var point))
            {
                _target = customer;
                PlanPath(_figure.position, point);
                _state = State.GoingToCustomer;
                Walk();
            }
        }

        private void UpdateGoing()
        {
            // The player may have served them first, or they ran out of patience.
            if (_target == null || !_target.IsWaitingForService)
            {
                StartReturning();
                return;
            }

            if (FollowPath())
            {
                _target.TryServe();
                StartReturning();
            }
        }

        private void StartReturning()
        {
            _target = null;
            PlanPath(_figure.position, _station);
            _state = State.Returning;
            Walk();
        }

        private void PlanPath(Vector3 from, Vector3 to)
        {
            _path.Clear();
            PathPlanner.Plan(from, to, _seating.Obstacles, _path);
            _pathIndex = 0;
        }

        private bool FollowPath()
        {
            if (_pathIndex >= _path.Count)
            {
                return true;
            }

            var target = _path[_pathIndex];
            FaceTowards(target);
            _figure.position = Vector3.MoveTowards(_figure.position, target, _walkSpeed * Time.deltaTime);

            if ((_figure.position - target).sqrMagnitude <= ArriveDistance * ArriveDistance)
            {
                _pathIndex++;
            }

            return _pathIndex >= _path.Count;
        }

        private void FaceTowards(Vector3 point)
        {
            var look = point - _figure.position;
            look.y = 0f;
            if (look.sqrMagnitude < MinLookSqrDistance)
            {
                return;
            }

            var rotation = Quaternion.LookRotation(look);
            _figure.rotation = Quaternion.RotateTowards(_figure.rotation, rotation, TurnSpeedDegrees * Time.deltaTime);
        }

        private GameObject BuildFigure(Vector3 position)
        {
            var root = new GameObject("Waiter");
            root.transform.SetParent(transform, false);
            root.transform.position = position;

            if (TryBuildModel(root))
            {
                return root;
            }

            PrimitiveFactory.Shape(PrimitiveType.Capsule, "Body", root.transform,
                new Vector3(0f, BodyHeight, 0f), BodyScale, UniformColor);
            PrimitiveFactory.Shape(PrimitiveType.Sphere, "Head", root.transform,
                new Vector3(0f, HeadHeight, 0f), HeadScale, SkinColor);
            return root;
        }
    }
}
