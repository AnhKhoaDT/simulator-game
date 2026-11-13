using UnityEngine;

namespace Startup.Features.BarGame
{
    // Plays one-shot sound effects in response to game events. Clips are loaded from Resources/Audio.
    public class BarAudio : MonoBehaviour
    {
        private const string Folder = "Audio/";
        private const float Volume = 0.6f;

        private OrderSystem _orders;
        private BarUpgrades _upgrades;
        private AudioSource _source;
        private AudioClip _serve;
        private AudioClip _coin;
        private AudioClip _abandon;
        private AudioClip _unlock;
        private AudioClip _click;

        public void Setup(OrderSystem orders, BarUpgrades upgrades)
        {
            _orders = orders;
            _upgrades = upgrades;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _serve = Resources.Load<AudioClip>(Folder + "sfx_serve");
            _coin = Resources.Load<AudioClip>(Folder + "sfx_coin");
            _abandon = Resources.Load<AudioClip>(Folder + "sfx_abandon");
            _unlock = Resources.Load<AudioClip>(Folder + "sfx_unlock");
            _click = Resources.Load<AudioClip>(Folder + "sfx_click");

            _orders.Served += OnServed;
            _orders.Paid += OnPaid;
            _orders.Abandoned += OnAbandoned;
            _orders.ItemUnlocked += OnItemUnlocked;
            _upgrades.Changed += OnUpgradeBought;
        }

        private void OnDestroy()
        {
            if (_orders == null)
            {
                return;
            }

            _orders.Served -= OnServed;
            _orders.Paid -= OnPaid;
            _orders.Abandoned -= OnAbandoned;
            _orders.ItemUnlocked -= OnItemUnlocked;
            _upgrades.Changed -= OnUpgradeBought;
        }

        private void OnServed()
        {
            Play(_serve);
        }

        private void OnPaid(int total, System.Collections.Generic.IReadOnlyList<MenuItem> items)
        {
            Play(_coin);
        }

        private void OnAbandoned()
        {
            Play(_abandon);
        }

        private void OnItemUnlocked(MenuItem item)
        {
            Play(_unlock);
        }

        private void OnUpgradeBought()
        {
            Play(_click);
        }

        private void Play(AudioClip clip)
        {
            if (clip != null)
            {
                _source.PlayOneShot(clip, Volume);
            }
        }
    }
}
