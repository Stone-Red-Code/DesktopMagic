namespace DesktopMagicPluginAPI.Settings;

/// <summary>
/// Represents a check box control.
/// </summary>
public class CheckBox : Setting
{
    private bool _value;

    /// <summary>
    /// Gets or set a value indicating whether the <see cref="CheckBox"/> is in the checked state.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckBox"/> class with the provided <paramref name="value"/>.
    /// </summary>
    /// <param name="value">A value indicating whether the <see cref="CheckBox"/> is in the checked state.</param>
    public CheckBox(bool value)
    {
        Value = value;
    }
}