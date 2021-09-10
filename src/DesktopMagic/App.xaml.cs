using System;
using System.Threading;
using System.Windows;

namespace DesktopMagic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex _mutex;

        public App()
        {
            // Try to grab mutex
            bool createdNew;
            _mutex = new Mutex(true, $"Stone_Red{DesktopMagic.MainWindow.AppName}", out createdNew);

            //check if creating new was succesfull
            if (!createdNew)
            {
                //Shutdown Aplication
                Current.Shutdown();
            }
            else
            {
                // Add Event handler to exit event.
                Exit += CloseMutexHandler;
            }
        }

        protected virtual void CloseMutexHandler(object sender, EventArgs e)
        {
            _mutex?.Close();
        }
    }
}