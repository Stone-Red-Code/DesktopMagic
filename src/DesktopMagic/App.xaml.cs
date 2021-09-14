using AlwaysUpToDate;
using System;
using System.Diagnostics;
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
#if DEBUG
        private readonly Updater updater = new Updater(TimeSpan.FromDays(1), "https://raw.githubusercontent.com/Stone-Red-Code/DesktopMagic/develop/update/updateInfo.json");
#else
        private readonly Updater updater = new Updater(TimeSpan.FromHours(1), "https://raw.githubusercontent.com/Stone-Red-Code/DesktopMagic/main/update/updateInfo.json");
#endif

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
                Exit += CloseMutexHandler;

                updater.ProgressChanged += Updater_ProgressChanged;
                updater.OnException += Updater_OnException;
                updater.NoUpdateAvailible += Updater_NoUpdateAvailible;
                updater.UpdateAvailible += Updater_UpdateAvailible;
                updater.Start();
            }
        }

        private void Updater_UpdateAvailible(string version, string additionalInformation)
        {
            updater.Update();
        }

        private void Updater_NoUpdateAvailible()
        {
            Debug.WriteLine("No update avalible.");
        }

        private void Updater_OnException(Exception exception)
        {
            Debug.WriteLine("Update exception: " + exception);
        }

        private void Updater_ProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            Debug.WriteLine($"{progressPercentage}% {totalBytesDownloaded}/{totalFileSize}");
        }

        protected virtual void CloseMutexHandler(object sender, EventArgs e)
        {
            _mutex?.Close();
        }
    }
}