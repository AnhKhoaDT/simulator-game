using UnityEngine;

namespace Startup.Features.BarGame
{
    // The notes in the till and what a customer hands over for a bill.
    public static class CashDenominations
    {
        private const float ExactChance = 0.15f;
        private const int OverpayChoices = 2;

        public static readonly int[] Values = { 1, 5, 10, 20, 50, 100, 200, 500 };

        // Mostly the next note up (sometimes the one after), now and then the exact amount.
        public static int CustomerCash(int bill)
        {
            if (Random.value < ExactChance)
            {
                return bill;
            }

            var higher = 0;
            var candidates = new int[OverpayChoices];
            foreach (var value in Values)
            {
                if (value > bill && higher < OverpayChoices)
                {
                    candidates[higher++] = value;
                }
            }

            if (higher == 0)
            {
                var biggest = Values[Values.Length - 1];
                return Mathf.CeilToInt(bill / (float)biggest) * biggest;
            }

            return candidates[Random.Range(0, higher)];
        }
    }
}
