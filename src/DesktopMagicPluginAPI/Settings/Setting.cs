using System;

namespace DesktopMagicPluginAPI.Settings;

/// <summary>
/// The element base class.
/// </summary>
public abstract class Setting
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
        OnValueChanged?.Invoke();
    }
}