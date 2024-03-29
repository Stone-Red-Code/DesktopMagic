﻿using DesktopMagic.Plugins;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DesktopMagic.Settings;

internal class Layout(string name) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string name = name;
    private Theme theme = new Theme();
    private Dictionary<uint, PluginSettings> plugins = [];

    public Theme Theme
    {
        get => theme;
        set
        {
            theme = value;
            OnPropertyChanged();
        }
    }

    public Dictionary<uint, PluginSettings> Plugins
    {
        get => plugins;
        set
        {
            plugins = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => name;
        set
        {
            name = value;
            OnPropertyChanged();
        }
    }

    public void UpdatePlugins()
    {
        Plugins = Plugins.ToDictionary();
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}