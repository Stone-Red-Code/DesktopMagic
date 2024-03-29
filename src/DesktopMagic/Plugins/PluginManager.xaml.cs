﻿using DesktopMagic.DataContexts;
using DesktopMagic.Helpers;

using Modio.NET;
using Modio.NET.Filters;
using Modio.NET.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using File = System.IO.File;
using Path = System.IO.Path;

namespace DesktopMagic.Plugins;

/// <summary>
/// Interaction logic for PluginManager.xaml
/// </summary>
public partial class PluginManager : Window
{
    private const int modIoGameId = 5665;
    private readonly Client client = new Client(new Credentials("88e6ea774c3a502b06114e7fee0829ac"));
    private readonly HttpClient httpClient = new();

    private readonly PluginManagerDataContext pluginManagerDataContext = new();
    private readonly string pluginsPath = Path.Combine(App.ApplicationDataPath, "Plugins");

    private readonly DispatcherTimer searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300),
    };

    public PluginManager()
    {
        InitializeComponent();

        DataContext = pluginManagerDataContext;
        searchTimer.Tick += async (sender, e) =>
        {
            searchTimer.Stop();
            await SearchAllPlugins(pluginManagerDataContext.AllPluginsSearchText);
        };
    }

    public void Remove(string pluginPath, uint? id)
    {
        pluginManagerDataContext.IsLoading = true;

        PluginEntryDataContext? pluginEntryDataContext = pluginManagerDataContext.InstalledPlugins.FirstOrDefault(p => p.Id == id);

        if (pluginEntryDataContext is not null)
        {
            _ = pluginManagerDataContext.InstalledPlugins.Remove(pluginEntryDataContext);
        }

        if (Directory.Exists(pluginPath))
        {
            Directory.Delete(pluginPath, true);
        }

        pluginManagerDataContext.IsLoading = false;
    }

    protected override async void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        HashSet<uint> pluginIds = [];

        foreach (string pluginPath in Directory.GetDirectories(pluginsPath))
        {
            string pluginMetadataPath = Path.Combine(pluginPath, "metadata.json");
            if (!File.Exists(pluginMetadataPath))
            {
                continue;
            }

            PluginMetadata? pluginMetadata = JsonSerializer.Deserialize<PluginMetadata>(File.ReadAllText(pluginMetadataPath));

            if (pluginMetadata is not null)
            {
                pluginManagerDataContext.InstalledPlugins.Add(new PluginEntryDataContext(pluginMetadata, new CommandHandler(() => Remove(pluginPath, pluginMetadata.Id)), true));
                _ = pluginIds.Add(pluginMetadata.Id);
            }
        }

        Filter filter = ModFilter.Popular.Desc().Limit(100);

        IAsyncEnumerable<Mod> mods = client.Games[modIoGameId].Mods.Search(filter).ToEnumerableAsync();
        await foreach (Mod mod in mods)
        {
            if (pluginIds.Contains(mod.Id))
            {
                continue;
            }

            pluginManagerDataContext.AllPlugins.Add(new PluginEntryDataContext(new(mod), new CommandHandler(async () => await Install(mod))));
        }

        pluginManagerDataContext.IsLoading = false;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = pluginManagerDataContext.IsLoading;
    }

    private async Task Install(Mod mod)
    {
        pluginManagerDataContext.IsLoading = true;

        if (mod.Modfile?.Download?.BinaryUrl is null)
        {
            return;
        }

        string pluginGuid = Guid.NewGuid().ToString();
        string pluginPath = Path.Combine(pluginsPath, pluginGuid);
        string zipFilePath = Path.Combine(pluginsPath, pluginGuid + ".zip");

        using (Stream fileStream = await httpClient.GetStreamAsync(mod.Modfile.Download.BinaryUrl))
        {
            using FileStream outputFileStream = new FileStream(zipFilePath, FileMode.Create);

            await fileStream.CopyToAsync(outputFileStream);
        }

        using (ZipArchive zipArchive = ZipFile.OpenRead(zipFilePath))
        {
            zipArchive.ExtractToDirectory(pluginPath);
        }

        string pluginMetadataPath = Path.Combine(pluginPath, "metadata.json");
        File.WriteAllText(pluginMetadataPath, JsonSerializer.Serialize(new PluginMetadata(mod)));

        File.Delete(zipFilePath);

        if (!File.Exists(Path.Combine(pluginPath, "main.dll")))
        {
            Remove(pluginPath, mod.Id);
            pluginManagerDataContext.IsLoading = false;
            _ = MessageBox.Show("The plugin you are trying to install does not contain a \"main.dll\" file. Please contact the plugin author.", "Plugin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        PluginEntryDataContext? pluginEntryDataContext = pluginManagerDataContext.AllPlugins.FirstOrDefault(p => p.Id == mod.Id);

        if (pluginEntryDataContext is not null)
        {
            _ = pluginManagerDataContext.AllPlugins.Remove(pluginEntryDataContext);
        }

        pluginManagerDataContext.InstalledPlugins.Add(new PluginEntryDataContext(new PluginMetadata(mod), new CommandHandler(() => Remove(pluginPath, mod.Id)), true));

        pluginManagerDataContext.IsLoading = false;
    }

    private void Image_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        string uri = "https://mod.io/g/DesktopMagic";
        ProcessStartInfo psi = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = uri
        };
        _ = Process.Start(psi);
    }

    private void AllPluginsSearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        pluginManagerDataContext.IsSearching = true;
        searchTimer.Stop();
        searchTimer.Start();
    }

    private void InstalledPluginsSearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(pluginManagerDataContext.InstalledPluginsSearchText))
        {
            foreach (PluginEntryDataContext pluginEntryDataContext in pluginManagerDataContext.InstalledPlugins)
            {
                pluginEntryDataContext.IsVisible = true;
            }
        }
        else
        {
            foreach (PluginEntryDataContext pluginEntryDataContext in pluginManagerDataContext.InstalledPlugins)
            {
                pluginEntryDataContext.IsVisible = pluginEntryDataContext.Name.Contains(pluginManagerDataContext.InstalledPluginsSearchText, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    private async Task SearchAllPlugins(string searchString)
    {
        pluginManagerDataContext.AllPlugins.Clear();

        if (!searchString.Contains('*'))
        {
            searchString = $"*{searchString.Trim()}*";
        }

        Filter filter = ModFilter.Name.Like($"{searchString}").And(ModFilter.Popular.Desc()).Limit(100);

        IAsyncEnumerable<Mod> mods = client.Games[modIoGameId].Mods.Search(filter).ToEnumerableAsync();
        await foreach (Mod mod in mods)
        {
            if (pluginManagerDataContext.InstalledPlugins.Any(p => p.Id == mod.Id))
            {
                continue;
            }

            pluginManagerDataContext.AllPlugins.Add(new PluginEntryDataContext(new(mod), new CommandHandler(async () => await Install(mod))));
        }

        pluginManagerDataContext.IsSearching = false;
    }
}