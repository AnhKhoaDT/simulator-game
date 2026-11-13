using System;
using System.Collections.Generic;
using Startup.Core.Localization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Startup.Features.BarGame
{
    // The checkout: a paper receipt on the left (what the customer ordered, the total, what they handed over),
    // a till on the right where the owner taps notes until the change is right. Too little change cannot be
    // confirmed; too much is allowed but comes out of the shop's pocket.
    public sealed class PaymentPanel : IDisposable
    {
        private const string NoteKeyClass = "note-key";

        // Rough Vietnamese note colors by value, so the tiles read as money at a glance.
        private static readonly Color[] NoteColors =
        {
            new Color32(150, 130, 175, 255), // 1k
            new Color32(140, 150, 150, 255), // 2k
            new Color32(70, 130, 200, 255),  // 5k
            new Color32(170, 145, 70, 255),  // 10k
            new Color32(60, 105, 200, 255),  // 20k
            new Color32(215, 110, 155, 255), // 50k
            new Color32(70, 155, 95, 255),   // 100k
            new Color32(195, 85, 65, 255),   // 200k
            new Color32(50, 175, 200, 255)   // 500k
        };

        private readonly LocalizedBindings _bindings = new LocalizedBindings();
        private readonly VisualElement _overlay;
        private readonly VisualElement _lines;
        private readonly Label _totalValue;
        private readonly Label _cashValue;
        private readonly Label _changeValue;
        private readonly Button _clear;
        private readonly Button _confirm;
        private readonly Label _shortLabel;
        private readonly Label _overLabel;
        private readonly List<Button> _noteButtons = new List<Button>();
        private readonly List<Action> _noteHandlers = new List<Action>();
        private readonly Action<object[]> _setDue;
        private readonly Action<object[]> _setGiven;
        private readonly Action<object[]> _setShort;
        private readonly Action<object[]> _setOver;

        private Action<int> _done;
        private int _due;
        private int _given;

        public PaymentPanel(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("payment-overlay");
            _lines = root.Q<VisualElement>("pay-lines");
            _bindings.Bind(root.Q<Label>("pay-title"), LocKeys.PayReceipt);
            _bindings.Bind(root.Q<Label>("pay-total-label"), LocKeys.PayTotal);
            _bindings.Bind(root.Q<Label>("pay-cash-label"), LocKeys.PayCashLabel);
            _bindings.Bind(root.Q<Label>("pay-change-label"), LocKeys.PayChangeLabel);
            _bindings.Bind(root.Q<Label>("pay-register-title"), LocKeys.PayTitle);
            _bindings.Bind(root.Q<Label>("pay-hint"), LocKeys.PayHint);
            _totalValue = root.Q<Label>("pay-total");
            _cashValue = root.Q<Label>("pay-cash");
            _changeValue = root.Q<Label>("pay-change");

            _setDue = _bindings.BindWithArgs(root.Q<Label>("pay-due"), LocKeys.PayDue, new object[] { string.Empty });
            _setGiven = _bindings.BindWithArgs(root.Q<Label>("pay-given"), LocKeys.PayGiven, new object[] { string.Empty });
            _shortLabel = root.Q<Label>("pay-short");
            _overLabel = root.Q<Label>("pay-over");
            _setShort = _bindings.BindWithArgs(_shortLabel, LocKeys.PayShort, new object[] { string.Empty });
            _setOver = _bindings.BindWithArgs(_overLabel, LocKeys.PayOver, new object[] { string.Empty });

            BuildNotes(root.Q<VisualElement>("pay-notes"));

            _clear = root.Q<Button>("btn-pay-clear");
            _confirm = root.Q<Button>("btn-pay-confirm");
            _bindings.Bind(_clear, LocKeys.PayClear);
            _bindings.Bind(_confirm, LocKeys.PayConfirm);
            _clear.clicked += Clear;
            _confirm.clicked += Confirm;
        }

        public bool IsOpen { get; private set; }

        // 'done' receives the total change handed back once the owner confirms.
        public void Open(IReadOnlyList<MenuItem> items, int bill, int cash, Action<int> done)
        {
            _done = done;
            _due = cash - bill;
            _given = 0;
            FillReceipt(items);
            _totalValue.text = Money.Format(bill);
            _cashValue.text = Money.Format(cash);
            _changeValue.text = Money.Format(_due);
            _setDue(new object[] { Money.Format(_due) });
            for (var i = 0; i < _noteButtons.Count; i++)
            {
                _noteButtons[i].text = Money.Short(CashDenominations.Values[i]);
                _noteButtons[i].Clear();
                _noteButtons[i].Add(KeyHint(i + 1));
            }

            IsOpen = true;
            _overlay.style.display = DisplayStyle.Flex;
            Refresh();
        }

        // Number keys tap the notes in order, Backspace clears, Enter confirms.
        public void HandleKeys(Keyboard keyboard)
        {
            if (!IsOpen || keyboard == null)
            {
                return;
            }

            for (var i = 0; i < CashDenominations.Values.Length; i++)
            {
                if (keyboard[Key.Digit1 + i].wasPressedThisFrame)
                {
                    AddNote(CashDenominations.Values[i]);
                }
            }

            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                Clear();
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                Confirm();
            }
        }

        public void Dispose()
        {
            _clear.clicked -= Clear;
            _confirm.clicked -= Confirm;
            for (var i = 0; i < _noteButtons.Count; i++)
            {
                _noteButtons[i].clicked -= _noteHandlers[i];
            }

            _bindings.Dispose();
        }

        // One line per dish: "2 x Beer ..... 100.000 VND". Identical dishes are merged.
        private void FillReceipt(IReadOnlyList<MenuItem> items)
        {
            _lines.Clear();
            var order = new List<string>();
            var quantity = new Dictionary<string, int>();
            var price = new Dictionary<string, int>();
            foreach (var item in items)
            {
                if (!quantity.ContainsKey(item.NameKey))
                {
                    order.Add(item.NameKey);
                    quantity[item.NameKey] = 0;
                    price[item.NameKey] = item.Price;
                }

                quantity[item.NameKey]++;
            }

            foreach (var key in order)
            {
                var row = new VisualElement();
                row.AddToClassList("receipt-line");
                var name = new Label(quantity[key] + " x " + LocalizationService.GetText(key));
                name.AddToClassList("receipt-name");
                var amount = new Label(Money.Format(price[key] * quantity[key]));
                amount.AddToClassList("receipt-amount");
                row.Add(name);
                row.Add(amount);
                _lines.Add(row);
            }
        }

        private void BuildNotes(VisualElement container)
        {
            for (var i = 0; i < CashDenominations.Values.Length; i++)
            {
                var value = CashDenominations.Values[i];
                var button = new Button();
                button.AddToClassList("pay-note");
                button.style.backgroundColor = NoteColors[i % NoteColors.Length];
                Action handler = () => AddNote(value);
                button.clicked += handler;
                container.Add(button);
                _noteButtons.Add(button);
                _noteHandlers.Add(handler);
            }
        }

        // The small key number on a note tile; replaced each time the panel opens.
        private Label KeyHint(int number)
        {
            var label = new Label(number.ToString()) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(NoteKeyClass);
            return label;
        }

        private void AddNote(int value)
        {
            _given += value;
            Refresh();
        }

        private void Clear()
        {
            _given = 0;
            Refresh();
        }

        private void Confirm()
        {
            if (!IsOpen || _given < _due)
            {
                return;
            }

            IsOpen = false;
            _overlay.style.display = DisplayStyle.None;
            var done = _done;
            _done = null;
            done?.Invoke(_given);
        }

        private void Refresh()
        {
            var missing = Math.Max(0, _due - _given);
            var extra = Math.Max(0, _given - _due);
            _setGiven(new object[] { Money.Format(_given) });
            _setShort(new object[] { Money.Format(missing) });
            _setOver(new object[] { Money.Format(extra) });
            _shortLabel.style.display = missing > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _overLabel.style.display = extra > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _confirm.SetEnabled(missing == 0);
        }
    }
}
