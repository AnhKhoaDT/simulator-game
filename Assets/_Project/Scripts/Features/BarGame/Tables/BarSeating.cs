using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Plain C# seat allocator: picks the free chair at a table nearest to the customer and reserves it.
    // Also owns the walk-around obstacles; BarFurniture calls Refresh after anything is moved.
    public class BarSeating
    {
        private readonly List<BarTable> _tables = new List<BarTable>();
        private readonly List<Rect> _fixed = new List<Rect>();
        private readonly List<Rect> _loose = new List<Rect>();
        // One list instance for the whole scene, so everyone holding it sees updates.
        private readonly List<Rect> _obstacles = new List<Rect>();

        public IReadOnlyList<Rect> Obstacles => _obstacles;

        public void AddTable(BarTable table)
        {
            _tables.Add(table);
            Refresh();
        }

        public void RemoveTable(BarTable table)
        {
            _tables.Remove(table);
            Refresh();
        }

        // Non-table furniture everyone must walk around, such as the cashier counter.
        public void AddObstacle(Rect footprint)
        {
            _fixed.Add(footprint);
            Refresh();
        }

        // Chairs standing on their own (not at a table) are obstacles too.
        public void SetLooseChairAreas(List<Rect> areas)
        {
            _loose.Clear();
            _loose.AddRange(areas);
            Refresh();
        }

        // Tables being carried are left out: they are not in the way, and nobody may sit at them.
        public void Refresh()
        {
            _obstacles.Clear();
            _obstacles.AddRange(_fixed);
            _obstacles.AddRange(_loose);
            foreach (var table in _tables)
            {
                if (!table.IsBeingMoved)
                {
                    _obstacles.Add(table.Footprint);
                }
            }
        }

        // Most chairs any single table has, so a group is never bigger than a table can seat.
        public int MaxSeatsAtOneTable
        {
            get
            {
                var most = 0;
                foreach (var table in _tables)
                {
                    most = Mathf.Max(most, table.Chairs.Count);
                }

                return most;
            }
        }

        // Reserves 'count' free chairs at the nearest table that has that many; false if no table does.
        public bool TryReserveGroup(Vector3 from, int count, out BarTable table, List<BarChair> chairs)
        {
            table = null;
            chairs.Clear();
            var bestDistance = float.MaxValue;
            var free = new List<BarChair>();

            foreach (var candidate in _tables)
            {
                free.Clear();
                candidate.CollectFreeSeats(free);
                var distance = (candidate.transform.position - from).sqrMagnitude;
                if (free.Count >= count && distance < bestDistance)
                {
                    bestDistance = distance;
                    table = candidate;
                    chairs.Clear();
                    chairs.AddRange(free);
                }
            }

            if (table == null)
            {
                return false;
            }

            chairs.RemoveRange(count, chairs.Count - count);
            foreach (var chair in chairs)
            {
                chair.Reserve();
            }

            return true;
        }

        public bool TryReserve(Vector3 from, out BarTable table, out BarChair chair)
        {
            table = null;
            chair = null;
            var bestDistance = float.MaxValue;

            foreach (var candidate in _tables)
            {
                if (candidate.NearestFreeSeat(from, out var seat, out var sqrDistance) && sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    table = candidate;
                    chair = seat;
                }
            }

            if (chair == null)
            {
                return false;
            }

            chair.Reserve();
            return true;
        }
    }
}
