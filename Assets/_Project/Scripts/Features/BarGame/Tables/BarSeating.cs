using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Plain C# seat allocator: picks the free seat nearest to the customer and reserves it.
    // Tables can be added at runtime when the player buys one.
    public class BarSeating
    {
        private readonly List<BarTable> _tables = new List<BarTable>();
        private readonly List<Rect> _obstacles = new List<Rect>();

        public IReadOnlyList<Rect> Obstacles => _obstacles;

        public void AddTable(BarTable table)
        {
            _tables.Add(table);
            _obstacles.Add(table.Footprint);
        }

        public bool TryReserve(Vector3 from, out BarTable table, out int seatIndex)
        {
            table = null;
            seatIndex = -1;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < _tables.Count; i++)
            {
                if (_tables[i].NearestFreeSeat(from, out var index, out var sqrDistance) && sqrDistance < bestDistance)
                {
                    bestDistance = sqrDistance;
                    table = _tables[i];
                    seatIndex = index;
                }
            }

            if (table == null)
            {
                return false;
            }

            table.Reserve(seatIndex);
            return true;
        }
    }
}
