using System;

namespace Startup.Features.BarGame
{
    // Plain C# money and progress tracker. Saving lives in BarSaveStore, not here.
    public class EconomyManager
    {
        public int Money { get; private set; }

        public int TotalRevenue { get; private set; }

        public int CustomersServed { get; private set; }

        public event Action Changed;

        public void Restore(BarSaveData data)
        {
            Money = data.Money;
            TotalRevenue = data.TotalRevenue;
            CustomersServed = data.CustomersServed;
            Changed?.Invoke();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || Money < amount)
            {
                return false;
            }

            Money -= amount;
            Changed?.Invoke();
            return true;
        }

        public void Earn(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Money += amount;
            TotalRevenue += amount;
            CustomersServed++;
            Changed?.Invoke();
        }
    }
}
