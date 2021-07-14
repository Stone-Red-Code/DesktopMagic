using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Inputs
{
    public abstract class Element
    {
        public event Action OnValueChanged;

        protected void ValueChanged()
        {
            OnValueChanged?.Invoke();
        }
    }
}