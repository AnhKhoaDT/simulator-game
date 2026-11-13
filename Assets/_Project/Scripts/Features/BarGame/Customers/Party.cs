using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // A group of friends who arrive together and sit at the same table. The first member to look for seats
    // reserves chairs for everyone; each member then claims their own.
    internal sealed class Party
    {
        private readonly List<Customer> _members = new List<Customer>();
        private readonly List<BarChair> _found = new List<BarChair>();
        private readonly Dictionary<Customer, BarChair> _claims = new Dictionary<Customer, BarChair>();
        private bool _assigned;

        public BarTable Table { get; private set; }

        public void Add(Customer member)
        {
            _members.Add(member);
        }

        // True once a table with enough free chairs has been reserved for the whole group.
        public bool TryAssign(BarSeating seating, Vector3 from)
        {
            if (_assigned)
            {
                return true;
            }

            if (!seating.TryReserveGroup(from, _members.Count, out var table, _found))
            {
                return false;
            }

            Table = table;
            _assigned = true;
            for (var i = 0; i < _members.Count; i++)
            {
                _claims[_members[i]] = _found[i];
            }

            return true;
        }

        public BarChair Claim(Customer member)
        {
            if (!_claims.TryGetValue(member, out var chair))
            {
                return null;
            }

            _claims.Remove(member);
            return chair;
        }

        // A member gave up waiting: the chair held for them goes back to the room.
        public void Abandon(Customer member)
        {
            _members.Remove(member);
            if (_claims.TryGetValue(member, out var chair))
            {
                chair.Release();
                _claims.Remove(member);
            }
        }
    }
}
