using System.Collections.Generic;
using Startup.Core.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // Brings the hired waiters to life: one figure per hired waiter. A working waiter stands at the staff corner,
    // walks to a waiting customer, serves them, and walks back. A resting waiter sits on a free bench or sofa.
    public class WaiterService : MonoBehaviour
    {
        private const float BodyHeight = 0.45f;
        private const float HeadHeight = 1.05f;
        private const float ArriveDistance = 0.05f;
        private const float TurnSpeedDegrees = 540f;
        private const float MinLookSqrDistance = 0.0001f;
        private const float RestInterval = 1.5f;
        private const float NameTagHeight = 1.95f;
        private const float HomeSpacing = 0.9f;
        private const int HomeColumns = 4;
        private const float StandOffset = 0.7f;
        private const float PickHeight = 1.7f;
        private const float PickRadius = 0.4f;

        private static readonly Vector3 BodyScale = new Vector3(0.5f, 0.45f, 0.5f);
        private static readonly Vector3 HeadScale = new Vector3(0.35f, 0.35f, 0.35f);
        private static readonly Color UniformColor = new Color(0.15f, 0.2f, 0.45f);
        private static readonly Color SkinColor = new Color(0.95f, 0.8f, 0.65f);

        private readonly List<Agent> _agents = new List<Agent>();

        private CustomerSpawner _spawner;
        private BarSeating _seating;
        private BarFurniture _furniture;
        private BarUpgrades _upgrades;
        private NameTagLayer _tags;
        private Vector3 _station;
        private float _walkSpeed;

        public void Setup(CustomerSpawner spawner, BarSeating seating, BarFurniture furniture, BarUpgrades upgrades,
            float walkSpeed, Vector3 stationPoint, NameTagLayer tags)
        {
            _spawner = spawner;
            _seating = seating;
            _furniture = furniture;
            _upgrades = upgrades;
            _walkSpeed = walkSpeed;
            _station = stationPoint;
            _tags = tags;
            upgrades.Changed += Sync;
            LocalizationService.LanguageChanged += RefreshNames;
            Sync();
        }

        private void Update()
        {
            if (_upgrades == null)
            {
                return;
            }

            foreach (var agent in _agents)
            {
                agent.Tick();
            }
        }

        private void OnDestroy()
        {
            LocalizationService.LanguageChanged -= RefreshNames;
            if (_upgrades != null)
            {
                _upgrades.Changed -= Sync;
            }
        }

        // Matches the figures to the hired list: new hires appear, dismissed ones leave, names stay current.
        private void Sync()
        {
            var records = _upgrades.Waiters;
            for (var i = _agents.Count - 1; i >= 0; i--)
            {
                if (!ContainsRecord(records, _agents[i].Record))
                {
                    _agents[i].Dispose();
                    _agents.RemoveAt(i);
                }
            }

            foreach (var record in records)
            {
                if (FindAgent(record) == null)
                {
                    _agents.Add(new Agent(this, record));
                }
            }

            for (var i = 0; i < _agents.Count; i++)
            {
                _agents[i].Home = HomeSpot(i);
            }

            RefreshNames();
        }

        private void RefreshNames()
        {
            for (var i = 0; i < _agents.Count; i++)
            {
                _agents[i].Tag.text = DisplayName(_agents[i].Record, i);
            }
        }

        private string DisplayName(WaiterRecord record, int index)
        {
            return string.IsNullOrWhiteSpace(record.Name)
                ? LocalizationService.GetText(LocKeys.StaffWaiterDefault) + " " + (index + 1)
                : record.Name;
        }

        // Each waiter stands on their own spot in the staff corner, filling rows toward the middle of the room.
        private Vector3 HomeSpot(int index)
        {
            return _station + new Vector3(index % HomeColumns * HomeSpacing, 0f, -(index / HomeColumns) * HomeSpacing);
        }

        private Agent FindAgent(WaiterRecord record)
        {
            foreach (var agent in _agents)
            {
                if (agent.Record == record)
                {
                    return agent;
                }
            }

            return null;
        }

        private static bool ContainsRecord(IReadOnlyList<WaiterRecord> records, WaiterRecord record)
        {
            for (var i = 0; i < records.Count; i++)
            {
                if (records[i] == record)
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject BuildFigure(Vector3 position)
        {
            var root = new GameObject("Waiter");
            root.transform.SetParent(transform, false);
            root.transform.position = position;

            var animator = CustomerFactory.BuildCharacter(root, CustomerFactory.WaiterLetter, out var primitiveBody);
            if (primitiveBody != null)
            {
                // No model: drop the generic primitive and build a uniformed one.
                foreach (Transform child in root.transform)
                {
                    Destroy(child.gameObject);
                }

                PrimitiveFactory.Shape(PrimitiveType.Capsule, "Body", root.transform,
                    new Vector3(0f, BodyHeight, 0f), BodyScale, UniformColor);
                PrimitiveFactory.Shape(PrimitiveType.Sphere, "Head", root.transform,
                    new Vector3(0f, HeadHeight, 0f), HeadScale, SkinColor);
                animator = null;
            }

            AddPickVolume(root.transform);
            _figureAnimator = animator;
            return root;
        }

        // The owner aims at this to manage the waiter (rest, work, rename, dismiss).
        private static void AddPickVolume(Transform figure)
        {
            var pick = new GameObject("PickVolume") { layer = BarFurniture.FurnitureLayer };
            pick.transform.SetParent(figure, false);
            var capsule = pick.AddComponent<CapsuleCollider>();
            capsule.height = PickHeight;
            capsule.radius = PickRadius;
            capsule.center = new Vector3(0f, PickHeight * 0.5f, 0f);
        }

        // Handed from BuildFigure to the agent that asked for it.
        private CharacterAnimator _figureAnimator;

        // One waiter's figure and behaviour.
        private sealed class Agent
        {
            private enum State
            {
                Idle,
                GoingToCustomer,
                Returning,
                GoingToSeat,
                Sitting
            }

            private readonly WaiterService _service;
            private readonly List<Vector3> _path = new List<Vector3>();
            private readonly Transform _figure;
            private readonly CharacterAnimator _animator;

            private Customer _target;
            private StaffSeat _seat;
            private int _pathIndex;
            private float _timer;
            private State _state;

            public Agent(WaiterService service, WaiterRecord record)
            {
                _service = service;
                Record = record;
                Home = service._station;
                _figure = service.BuildFigure(Home).transform;
                _animator = service._figureAnimator;
                var handle = _figure.GetComponentInChildren<StaffHandle>(true);
                if (handle == null)
                {
                    handle = _figure.Find("PickVolume").gameObject.AddComponent<StaffHandle>();
                }

                handle.Record = record;
                Tag = service._tags.Add(_figure, NameTagHeight, string.Empty);
            }

            public WaiterRecord Record { get; }

            public Vector3 Home { get; set; }

            public Label Tag { get; }

            public void Tick()
            {
                if (Record.Resting)
                {
                    TickResting();
                }
                else
                {
                    TickWorking();
                }
            }

            public void Dispose()
            {
                ReleaseTarget();
                _seat?.Vacate();
                _service._tags.Remove(Tag);
                Destroy(_figure.gameObject);
            }

            private void TickWorking()
            {
                switch (_state)
                {
                    case State.Sitting:
                        StandUp();
                        break;
                    case State.GoingToSeat:
                        _seat?.Vacate();
                        _seat = null;
                        GoHome();
                        break;
                    case State.Idle:
                        UpdateIdle();
                        break;
                    case State.GoingToCustomer:
                        UpdateGoing();
                        break;
                    case State.Returning:
                        if (FollowPath())
                        {
                            _state = State.Idle;
                            _timer = 0f;
                            _animator?.PlayIdle();
                        }

                        break;
                }
            }

            private void TickResting()
            {
                switch (_state)
                {
                    case State.Sitting:
                        break;
                    case State.GoingToSeat:
                        if (FollowPath())
                        {
                            Sit();
                        }

                        break;
                    case State.GoingToCustomer:
                        ReleaseTarget();
                        GoHome();
                        break;
                    default:
                        TryGoToSeat();
                        break;
                }
            }

            private void UpdateIdle()
            {
                _timer += Time.deltaTime;
                if (_timer < RestInterval)
                {
                    return;
                }

                // Stay ready (timer full) until someone is waiting, then start a trip.
                _timer = RestInterval;
                var customer = _service._spawner.FindWaitingCustomer();
                if (customer != null && customer.TryGetServicePoint(out var point))
                {
                    _target = customer;
                    customer.IsClaimed = true;
                    PlanPath(_figure.position, point);
                    _state = State.GoingToCustomer;
                    _animator?.PlayWalk();
                }
            }

            private void UpdateGoing()
            {
                // The owner may have served them first, or they ran out of patience.
                if (_target == null || !_target.IsWaitingForService)
                {
                    ReleaseTarget();
                    GoHome();
                    return;
                }

                if (FollowPath())
                {
                    _target.TryServe();
                    ReleaseTarget();
                    GoHome();
                }
            }

            private void ReleaseTarget()
            {
                if (_target != null)
                {
                    _target.IsClaimed = false;
                    _target = null;
                }
            }

            private void GoHome()
            {
                PlanPath(_figure.position, Home);
                _state = State.Returning;
                _animator?.PlayWalk();
            }

            // A resting waiter heads for the nearest free seat; with none free they simply wait at their spot.
            private void TryGoToSeat()
            {
                var seat = _service._furniture.FindFreeStaffSeat(_figure.position);
                if (seat == null)
                {
                    if (_state != State.Idle)
                    {
                        if (FollowPath())
                        {
                            _state = State.Idle;
                            _animator?.PlayIdle();
                        }
                    }
                    else if ((_figure.position - Home).sqrMagnitude > ArriveDistance)
                    {
                        GoHome();
                    }

                    return;
                }

                _seat = seat;
                seat.Occupy();
                var front = seat.SitPoint + seat.SitRotation * Vector3.forward * StandOffset;
                front.y = 0f;
                PlanPath(_figure.position, front);
                _state = State.GoingToSeat;
                _animator?.PlayWalk();
            }

            private void Sit()
            {
                if (_seat == null)
                {
                    GoHome();
                    return;
                }

                _figure.SetPositionAndRotation(SeatPose.BodyPosition(_seat), _seat.SitRotation);
                _state = State.Sitting;
                _animator?.PlaySit();
            }

            private void StandUp()
            {
                if (_seat != null)
                {
                    var front = _seat.SitPoint + _seat.SitRotation * Vector3.forward * StandOffset;
                    front.y = 0f;
                    _figure.position = front;
                    _seat.Vacate();
                    _seat = null;
                }

                GoHome();
            }

            private void PlanPath(Vector3 from, Vector3 to)
            {
                _path.Clear();
                PathPlanner.Plan(from, to, _service._seating.Obstacles, _path);
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
                _figure.position = Vector3.MoveTowards(_figure.position, target, _service._walkSpeed * Time.deltaTime);
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
        }
    }
}
