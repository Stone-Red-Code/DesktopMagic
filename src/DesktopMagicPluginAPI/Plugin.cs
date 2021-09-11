using DesktopMagicPluginAPI.Inputs;
using System;
using System.Drawing;

namespace DesktopMagicPluginAPI
{
    /// <summary>
    /// The plugin class.
    /// </summary>
    public abstract class Plugin
    {
        private IPluginData application = null;

        /// <summary>
        /// Informations about the main application.
        /// </summary>
        public IPluginData Application
        {
            get => application;
            set
            {
                if (application is null)
                {
                    application = value;
                }
                else
                {
                    throw new InvalidOperationException($"You cannot set the value of the {nameof(Application)} property");
                }
            }
        }

        /// <summary>
        /// Gets or sets the interval, expressed in milliseconds, at which to call the <see cref="Main"/> method.
        /// </summary>
        public virtual int UpdateInterval { get; set; } = 1000;

        /// <summary>
        /// Occurs once when the pugin gets activated.
        /// </summary>
        public virtual void Start()
        {
        }

        /// <summary>
        /// Occurs when the <see cref="UpdateInterval"/> elapses.
        /// </summary>
        /// <returns></returns>
        public abstract Bitmap Main();

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
        /// <param name="Delta">A value that indicates the amount that the mouse wheel has changed.</param>
        public virtual void OnMouseWheel(Point position, int Delta)
        {
        }
    }
}