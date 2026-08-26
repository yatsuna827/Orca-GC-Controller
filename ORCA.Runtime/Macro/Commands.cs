using System.Collections.Generic;
using System.Threading;
using ArduinoAPI;
using ORCA.Core;

namespace ORCA.Runtime.Macro
{
    class PressCommand : MacroCommand
    {
        private readonly IEnumerable<ControllerInput> _buttons;
        private readonly MacroArg _duration;
        private readonly MacroArg _interval;
        private readonly int _label;

        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            if (_label != -1)
            {
                context.StartTimer(_label);
            }

            var duration = _duration.Resolve(context);
            var interval = _interval.Resolve(context);

            foreach (var button in _buttons)
            {
                if (token.IsCancellationRequested) return;

                port.SetButtonState(button);

                if (context.Wait(duration, token)) return;

                port.SetButtonState(ControllerInput.KeysAllUp);

                if (context.Wait(interval, token)) return;
            }
        }

        public PressCommand(IEnumerable<ControllerInput> buttons, MacroArg duration, MacroArg interval, int label)
        {
            _buttons = buttons;
            _duration = duration;
            _interval = interval;
            _label = label;
        }
    }

    class WaitCommand : MacroCommand
    {
        private readonly MacroArg _duration;

        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            context.Wait(_duration.Resolve(context), token);
        }

        public WaitCommand(MacroArg duration)
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
        internal static int ResolveFrame(MacroArg frame, MacroArg correct, IMacroContext context)
        {
            var resolved = frame.Resolve(context) + correct.Resolve(context);
            return resolved < 0 ? 0 : resolved;
        }

        private readonly ControllerInput _button;
        private readonly MacroArg _frame;
        private readonly MacroArg _correct;
        private readonly int _label;
        private readonly MacroArg _duration;
        private readonly int _startLabel;

        public override void Execute(IWritable port, in CancellationToken token, IMacroContext context)
        {
            var border = (int)(ResolveFrame(_frame, _correct, context) * 1000 / 59.7275);

            if (context.Wait(_label, border, token, false)) return;
            context.GetNextHitIndex();

            if (_startLabel != -1)
                context.StartTimer(_startLabel);

            port.SetButtonState(_button);

            if (context.Wait(_duration.Resolve(context), token)) return;
            port.SetButtonState(ControllerInput.KeysAllUp);
        }

        public HitCommand(ControllerInput button, MacroArg frame, MacroArg correct, int label, MacroArg duration, int startLabel)
        {
            _button = button;
            _frame = frame;
            _correct = correct;
            _label = label;
            _duration = duration;
            _startLabel = startLabel;
        }
    }
}
