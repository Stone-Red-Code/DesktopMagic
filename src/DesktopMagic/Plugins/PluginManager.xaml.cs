using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Helpers;

using Modio;
using Modio.Filters;
using Modio.Models;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    private readonly string pluginDevelopmentPath = Path.Combine(App.ApplicationDataPath, "PluginDevelopment");

    private readonly DispatcherTimer searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300),
    };

    public PluginManager()
    {
        InitializeComponent();

        Resources.MergedDictionaries.Add(App.LanguageDictionary);

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

        if (Directory.Exists(pluginPath))
        {
            try
            {
                Directory.Delete(pluginPath, true);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.Message, "Plugin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                App.Logger.LogError(ex.Message, source: "PluginManager");
            }
        }

        if (pluginEntryDataContext is not null)
        {
            _ = pluginManagerDataContext.InstalledPlugins.Remove(pluginEntryDataContext);
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

            PluginMetadata? pluginMetadata = JsonSerializer.Deserialize<PluginMetadata>(await File.ReadAllTextAsync(pluginMetadataPath));

            if (pluginMetadata is not null)
            {
                pluginManagerDataContext.InstalledPlugins.Add(new PluginEntryDataContext(pluginMetadata, new CommandHandler(() => Remove(pluginPath, pluginMetadata.Id)), PluginEntryDataContext.Mode.Uninstall, pluginPath));
                _ = pluginIds.Add(pluginMetadata.Id);
            }
        }

        Filter filter = ModFilter.Popular.Desc().Limit(100);

        IAsyncEnumerable<Mod> mods = client.Games[modIoGameId].Mods.Search(filter).ToEnumerable();
        await foreach (Mod mod in mods)
        {
            if (pluginIds.Contains(mod.Id))
            {
                continue;
            }

            pluginManagerDataContext.AllPlugins.Add(new PluginEntryDataContext(new(mod), new CommandHandler(async () => await Install(mod)), PluginEntryDataContext.Mode.Install));
        }

        pluginManagerDataContext.IsLoading = false;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = pluginManagerDataContext.IsLoading;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex IdentifierNameRegex();

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
        await File.WriteAllTextAsync(pluginMetadataPath, JsonSerializer.Serialize(new PluginMetadata(mod)));

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

        pluginManagerDataContext.InstalledPlugins.Add(new PluginEntryDataContext(new PluginMetadata(mod), new CommandHandler(() => Remove(pluginPath, mod.Id)), PluginEntryDataContext.Mode.Uninstall));

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

        IAsyncEnumerable<Mod> mods = client.Games[modIoGameId].Mods.Search(filter).ToEnumerable();
        await foreach (Mod mod in mods)
        {
            if (pluginManagerDataContext.InstalledPlugins.Any(p => p.Id == mod.Id))
            {
                continue;
            }

            pluginManagerDataContext.AllPlugins.Add(new PluginEntryDataContext(new(mod), new CommandHandler(async () => await Install(mod)), PluginEntryDataContext.Mode.Install));
        }

        pluginManagerDataContext.IsSearching = false;
    }

    private void CreatePluginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CreateNewPlugin();
        }
        catch (Exception ex)
        {
            App.Logger.LogError(ex.Message, source: "PluginManager");
            _ = MessageBox.Show(ex.Message, "Plugin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CreateNewPlugin()
    {
        if (!FileUtilities.ExistsOnPath("dotnet.exe"))
        {
            _ = MessageBox.Show("The .NET SDK is required to create a plugin. Please install it and try again.", "Plugin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string pluginGuid = Guid.NewGuid().ToString();
        string pluginPath = Path.Combine(pluginsPath, pluginGuid);
        uint pluginId = (uint)Random.Shared.Next(1000, 9999);

        InputDialog inputDialog = new((string)FindResource("enterPluginName"), "Plugin Manager")
        {
            Owner = this,
        };

        if (inputDialog.ShowDialog() is not true)
        {
            return;
        }

        string pluginName = inputDialog.ResponseText;
        string pluginSafeName = pluginName.ToLower().Replace("_", " ");

        TextInfo info = CultureInfo.CurrentCulture.TextInfo;
        pluginSafeName = info.ToTitleCase(pluginSafeName);
        pluginSafeName = IdentifierNameRegex().Replace(pluginSafeName, "");

        if (!Directory.Exists(pluginDevelopmentPath))
        {
            _ = Directory.CreateDirectory(pluginDevelopmentPath);
        }

        string pluginProjectPath = Path.Combine(pluginDevelopmentPath, pluginSafeName);

        if (Directory.Exists(pluginProjectPath))
        {
            _ = MessageBox.Show("A plugin with the same name already exists. Please choose a different name.", "Plugin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        PluginMetadata pluginMetadata = new(pluginName, pluginId);

        _ = Directory.CreateDirectory(pluginPath);
        _ = Directory.CreateDirectory(pluginProjectPath);

        string pluginMetadataPath = Path.Combine(pluginPath, "metadata.json");
        File.WriteAllText(pluginMetadataPath, JsonSerializer.Serialize(pluginMetadata));

        string cmd = $"new classlib -n {pluginSafeName} -o {pluginProjectPath} -f net8.0 --target-framework-override net8.0-windows7";
        Process process = Process.Start("dotnet", cmd);
        process.WaitForExit();

        // Install the required NuGet packages
        process = Process.Start("dotnet", $"add {pluginProjectPath} package DesktopMagic.Api");
        process.WaitForExit();

        File.Move(Path.Combine(pluginProjectPath, "Class1.cs"), Path.Combine(pluginProjectPath, $"{pluginSafeName}.cs"));

        string code = $@"using DesktopMagic.Api;
using System.Drawing;

namespace {pluginSafeName};

public class {pluginSafeName}Plugin : Plugin
{{
    public override Bitmap Main()
    {{
        Bitmap bmp = new Bitmap(1000, 1000);

        using (Graphics g = Graphics.FromImage(bmp))
        {{
            g.Clear(Application.Theme.PrimaryColor); // Set the background color to the color specified in the DesktopMagic application.

            g.DrawString(""Hello World"", new Font(Application.Theme.Font, 100), Brushes.Black, new PointF(0, 0)); // Draw ""Hello World"" to the image.
        }}

        bmp.SetResolution(300, 300); // Set DPI to avoid scaling issues.

        return bmp; // Return the image.
    }}
}}
";

        File.WriteAllText(Path.Combine(pluginProjectPath, $"{pluginSafeName}.cs"), code);

        // Open the project in the default IDE

        string? associatedProgram = FileUtilities.GetAssociatedProgram(".csproj");

        if (associatedProgram is null)
        {
            _ = Process.Start("explorer.exe", pluginProjectPath);
            return;
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = associatedProgram,
            Arguments = Path.Combine(pluginProjectPath, $"{pluginSafeName}.csproj"),
        };

        _ = Process.Start(psi);
    }
}