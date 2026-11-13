using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // While the bar is open, sends passers-by in from the street on a timer; recycles them through a pool.
    public class CustomerSpawner : MonoBehaviour
    {
        private const float IntervalJitter = 0.25f;
        private const float MinRate = 0.05f;
        // Keeps guests in the middle of the sidewalk, clear of trees at the curb and the dumpster by the wall.
        private const float LaneSpread = 0.5f;
        private const float WaitSpreadX = 0.4f;
        private const float WaitInside = 0.9f;
        private const float GroupSpacing = 0.8f;
        private const float GroupLaneJitter = 0.2f;
        // Guests appear part-way down the street so they do not take ages to arrive.
        private const float SpawnDistanceRatio = 0.6f;

        private static readonly Color[] Palette =
        {
            new Color(0.85f, 0.3f, 0.3f),
            new Color(0.3f, 0.6f, 0.85f),
            new Color(0.4f, 0.75f, 0.4f),
            new Color(0.9f, 0.75f, 0.3f),
            new Color(0.7f, 0.45f, 0.8f)
        };

        private readonly Stack<Customer> _pool = new Stack<Customer>();
        private readonly List<Customer> _active = new List<Customer>();

        private CustomerContext _context;
        private ShiftClock _clock;
        private float _timer;
        private float _nextInterval;
        private bool _ready;

        public void Setup(CustomerContext context, ShiftClock clock)
        {
            _context = context;
            _clock = clock;
            _nextInterval = NextInterval();
            _ready = true;
        }

        // Serves every ordering customer within radius. Used by the owner walking around.
        public void ServeNear(Vector3 position, float radius)
        {
            for (var i = 0; i < _active.Count; i++)
            {
                var customer = _active[i];
                var offset = customer.transform.position - position;
                offset.y = 0f;
                if (customer.IsWaitingForService && offset.sqrMagnitude <= radius * radius)
                {
                    customer.TryServe();
                }
            }
        }

        // First customer waiting for service, or null. Used by the hired waiter.
        public Customer FindWaitingCustomer()
        {
            for (var i = 0; i < _active.Count; i++)
            {
                if (_active[i].IsWaitingForService && !_active[i].IsClaimed)
                {
                    return _active[i];
                }
            }

            return null;
        }

        private void Update()
        {
            // Closing only stops new arrivals; guests already inside finish normally.
            if (!_ready || !_clock.IsOpen)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < _nextInterval || _active.Count >= _context.Settings.MaxActive)
            {
                return;
            }

            _timer = 0f;
            _nextInterval = NextInterval();
            Spawn();
        }

        // One walk-in, or a group of friends who sit together.
        private void Spawn()
        {
            var layout = _context.Layout;
            var size = PickGroupSize();
            var party = size > 1 ? new Party() : null;
            var side = RandomSide();
            var lane = layout.SidewalkZ + Random.Range(-LaneSpread, LaneSpread);
            var exitX = RandomSide() * BarLayout.StreetHalfLength;

            for (var i = 0; i < size; i++)
            {
                var customer = _pool.Count > 0 ? _pool.Pop() : CreateCustomer();
                var x = side * BarLayout.StreetHalfLength * SpawnDistanceRatio - side * i * GroupSpacing;
                var spawn = new Vector3(x, 0f, lane + Random.Range(-GroupLaneJitter, GroupLaneJitter));
                var wait = layout.DoorInside + new Vector3(Random.Range(-WaitSpreadX, WaitSpreadX), 0f, WaitInside);
                party?.Add(customer);
                customer.Begin(_context, spawn, wait, exitX, Palette[Random.Range(0, Palette.Length)], party);
                _active.Add(customer);
            }
        }

        // Groups need a table that can seat them all, and room under the active-guest limit.
        private int PickGroupSize()
        {
            var settings = _context.Settings;
            var room = settings.MaxActive - _active.Count;
            var limit = Mathf.Min(settings.MaxGroupSize, _context.Seating.MaxSeatsAtOneTable, room);
            if (limit < 2 || Random.value >= settings.GroupChance)
            {
                return 1;
            }

            return Random.Range(2, limit + 1);
        }

        private Customer CreateCustomer()
        {
            var customer = CustomerFactory.Create(transform);
            customer.Finished += Recycle;
            return customer;
        }

        private void Recycle(Customer customer)
        {
            customer.gameObject.SetActive(false);
            _active.Remove(customer);
            _pool.Push(customer);
        }

        private float NextInterval()
        {
            var rate = Mathf.Max(MinRate, _clock != null ? _clock.SpawnRate : 1f);
            return _context.Settings.SpawnIntervalSeconds / rate * Random.Range(1f - IntervalJitter, 1f + IntervalJitter);
        }

        private static float RandomSide()
        {
            return Random.value < 0.5f ? -1f : 1f;
        }
    }
}
