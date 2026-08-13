using System.Collections.Generic;
using System.Threading;
using ArduinoAPI;
using ORCA.Plugin;

namespace ORCA.Core.Macro
{
    class PressCommand : MacroCommand
    {
        private readonly IEnumerable<ControllerInput> _buttons;
        private readonly int _duration;
        private readonly int _interval;
        private readonly int _label;

        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            if (_label != -1)
            {
                context.StartTimer(_label);
            }

            foreach (var button in _buttons)
            {
                if (token.IsCancellationRequested) return;

                port.SetButtonState(button);

                if (context.Wait(_duration, token)) return;

                port.SetButtonState(ControllerInput.KeysAllUp);

                if (context.Wait(_interval, token)) return;
            }
        }

        public PressCommand(IEnumerable<ControllerInput> buttons, int duration, int interval, int label)
        {
            _buttons = buttons;
            _duration = duration;
            _interval = interval;
            _label = label;
        }
    }

    class WaitCommand : MacroCommand
    {
        private readonly int _duration;

        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            context.Wait(_duration, token);
        }

        public WaitCommand(int duration)
        {
            _duration = duration;
        }
    }

    class StartCommand : MacroCommand
    {
        private readonly int _label;

        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            context.StartTimer(_label);
        }

        public StartCommand(int label)
        {
            _label = label;
        }
    }

    class HitCommand : MacroCommand
    {
        private readonly ControllerInput _button;
        private readonly int _frame;
        private readonly int _label;
        private readonly int _duration;
        private readonly int _startLabel;

        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            var border = (int)(_frame * 1000 / 59.7275);

            if (context.Wait(_label, border, token, false)) return;
            context.GetNextHitIndex();

            if (_startLabel != -1)
                context.StartTimer(_startLabel);

            port.SetButtonState(_button);

            if (context.Wait(_duration, token)) return;
            port.SetButtonState(ControllerInput.KeysAllUp);
        }

        public HitCommand(ControllerInput button, int frame, int label, int duration, int startLabel)
        {
            _button = button;
            _frame = frame;
            _label = label;
            _duration = duration;
            _startLabel = startLabel;
        }
    }
}
