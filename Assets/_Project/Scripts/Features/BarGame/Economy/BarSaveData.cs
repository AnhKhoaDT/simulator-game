using System;
using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Plain serializable snapshot of bar progress; JsonUtility reads and writes these public fields.
    [Serializable]
    public class BarSaveData
    {
        // One entry per table, in purchase order: (x, yaw degrees, z). Empty = default layout.
        public List<Vector3> TablePlacements = new List<Vector3>();
        // Every chair, wherever it stands: (x, yaw degrees, z). Empty = four default chairs per table.
        public List<Vector3> ChairPlacements = new List<Vector3>();
        // Every standing piece that is neither table nor chair.
        public List<DecorSave> DecorPlacements = new List<DecorSave>();
        // Furniture owned but not placed.
        public List<InventoryEntry> InventoryItems = new List<InventoryEntry>();
        // False for saves from before the inventory existed, whose placement lists are not the whole story.
        public bool LayoutSaved;
        public int Money;
        public int TotalRevenue;
        public int CustomersServed;
        // Old saves stored only a count; new ones store each waiter.
        public int WaiterLevel;
        public List<WaiterRecord> Waiters = new List<WaiterRecord>();
        // False for saves from before the default plant, lamp and sofa could be moved.
        public bool DecorSaved;
        // -1 = never saved, so the clock starts at its configured hour.
        public int ClockMinutes = -1;
    }
}
