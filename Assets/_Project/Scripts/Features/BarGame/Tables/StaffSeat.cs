using UnityEngine;

namespace Startup.Features.BarGame
{
    // One sitting place on a bench, sofa or armchair. The owner and resting waiters use these; customers do not.
    // The object's forward axis is the direction the sitter faces.
    public class StaffSeat : MonoBehaviour, ISeat
    {
        private bool _occupied;

        public bool IsFree => !_occupied;

        public Vector3 SitPoint => transform.position;

        public Quaternion SitRotation => transform.rotation;

        public void Occupy()
        {
            _occupied = true;
        }

        public void Vacate()
        {
            _occupied = false;
        }
    }
}
