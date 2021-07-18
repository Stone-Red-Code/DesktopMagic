using System;

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