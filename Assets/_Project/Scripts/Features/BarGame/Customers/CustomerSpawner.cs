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
                if (_active[i].IsWaitingForService)
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

        private void Spawn()
        {
            var layout = _context.Layout;
            var customer = _pool.Count > 0 ? _pool.Pop() : CreateCustomer();

            var lane = layout.SidewalkZ + Random.Range(-LaneSpread, LaneSpread);
            var spawn = new Vector3(RandomSide() * BarLayout.StreetHalfLength * SpawnDistanceRatio, 0f, lane);
            var wait = layout.DoorInside + new Vector3(Random.Range(-WaitSpreadX, WaitSpreadX), 0f, WaitInside);
            var exitX = RandomSide() * BarLayout.StreetHalfLength;

            customer.Begin(_context, spawn, wait, exitX, Palette[Random.Range(0, Palette.Length)]);
            _active.Add(customer);
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
