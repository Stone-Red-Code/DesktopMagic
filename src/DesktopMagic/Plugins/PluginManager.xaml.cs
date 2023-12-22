using DesktopMagic.DataContexts;
using DesktopMagic.Helpers;

using Modio.NET;
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

using File = System.IO.File;
using Path = System.IO.Path;

namespace DesktopMagic.Plugins;

/// <summary>
/// Interaction logic for PluginManager.xaml
/// </summary>
public partial class PluginManager : Window
{
    private readonly Client client = new Client(new Credentials("88e6ea774c3a502b06114e7fee0829ac"));
    private readonly HttpClient httpClient = new();

    private readonly PluginManagerDataContext pluginManagerDataContext = new();
    private readonly string pluginsPath = Path.Combine(App.ApplicationDataPath, "Plugins");

    public PluginManager()
    {
        InitializeComponent();

        DataContext = pluginManagerDataContext;
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

        IAsyncEnumerable<Mod> mods = client.Games[5665].Mods.Search().ToEnumerableAsync();
        await foreach (Mod mod in mods)
        {
            if (pluginIds.Contains(mod.Id))
            {
                continue;
            }

            pluginManagerDataContext.AllPlugins.Add(new PluginEntryDataContext(new(mod), new CommandHandler(async () => await Install(mod))));
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = pluginManagerDataContext.IsLoading;
    }

    private async Task Install(Mod mod)
    {
        pluginManagerDataContext.IsLoading = true;

        Debug.WriteLine(mod.Modfile?.Download?.BinaryUrl + " | " + pluginsPath);

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
}