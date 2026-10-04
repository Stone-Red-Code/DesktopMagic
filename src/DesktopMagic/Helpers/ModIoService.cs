using DesktopMagic.Dialogs;
using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using Modio;
using Modio.Filters;
using Modio.Models;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;

using File = System.IO.File;
using Path = System.IO.Path;

namespace DesktopMagic.Helpers;

/// <summary>
/// Shared mod.io access: authentication and sharing/syncing of layouts and themes.
/// </summary>
internal static class ModIoService
{
    public const uint GameId = 5665;
    public const string ApiKey = "88e6ea774c3a502b06114e7fee0829ac";

    private const string Source = "ModIo";
    private const int MaxSummaryLength = 250;

    private static readonly HttpClient httpClient = new();
    private static (string Token, uint UserId)? currentUser;

    public static bool IsAuthenticated => AppSettings.ModIoAccessToken is not null;

    private static DesktopMagicSettings AppSettings => Manager.Instance.Settings;

    public static Client CreateClient()
    {
        Client.Builder builder = CreateBuilder();

        if (AppSettings.ModIoAccessToken is string token)
        {
            _ = builder.WithToken(token);
        }

        return builder.Build();
    }

    /// <summary>
    /// The shared api.mod.io host is deprecated, requests have to go to the game specific host.
    /// </summary>
    private static Client.Builder CreateBuilder()
    {
        return Client.GetBuilder(ApiKey).WithGameHost(GameId);
    }

    /// <summary>
    /// Whether the mod is a plugin (as opposed to a shared layout or theme).
    /// </summary>
    public static bool IsPluginMod(Mod mod)
    {
        return !mod.Tags.Any(tag => tag.Name is SharePackage.LayoutTag or SharePackage.ThemeTag);
    }

    /// <summary>
    /// Runs the e-mail security code login. Returns false when cancelled.
    /// </summary>
    public static async Task<bool> LogInAsync(Window? owner, string title)
    {
        App.Logger.LogInfo("Starting mod.io authentication", source: Source);
        Client client = CreateBuilder().Build();

        InputDialog inputDialog = new(GetString("enterModIoEmail"), title)
        {
            Owner = owner,
        };

        if (inputDialog.ShowDialog() != true)
        {
            App.Logger.LogInfo("Authentication cancelled by user", source: Source);
            return false;
        }

        await client.Auth.RequestCode(ApiKey, inputDialog.ResponseText);

        inputDialog = new(GetString("enterModIoAccessToken"), title)
        {
            Owner = owner,
        };

        if (inputDialog.ShowDialog() != true)
        {
            App.Logger.LogInfo("Authentication cancelled by user", source: Source);
            return false;
        }

        AccessToken accessToken = await client.Auth.SecurityCode(ApiKey, inputDialog.ResponseText);
        if (accessToken.Value is null)
        {
            return false;
        }

        AppSettings.ModIoAccessToken = accessToken.Value;
        Manager.Instance.SaveSettings();
        App.Logger.LogInfo("Successfully authenticated with mod.io", source: Source);
        return true;
    }

    public static void LogOut()
    {
        App.Logger.LogInfo("Logging out from mod.io", source: Source);
        AppSettings.ModIoAccessToken = null;
        currentUser = null;
        Manager.Instance.SaveSettings();
    }

    /// <summary>
    /// Compares the local content with the last published or downloaded version.
    /// </summary>
    public static void RefreshShareState(IModIoShareable item)
    {
        item.ShareState = item.ModIo switch
        {
            null => ModIoShareState.NotShared,
            { IsOwner: false } => ModIoShareState.NotOwner,
            { PublishedHash: string hash } when hash == SharePackage.Build(item).Hash => ModIoShareState.UpToDate,
            _ => ModIoShareState.Modified,
        };
    }

