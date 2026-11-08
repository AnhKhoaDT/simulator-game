namespace Startup.Features.BarGame
{
    // Shared references every customer needs; built once and handed to each customer by the spawner.
    public sealed class CustomerContext
    {
        public CustomerContext(BarLayout layout, BarSeating seating, OrderSystem orders,
            CashierCounter counter, CustomerSettings settings)
        {
            Layout = layout;
            Seating = seating;
            Orders = orders;
            Counter = counter;
            Settings = settings;
        }

        public BarLayout Layout { get; }
        public BarSeating Seating { get; }
        public OrderSystem Orders { get; }
        public CashierCounter Counter { get; }
        public CustomerSettings Settings { get; }
    }
}
