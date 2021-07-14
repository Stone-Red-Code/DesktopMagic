using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Inputs
{
    public sealed class Slider : Element
    {
        private double _value;
        public double Maximum { get; }
        public double Minimum { get; }

        public double Value
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

        public Slider(double min, double max, double value = 0)
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