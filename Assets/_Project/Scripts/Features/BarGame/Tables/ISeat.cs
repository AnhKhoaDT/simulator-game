using UnityEngine;

namespace Startup.Features.BarGame
{
    // Anything a person can sit on: a bar chair, or a place on a bench or sofa.
    public interface ISeat
    {
        bool IsFree { get; }

        // The middle of the sitting surface.
        Vector3 SitPoint { get; }

        // Which way a person sitting here faces.
        Quaternion SitRotation { get; }

        void Occupy();

        void Vacate();
    }

    // Where a character's feet-level pivot goes when sitting. The sit animation already bends the legs,
    // so the pivot sits below the seat surface and a little back; tune the two numbers if people float or sink.
    internal static class SeatPose
    {
        public const float Lower = 0.25f;
        public const float Back = 0.1f;

        public static Vector3 BodyPosition(ISeat seat)
        {
            var facing = seat.SitRotation * Vector3.forward;
            return seat.SitPoint - Vector3.up * Lower - facing * Back;
        }
    }
}
