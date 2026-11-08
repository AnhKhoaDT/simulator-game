using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // A table with seat anchors. Tracks which seats are taken so customers never share one.
    public class BarTable : MonoBehaviour
    {
        [SerializeField] private Transform[] _seats = new Transform[0];
        [Tooltip("Half width of the area customers must walk around (table plus seats).")]
        [SerializeField, Min(0.5f)] private float _footprintHalfSize = 1.65f;
        [Tooltip("How far outside a seat the customer stands before sitting down.")]
        [SerializeField, Min(0.1f)] private float _approachDistance = 0.8f;

        [Tooltip("Height of the sitting surface above each seat anchor.")]
        [SerializeField] private float _seatSurfaceOffset;

        private bool[] _occupied;

        public IReadOnlyList<Transform> Seats => _seats;

        public Rect Footprint
        {
            get
            {
                var p = transform.position;
                return new Rect(p.x - _footprintHalfSize, p.z - _footprintHalfSize,
                    _footprintHalfSize * 2f, _footprintHalfSize * 2f);
            }
        }

        public void SetSeats(Transform[] seats, float seatSurfaceOffset)
        {
            _seats = seats;
            _seatSurfaceOffset = seatSurfaceOffset;
            _occupied = new bool[seats.Length];
        }

        public bool NearestFreeSeat(Vector3 from, out int index, out float sqrDistance)
        {
            EnsureOccupancy();
            index = -1;
            sqrDistance = float.MaxValue;

            for (var i = 0; i < _seats.Length; i++)
            {
                if (_occupied[i])
                {
                    continue;
                }

                var distance = (_seats[i].position - from).sqrMagnitude;
                if (distance < sqrDistance)
                {
                    sqrDistance = distance;
                    index = i;
                }
            }

            return index >= 0;
        }

        public void Reserve(int index)
        {
            EnsureOccupancy();
            _occupied[index] = true;
        }

        public void Release(int index)
        {
            EnsureOccupancy();
            _occupied[index] = false;
        }

        // Top surface of the seat, so a seated customer rests on it.
        public Vector3 GetSeatPosition(int index)
        {
            return _seats[index].position + Vector3.up * _seatSurfaceOffset;
        }

        public Vector3 GetApproachPoint(int index)
        {
            var center = transform.position;
            var offset = _seats[index].position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f)
            {
                offset = Vector3.back;
            }

            var point = center + offset.normalized * (offset.magnitude + _approachDistance);
            point.y = 0f;
            return point;
        }

        private void EnsureOccupancy()
        {
            if (_occupied == null || _occupied.Length != _seats.Length)
            {
                _occupied = new bool[_seats.Length];
            }
        }
    }
}
