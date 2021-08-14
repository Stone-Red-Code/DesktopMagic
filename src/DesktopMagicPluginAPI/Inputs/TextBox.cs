namespace DesktopMagicPluginAPI.Inputs
{
    /// <summary>
    /// Represents a text box control.
    /// </summary>
    public class TextBox : Element
    {
        private string _value;

        /// <summary>
        /// Gets or sets the text associated with this control.
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
        /// Initializes a new instance of the <see cref="TextBox"/> class with the provided <paramref name="value"/>.
        /// </summary>
        /// <param name="value"></param>
        public TextBox(string value)
        {
            Value = value;
        }
    }
}