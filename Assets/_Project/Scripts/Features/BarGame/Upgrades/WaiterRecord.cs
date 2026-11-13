using System;

namespace Startup.Features.BarGame
{
    // One hired waiter as saved: a name the owner chose (empty = default) and whether they are resting on a seat.
    [Serializable]
    public class WaiterRecord
    {
        public string Name = string.Empty;
        public bool Resting;
    }
}
