using DesktopMagic.Plugins;

using System;

namespace DesktopMagic;

public interface IPluginWindow
{
    event Action? PluginLoaded;
    event Action? OnExit;

    bool IsRunning { get; }
    PluginMetadata PluginMetadata { get; }
    string PluginFolderPath { get; }
    string Title { get; set; }
    string ScreenDeviceName { get; }

    void Exit();
    void SetEditMode(bool enabled);
    void Show();
    void Hide();
    void Close();

    void ApplySettingValue(string id, string value);
    void ApplyButtonClick(string id);

    event System.ComponentModel.CancelEventHandler Closing;
}
