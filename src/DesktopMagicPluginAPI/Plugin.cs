using System;
using System.Drawing;

namespace DesktopMagicPluginAPI
{
    public abstract class Plugin
    {
        private IPluginData application = null;

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

        public virtual int UpdateInterval { get; set; } = 1000;

        public abstract Bitmap Main();

        public virtual void OnMouseClick(Point position)
        {
        }

        public virtual void OnMouseMove(Point position)
        {
        }
    }
}