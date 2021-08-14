using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopMagicPluginAPI.Inputs
{
    /// <summary>
    /// Represents a button control.
    /// </summary>
    public class Button : Element
    {
        /// <summary>
        /// Occurs when the button is clicked.
        /// </summary>
        public event Action OnClick;

        private string _value;

        /// <summary>
        /// Gets or sets the text caption displayed in the <see cref="Button"/> element.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Button"/> class with the provided <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The text caption displayed in the <see cref="Button"/> control.</param>
        public Button(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Triggers the <see cref="OnClick"/> event.
        /// </summary>
        public void Click()
        {
            OnClick?.Invoke();
        }
    }
}