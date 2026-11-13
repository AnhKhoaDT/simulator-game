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

        // Change given back to a customer comes out of the till; revenue totals are left alone.
        public void Refund(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Money = Math.Max(0, Money - amount);
            Changed?.Invoke();
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
