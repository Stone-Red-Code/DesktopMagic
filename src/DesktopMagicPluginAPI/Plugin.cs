﻿using DesktopMagic.Api.Drawing;
using DesktopMagic.Api.Settings;

using System;
using System.Drawing;

namespace DesktopMagic.Api;

/// <summary>
/// The plugin class.
/// </summary>
public abstract class Plugin
{
    [Setting("horizontalAlignment", "Horizontal Alignment", -999)]
    internal ComboBox horizontalAlignment = new ComboBox("Center", "Left", "Right");

    [Setting("verticalAlignment", "Vertical Alignment", -998)]
    internal ComboBox verticalAlignment = new ComboBox("Center", "Top", "Bottom");

    private IPluginData application = null!;

    /// <summary>
    /// Informations about the main application.
    /// </summary>
    public IPluginData Application
    {
        get => application;
        set => application = application is null ? value : throw new InvalidOperationException($"You cannot set the value of the {nameof(Application)} property");
    }

    /// <summary>
    /// Gets or sets the interval, expressed in milliseconds, at which to call the <see cref="Main"/> method.
    /// </summary>
    public virtual int UpdateInterval { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the render quality of the bitmap image.
    /// </summary>
    public virtual RenderQuality RenderQuality { get; set; } = RenderQuality.High;

    /// <summary>
    /// Occurs once when the plugin gets activated.
    /// </summary>
    public virtual void Start()
    {
    }

    /// <summary>
    /// Occurs once when the plugin gets deactivated.
    /// </summary>
    public virtual void Stop()
    {
    }

    /// <summary>
    /// Occurs when the <see cref="UpdateInterval"/> elapses.
    /// </summary>
    /// <returns></returns>
    public abstract Bitmap? Main();

    /// <summary>
    /// Occurs when the window is clicked by the mouse.
    /// </summary>
    /// <param name="position">The x- and y-coordinates of the mouse pointer position relative to the plugin window.</param>
    /// <param name="mouseButton">The button associated with the event.</param>
    public virtual void OnMouseClick(Point position, MouseButton mouseButton)
    {
    }

    /// <summary>
    /// Occurs when the mouse pointer is moved over the control.
    /// </summary>
    /// <param name="position">The x- and y-coordinates of the mouse pointer position relative to the plugin window.</param>
    public virtual void OnMouseMove(Point position)
    {
    }

    /// <summary>
    /// Occurs when the user rotates the mouse wheel while the mouse pointer is over this element.
    /// </summary>
    /// <param name="position">The x- and y-coordinates of the mouse pointer position relative to the plugin window.</param>
    /// <param name="delta">A value that indicates the amount that the mouse wheel has changed.</param>
    public virtual void OnMouseWheel(Point position, int delta)
    {
    }
}