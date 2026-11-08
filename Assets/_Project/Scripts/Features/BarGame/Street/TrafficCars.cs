using UnityEngine;

namespace Startup.Features.BarGame
{
    // Cars driving both ways along the road and looping at the ends. Purely decorative.
    public class TrafficCars : MonoBehaviour
    {
        private const float CarHeight = 1.5f;
        private const float LaneOffset = 1.5f;
        private const float MinSpeed = 6f;
        private const float MaxSpeed = 10f;
        private const float EndMargin = 8f;

        private Transform[] _cars;
        private float[] _speeds;
        private float[] _directions;
        private float _roadZ;
        private float _halfLength;

        public void Setup(BarLayout layout, int count)
        {
            _roadZ = layout.RoadZ;
            _halfLength = BarLayout.StreetHalfLength + EndMargin;

            var names = ModelCatalog.Traffic;
            _cars = new Transform[count];
            _speeds = new float[count];
            _directions = new float[count];

            for (var i = 0; i < count; i++)
            {
                var model = ModelLoader.Instantiate(ModelCatalog.Cars + names[i % names.Length], transform, "Car");
                if (model == null)
                {
                    // No car pack: leave the road empty rather than draw boxes.
                    _cars = new Transform[0];
                    return;
                }

                ModelLoader.FitToHeight(model, CarHeight);
                // Alternate directions and keep right (as in Vietnam): +X traffic takes the lane farther from the bar.
                _directions[i] = i % 2 == 0 ? 1f : -1f;
                _speeds[i] = Random.Range(MinSpeed, MaxSpeed);
                _cars[i] = model.transform;

                var lane = _roadZ - _directions[i] * LaneOffset;
                var x = Random.Range(-_halfLength, _halfLength);
                model.transform.SetPositionAndRotation(new Vector3(x, 0f, lane),
                    Quaternion.LookRotation(new Vector3(_directions[i], 0f, 0f)) * Quaternion.Euler(0f, ModelCatalog.CarYawOffset, 0f));
            }
        }

        private void Update()
        {
            if (_cars == null)
            {
                return;
            }

            for (var i = 0; i < _cars.Length; i++)
            {
                var car = _cars[i];
                var position = car.position;
                position.x += _directions[i] * _speeds[i] * Time.deltaTime;

                // Off one end: come back in at the other.
                if (Mathf.Abs(position.x) > _halfLength)
                {
                    position.x = -Mathf.Sign(position.x) * _halfLength;
                    _speeds[i] = Random.Range(MinSpeed, MaxSpeed);
                }

                car.position = position;
            }
        }
    }
}
