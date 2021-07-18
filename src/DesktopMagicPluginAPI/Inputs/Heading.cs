namespace DesktopMagicPluginAPI.Inputs
{
    public class Heading : Element
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

        public Heading(string value)
        {
            Value = value;
        }
    }
}