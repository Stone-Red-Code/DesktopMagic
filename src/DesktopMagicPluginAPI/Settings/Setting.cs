using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DesktopMagic")]

namespace DesktopMagic.Api.Settings;

/// <summary>
/// The element base class.
/// </summary>
public abstract class Setting
{
    /// <summary>
    /// Occurs when the value has been changed.
    /// </summary>
    public event Action? OnValueChanged;

    internal virtual string GetJsonValue()
    {
        return string.Empty;
    }

    internal virtual void SetJsonValue(string value)
    { }

    /// <summary>
    /// Triggers the <see cref="OnValueChanged"/> event.
    /// </summary>
    protected void ValueChanged()
    {
        OnValueChanged?.Invoke();
    }
}