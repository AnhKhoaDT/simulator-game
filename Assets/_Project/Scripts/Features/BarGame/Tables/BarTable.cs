using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // A table and the chairs currently grouped with it (chairs are separate objects; see BarFurniture).
    // Its footprint is the real area of the top and its chairs plus a walking margin.
    public class BarTable : MonoBehaviour
    {
        // A chair closer than this to the table center would sit under the top.
        public const float MinSeatDistance = 0.9f;
        // A chair within this distance of a table's center belongs to that table.
        public const float SeatRange = 1.6f;
        public const float MinSeatGap = 0.6f;

        [Tooltip("Free space kept around the top and chairs so customers can walk past.")]
        [SerializeField, Min(0f)] private float _walkMargin = 0.3f;
        [Tooltip("How far outside a seat the customer stands before sitting down.")]
        [SerializeField, Min(0.1f)] private float _approachDistance = 0.8f;
        [Tooltip("Height of the table top, where food is placed.")]
        [SerializeField, Min(0f)] private float _surfaceHeight = 0.75f;
        [Tooltip("How far from the table center, toward the seat, food is placed.")]
        [SerializeField, Min(0f)] private float _foodInset = 0.4f;

        private readonly List<BarChair> _chairs = new List<BarChair>();

        public IReadOnlyList<BarChair> Chairs => _chairs;

        // True while the owner carries this table; nobody can be seated at it then.
        public bool IsBeingMoved { get; set; }

        // True if any of its chairs is taken or reserved, so the table must not be picked up.
        public bool HasOccupants
        {
            get
            {
                foreach (var chair in _chairs)
                {
                    if (chair.IsTaken)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Rect Footprint => FootprintAt(transform.position);

        // Footprint if the table and its chairs were shifted so the table stood at 'center'.
        public Rect FootprintAt(Vector3 center)
        {
            return Offset(Measure(null, Vector3.zero), center - transform.position);
        }

        // Footprint if 'chair' (one of ours or a newcomer) stood at 'position' and belonged to this table.
        public Rect FootprintWithChair(BarChair chair, Vector3 position)
        {
            return Measure(chair, position);
        }

        public void SetChairs(List<BarChair> chairs)
        {
            _chairs.Clear();
            _chairs.AddRange(chairs);
        }

        // Adds every free chair to 'result'; a table being carried has none.
        public void CollectFreeSeats(List<BarChair> result)
        {
            if (IsBeingMoved)
            {
                return;
            }

            foreach (var chair in _chairs)
            {
                if (!chair.IsTaken)
                {
                    result.Add(chair);
                }
            }
        }

        public bool NearestFreeSeat(Vector3 from, out BarChair chair, out float sqrDistance)
        {
            chair = null;
            sqrDistance = float.MaxValue;
            if (IsBeingMoved)
            {
                return false;
            }

            foreach (var candidate in _chairs)
            {
                if (candidate.IsTaken)
                {
                    continue;
                }

                var distance = (candidate.transform.position - from).sqrMagnitude;
                if (distance < sqrDistance)
                {
                    sqrDistance = distance;
                    chair = candidate;
                }
            }

            return chair != null;
        }

        // A spot on the table top in front of this chair, where the customer's food goes.
        public Vector3 GetFoodPoint(BarChair chair)
        {
            var center = transform.position;
            var toSeat = chair.transform.position - center;
            toSeat.y = 0f;
            var point = center + (toSeat.sqrMagnitude > 0.0001f ? toSeat.normalized : Vector3.back) * _foodInset;
            point.y = center.y + _surfaceHeight;
            return point;
        }

        // Where a guest (or the waiter) stands just outside this chair.
        public Vector3 GetApproachPoint(BarChair chair)
        {
            var center = transform.position;
            var offset = chair.transform.position - center;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.0001f)
            {
                offset = Vector3.back;
            }

            var point = center + offset.normalized * (offset.magnitude + _approachDistance);
            point.y = 0f;
            return point;
        }

        // Bounds of the top and chairs plus the margin. 'moved' (if set) is counted at 'movedTo' instead.
        private Rect Measure(BarChair moved, Vector3 movedTo)
        {
            var center = transform.position;
            var top = TableBuilder.TopHalfSize;
            var rect = Rect.MinMaxRect(center.x - top, center.z - top, center.x + top, center.z + top);

            foreach (var chair in _chairs)
            {
                if (chair != moved)
                {
                    rect = Include(rect, chair.transform.position);
                }
            }

            if (moved != null)
            {
                rect = Include(rect, movedTo);
            }

            var m = _walkMargin;
            return Rect.MinMaxRect(rect.xMin - m, rect.yMin - m, rect.xMax + m, rect.yMax + m);
        }

        private static Rect Include(Rect rect, Vector3 p)
        {
            var c = TableBuilder.ChairHalfSize;
            return Rect.MinMaxRect(Mathf.Min(rect.xMin, p.x - c), Mathf.Min(rect.yMin, p.z - c),
                Mathf.Max(rect.xMax, p.x + c), Mathf.Max(rect.yMax, p.z + c));
        }

        private static Rect Offset(Rect rect, Vector3 delta)
        {
            return new Rect(rect.x + delta.x, rect.y + delta.z, rect.width, rect.height);
        }
    }
}
