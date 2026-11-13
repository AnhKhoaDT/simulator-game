using UnityEngine;

namespace Startup.Features.BarGame
{
    // Passers-by walking along the sidewalk so the street feels alive. Purely decorative.
    public class AmbientPedestrians : MonoBehaviour
    {
        private const float MinSpeed = 1.6f;
        private const float MaxSpeed = 2.4f;
        private const float LaneSpread = 1.1f;

        private Transform[] _walkers;
        private float[] _speeds;
        private float[] _directions;
        private float _sidewalkZ;
        private float _oppositeSidewalkZ;
        private float _halfLength;

        public void Setup(BarLayout layout, int count)
        {
            _sidewalkZ = layout.SidewalkZ;
            _oppositeSidewalkZ = layout.OppositeSidewalkZ;
            _halfLength = BarLayout.StreetHalfLength;
            _walkers = new Transform[count];
            _speeds = new float[count];
            _directions = new float[count];

            for (var i = 0; i < count; i++)
            {
                var walker = new GameObject("Pedestrian");
                walker.transform.SetParent(transform, false);
                var animator = CustomerFactory.BuildCharacter(walker, CustomerFactory.RandomGuestLetter(), out _);
                if (animator != null)
                {
                    animator.PlayWalk();
                }

                _walkers[i] = walker.transform;
                // Spread them out along the street from the start.
                Restart(i, Random.Range(-_halfLength, _halfLength));
            }
        }

        private void Update()
        {
            if (_walkers == null)
            {
                return;
            }

            for (var i = 0; i < _walkers.Length; i++)
            {
                var walker = _walkers[i];
                var position = walker.position;
                position.x += _directions[i] * _speeds[i] * Time.deltaTime;
                walker.position = position;

                if (Mathf.Abs(position.x) > _halfLength)
                {
                    Restart(i, -Mathf.Sign(position.x) * _halfLength);
                }
            }
        }

        private void Restart(int index, float startX)
        {
            // Walk away from the end they start at; mid-street starts pick a random direction.
            var direction = Mathf.Abs(startX) >= _halfLength
                ? -Mathf.Sign(startX)
                : (Random.value < 0.5f ? -1f : 1f);

            _directions[index] = direction;
            _speeds[index] = Random.Range(MinSpeed, MaxSpeed);
            var walker = _walkers[index];
            var sidewalk = Random.value < 0.5f ? _sidewalkZ : _oppositeSidewalkZ;
            walker.position = new Vector3(startX, 0f, sidewalk + Random.Range(-LaneSpread, LaneSpread));
            walker.rotation = Quaternion.LookRotation(new Vector3(direction, 0f, 0f));
        }
    }
}
