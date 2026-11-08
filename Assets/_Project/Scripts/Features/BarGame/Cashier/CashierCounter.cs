using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Pay counter: customers queue in front of it and pay one by one while someone stands behind it.
    public class CashierCounter : MonoBehaviour
    {
        private const float PaySeconds = 0.6f;
        private const float SpotSpacing = 0.8f;
        private const float AttendRadius = 0.9f;
        private const float TopThickness = 0.08f;
        private const float MarkerSize = 0.9f;
        private const float MarkerHeight = 0.02f;
        private const float RegisterHeight = 0.35f;
        // Kenney models face -Z; the cashier stands on the +X side, so turn the screen toward +X.
        private const float RegisterYaw = -90f;

        private static readonly Color BodyColor = new Color(0.36f, 0.24f, 0.15f);
        private static readonly Color TopColor = new Color(0.62f, 0.45f, 0.3f);
        private static readonly Color RegisterColor = new Color(0.15f, 0.16f, 0.18f);
        private static readonly Color MarkerIdleColor = new Color(0.95f, 0.75f, 0.2f);
        private static readonly Color MarkerActiveColor = new Color(0.3f, 0.85f, 0.4f);

        private readonly List<Customer> _queue = new List<Customer>();
        private readonly List<Transform> _attendants = new List<Transform>();

        private Vector3 _cashierSpot;
        private Vector3 _firstSpot;
        private Vector3 _queueDirection;
        private Renderer _marker;
        private bool _attended;
        private float _timer;

        public Rect Footprint { get; private set; }

        public void Setup(BarLayout layout)
        {
            _cashierSpot = layout.CashierSpot;
            _firstSpot = layout.QueueFirstSpot;
            _queueDirection = layout.QueueDirection;
            Footprint = layout.CounterFootprint;
            Build(layout.CounterCenter, layout.CounterSize);
        }

        // Anyone standing behind the counter (the owner now, a hired cashier later) takes payment.
        public void AddAttendant(Transform attendant)
        {
            if (!_attendants.Contains(attendant))
            {
                _attendants.Add(attendant);
            }
        }

        public void Join(Customer customer)
        {
            if (!_queue.Contains(customer))
            {
                _queue.Add(customer);
            }
        }

        public void Leave(Customer customer)
        {
            var index = _queue.IndexOf(customer);
            if (index < 0)
            {
                return;
            }

            _queue.RemoveAt(index);
            if (index == 0)
            {
                _timer = 0f;
            }
        }

        // Where this customer should stand; customers not yet in the queue get the next free spot.
        public Vector3 GetSpot(Customer customer)
        {
            var index = _queue.IndexOf(customer);
            if (index < 0)
            {
                index = _queue.Count;
            }

            return _firstSpot + _queueDirection * (SpotSpacing * index);
        }

        private void Update()
        {
            SetAttended(IsAttended());
            if (!_attended || _queue.Count == 0 || !_queue[0].IsReadyToPay)
            {
                _timer = 0f;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= PaySeconds)
            {
                _timer = 0f;
                _queue[0].Pay();
            }
        }

        private bool IsAttended()
        {
            for (var i = 0; i < _attendants.Count; i++)
            {
                var offset = _attendants[i].position - _cashierSpot;
                offset.y = 0f;
                if (offset.sqrMagnitude <= AttendRadius * AttendRadius)
                {
                    return true;
                }
            }

            return false;
        }

        // The floor marker turns green while someone is collecting, so the player sees where to stand.
        private void SetAttended(bool attended)
        {
            if (attended == _attended)
            {
                return;
            }

            _attended = attended;
            _marker.sharedMaterial = PrimitiveFactory.GetMaterial(attended ? MarkerActiveColor : MarkerIdleColor);
        }

        // The Mini Shop cash register on the counter top, facing the cashier; a dark box if the pack is missing.
        private void BuildRegister(Vector3 topPosition)
        {
            var register = ModelLoader.Instantiate(ModelCatalog.Shop + "cash-register", transform, "CashRegister");
            if (register == null)
            {
                PrimitiveFactory.Box("Register", transform, topPosition + Vector3.up * 0.12f,
                    new Vector3(0.35f, 0.24f, 0.3f), RegisterColor);
                return;
            }

            register.transform.SetPositionAndRotation(topPosition, Quaternion.Euler(0f, RegisterYaw, 0f));
            ModelLoader.FitToHeight(register, RegisterHeight);
        }

        private void Build(Vector3 center, Vector3 size)
        {
            var bodyHeight = size.y - TopThickness;
            PrimitiveFactory.Box("CounterBody", transform, center + Vector3.up * (bodyHeight * 0.5f),
                new Vector3(size.x, bodyHeight, size.z), BodyColor);
            PrimitiveFactory.Box("CounterTop", transform, center + Vector3.up * (size.y - TopThickness * 0.5f),
                new Vector3(size.x + 0.1f, TopThickness, size.z + 0.1f), TopColor);
            BuildRegister(center + new Vector3(0.1f, size.y, 0.4f));

            var marker = PrimitiveFactory.Shape(PrimitiveType.Cylinder, "CashierMarker", transform,
                _cashierSpot + Vector3.up * MarkerHeight, new Vector3(MarkerSize, MarkerHeight, MarkerSize), MarkerIdleColor);
            Destroy(marker.GetComponent<Collider>());
            _marker = marker.GetComponent<Renderer>();
        }
    }
}
