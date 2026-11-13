using UnityEngine;
using UnityEngine.InputSystem;

namespace Startup.Features.BarGame
{
    // The owner's side of taking payment: stand behind the counter, press E to hand the next customer their bill,
    // then count out the change in the payment panel. Nobody pays unless the owner does this.
    public sealed class CashierController : MonoBehaviour
    {
        private CashierCounter _counter;
        private EconomyManager _economy;
        private BarHud _hud;
        private Customer _serving;
        private bool _promptShown;

        // True while a prompt is up or a customer is being served, so other E actions stay quiet.
        public bool IsBusy => _promptShown || _serving != null;

        public void Setup(CashierCounter counter, EconomyManager economy, BarHud hud)
        {
            _counter = counter;
            _economy = economy;
            _hud = hud;
        }

        private void Update()
        {
            if (_serving != null)
            {
                return;
            }

            var next = Time.timeScale == 0f ? null : _counter.NextToServe;
            SetPrompt(next != null);

            var keyboard = Keyboard.current;
            if (next == null || keyboard == null || !keyboard.eKey.wasPressedThisFrame)
            {
                return;
            }

            _serving = next;
            SetPrompt(false);
            next.ReceiveBill(OnCashReady);
        }

        private void OnCashReady(Customer customer)
        {
            _hud.OpenPayment(customer.Items, customer.Bill, customer.Cash, OnChangeGiven);
        }

        // The sale counts in full; any change given beyond what was owed comes back out of the till.
        private void OnChangeGiven(int changeGiven)
        {
            var overpaid = changeGiven - (_serving.Cash - _serving.Bill);
            _serving.CompletePayment();
            _economy.Refund(overpaid);
            _serving = null;
        }

        private void SetPrompt(bool shown)
        {
            if (shown == _promptShown)
            {
                return;
            }

            _promptShown = shown;
            _hud.ShowCashierPrompt(shown);
        }
    }
}
