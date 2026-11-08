using System;

namespace Startup.Features.BarGame
{
    // Plain serializable snapshot of bar progress; JsonUtility reads and writes these public fields.
    [Serializable]
    public class BarSaveData
    {
        public int Money;
        public int TotalRevenue;
        public int CustomersServed;
        public int TablesOwned;
        public int WaiterLevel;
    }
}
