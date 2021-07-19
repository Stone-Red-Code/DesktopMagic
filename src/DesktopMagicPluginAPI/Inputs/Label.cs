namespace DesktopMagicPluginAPI.Inputs
{
    public class Label : Element
    {
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

        public bool Bold { get; set; }

        public Label(string value, bool bold = false)
        {
            Value = value;
            Bold = bold;
        }
    }
}