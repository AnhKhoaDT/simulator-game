using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Spawns customers on a timer at the door and recycles them through a pool.
    public class CustomerSpawner : MonoBehaviour
    {
        private const float IntervalJitter = 0.2f;
        private const float WaitSpreadX = 0.4f;
        private const float WaitSpreadZ = 0.3f;

        private static readonly Color[] Palette =
        {
            new Color(0.85f, 0.3f, 0.3f),
            new Color(0.3f, 0.6f, 0.85f),
            new Color(0.4f, 0.75f, 0.4f),
            new Color(0.9f, 0.75f, 0.3f),
            new Color(0.7f, 0.45f, 0.8f)
        };

        private readonly Stack<Customer> _pool = new Stack<Customer>();
        private readonly List<Customer> _activeCustomers = new List<Customer>();

        private BarSeating _seating;
        private OrderSystem _orders;
        private CustomerSettings _settings;
        private Quaternion _bubbleRotation;
        private Vector3 _spawnPoint;
        private Vector3 _waitPoint;
        private int _active;
        private float _timer;
        private float _nextInterval;
        private bool _ready;

        public void Setup(BarSeating seating, OrderSystem orders, CustomerSettings settings,
            Quaternion bubbleRotation, Vector3 spawnPoint, Vector3 waitPoint)
        {
            _seating = seating;
            _orders = orders;
            _settings = settings;
            _bubbleRotation = bubbleRotation;
            _spawnPoint = spawnPoint;
            _waitPoint = waitPoint;
            _nextInterval = NextInterval();
            _ready = true;
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < _nextInterval || _active >= _settings.MaxActive)
            {
                return;
            }

            _timer = 0f;
            _nextInterval = NextInterval();
            Spawn();
        }

        private void Spawn()
        {
            var customer = _pool.Count > 0 ? _pool.Pop() : CreateCustomer();
            var color = Palette[Random.Range(0, Palette.Length)];
            var jitter = new Vector3(Random.Range(-WaitSpreadX, WaitSpreadX), 0f, Random.Range(-WaitSpreadZ, WaitSpreadZ));

            customer.Begin(_seating, _orders, _settings, _bubbleRotation,
                _spawnPoint, _waitPoint + jitter, _spawnPoint, color);
            _activeCustomers.Add(customer);
            _active++;
        }

        // First customer waiting for service, or null. Used by the hired waiter.
        public Customer FindWaitingCustomer()
        {
            for (var i = 0; i < _activeCustomers.Count; i++)
            {
                if (_activeCustomers[i].IsWaitingForService)
                {
                    return _activeCustomers[i];
                }
            }

            return null;
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
            _activeCustomers.Remove(customer);
            _pool.Push(customer);
            _active--;
        }

        private float NextInterval()
        {
            return _settings.SpawnIntervalSeconds * Random.Range(1f - IntervalJitter, 1f + IntervalJitter);
        }
    }
}
