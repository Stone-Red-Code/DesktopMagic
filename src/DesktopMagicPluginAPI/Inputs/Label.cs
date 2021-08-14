namespace DesktopMagicPluginAPI.Inputs
{
    /// <summary>
    /// Represents a label control.
    /// </summary>
    public class Label : Element
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
        /// Gets or set a value indicating whether the content of the <see cref="Label"/> is bold or not.
        /// </summary>
        public bool Bold { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Label"/> class with the provided <paramref name="value"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="bold"></param>
        public Label(string value, bool bold = false)
        {
            Value = value;
            Bold = bold;
        }
    }
}