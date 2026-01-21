using DesktopMagic.DataContexts;
using DesktopMagic.Dialogs;
using DesktopMagic.Helpers;

using Modio;
using Modio.Filters;
using Modio.Models;

using System;
using System.Collections.Generic;
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
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Linq;

using File = System.IO.File;
using Path = System.IO.Path;

namespace DesktopMagic.Plugins;

/// <summary>
/// Interaction logic for PluginManager.xaml
/// </summary>
public partial class PluginManager : Page
{
    private const int ModIoGameId = 5665;
    private const string ModIoApiKey = "88e6ea774c3a502b06114e7fee0829ac";
    private readonly HttpClient httpClient = new();
    private readonly PluginManagerDataContext pluginManagerDataContext = new();
    private readonly string pluginsPath = Path.Combine(App.ApplicationDataPath, "Plugins");
    private readonly string pluginDevelopmentPath = Path.Combine(App.ApplicationDataPath, "PluginDevelopment");
    private readonly Manager _manager = Manager.Instance;

    private bool changed = false;

    private readonly DispatcherTimer searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300),
    };

    private Client modIoClient;

    public PluginManager()
    {
        Resources.MergedDictionaries.Add(App.LanguageDictionary);

        string? modIoAccessToken = MainWindowDataContext.GetSettings().ModIoAccessToken;

        if (modIoAccessToken is null)
        {
            modIoClient = new Client(new Credentials(ModIoApiKey));
            App.Logger.LogInfo("Initialized mod.io client without authentication", source: "PluginManager");
        }
        else
        {
            modIoClient = new Client(new Credentials(ModIoApiKey, modIoAccessToken));
            pluginManagerDataContext.IsAuthenticated = true;
            App.Logger.LogInfo("Initialized mod.io client with authentication", source: "PluginManager");
        }

        InitializeComponent();

        DataContext = pluginManagerDataContext;
        searchTimer.Tick += async (sender, e) =>
        {
            searchTimer.Stop();
            await SearchAllPlugins(pluginManagerDataContext.AllPluginsSearchText);
        };

        Loaded += PluginManager_Loaded;
        Unloaded += PluginManager_Unloaded;
    }

    private async void PluginManager_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializePluginManager();
    }

    private void PluginManager_Unloaded(object sender, RoutedEventArgs e)
    {
        searchTimer.Stop();

        if (changed)
        {
            _manager.ReloadPlugins();
        }
    }

    private async Task InitializePluginManager()
    {
        App.Logger.LogInfo("Initializing Plugin Manager", source: "PluginManager");
        HashSet<uint> pluginIds = [];

        pluginManagerDataContext.IsLoading = true;
        pluginManagerDataContext.InstalledPlugins.Clear();
        pluginManagerDataContext.AllPlugins.Clear();

        foreach (string pluginPath in Directory.GetDirectories(pluginsPath))
        {
            string pluginMetadataPath = Path.Combine(pluginPath, "metadata.json");

            if (!File.Exists(pluginMetadataPath))
            {
                App.Logger.LogWarn($"Plugin metadata not found at: {pluginMetadataPath}", source: "PluginManager");
                continue;
            }

            PluginMetadata? pluginMetadata = JsonSerializer.Deserialize<PluginMetadata>(await File.ReadAllTextAsync(pluginMetadataPath));

            if (pluginMetadata is null)
            {
                App.Logger.LogWarn($"Failed to deserialize plugin metadata from: {pluginMetadataPath}", source: "PluginManager");
                continue;
            }

            string csprojPath = GetCsprojPath(pluginMetadata.Name);

            App.Logger.LogInfo($"Loaded plugin: {pluginMetadata.Name} (ID: {pluginMetadata.Id})", source: "PluginManager");
            pluginManagerDataContext.InstalledPlugins.Add(new PluginEntryDataContext(pluginMetadata, new CommandHandler(async () => await Remove(pluginPath, pluginMetadata.Id)), PluginEntryDataContext.Mode.Uninstall, pluginPath, csprojPath));
            _ = pluginIds.Add(pluginMetadata.Id);

            if (pluginMetadata.IsLocalPlugin)
            {
                continue;
            }

            try
            {
                Mod mod = await modIoClient.Games[ModIoGameId].Mods[pluginMetadata.Id].Get();

                if (DateTimeOffset.FromUnixTimeSeconds(mod.DateUpdated).DateTime > pluginMetadata.Updated && pluginMetadata.SupportsUnloading)
                {
                    App.Logger.LogInfo($"Plugin {pluginMetadata.Id} has an update available, reinstalling", source: "PluginManager");
                    await Remove(pluginPath, pluginMetadata.Id);
                    await Install(mod);
                    pluginManagerDataContext.IsLoading = true;
                }
            }
            catch (Exception ex)
            {
                App.Logger.LogError($"Failed to check for plugin {pluginMetadata.Id} updates: {ex.Message}", source: "PluginManager");
            }
        }

        Filter filter = ModFilter.Popular.Desc().Limit(100);

        try
        {
            App.Logger.LogInfo("Fetching popular plugins from mod.io", source: "PluginManager");
            IAsyncEnumerable<Mod> mods = modIoClient.Games[ModIoGameId].Mods.Search(filter).ToEnumerable();
            await foreach (Mod mod in mods)
            {
                if (pluginIds.Contains(mod.Id))
                {
                    continue;
                }

                pluginManagerDataContext.AllPlugins.Add(new PluginEntryDataContext(new(mod), new CommandHandler(async () => await Install(mod)), PluginEntryDataContext.Mode.Install));
            }
            App.Logger.LogInfo($"Loaded {pluginManagerDataContext.AllPlugins.Count} available plugins", source: "PluginManager");
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Failed to fetch plugins from mod.io: {ex.Message}", source: "PluginManager");
        }

        await SyncPlugins();

        pluginManagerDataContext.IsLoading = false;
        App.Logger.LogInfo("Plugin Manager initialization complete", source: "PluginManager");
    }

    public async Task Remove(string pluginPath, uint id)
    {
        App.Logger.LogInfo($"Removing plugin with ID {id} from path: {pluginPath}", source: "PluginManager");
        pluginManagerDataContext.IsLoading = true;
        changed = true;

        PluginEntryDataContext? pluginEntryDataContext = pluginManagerDataContext.InstalledPlugins.FirstOrDefault(p => p.Id == id);

        if (Directory.Exists(pluginPath))
        {
            try
            {
                Directory.Delete(pluginPath, true);
                App.Logger.LogInfo($"Successfully deleted plugin directory: {pluginPath}", source: "PluginManager");
            }
            catch (Exception ex)
            {
                Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Plugin Manager",
                    Content = ex.Message,
                    CloseButtonText = "Ok"
                };
                _ = await messageBox.ShowDialogAsync();
                App.Logger.LogError(ex.Message, source: "PluginManager");
            }
        }

        if (pluginEntryDataContext is not null)
        {
            _ = pluginManagerDataContext.InstalledPlugins.Remove(pluginEntryDataContext);
            App.Logger.LogInfo($"Removed plugin {id} from installed plugins list", source: "PluginManager");
        }

        if (pluginManagerDataContext.IsAuthenticated)
        {
            try
            {
                await modIoClient.Games[ModIoGameId].Mods.Unsubscribe(id);
                App.Logger.LogInfo($"Unsubscribed from plugin {id} on mod.io", source: "PluginManager");
            }
            catch (Exception ex)
            {
                App.Logger.LogError($"Failed to unsubscribe from plugin {id}: {ex.Message}", source: "PluginManager");
            }
        }

        pluginManagerDataContext.IsLoading = false;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9]")]
    private static partial Regex IdentifierNameRegex();

    private async Task Install(Mod mod)
    {
        App.Logger.LogInfo($"Installing plugin: {mod.Name} (ID: {mod.Id})", source: "PluginManager");
        pluginManagerDataContext.IsLoading = true;
        changed = true;

        if (mod.Modfile?.Download?.BinaryUrl is null)
        {
            App.Logger.LogWarn($"Plugin {mod.Id} has no download URL", source: "PluginManager");
            return;
        }

        string pluginGuid = Guid.NewGuid().ToString();
        string pluginPath = Path.Combine(pluginsPath, pluginGuid);
        string zipFilePath = Path.Combine(pluginsPath, pluginGuid + ".zip");

        try
        {
            App.Logger.LogInfo($"Downloading plugin from: {mod.Modfile.Download.BinaryUrl}", source: "PluginManager");
            using (Stream fileStream = await httpClient.GetStreamAsync(mod.Modfile.Download.BinaryUrl))
            {
                using FileStream outputFileStream = new FileStream(zipFilePath, FileMode.Create);
                await fileStream.CopyToAsync(outputFileStream);
            }
            App.Logger.LogInfo($"Plugin downloaded to: {zipFilePath}", source: "PluginManager");

            using (ZipArchive zipArchive = ZipFile.OpenRead(zipFilePath))
            {
                zipArchive.ExtractToDirectory(pluginPath);
            }
            App.Logger.LogInfo($"Plugin extracted to: {pluginPath}", source: "PluginManager");

            string pluginMetadataPath = Path.Combine(pluginPath, "metadata.json");
            await File.WriteAllTextAsync(pluginMetadataPath, JsonSerializer.Serialize(new PluginMetadata(mod)));

            File.Delete(zipFilePath);

            if (!File.Exists(Path.Combine(pluginPath, "main.dll")) && !File.Exists(Path.Combine(pluginPath, "main.html")))
            {
                App.Logger.LogError($"Plugin {mod.Id} does not contain main.dll or main.html", source: "PluginManager");
                await Remove(pluginPath, mod.Id);
                pluginManagerDataContext.IsLoading = false;
                Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
                {
                    Title = "Plugin Manager",
                    Content = "The plugin you are trying to install does not contain a \"main.dll\" or \"main.html\" file. Please contact the plugin author.",
                    CloseButtonText = "Ok"
                };
                _ = await messageBox.ShowDialogAsync();
                return;
            }

            PluginEntryDataContext? pluginEntryDataContext = pluginManagerDataContext.AllPlugins.FirstOrDefault(p => p.Id == mod.Id);

            if (pluginEntryDataContext is not null)
            {
                _ = pluginManagerDataContext.AllPlugins.Remove(pluginEntryDataContext);
            }

            if (pluginManagerDataContext.IsAuthenticated)
            {
                try
                {
                    await modIoClient.Games[ModIoGameId].Mods.Subscribe(mod.Id);
                    App.Logger.LogInfo($"Subscribed to plugin {mod.Id} on mod.io", source: "PluginManager");
                }
                catch (Exception ex)
                {
                    App.Logger.LogError($"Failed to subscribe to plugin {mod.Id}: {ex.Message}", source: "PluginManager");
                }
            }

            pluginManagerDataContext.InstalledPlugins.Add(new PluginEntryDataContext(new PluginMetadata(mod), new CommandHandler(async () => await Remove(pluginPath, mod.Id)), PluginEntryDataContext.Mode.Uninstall, pluginPath));
            App.Logger.LogInfo($"Successfully installed plugin: {mod.Name} (ID: {mod.Id})", source: "PluginManager");
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Failed to install plugin {mod.Id}: {ex.Message}", source: "PluginManager");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Plugin Manager",
                Content = $"Failed to install plugin: {ex.Message}",
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
        }

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
        App.Logger.LogInfo($"Opened mod.io page: {uri}", source: "PluginManager");
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
        App.Logger.LogInfo($"Searching plugins with query: {searchString}", source: "PluginManager");
        pluginManagerDataContext.AllPlugins.Clear();

        if (!searchString.Contains('*'))
        {
            searchString = $"*{searchString.Trim()}*";
        }

        Filter filter = ModFilter.Name.Like($"{searchString}").And(ModFilter.Popular.Desc()).Limit(100);

        try
        {
            IAsyncEnumerable<Mod> mods = modIoClient.Games[ModIoGameId].Mods.Search(filter).ToEnumerable();
            await foreach (Mod mod in mods)
            {
                if (pluginManagerDataContext.InstalledPlugins.Any(p => p.Id == mod.Id))
                {
                    continue;
                }

                pluginManagerDataContext.AllPlugins.Add(new PluginEntryDataContext(new(mod), new CommandHandler(async () => await Install(mod)), PluginEntryDataContext.Mode.Install));
            }
            App.Logger.LogInfo($"Search completed: {pluginManagerDataContext.AllPlugins.Count} plugins found", source: "PluginManager");
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Plugin search failed: {ex.Message}", source: "PluginManager");
        }

        pluginManagerDataContext.IsSearching = false;
    }

    private async void ReloadPluginsButton_Click(object sender, RoutedEventArgs e)
    {
        _manager.ReloadPlugins();
        await InitializePluginManager();
        changed = false;
    }

    private async void CreatePluginButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await CreateNewPlugin();
        }
        catch (Exception ex)
        {
            App.Logger.LogError(ex.Message, source: "PluginManager");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Plugin Manager",
                Content = ex.Message,
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
        }
    }

    private async Task CreateNewPlugin()
    {
        App.Logger.LogInfo("Creating new plugin", source: "PluginManager");

        if (!FileUtilities.ExistsOnPath("dotnet.exe"))
        {
            App.Logger.LogError(".NET SDK not found on PATH", source: "PluginManager");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Plugin Manager",
                Content = "The .NET SDK is required to create a plugin. Please install it and try again.",
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
            return;
        }

        string pluginGuid = Guid.NewGuid().ToString();
        string pluginPath = Path.Combine(pluginsPath, pluginGuid);
        uint pluginId = (uint)Random.Shared.Next(1000, 9999);

        InputDialog inputDialog = new((string)FindResource("enterPluginName"), "Plugin Manager")
        {
            Owner = Window.GetWindow(this),
        };

        if (inputDialog.ShowDialog() != true)
        {
            App.Logger.LogInfo("Plugin creation cancelled by user", source: "PluginManager");
            return;
        }

        string pluginName = inputDialog.ResponseText;
        string pluginSafeName = GetPluginSafeName(pluginName);

        App.Logger.LogInfo($"Creating plugin with name: {pluginName} (safe name: {pluginSafeName})", source: "PluginManager");

        if (!Directory.Exists(pluginDevelopmentPath))
        {
            _ = Directory.CreateDirectory(pluginDevelopmentPath);
        }

        string pluginProjectPath = Path.Combine(pluginDevelopmentPath, pluginSafeName);

        if (Directory.Exists(pluginProjectPath))
        {
            App.Logger.LogWarn($"Plugin project already exists: {pluginProjectPath}", source: "PluginManager");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Plugin Manager",
                Content = "A plugin with the same name already exists. Please choose a different name.",
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
            return;
        }

        pluginManagerDataContext.IsLoading = true;

        PluginMetadata pluginMetadata = new(pluginName, pluginId);

        _ = Directory.CreateDirectory(pluginPath);
        _ = Directory.CreateDirectory(pluginProjectPath);

        string pluginMetadataPath = Path.Combine(pluginPath, "metadata.json");
        await File.WriteAllTextAsync(pluginMetadataPath, JsonSerializer.Serialize(pluginMetadata));

        App.Logger.LogInfo($"Creating .NET project at: {pluginProjectPath}", source: "PluginManager");
        string cmd = $"new classlib -n {pluginSafeName} -o {pluginProjectPath} -f net8.0 --target-framework-override net8.0-windows7";
        Process process = Process.Start("dotnet", cmd);
        await process.WaitForExitAsync();

        App.Logger.LogInfo("Installing NuGet package: DesktopMagic.Api", source: "PluginManager");
        process = Process.Start("dotnet", $"add {pluginProjectPath} package DesktopMagic.Api");
        await process.WaitForExitAsync();

        File.Move(Path.Combine(pluginProjectPath, "Class1.cs"), Path.Combine(pluginProjectPath, $"{pluginSafeName}.cs"));

        string code = $@"using DesktopMagic.Api;
using System.Drawing;

namespace {pluginSafeName};

public class {pluginSafeName}Plugin : Plugin
{{
    public override Bitmap Main()
    {{
        Bitmap bmp = new Bitmap(2000, 1000);
        bmp.SetResolution(100, 100); // Set DPI to avoid scaling issues.

        using (Graphics g = Graphics.FromImage(bmp))
        {{
            g.DrawString(""Hello World!"", new Font(Application.Theme.Font, 100), new SolidBrush(Application.Theme.PrimaryColor), new PointF(0, 0)); // Draw ""Hello World"" to the image.
        }}

        return bmp; // Return the image.
    }}
}}
";

        string csprojPath = Path.Combine(pluginProjectPath, $"{pluginSafeName}.csproj");

        await File.WriteAllTextAsync(Path.Combine(pluginProjectPath, $"{pluginSafeName}.cs"), code);

        XDocument doc = XDocument.Load(csprojPath);
        XElement? propertyGroup = doc.Root?.Element("PropertyGroup");

        if (propertyGroup is not null)
        {
            XElement? outputPathElement = propertyGroup.Element("OutputPath");

            if (outputPathElement is not null)
            {
                outputPathElement.Value = pluginPath;
            }
            else
            {
                propertyGroup.Add(new XElement("OutputPath", pluginPath));
            }

            XElement? appendTargetFrameworkToOutputPathElement = propertyGroup.Element("AppendTargetFrameworkToOutputPath");

            if (appendTargetFrameworkToOutputPathElement is not null)
            {
                appendTargetFrameworkToOutputPathElement.Value = "false";
            }
            else
            {
                propertyGroup.Add(new XElement("AppendTargetFrameworkToOutputPath", "false"));
            }

            XElement? targetNameElement = propertyGroup.Element("TargetName");

            if (targetNameElement is not null)
            {
                targetNameElement.Value = "main";
            }
            else
            {
                propertyGroup.Add(new XElement("TargetName", "main"));
            }

            doc.Save(csprojPath);
            App.Logger.LogInfo("Updated .csproj file with custom build settings", source: "PluginManager");
        }
        else
        {
            App.Logger.LogError("PropertyGroup element not found in .csproj", source: "PluginManager");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Error",
                Content = "PropertyGroup element not found in .csproj",
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
            return;
        }

        App.Logger.LogInfo("Building plugin project", source: "PluginManager");
        process = Process.Start("dotnet", $"build {csprojPath} -c Release");
        await process.WaitForExitAsync();

        changed = true;
        await InitializePluginManager();

        string? associatedProgram = FileUtilities.GetAssociatedProgram(".csproj");

        if (associatedProgram is null)
        {
            App.Logger.LogInfo($"Opening plugin project in Explorer: {pluginProjectPath}", source: "PluginManager");
            _ = Process.Start("explorer.exe", pluginProjectPath);
        }
        else
        {
            App.Logger.LogInfo($"Opening plugin project in IDE: {associatedProgram}", source: "PluginManager");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = associatedProgram,
                Arguments = csprojPath,
            };

            _ = Process.Start(psi);
        }

        App.Logger.LogInfo($"Successfully created plugin: {pluginName}", source: "PluginManager");

        pluginManagerDataContext.IsLoading = false;
    }

    private async void LogInButton_Click(object sender, RoutedEventArgs e)
    {
        if (pluginManagerDataContext.IsAuthenticated)
        {
            App.Logger.LogInfo("Logging out from mod.io", source: "PluginManager");
            MainWindowDataContext.GetSettings().ModIoAccessToken = null;
            pluginManagerDataContext.IsAuthenticated = false;
            return;
        }

        App.Logger.LogInfo("Starting mod.io authentication", source: "PluginManager");

        try
        {
            InputDialog inputDialog = new((string)FindResource("enterModIoEmail"), "Plugin Manager")
            {
                Owner = Window.GetWindow(this),
            };

            if (inputDialog.ShowDialog() != true)
            {
                App.Logger.LogInfo("Authentication cancelled by user", source: "PluginManager");
                return;
            }

            App.Logger.LogInfo($"Requesting authentication code for email: {inputDialog.ResponseText}", source: "PluginManager");
            await modIoClient.Auth.RequestCode(ModIoApiKey, inputDialog.ResponseText);

            inputDialog = new((string)FindResource("enterModIoAccessToken"), "Plugin Manager")
            {
                Owner = Window.GetWindow(this),
            };

            if (inputDialog.ShowDialog() != true)
            {
                App.Logger.LogInfo("Authentication cancelled by user", source: "PluginManager");
                return;
            }

            pluginManagerDataContext.IsLoading = true;

            AccessToken accessToken = await modIoClient.Auth.SecurityCode(ModIoApiKey, inputDialog.ResponseText);

            if (accessToken.Value is not null)
            {
                modIoClient = new Client(new Credentials(ModIoApiKey, accessToken.Value));
                App.Logger.LogInfo("Successfully authenticated with mod.io", source: "PluginManager");
            }

            MainWindowDataContext.GetSettings().ModIoAccessToken = accessToken.Value;
            pluginManagerDataContext.IsAuthenticated = true;

            App.Logger.LogInfo("Subscribing to installed plugins on mod.io", source: "PluginManager");
            foreach (PluginEntryDataContext plugin in pluginManagerDataContext.InstalledPlugins)
            {
                if (!plugin.IsLocalPlugin)
                {
                    try
                    {
                        await modIoClient.Games[ModIoGameId].Mods.Subscribe(plugin.Id);
                        App.Logger.LogInfo($"Subscribed to plugin {plugin.Id}", source: "PluginManager");
                    }
                    catch (Exception ex)
                    {
                        App.Logger.LogError($"Failed to subscribe to plugin {plugin.Id}: {ex.Message}", source: "PluginManager");
                    }
                }
            }

            await SyncPlugins();
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Authentication failed: {ex.Message}", source: "PluginManager");
            Wpf.Ui.Controls.MessageBox messageBox = new Wpf.Ui.Controls.MessageBox
            {
                Title = "Plugin Manager",
                Content = ex.Message,
                CloseButtonText = "Ok"
            };
            _ = await messageBox.ShowDialogAsync();
        }

        pluginManagerDataContext.IsLoading = false;
    }

    private async Task SyncPlugins()
    {
        if (!pluginManagerDataContext.IsAuthenticated)
        {
            return;
        }

        App.Logger.LogInfo("Syncing plugins with mod.io subscriptions", source: "PluginManager");

        try
        {
            IReadOnlyList<Mod> mods = await modIoClient.User.GetSubscriptions(ModFilter.GameId.Eq(ModIoGameId)).ToList();
            App.Logger.LogInfo($"Found {mods.Count} subscribed plugins on mod.io", source: "PluginManager");

            List<PluginEntryDataContext> pluginsToRemove = [];

            foreach (PluginEntryDataContext plugin in pluginManagerDataContext.InstalledPlugins)
            {
                if (!plugin.IsLocalPlugin && !mods.Any(m => m.Id == plugin.Id))
                {
                    App.Logger.LogInfo($"Plugin {plugin.Id} is no longer subscribed, marking for removal", source: "PluginManager");
                    pluginsToRemove.Add(plugin);
                }
            }

            foreach (PluginEntryDataContext plugin in pluginsToRemove)
            {
                if (plugin.Path is null)
                {
                    continue;
                }

                await Remove(plugin.Path, plugin.Id);
            }

            IEnumerable<Mod> notInstalledMods = mods.Where(m => !pluginManagerDataContext.InstalledPlugins.Any(p => p.Id == m.Id));

            foreach (Mod mod in notInstalledMods)
            {
                App.Logger.LogInfo($"Installing subscribed plugin: {mod.Name} (ID: {mod.Id})", source: "PluginManager");
                await Install(mod);
            }

            App.Logger.LogInfo("Plugin sync completed", source: "PluginManager");
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Plugin sync failed: {ex.Message}", source: "PluginManager");
        }
    }

    private static string GetPluginSafeName(string pluginName)
    {
        string pluginSafeName = pluginName.ToLower().Replace("_", " ");

        TextInfo info = CultureInfo.CurrentCulture.TextInfo;
        pluginSafeName = info.ToTitleCase(pluginSafeName);
        pluginSafeName = IdentifierNameRegex().Replace(pluginSafeName, "");

        return pluginSafeName;
    }

    private string GetCsprojPath(string pluginName)
    {
        string pluginSafeName = GetPluginSafeName(pluginName);
        return Path.Combine(pluginDevelopmentPath, pluginSafeName, $"{pluginSafeName}.csproj");
    }
}