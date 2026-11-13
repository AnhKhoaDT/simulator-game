using System;
using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // One customer: walk in from the street, sit, order, eat, pay at the counter, walk away. Small state machine.
    public class Customer : MonoBehaviour
    {
        private const float ArriveDistance = 0.05f;
        private const float QueueArriveDistance = 0.15f;
        private const float RetryInterval = 0.25f;
        private const float TurnSpeedDegrees = 540f;
        private const float MinLookSqrDistance = 0.0001f;
        private const float FoodSize = 0.3f;
        private const float HandCashSeconds = 0.9f;

        private enum State
        {
            Entering,
            WaitingForSeat,
            WalkingToSeat,
            Ordering,
            Eating,
            WalkingToCashier,
            WaitingToPay,
            HandingCash,
            AwaitingChange,
            Leaving
        }

        private readonly List<Vector3> _path = new List<Vector3>();
        private readonly Dictionary<ItemShape, GameObject> _food = new Dictionary<ItemShape, GameObject>();

        private Renderer _body;
        private CustomerBubble _bubble;
        private CharacterAnimator _animator;
        private CustomerContext _context;
        private readonly List<MenuItem> _items = new List<MenuItem>();
        private Party _party;
        private GameObject _servedFood;
        private BarTable _table;
        private BarChair _seat;
        private Action<Customer> _cashReady;
        private int _pathIndex;
        private float _laneZ;
        private float _exitX;
        private State _state;
        private float _timer;
        private float _retryTimer;
        private float _eatSeconds;

        public event Action<Customer> Finished;

        public bool IsWaitingForService => _state == State.Ordering;

        // What this customer owes, and the cash they hand over once they have the bill.
        internal int Bill
        {
            get
            {
                var total = 0;
                foreach (var item in _items)
                {
                    total += item.Price;
                }

                return total;
            }
        }

        internal IReadOnlyList<MenuItem> Items => _items;

        // A waiter has set off for this customer, so other waiters pick someone else.
        internal bool IsClaimed { get; set; }

        internal int Cash { get; private set; }

        internal bool IsReadyToPay =>
            _state == State.WaitingToPay
            && FlatSqrDistance(transform.position, _context.Counter.GetSpot(this)) <= QueueArriveDistance * QueueArriveDistance;

        // body is null for character models (texture is baked in); animator is null for primitives.
        internal void Initialize(Renderer body, CustomerBubble bubble, CharacterAnimator animator)
        {
            _body = body;
            _bubble = bubble;
            _animator = animator;
        }

        internal void Begin(CustomerContext context, Vector3 spawnPoint, Vector3 waitPoint, float exitX, Color color,
            Party party = null)
        {
            _context = context;
            _table = null;
            _items.Clear();
            _party = party;
            IsClaimed = false;
            _seat = null;
            _cashReady = null;
            Cash = 0;
            _laneZ = spawnPoint.z;
            _exitX = exitX;

            if (_body != null)
            {
                _body.sharedMaterial = PrimitiveFactory.GetMaterial(color);
            }

            _bubble.Hide();
            HideFood();

            transform.position = spawnPoint;
            gameObject.SetActive(true);

            var layout = context.Layout;
            _path.Clear();
            _path.Add(layout.SidewalkPoint(layout.DoorX, _laneZ));
            _path.Add(layout.DoorOutside);
            _path.Add(layout.DoorInside);
            _path.Add(waitPoint);
            _pathIndex = 0;
            SetState(State.Entering);
        }

        // Where a waiter should stand to serve this customer; false if they are not ordering at a seat.
        public bool TryGetServicePoint(out Vector3 point)
        {
            if (_state != State.Ordering || _table == null)
            {
                point = default;
                return false;
            }

            point = _table.GetApproachPoint(_seat);
            return true;
        }

        // Returns true if this customer was ordering and is now served (food appears on the table).
        public bool TryServe()
        {
            if (_state != State.Ordering)
            {
                return false;
            }

            _bubble.Hide();
            ShowFood();
            IsClaimed = false;
            _eatSeconds = EatSeconds() * UnityEngine.Random.Range(0.85f, 1.2f);
            _timer = 0f;
            SetState(State.Eating);
            _context.Orders.NotifyServed();
            return true;
        }

        // The owner hands over the bill; after a moment the customer holds out cash and 'cashReady' fires.
        internal void ReceiveBill(Action<Customer> cashReady)
        {
            if (_state != State.WaitingToPay)
            {
                return;
            }

            _cashReady = cashReady;
            _timer = 0f;
            _bubble.Hide();
            SetState(State.HandingCash);
        }

        // The owner gave the change: the customer pays and walks out.
        internal void CompletePayment()
        {
            if (_state != State.AwaitingChange)
            {
                return;
            }

            _context.Counter.Leave(this);
            _context.Orders.CompleteOrder(_items);
            _bubble.Hide();
            StartLeaving(null);
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Entering:
                    if (FollowPath())
                    {
                        _timer = 0f;
                        _retryTimer = 0f;
                        SetState(State.WaitingForSeat);
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
                case State.Ordering:
                    UpdateOrdering();
                    break;
                case State.Eating:
                    UpdateEating();
                    break;
                case State.WalkingToCashier:
                    if (FollowPath())
                    {
                        _timer = 0f;
                        SetState(State.WaitingToPay);
                    }

                    break;
                case State.WaitingToPay:
                    UpdateWaitingToPay();
                    break;
                case State.HandingCash:
                    UpdateHandingCash();
                    break;
                case State.AwaitingChange:
                    FaceTowards(_context.Layout.CounterCenter);
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
                if (TryGetSeat())
                {
                    WalkToSeat();
                    return;
                }
            }

            if (_timer >= _context.Settings.WaitForSeatSeconds)
            {
                _party?.Abandon(this);
                _context.Orders.NotifyAbandoned();
                StartLeaving(null);
            }
        }

        // A lone guest takes the nearest free chair; a group waits until one table can seat all of them.
        private bool TryGetSeat()
        {
            if (_party == null)
            {
                return _context.Seating.TryReserve(transform.position, out _table, out _seat);
            }

            if (!_party.TryAssign(_context.Seating, transform.position))
            {
                return false;
            }

            _table = _party.Table;
            _seat = _party.Claim(this);
            return _seat != null;
        }

        private void WalkToSeat()
        {
            _path.Clear();
            PathPlanner.Plan(transform.position, _table.GetApproachPoint(_seat), _context.Seating.Obstacles, _path);
            _path.Add(SeatPose.BodyPosition(_seat));
            _pathIndex = 0;
            SetState(State.WalkingToSeat);
        }

        private void Sit()
        {
            FaceTowards(_table.transform.position);
            if (!PickItems())
            {
                StandUpAndLeave();
                return;
            }

            _bubble.ShowOrder(_items[0].Shape);
            _timer = 0f;
            SetState(State.Ordering);
        }

        // Most guests order one or two dishes, a few order the full limit.
        private bool PickItems()
        {
            var count = UnityEngine.Random.value < 0.5f ? 1 : UnityEngine.Random.Range(1, _context.Settings.MaxItemsPerGuest + 1);
            for (var i = 0; i < count; i++)
            {
                var item = _context.Orders.PickItem();
                if (item == null)
                {
                    break;
                }

                _items.Add(item);
            }

            return _items.Count > 0;
        }

        private float EatSeconds()
        {
            var total = 0f;
            foreach (var item in _items)
            {
                total += item.EatSeconds;
            }

            return total * _context.Settings.EatTimeMultiplier;
        }

        private void UpdateOrdering()
        {
            _timer += Time.deltaTime;
            var wait = _context.Orders.ServiceWaitSeconds;
            _bubble.SetPatience(1f - _timer / wait);

            // Out of patience: leave without paying.
            if (_timer >= wait)
            {
                _context.Orders.NotifyAbandoned();
                StandUpAndLeave();
            }
        }

        private void UpdateEating()
        {
            _timer += Time.deltaTime;
            if (_timer >= _eatSeconds)
            {
                GoToCashier();
            }
        }

        private void GoToCashier()
        {
            HideFood();
            var approach = StandUp();
            var counter = _context.Counter;
            counter.Join(this);
            _bubble.ShowPay();

            _path.Clear();
            _path.Add(approach);
            PathPlanner.Plan(approach, counter.GetSpot(this), _context.Seating.Obstacles, _path);
            _pathIndex = 0;
            SetState(State.WalkingToCashier);
        }

        // The queue moves forward as others pay, so keep stepping toward the current spot.
        private void UpdateWaitingToPay()
        {
            var counter = _context.Counter;
            var spot = counter.GetSpot(this);
            if (FlatSqrDistance(transform.position, spot) > QueueArriveDistance * QueueArriveDistance)
            {
                FaceTowards(spot);
                transform.position = Vector3.MoveTowards(transform.position, spot, _context.Settings.WalkSpeed * Time.deltaTime);
            }
            else
            {
                FaceTowards(_context.Layout.CounterCenter);
            }

            _timer += Time.deltaTime;
            var wait = _context.Settings.WaitToPaySeconds;
            _bubble.SetPatience(1f - _timer / wait);

            // Nobody came to the counter in time: they walk out without paying.
            if (_timer >= wait)
            {
                counter.Leave(this);
                _context.Orders.NotifyAbandoned();
                _bubble.Hide();
                StartLeaving(null);
            }
        }

        private void UpdateHandingCash()
        {
            FaceTowards(_context.Layout.CounterCenter);
            _timer += Time.deltaTime;
            if (_timer < HandCashSeconds)
            {
                return;
            }

            Cash = CashDenominations.CustomerCash(Bill);
            SetState(State.AwaitingChange);
            _cashReady?.Invoke(this);
        }

        private Vector3 StandUp()
        {
            var approach = _table.GetApproachPoint(_seat);
            _seat.Release();
            _seat = null;
            _table = null;
            return approach;
        }

        private void StandUpAndLeave()
        {
            _bubble.Hide();
            HideFood();
            StartLeaving(StandUp());
        }

        // Back out through the door and along the sidewalk; 'firstStep' is where to step off a seat first.
        private void StartLeaving(Vector3? firstStep)
        {
            var layout = _context.Layout;
            _path.Clear();
            var from = transform.position;
            if (firstStep.HasValue)
            {
                _path.Add(firstStep.Value);
                from = firstStep.Value;
            }

            PathPlanner.Plan(from, layout.DoorInside, _context.Seating.Obstacles, _path);
            _path.Add(layout.DoorOutside);
            _path.Add(layout.SidewalkPoint(layout.DoorX, _laneZ));
            _path.Add(layout.SidewalkPoint(_exitX, _laneZ));
            _pathIndex = 0;
            SetState(State.Leaving);
        }

        private void ShowFood()
        {
            var shape = _items[0].Shape;
            if (!_food.TryGetValue(shape, out var food))
            {
                food = ItemVisuals.Build(shape, transform, FoodSize);
                _food[shape] = food;
            }

            food.transform.SetPositionAndRotation(_table.GetFoodPoint(_seat), Quaternion.identity);
            food.SetActive(true);
            _servedFood = food;
        }

        private void HideFood()
        {
            if (_servedFood != null)
            {
                _servedFood.SetActive(false);
                _servedFood = null;
            }
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
                case State.WalkingToCashier:
                case State.Leaving:
                    _animator.PlayWalk();
                    break;
                case State.WaitingForSeat:
                case State.WaitingToPay:
                case State.HandingCash:
                case State.AwaitingChange:
                    _animator.PlayIdle();
                    break;
                case State.Ordering:
                case State.Eating:
                    _animator.PlaySit();
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
            transform.position = Vector3.MoveTowards(transform.position, target, _context.Settings.WalkSpeed * Time.deltaTime);

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

        private static float FlatSqrDistance(Vector3 a, Vector3 b)
        {
            var offset = a - b;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }
    }
}
