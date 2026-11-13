using System;
using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // One decoration entry in the save file: furniture id, x, yaw degrees, z.
    [Serializable]
    public class DecorSave
    {
        public string Id;
        public float X;
        public float Yaw;
        public float Z;
    }

    // A piece of furniture that is neither table nor chair. Standing items block the floor; flat ones (rugs) do not.
    public class PlacedDecor : MonoBehaviour
    {
        private FurnitureDef _def;
        private readonly List<StaffSeat> _seats = new List<StaffSeat>();
        private Vector2 _size;

        public FurnitureDef Def => _def;

        public bool IsFlat => _def.IsFlat;

        public IReadOnlyList<StaffSeat> Seats => _seats;

        // True while someone sits here, so it must not be picked up.
        public bool HasSitter
        {
            get
            {
                foreach (var seat in _seats)
                {
                    if (!seat.IsFree)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public Rect Footprint => FootprintAt(transform.position);

        public void Initialize(FurnitureDef def, Vector2 size)
        {
            _def = def;
            _size = size;
        }

        public void AddSeat(StaffSeat seat)
        {
            _seats.Add(seat);
        }

        // Floor rectangle if the item stood at 'center' with its current heading (turns come in quarter steps).
        public Rect FootprintAt(Vector3 center)
        {
            var quarterTurns = Mathf.Abs(Mathf.RoundToInt(transform.eulerAngles.y / BarFurniture.RotateStep));
            var sideways = quarterTurns % 2 == 1;
            var halfX = (sideways ? _size.y : _size.x) * 0.5f;
            var halfZ = (sideways ? _size.x : _size.y) * 0.5f;
            return Rect.MinMaxRect(center.x - halfX, center.z - halfZ, center.x + halfX, center.z + halfZ);
        }
    }
}
