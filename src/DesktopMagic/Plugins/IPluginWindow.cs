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

    /// <summary>
    /// Pauses or resumes rendering of the plugin window (e.g. when it is fully occluded by other windows).
    /// </summary>
    void SetRenderPaused(bool paused);

    event System.ComponentModel.CancelEventHandler Closing;
}
