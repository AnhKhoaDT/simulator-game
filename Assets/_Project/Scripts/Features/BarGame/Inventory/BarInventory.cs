using System;
using System.Collections.Generic;

namespace Startup.Features.BarGame
{
    // One stack in the save file: furniture id and how many the owner holds.
    [Serializable]
    public class InventoryEntry
    {
        public string Id;
        public int Count;
    }

    // The owner's storage: everything bought or stored but not standing in the bar. Plain C#, saved by BarSaveStore.
    public sealed class BarInventory
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        public event Action Changed;

        public int Count(string id)
        {
            return _counts.TryGetValue(id, out var count) ? count : 0;
        }

        public void Add(string id)
        {
            _counts[id] = Count(id) + 1;
            Changed?.Invoke();
        }

        public bool TryTake(string id)
        {
            var count = Count(id);
            if (count <= 0)
            {
                return false;
            }

            if (count == 1)
            {
                _counts.Remove(id);
            }
            else
            {
                _counts[id] = count - 1;
            }

            Changed?.Invoke();
            return true;
        }

        public void Restore(List<InventoryEntry> entries)
        {
            _counts.Clear();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry != null && entry.Count > 0 && FurnitureCatalog.Find(entry.Id) != null)
                    {
                        _counts[entry.Id] = entry.Count;
                    }
                }
            }

            Changed?.Invoke();
        }

        public List<InventoryEntry> Snapshot()
        {
            var result = new List<InventoryEntry>(_counts.Count);
            foreach (var pair in _counts)
            {
                result.Add(new InventoryEntry { Id = pair.Key, Count = pair.Value });
            }

            return result;
        }
    }
}
