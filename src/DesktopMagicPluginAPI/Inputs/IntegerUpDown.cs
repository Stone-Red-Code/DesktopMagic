using System;

namespace DesktopMagicPluginAPI.Inputs
{
    public class IntegerUpDown : Element
    {
        private int _value;
        public int Maximum { get; }
        public int Minimum { get; }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    ValueChanged();
                }
            }
        }

        public IntegerUpDown(int min, int max, int value = 0)
        {
            if (min < 0)
            {
                throw new ArgumentException("Value can not be negative!", nameof(min));
            }
            if (max < 0)
            {
                throw new ArgumentException("Value can not be negative!", nameof(max));
            }
            if (min > max)
            {
                throw new ArgumentException($"{nameof(min)} is greater than or equal to {nameof(max)}!");
            }
            if (value > max)
            {
                throw new ArgumentException($"{nameof(value)} is greater than {nameof(max)}!");
            }
            if (value < min)
            {
                throw new ArgumentException($"{nameof(value)} is less than {nameof(min)}!");
            }

            Minimum = min;
            Maximum = max;
            Value = value;
        }
    }
}