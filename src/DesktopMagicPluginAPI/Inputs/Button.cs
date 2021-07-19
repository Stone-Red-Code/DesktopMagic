using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Inputs
{
    public class Button : Element
    {
        public event Action OnClick;

        private string _value;

        public string Value
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

        public Button(string value)
        {
            Value = value;
        }

        public void Click()
        {
            OnClick?.Invoke();
        }
    }
}