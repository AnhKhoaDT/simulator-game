using UnityEngine;

namespace Startup.Features.BarGame
{
    // Sits on a waiter's pick volume so the owner's crosshair can find which waiter it is looking at.
    public class StaffHandle : MonoBehaviour
    {
        public WaiterRecord Record { get; set; }
    }
}