    /// <summary>
    /// Publishes the item as a new mod, uploads a new version of the linked mod, or opens the mod page when nothing changed.
    /// </summary>
    /// <param name="setBusy">Shows or hides the caller's waiting overlay while the upload runs.</param>
    public static async Task ShareAsync(IModIoShareable item, Window? owner, Action<bool> setBusy)
    {
        if (item is Layout { Name: Manager.EmptyLayoutName })
        {
            await ShowMessageAsync(GetString("cannotShareEmptyLayout"));
            return;
        }

        RefreshShareState(item);

        if (item.ShareState == ModIoShareState.UpToDate)
        {
            OpenProfile(item.ModIo?.ProfileUri);
            return;
        }

        if (!IsAuthenticated && !await LogInAsync(owner, App.AppName))
        {
            return;
        }

        SharePackage.Package package = SharePackage.Build(item);
        bool isUpdate = item.ModIo is { IsOwner: true };

        string? summary = null;
        string? changelog = null;

        if (isUpdate)
        {
            InputDialog changelogDialog = new(GetString("enterModIoChangelog")) { Owner = owner };
            if (changelogDialog.ShowDialog() != true)
            {
                return;
            }

            changelog = changelogDialog.ResponseText.Trim();
        }
        else
        {
            string warning = GetString(item is Layout ? "modIoShareLayoutWarning" : "modIoShareThemeWarning");
            if (package.LocalPlugins.Count > 0)
            {
                warning += Environment.NewLine + Environment.NewLine + string.Format(GetString("modIoLocalPluginsWarning"), string.Join(", ", package.LocalPlugins));
            }

            if (!await ConfirmAsync(warning, GetString("shareOnModIo")))
            {
                return;
            }

            InputDialog summaryDialog = new(GetString("enterModIoSummary")) { Owner = owner };
            if (summaryDialog.ShowDialog() != true)
            {
                return;
            }

            summary = summaryDialog.ResponseText.Trim();
            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = item.Name;
            }

            if (summary.Length > MaxSummaryLength)
            {
                summary = summary[..MaxSummaryLength];
            }
        }

        string workingDirectory = Path.Combine(Path.GetTempPath(), App.AppName, Guid.NewGuid().ToString());
        setBusy(true);

