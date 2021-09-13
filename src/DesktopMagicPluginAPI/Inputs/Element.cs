using System;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Inputs
{
    /// <summary>
    /// The element base class.
    /// </summary>
    public abstract class Element
    {
        /// <summary>
        /// Occurs when the value has been changed.
        /// </summary>
        public event Action OnValueChanged;

        /// <summary>
        /// Triggers the <see cref="OnValueChanged"/> event.
        /// </summary>
        protected void ValueChanged()
        {
            _ = Task.Run(() => OnValueChanged?.Invoke());
        }
    }
}