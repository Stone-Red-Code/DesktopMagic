namespace DesktopMagicPluginAPI.Inputs
{
    public class TextBox : Element
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

        public TextBox(string value)
        {
            Value = value;
        }
    }
}