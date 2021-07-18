namespace DesktopMagicPluginAPI.Inputs
{
    public class CheckBox : Element
    {
        private bool _value;

        public bool Value
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

        public CheckBox(bool value)
        {
            Value = value;
        }
    }
}