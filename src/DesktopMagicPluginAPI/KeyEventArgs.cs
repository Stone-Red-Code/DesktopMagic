using System;

namespace DesktopMagic.Api;

/// <summary>
/// Provides data for keyboard events.
/// </summary>
public class KeyEventArgs : EventArgs
{
    /// <summary>
    /// The key that was pressed or released.
    /// </summary>
    public Keys Key { get; }

    /// <summary>
    /// Whether the Alt modifier was pressed.
    /// </summary>
    public bool Alt { get; }

    /// <summary>
    /// Whether the Control modifier was pressed.
    /// </summary>
    public bool Control { get; }

    /// <summary>
    /// Whether the Shift modifier was pressed.
    /// </summary>
    public bool Shift { get; }

    /// <summary>
    /// Whether the Windows logo key was pressed.
    /// </summary>
    public bool Windows { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyEventArgs"/> class.
    /// </summary>
    public KeyEventArgs(Keys key, bool alt, bool control, bool shift, bool windows)
    {
        Key = key;
        Alt = alt;
        Control = control;
        Shift = shift;
        Windows = windows;
    }
}
