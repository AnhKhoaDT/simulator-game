using System.Collections.Generic;
using UnityEngine;

namespace Startup.Features.BarGame
{
    // Cars driving both ways along the road and looping at the ends. Purely decorative.
    // Each car keeps a safe gap to the car ahead in its lane, so cars never drive through each other.
    public class TrafficCars : MonoBehaviour
    {
        private const float CarHeight = 1.5f;
        private const float LaneOffset = 1.5f;
        private const float MinSpeed = 6f;
        private const float MaxSpeed = 10f;
        private const float EndMargin = 8f;
        // Front-to-front distance cars keep (about one car length plus a gap).
        private const float SafeSpacing = 7f;
        // Start slowing down this far behind the safe spacing.
        private const float BrakeZone = 4f;
        private const float Acceleration = 6f;
        // Cars stop this far from a person on the road, and start easing off a bit earlier.
        private const float PersonStop = 4.5f;
        private const float PersonBrakeZone = 6f;
        private const float PersonMinGap = 3.4f;
        private const float RoadHalfWidth = 3.4f;
        // A person slightly behind the car's middle is already past its nose.
        private const float PersonBehindAllowance = 0.5f;

        private readonly List<Transform> _crossers = new List<Transform>();

        private Transform[] _cars;
        private float[] _cruise;
        private float[] _speed;
        private float[] _direction;
        // Distance travelled along the car's own direction: x * direction.
        private float[] _progress;
        private float _roadZ;
        private float _halfLength;

        // Cars give way to this person whenever they are on the road ahead of them.
        public void AddCrosser(Transform person)
        {
            _crossers.Add(person);
        }

        public void Setup(BarLayout layout, int count)
        {
            _roadZ = layout.RoadZ;
            _halfLength = BarLayout.StreetHalfLength + EndMargin;

            var names = ModelCatalog.Traffic;
            _cars = new Transform[count];
            _cruise = new float[count];
            _speed = new float[count];
            _direction = new float[count];
            _progress = new float[count];

            // Spread each lane's cars evenly so none start on top of each other.
            var perLane = Mathf.Max(1, (count + 1) / 2);
            var laneSpacing = Mathf.Max(SafeSpacing, _halfLength * 2f / perLane);

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
                _direction[i] = i % 2 == 0 ? 1f : -1f;
                _cruise[i] = Random.Range(MinSpeed, MaxSpeed);
                _speed[i] = _cruise[i];
                _progress[i] = -_halfLength + (i / 2) * laneSpacing + Random.Range(0f, laneSpacing - SafeSpacing);
                _cars[i] = model.transform;

                model.transform.rotation = Quaternion.LookRotation(new Vector3(_direction[i], 0f, 0f))
                                           * Quaternion.Euler(0f, ModelCatalog.CarYawOffset, 0f);
                Place(i);
            }
        }

        private void Update()
        {
            if (_cars == null || Time.deltaTime <= 0f)
            {
                return;
            }

            for (var i = 0; i < _cars.Length; i++)
            {
                var target = _cruise[i];
                var gap = GapToLeader(i, out var leaderSpeed);
                if (gap <= SafeSpacing)
                {
                    target = 0f;
                }
                else if (gap < SafeSpacing + BrakeZone)
                {
                    // Inside the brake zone: blend from the leader's speed (closest) up to our own cruise speed.
                    var t = (gap - SafeSpacing) / BrakeZone;
                    target = Mathf.Min(_cruise[i], Mathf.Lerp(leaderSpeed, _cruise[i], t));
                }

                var personGap = GapToPerson(i);
                if (personGap < PersonStop)
                {
                    target = 0f;
                }
                else if (personGap < PersonStop + PersonBrakeZone)
                {
                    target = Mathf.Min(target, _cruise[i] * (personGap - PersonStop) / PersonBrakeZone);
                }

                _speed[i] = Mathf.MoveTowards(_speed[i], target, Acceleration * Time.deltaTime);
                // Never move past the safe spacing or into a person, even with a large frame time.
                var step = Mathf.Min(_speed[i] * Time.deltaTime, Mathf.Max(0f, gap - SafeSpacing),
                    Mathf.Max(0f, personGap - PersonMinGap));
                _progress[i] += step;

                if (_progress[i] > _halfLength)
                {
                    Respawn(i);
                }

                Place(i);
            }
        }

        // Distance to the nearest person standing on the road ahead of this car; infinity if none.
        private float GapToPerson(int index)
        {
            var best = float.PositiveInfinity;
            var carX = _cars[index].position.x;
            foreach (var person in _crossers)
            {
                if (person == null || !person.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var position = person.position;
                if (Mathf.Abs(position.z - _roadZ) > RoadHalfWidth)
                {
                    continue;
                }

                var ahead = (position.x - carX) * _direction[index];
                if (ahead > -PersonBehindAllowance && ahead < best)
                {
                    best = Mathf.Max(0f, ahead);
                }
            }

            return best;
        }

        // Distance to the nearest car ahead in the same lane; infinity if the lane ahead is clear.
        private float GapToLeader(int index, out float leaderSpeed)
        {
            leaderSpeed = _cruise[index];
            var best = float.PositiveInfinity;
            for (var j = 0; j < _cars.Length; j++)
            {
                if (j == index || !Mathf.Approximately(_direction[j], _direction[index]))
                {
                    continue;
                }

                var gap = _progress[j] - _progress[index];
                if (gap > 0f && gap < best)
                {
                    best = gap;
                    leaderSpeed = _speed[j];
                }
            }

            return best;
        }

        // Off the far end: come back in at the start, queued behind the last car of the lane if needed.
        // Cars waiting beyond the start are hidden behind the towers that close off the street.
        private void Respawn(int index)
        {
            var entry = -_halfLength;
            for (var j = 0; j < _cars.Length; j++)
            {
                if (j != index && Mathf.Approximately(_direction[j], _direction[index]))
                {
                    entry = Mathf.Min(entry, _progress[j] - SafeSpacing);
                }
            }

            _progress[index] = entry;
            _cruise[index] = Random.Range(MinSpeed, MaxSpeed);
            _speed[index] = _cruise[index];
        }

        private void Place(int index)
        {
            var lane = _roadZ - _direction[index] * LaneOffset;
            _cars[index].position = new Vector3(_progress[index] * _direction[index], 0f, lane);
        }
    }
}
