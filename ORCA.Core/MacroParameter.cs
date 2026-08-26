namespace ORCA.Core
{
    public sealed class MacroParameter
    {
        public string Name { get; }

        public int? DefaultValue { get; }

        public bool AllowsNegative { get; }

        public MacroParameter(string name, int? defaultValue, bool allowsNegative)
        {
            Name = name;
            DefaultValue = defaultValue;
            AllowsNegative = allowsNegative;
        }
    }
}
