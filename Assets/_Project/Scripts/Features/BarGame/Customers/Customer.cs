using System;
using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // One customer: walk in, sit, order, wait to be served, eat, pay, leave. Small state machine.
    public class Customer : MonoBehaviour
    {
        private const float ArriveDistance = 0.05f;
        private const float RetryInterval = 0.25f;
        private const float TurnSpeedDegrees = 540f;
        private const float MinLookSqrDistance = 0.0001f;
        private const float UrgentRemainingRatio = 0.3f;

        private static readonly Color UrgentColor = new Color(0.9f, 0.15f, 0.15f);

        private enum State
        {
            Entering,
            WaitingForSeat,
            WalkingToSeat,
            WaitingForService,
            Eating,
            Leaving
        }

        private readonly List<Vector3> _path = new List<Vector3>();

        private Renderer _body;
        private CharacterAnimator _animator;
        private Transform _bubble;
        private Renderer _bubbleRenderer;
        private BarSeating _seating;
        private OrderSystem _orders;
        private CustomerSettings _settings;
        private MenuItem _order;
        private Quaternion _bubbleRotation;
        private Vector3 _exitPoint;
        private BarTable _table;
        private int _seatIndex = -1;
        private int _pathIndex;
        private State _state;
        private float _timer;
        private float _retryTimer;
        private bool _urgent;

        public event Action<Customer> Finished;

        public bool IsWaitingForService => _state == State.WaitingForService;

        // body is null for character models (texture is baked in); animator is null for primitives.
        internal void Initialize(Renderer body, Transform bubble, Renderer bubbleRenderer, CharacterAnimator animator)
        {
            _body = body;
            _bubble = bubble;
            _bubbleRenderer = bubbleRenderer;
            _animator = animator;
        }

        internal void Begin(BarSeating seating, OrderSystem orders, CustomerSettings settings,
            Quaternion bubbleRotation, Vector3 spawnPoint, Vector3 waitPoint, Vector3 exitPoint, Color color)
        {
            _seating = seating;
            _orders = orders;
            _settings = settings;
            _bubbleRotation = bubbleRotation;
            _exitPoint = exitPoint;
            _table = null;
            _order = null;
            _seatIndex = -1;
            if (_body != null)
            {
                _body.sharedMaterial = PrimitiveFactory.GetMaterial(color);
            }

            _bubble.gameObject.SetActive(false);

            transform.position = spawnPoint;
            gameObject.SetActive(true);

            _path.Clear();
            _path.Add(waitPoint);
            _pathIndex = 0;
            SetState(State.Entering);
        }

        // Where a waiter should stand to serve this customer; false if they are not waiting at a seat.
        public bool TryGetServicePoint(out Vector3 point)
        {
            if (_state != State.WaitingForService || _table == null)
            {
                point = default;
                return false;
            }

            point = _table.GetApproachPoint(_seatIndex);
            return true;
        }

        // Returns true if this customer was waiting for service and is now being served.
        public bool TryServe()
        {
            if (_state != State.WaitingForService)
            {
                return false;
            }

            _bubble.gameObject.SetActive(false);
            _timer = 0f;
            SetState(State.Eating);
            _orders.NotifyServed();
            return true;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Entering:
                    if (FollowPath())
                    {
                        SetState(State.WaitingForSeat);
                        _timer = 0f;
                        _retryTimer = 0f;
                    }

                    break;
                case State.WaitingForSeat:
                    UpdateWaitingForSeat();
                    break;
                case State.WalkingToSeat:
                    if (FollowPath())
                    {
                        Sit();
                    }

                    break;
                case State.WaitingForService:
                    UpdateWaitingForService();
                    break;
                case State.Eating:
                    UpdateEating();
                    break;
                case State.Leaving:
                    if (FollowPath())
                    {
                        Finished?.Invoke(this);
                    }

                    break;
            }
        }

        private void UpdateWaitingForSeat()
        {
            _timer += Time.deltaTime;
            _retryTimer -= Time.deltaTime;

            if (_retryTimer <= 0f)
            {
                _retryTimer = RetryInterval;
                if (_seating.TryReserve(transform.position, out _table, out _seatIndex))
                {
                    WalkToSeat();
                    return;
                }
            }

            if (_timer >= _settings.WaitForSeatSeconds)
            {
                _orders.NotifyAbandoned();
                StartLeaving(null);
            }
        }

        private void WalkToSeat()
        {
            _path.Clear();
            PathPlanner.Plan(transform.position, _table.GetApproachPoint(_seatIndex), _seating.Obstacles, _path);
            _path.Add(_table.GetSeatPosition(_seatIndex));
            _pathIndex = 0;
            SetState(State.WalkingToSeat);
        }

        private void Sit()
        {
            FaceTowards(_table.transform.position);
            _order = _orders.PickItem();
            if (_order == null)
            {
                LeaveSeat();
                return;
            }

            ShowBubble();
            _timer = 0f;
            _urgent = false;
            SetState(State.WaitingForService);
        }

        private void UpdateWaitingForService()
        {
            _timer += Time.deltaTime;
            var wait = _orders.ServiceWaitSeconds;

            if (!_urgent && _timer >= wait * (1f - UrgentRemainingRatio))
            {
                _urgent = true;
                _bubbleRenderer.sharedMaterial = PrimitiveFactory.GetMaterial(UrgentColor);
            }

            // Out of patience: leave without paying.
            if (_timer >= wait)
            {
                _orders.NotifyAbandoned();
                LeaveSeat();
            }
        }

        private void UpdateEating()
        {
            _timer += Time.deltaTime;
            if (_timer < _order.EatSeconds)
            {
                return;
            }

            _orders.Complete(_order);
            LeaveSeat();
        }

        private void ShowBubble()
        {
            _bubble.rotation = _bubbleRotation;
            _bubbleRenderer.sharedMaterial = PrimitiveFactory.GetMaterial(_order.Color);
            _bubble.gameObject.SetActive(true);
        }

        private void LeaveSeat()
        {
            var approach = _table.GetApproachPoint(_seatIndex);
            _table.Release(_seatIndex);
            _table = null;
            _bubble.gameObject.SetActive(false);
            StartLeaving(approach);
        }

        // 'firstStep' is where to step off the seat to before heading for the door; null if already standing.
        private void StartLeaving(Vector3? firstStep)
        {
            _path.Clear();
            var from = transform.position;
            if (firstStep.HasValue)
            {
                _path.Add(firstStep.Value);
                from = firstStep.Value;
            }

            PathPlanner.Plan(from, _exitPoint, _seating.Obstacles, _path);
            _pathIndex = 0;
            SetState(State.Leaving);
        }

        private void SetState(State state)
        {
            _state = state;
            if (_animator == null)
            {
                return;
            }

            switch (state)
            {
                case State.Entering:
                case State.WalkingToSeat:
                case State.Leaving:
                    _animator.PlayWalk();
                    break;
                case State.WaitingForSeat:
                    _animator.PlayIdle();
                    break;
                case State.WaitingForService:
                    _animator.PlaySit();
                    break;
                case State.Eating:
                    _animator.PlayEat();
                    break;
            }
        }

        private bool FollowPath()
        {
            if (_pathIndex >= _path.Count)
            {
                return true;
            }

            var target = _path[_pathIndex];
            FaceTowards(target);
            transform.position = Vector3.MoveTowards(transform.position, target, _settings.WalkSpeed * Time.deltaTime);

            if ((transform.position - target).sqrMagnitude <= ArriveDistance * ArriveDistance)
            {
                _pathIndex++;
            }

            return _pathIndex >= _path.Count;
        }

        private void FaceTowards(Vector3 point)
        {
            var look = point - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude < MinLookSqrDistance)
            {
                return;
            }

            var rotation = Quaternion.LookRotation(look);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, TurnSpeedDegrees * Time.deltaTime);
        }
    }
}
