using System.Collections.Generic;
using ArduinoAPI;

namespace ORCA.Plugin
{
    public static class Utils
    {
        public static readonly IReadOnlyDictionary<string, ControllerInput> buttonMap = new Dictionary<string, ControllerInput>()
        {
            { "A", ControllerInput.A },
            { "B", ControllerInput.B },
            { "X", ControllerInput.X },
            { "Y", ControllerInput.Y },
            { "Z", ControllerInput.Z },
            { "St", ControllerInput.Start },
            { "Sl", ControllerInput.Y },
            { "L", ControllerInput.L },
            { "R", ControllerInput.R },
            { "dU", ControllerInput.Up },
            { "dD", ControllerInput.Down },
            { "dL", ControllerInput.Left },
            { "dR", ControllerInput.Right },
            { "tl", ControllerInput.Start | ControllerInput.Y }
        };
    }
}