        try
        {
            _ = Directory.CreateDirectory(workingDirectory);

            string contentDirectory = Path.Combine(workingDirectory, "content");
            _ = Directory.CreateDirectory(contentDirectory);
            await File.WriteAllTextAsync(Path.Combine(contentDirectory, SharePackage.ContentFileName), package.Json.ToJsonString());

            string zipPath = Path.Combine(workingDirectory, "content.zip");
            ZipFile.CreateFromDirectory(contentDirectory, zipPath);

            string logoPath = Path.Combine(workingDirectory, "logo.png");
            SharePreviewRenderer.Render(item, logoPath);

            Client client = CreateClient();
            ModIoLink link;

            if (isUpdate)
            {
                link = item.ModIo!;
            }
            else
            {
                App.Logger.LogInfo($"Creating mod.io mod for \"{item.Name}\"", source: Source);
                NewMod newMod = new(item.Name, summary!, new FileInfo(logoPath))
                {
                    Visible = Modio.Models.Visibility.Public,
                };
                newMod.Tags.Add(SharePackage.GetTag(item));

                Mod mod = await client.Games[GameId].Mods.Add(newMod);

                // A downloaded item shared as a new mod no longer follows the original, which would otherwise be downloaded again.
                if (item.ModIo is { IsOwner: false } originalLink)
                {
                    await UnsubscribeAsync(originalLink.ModId);
                }

                // Save the link right away, so a failed upload is retried on the same mod instead of creating another one.
                link = new ModIoLink
                {
                    ModId = mod.Id,
                    IsOwner = true,
                    ProfileUri = mod.ProfileUrl,
                };
                item.ModIo = link;
                Manager.Instance.SaveSettings();
            }

            App.Logger.LogInfo($"Uploading new version of \"{item.Name}\" (mod {link.ModId})", source: Source);
            Modio.Models.File modfile = await client.Games[GameId].Mods[link.ModId].Files.Add(new NewFile(new FileInfo(zipPath))
            {
                Version = DateTime.UtcNow.ToString("yyyy.MM.dd.HHmm"),
                Changelog = string.IsNullOrWhiteSpace(changelog) ? null : changelog,
                Active = true,
            }, new UploadOptions());

            link.ModfileId = modfile.Id;
            link.PublishedHash = package.Hash;
            Manager.Instance.SaveSettings();

            if (isUpdate)
            {
                await TryUpdateLogo(client, link.ModId, logoPath);
            }

            if (item is Layout)
            {
                await TryUpdateDependencies(client, link.ModId, package.RequiredPlugins);
            }

            RefreshShareState(item);
            App.Logger.LogInfo($"Published \"{item.Name}\" on mod.io", source: Source);

            setBusy(false);
            if (await ConfirmAsync(string.Format(GetString(isUpdate ? "modIoUpdateSuccess" : "modIoShareSuccess"), item.Name), GetString("open"), GetString("close")))
            {
                OpenProfile(link.ProfileUri);
            }
        }
        catch (UnauthorizedException ex)
        {
            App.Logger.LogError($"Sharing \"{item.Name}\" failed, not authorized: {ex.Message}", source: Source);
            LogOut();
            setBusy(false);
            await ShowMessageAsync(GetString("modIoSessionExpired"));
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Sharing \"{item.Name}\" failed: {ex.Message}", source: Source);
            setBusy(false);
            await ShowMessageAsync(string.Format(GetString("modIoShareFailed"), ex.Message));
        }
        finally
        {
            setBusy(false);
            RefreshShareState(item);
            TryDeleteDirectory(workingDirectory);
        }
    }

    /// <summary>
    /// Unsubscribes from the mod an item is linked to, so deleting it locally does not bring it back on the next sync.
    /// </summary>
    public static async Task UnsubscribeAsync(IModIoShareable item)
    {
        if (item.ModIo is not null)
        {
            await UnsubscribeAsync(item.ModIo.ModId);
        }
    }

    private static async Task UnsubscribeAsync(uint modId)
    {
        if (!IsAuthenticated)
        {
            return;
        }

        try
        {
            await CreateClient().Games[GameId].Mods.Unsubscribe(modId);
            App.Logger.LogInfo($"Unsubscribed from mod {modId}", source: Source);
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Failed to unsubscribe from mod {modId}: {ex.Message}", source: Source);
        }
    }

    /// <summary>
    /// Subscribes to the mods of downloaded layouts and themes, e.g. after logging in,
    /// so the subscription sync keeps them instead of treating them as unsubscribed.
    /// </summary>
    public static async Task SubscribeToLinkedItemsAsync()
    {
        if (!IsAuthenticated)
        {
            return;
        }

        Client client = CreateClient();

        foreach (ModIoLink link in GetSharedItems().Select(item => item.ModIo).OfType<ModIoLink>().Where(link => !link.IsOwner).ToList())
        {
            try
            {
                await client.Games[GameId].Mods.Subscribe(link.ModId);
                App.Logger.LogInfo($"Subscribed to mod {link.ModId}", source: Source);
            }
            catch (Exception ex)
            {
                App.Logger.LogError($"Failed to subscribe to mod {link.ModId}: {ex.Message}", source: Source);
            }
        }
    }

    public static IEnumerable<IModIoShareable> GetSharedItems()
    {
        DesktopMagicSettings settings = AppSettings;
        return settings.Layouts.Cast<IModIoShareable>().Concat(settings.Themes);
    }

    public static IModIoShareable? FindLinkedItem(uint modId)
    {
        return GetSharedItems().FirstOrDefault(item => item.ModIo?.ModId == modId);
    }

    /// <summary>
    /// Downloads a layout or theme from mod.io and subscribes to it when logged in.
    /// </summary>
    /// <param name="ensurePluginInstalled">Installs the mod.io plugin with the given ID if it is missing.</param>
    /// <returns>Whether the item was installed.</returns>
    public static async Task<bool> InstallAsync(Mod mod, Func<uint, Task> ensurePluginInstalled)
    {
        try
        {
            if (!await ApplyLatestVersionAsync(mod, FindLinkedItem(mod.Id), ensurePluginInstalled))
            {
                return false;
            }

            if (IsAuthenticated)
            {
                try
                {
                    await CreateClient().Games[GameId].Mods.Subscribe(mod.Id);
                    App.Logger.LogInfo($"Subscribed to mod {mod.Id}", source: Source);
                }
                catch (Exception ex)
                {
                    App.Logger.LogError($"Failed to subscribe to mod {mod.Id}: {ex.Message}", source: Source);
                }
            }

            Manager.Instance.SaveSettings();
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Failed to install \"{mod.Name}\" (mod {mod.Id}): {ex.Message}", source: Source);
            await ShowMessageAsync(string.Format(GetString("modIoInstallFailed"), mod.Name, ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Updates a linked layout or theme when mod.io has a newer version.
    /// </summary>
    /// <returns>Whether the item changed.</returns>
    public static async Task<bool> UpdateAsync(IModIoShareable item, Mod mod, Func<uint, Task> ensurePluginInstalled)
    {
        try
        {
            if (!await ApplyLatestVersionAsync(mod, item, ensurePluginInstalled))
            {
                return false;
            }

            Manager.Instance.SaveSettings();
            return true;
        }
        catch (Exception ex)
        {
            App.Logger.LogError($"Failed to update \"{item.Name}\" (mod {mod.Id}): {ex.Message}", source: Source);
            return false;
        }
    }

    /// <summary>
    /// Deletes a layout or theme locally after confirmation and unsubscribes from its mod.
    /// The mod itself stays on mod.io.
    /// </summary>
    /// <returns>Whether the item was removed.</returns>
    public static async Task<bool> RemoveAsync(IModIoShareable item)
    {
        DesktopMagicSettings settings = AppSettings;

        string? blocker = item switch
        {
            Layout { Name: Manager.EmptyLayoutName } => "cannotDeleteEmptyLayout",
            Layout when settings.Layouts.Count <= 1 => "cannotDeleteLastLayout",
            Theme when settings.Themes.Count <= 1 => "cannotDeleteLastTheme",
            _ => null
        };

        if (blocker is not null)
        {
            await ShowMessageAsync(GetString(blocker));
            return false;
        }

        if (!await ConfirmAsync(GetString(item is Layout ? "confirmDeleteLayout" : "confirmDeleteTheme"), "Yes", "No"))
        {
            return false;
        }

        await UnsubscribeAsync(item);

        switch (item)
        {
            case Layout layout:
                Manager.Instance.RemoveLayout(layout);
                break;

            case Theme theme:
                _ = settings.Themes.Remove(theme);
                Manager.Instance.SaveSettings();
                break;
        }

        App.Logger.LogInfo($"Removed \"{item.Name}\"", source: Source);
        return true;
    }

    /// <summary>
    /// Downloads subscribed layouts and themes that are new or have a newer version on mod.io.
    /// </summary>
    /// <param name="subscriptions">The user's subscribed mods for this game.</param>
    /// <param name="ensurePluginInstalled">Installs the mod.io plugin with the given ID if it is missing.</param>
    /// <returns>Whether any layout or theme changed.</returns>
    public static async Task<bool> SyncSubscriptionsAsync(IReadOnlyList<Mod> subscriptions, Func<uint, Task> ensurePluginInstalled)
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        bool changed = false;

        List<Mod> sharedMods = subscriptions.Where(mod => !IsPluginMod(mod)).ToList();
        App.Logger.LogInfo($"Found {sharedMods.Count} subscribed layouts and themes on mod.io", source: Source);

        foreach (Mod mod in sharedMods)
        {
            try
            {
                changed |= await ApplyLatestVersionAsync(mod, FindLinkedItem(mod.Id), ensurePluginInstalled);
            }
            catch (Exception ex)
            {
                App.Logger.LogError($"Failed to install \"{mod.Name}\" (mod {mod.Id}): {ex.Message}", source: Source);
            }
        }

        // Downloaded items whose subscription was removed on mod.io stay as local copies.
        foreach (IModIoShareable item in GetSharedItems().Where(item => item.ModIo is { IsOwner: false } && !subscriptions.Any(mod => mod.Id == item.ModIo.ModId)).ToList())
        {
            App.Logger.LogInfo($"\"{item.Name}\" is no longer subscribed, keeping it as a local copy", source: Source);
            item.ModIo = null;
            RefreshShareState(item);
            changed = true;
        }

        if (changed)
        {
            Manager.Instance.SaveSettings();
        }

        return changed;
    }

    /// <summary>
    /// Downloads the current version of the mod into a new item, or into <paramref name="local"/> when it is outdated.
    /// Asks before overwriting local changes. Throws when the download or import fails.
    /// </summary>
    /// <returns>Whether anything changed.</returns>
    private static async Task<bool> ApplyLatestVersionAsync(Mod mod, IModIoShareable? local, Func<uint, Task> ensurePluginInstalled)
    {
        if (mod.Modfile?.Download?.BinaryUrl is null)
        {
            throw new InvalidOperationException("The mod has no file to download.");
        }

        if (local?.ModIo?.ModfileId == mod.Modfile.Id)
        {
            return false;
        }

        if (local?.ModIo is ModIoLink localLink && localLink.PublishedHash != SharePackage.Build(local).Hash)
        {
            bool overwrite = await ConfirmAsync(string.Format(GetString("modIoUpdateConflict"), local.Name), GetString("overwrite"), GetString("keepMine"));

            if (!overwrite)
            {
                App.Logger.LogInfo($"Keeping local changes of \"{local.Name}\" instead of updating to mod {mod.Id} version {mod.Modfile.Id}", source: Source);

                if (localLink.IsOwner)
                {
                    // The owner can publish their version over the remote one.
                    localLink.ModfileId = mod.Modfile.Id;
                }
                else
                {
                    // Detach the local copy so it is no longer replaced by updates.
                    await UnsubscribeAsync(local);
                    local.ModIo = null;
                }

                return true;
            }
        }

        App.Logger.LogInfo($"Downloading \"{mod.Name}\" (mod {mod.Id}, version {mod.Modfile.Id})", source: Source);
        JsonObject content = await DownloadContentAsync(mod.Modfile.Download.BinaryUrl);

        foreach (uint pluginId in SharePackage.GetRequiredPlugins(content).Where(id => !Manager.Instance.Plugins.ContainsKey(id)))
        {
            await ensurePluginInstalled(pluginId);
        }

        uint? userId = IsAuthenticated ? await GetCurrentUserIdAsync(CreateClient()) : null;

        IModIoShareable item = SharePackage.Apply(content, local);
        item.ModIo = new ModIoLink
        {
            ModId = mod.Id,
            ModfileId = mod.Modfile.Id,
            IsOwner = userId is not null && mod.SubmittedBy?.Id == userId,
            ProfileUri = mod.ProfileUrl,
        };
        item.ModIo.PublishedHash = SharePackage.Build(item).Hash;
        RefreshShareState(item);

        App.Logger.LogInfo($"Installed \"{item.Name}\" from mod.io", source: Source);
        return true;
    }

    private static async Task<JsonObject> DownloadContentAsync(Uri url)
    {
        using MemoryStream zipStream = new();
        using (Stream download = await httpClient.GetStreamAsync(url))
        {
            await download.CopyToAsync(zipStream);
        }

        zipStream.Position = 0;
        using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
        ZipArchiveEntry entry = archive.GetEntry(SharePackage.ContentFileName) ?? throw new FormatException($"The download does not contain \"{SharePackage.ContentFileName}\".");

        using StreamReader reader = new(entry.Open());
        return SharePackage.Parse(await reader.ReadToEndAsync());
    }

    private static async Task<uint> GetCurrentUserIdAsync(Client client)
    {
        string token = AppSettings.ModIoAccessToken ?? string.Empty;

        if (currentUser is not { } user || user.Token != token)
        {
            User me = await client.User.GetCurrentUser();
            user = (token, me.Id);
            currentUser = user;
        }

        return user.UserId;
    }

    private static async Task TryUpdateLogo(Client client, uint modId, string logoPath)
    {
        try
        {
            await client.Games[GameId].Mods[modId].AddMedia(new NewModMedia { Logo = new FileInfo(logoPath) });
        }
        catch (Exception ex)
        {
            App.Logger.LogWarn($"Failed to update logo of mod {modId}: {ex.Message}", source: Source);
        }
    }

    private static async Task TryUpdateDependencies(Client client, uint modId, IReadOnlyList<uint> requiredPlugins)
    {
        try
        {
            DependenciesClient dependencies = client.Games[GameId].Mods[modId].Dependencies;
            List<uint> current = (await dependencies.Get(false)).Select(dependency => dependency.ModId).ToList();

            List<uint> removed = current.Except(requiredPlugins).ToList();
            List<uint> added = requiredPlugins.Except(current).ToList();

            if (removed.Count > 0)
            {
                await dependencies.Delete(removed);
            }

            if (added.Count > 0)
            {
                await dependencies.Add(added);
            }
        }
        catch (Exception ex)
        {
            App.Logger.LogWarn($"Failed to update dependencies of mod {modId}: {ex.Message}", source: Source);
        }
    }

    private static void OpenProfile(Uri? profileUri)
    {
        if (profileUri is null)
        {
            return;
        }

        _ = Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = profileUri.ToString()
        });
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception ex)
        {
            App.Logger.LogWarn($"Failed to delete temporary directory {path}: {ex.Message}", source: Source);
        }
    }

    private static async Task ShowMessageAsync(string content)
    {
        Wpf.Ui.Controls.MessageBox messageBox = new()
        {
            Title = App.AppName,
            Content = content,
            CloseButtonText = "Ok"
        };
        _ = await messageBox.ShowDialogAsync();
    }

    private static async Task<bool> ConfirmAsync(string content, string confirmText, string? cancelText = null)
    {
        Wpf.Ui.Controls.MessageBox messageBox = new()
        {
            Title = App.AppName,
            Content = content,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText ?? GetString("cancel"),
        };

        return await messageBox.ShowDialogAsync() == Wpf.Ui.Controls.MessageBoxResult.Primary;
    }

    private static string GetString(string key)
    {
        return App.LanguageDictionary[key] as string ?? key;
    }
}
