using UnityEngine;

namespace Startup.Features.BarGame
{
    // A chair standing on its own. Placed near a table it becomes one of that table's seats and turns to face it;
    // anywhere else it is just decoration that keeps its own heading. The anchor is at sitting height.
    public class BarChair : MonoBehaviour, ISeat
    {
        [Tooltip("Height of the sitting surface above this anchor.")]
        [SerializeField] private float _surfaceOffset;

        // The table this chair belongs to right now, or null when it stands alone.
        public BarTable Table { get; private set; }

        // Taken by a seated guest or reserved by one walking to it.
        public bool IsTaken { get; private set; }

        public Vector3 SeatPosition => transform.position + Vector3.up * _surfaceOffset;

        public bool IsFree => !IsTaken;

        public Vector3 SitPoint => SeatPosition;

        // The chair model's back is toward +Z of the object when it faces the table, so a sitter looks the other way.
        public Quaternion SitRotation => Quaternion.LookRotation(-Flat(transform.forward));

        public void Occupy()
        {
            Reserve();
        }

        public void Vacate()
        {
            Release();
        }

        public void Initialize(float surfaceOffset)
        {
            _surfaceOffset = surfaceOffset;
        }

        public void Reserve()
        {
            IsTaken = true;
        }

        public void Release()
        {
            IsTaken = false;
        }

        // Called by BarFurniture when chairs are regrouped; a chair at a table always faces it.
        public void AssignTo(BarTable table)
        {
            Table = table;
            if (table != null)
            {
                FaceTable();
            }
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        public void FaceTable()
        {
            if (Table == null)
            {
                return;
            }

            var toTable = Table.transform.position - transform.position;
            toTable.y = 0f;
            if (toTable.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(toTable) * Quaternion.Euler(0f, TableBuilder.ChairYawOffset, 0f);
            }
        }
    }
}
