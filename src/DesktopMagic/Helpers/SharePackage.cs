using DesktopMagic.Plugins;
using DesktopMagic.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DesktopMagic.Helpers;

/// <summary>
/// Converts layouts and themes to and from the portable JSON content that is uploaded to mod.io.
/// </summary>
internal static class SharePackage
{
    public const int FormatVersion = 1;
    public const string LayoutTag = "Layout";
    public const string ThemeTag = "Theme";
    public const string ContentFileName = "content.json";

    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        Converters = { new ColorJsonConverter() }
    };

    /// <param name="Json">The content written to <see cref="ContentFileName"/>.</param>
    /// <param name="Hash">Hash of the shared content, used to detect local changes.</param>
    /// <param name="RequiredPlugins">mod.io plugin IDs the layout uses.</param>
    /// <param name="LocalPlugins">Names of locally developed plugins the layout uses, which others cannot install.</param>
    public sealed record Package(JsonObject Json, string Hash, IReadOnlyList<uint> RequiredPlugins, IReadOnlyList<string> LocalPlugins);

    public static string GetTag(IModIoShareable item) => item is Layout ? LayoutTag : ThemeTag;

    public static Package Build(IModIoShareable item)
    {
        return item switch
        {
            Layout layout => BuildLayout(layout),
            Theme theme => BuildTheme(theme),
            _ => throw new ArgumentException($"Unsupported item type {item.GetType().Name}", nameof(item))
        };
    }

    private static Package BuildTheme(Theme theme)
    {
        JsonObject root = new()
        {
            ["format"] = FormatVersion,
            ["type"] = ThemeTag,
            ["themes"] = new JsonArray(SerializeTheme(theme)),
        };

        return new Package(root, ComputeHash(root), [], []);
    }

    private static Package BuildLayout(Layout layout)
    {
        DesktopMagicSettings settings = Manager.Instance.Settings;
        IReadOnlyDictionary<uint, InternalPluginData> installedPlugins = Manager.Instance.Plugins;

        Layout copy = new(layout.Name)
        {
            ScreenAspectRatio = layout.ScreenAspectRatio,
        };

        List<string> themeNames = [layout.Theme.Name];
        List<uint> requiredPlugins = [];
        List<string> localPlugins = [];

        // Only enabled widgets are shared. Disabled entries are just placeholders for every installed plugin.
        foreach ((uint pluginId, PluginSettings pluginSettings) in layout.Plugins.Where(kvp => kvp.Value.Enabled).OrderBy(kvp => kvp.Key))
        {
            copy.Plugins[pluginId] = new PluginSettings
            {
                Enabled = true,
                Position = pluginSettings.Position,
                Size = pluginSettings.Size,
                CurrentThemeName = pluginSettings.CurrentThemeName,
                Settings = pluginSettings.Settings.Where(setting => setting.Shareable).ToList(),
            };

            if (pluginSettings.CurrentThemeName is not null && settings.Themes.Any(t => t.Name == pluginSettings.CurrentThemeName) && !themeNames.Contains(pluginSettings.CurrentThemeName))
            {
                themeNames.Add(pluginSettings.CurrentThemeName);
            }

            // Built-in plugins have no directory and are available everywhere.
            if (installedPlugins.TryGetValue(pluginId, out InternalPluginData? pluginData) && !string.IsNullOrEmpty(pluginData.DirectoryPath))
            {
                if (pluginData.Metadata.IsLocalPlugin)
                {
                    localPlugins.Add(pluginData.Metadata.Name);
                }
                else
                {
                    requiredPlugins.Add(pluginId);
                }
            }
        }

        copy.CurrentThemeName = layout.Theme.Name;

        JsonObject layoutNode = JsonSerializer.SerializeToNode(copy, jsonOptions)!.AsObject();
        _ = layoutNode.Remove(nameof(Layout.ModIo));

        JsonArray themesNode = [];
        foreach (string themeName in themeNames)
        {
            Theme? theme = settings.Themes.FirstOrDefault(t => t.Name == themeName);
            if (theme is not null)
            {
                themesNode.Add(SerializeTheme(theme));
            }
        }

        JsonObject root = new()
        {
            ["format"] = FormatVersion,
            ["type"] = LayoutTag,
            ["layout"] = layoutNode,
            ["themes"] = themesNode,
        };

        // Required plugins are derived from the layout and depend on what is currently loaded, so they are not part of the hash.
        string hash = ComputeHash(root);
        root["requiredPlugins"] = new JsonArray(requiredPlugins.Select(id => (JsonNode)id).ToArray());

        return new Package(root, hash, requiredPlugins, localPlugins);
    }

    public static JsonObject Parse(string json)
    {
        JsonObject root = JsonNode.Parse(json)?.AsObject() ?? throw new FormatException("The downloaded content is empty.");

        int format = root["format"]?.GetValue<int>() ?? 0;
        if (format is < 1 or > FormatVersion)
        {
            throw new FormatException($"Unsupported content format {format}. Please update DesktopMagic.");
        }

        return root;
    }

    public static IReadOnlyList<uint> GetRequiredPlugins(JsonObject root)
    {
        return root["requiredPlugins"]?.AsArray().Select(node => node!.GetValue<uint>()).ToList() ?? [];
    }

    /// <summary>
    /// Adds the downloaded content to the settings, or updates <paramref name="existing"/> with it.
    /// </summary>
    public static IModIoShareable Apply(JsonObject root, IModIoShareable? existing)
    {
        DesktopMagicSettings settings = Manager.Instance.Settings;
        string type = root["type"]?.GetValue<string>() ?? string.Empty;

        List<Theme> themes = root["themes"]?.AsArray()
            .Select(node => node.Deserialize<Theme>(jsonOptions))
            .OfType<Theme>()
            .ToList() ?? [];

        if (type == ThemeTag)
        {
            Theme incoming = themes.FirstOrDefault() ?? throw new FormatException("The downloaded theme is empty.");
            incoming.ModIo = null;

            if (existing is Theme existingTheme)
            {
                CopyThemeValues(incoming, existingTheme);
                return existingTheme;
            }

            incoming.Name = GetUniqueName(incoming.Name, settings.Themes.Select(t => t.Name));
            settings.Themes.Add(incoming);
            return incoming;
        }

        if (type != LayoutTag)
        {
            throw new FormatException($"Unknown content type \"{type}\".");
        }

        JsonObject layoutNode = root["layout"]?.AsObject() ?? throw new FormatException("The downloaded layout is empty.");
        Layout incomingLayout = layoutNode.Deserialize<Layout>(jsonOptions) ?? throw new FormatException("The downloaded layout is invalid.");
        string layoutName = existing?.Name ?? GetUniqueName(incomingLayout.Name, settings.Layouts.Select(l => l.Name));

        Dictionary<string, string> themeNameMap = [];
        foreach (Theme theme in themes)
        {
            themeNameMap[theme.Name] = AddEmbeddedTheme(theme, layoutName);
        }

        // Read the raw name, the getter falls back to another theme when the name does not exist yet.
        string? layoutThemeName = layoutNode[nameof(Layout.CurrentThemeName)]?.GetValue<string>();
        string? mappedLayoutThemeName = layoutThemeName is not null && themeNameMap.TryGetValue(layoutThemeName, out string? mapped) ? mapped : layoutThemeName;

        Layout target = existing as Layout ?? new Layout(layoutName);

        Dictionary<uint, PluginSettings> plugins = [];
        foreach ((uint pluginId, PluginSettings incomingPlugin) in incomingLayout.Plugins)
        {
            if (incomingPlugin.CurrentThemeName is not null && themeNameMap.TryGetValue(incomingPlugin.CurrentThemeName, out string? pluginThemeName))
            {
                incomingPlugin.CurrentThemeName = pluginThemeName;
            }

            // Keep values that are never shared (e.g. URLs or locations) and saved widget data from the local copy.
            if (target.Plugins.TryGetValue(pluginId, out PluginSettings? localPlugin))
            {
                incomingPlugin.State = localPlugin.State;
                incomingPlugin.Settings = [.. incomingPlugin.Settings, .. localPlugin.Settings.Where(local => !incomingPlugin.Settings.Exists(s => s.Id == local.Id))];
            }

            incomingPlugin.Owner = target;
            plugins[pluginId] = incomingPlugin;
        }

        // Widgets the new version no longer uses are disabled, keeping their settings.
        foreach ((uint pluginId, PluginSettings localPlugin) in target.Plugins.Where(kvp => !plugins.ContainsKey(kvp.Key)))
        {
            localPlugin.Enabled = false;
            plugins[pluginId] = localPlugin;
        }

        target.ScreenAspectRatio = incomingLayout.ScreenAspectRatio;
        target.Plugins = plugins;
        target.CurrentThemeName = mappedLayoutThemeName;

        if (existing is null)
        {
            settings.Layouts.Add(target);
        }

        return target;
    }

    /// <summary>
    /// Adds a theme that is embedded in a layout and returns the local name it is available under.
    /// </summary>
    private static string AddEmbeddedTheme(Theme theme, string layoutName)
    {
        DesktopMagicSettings settings = Manager.Instance.Settings;
        theme.ModIo = null;

        Theme? sameName = settings.Themes.FirstOrDefault(t => t.Name == theme.Name);
        if (sameName is null)
        {
            settings.Themes.Add(theme);
            return theme.Name;
        }

        if (HasSameValues(sameName, theme))
        {
            return sameName.Name;
        }

        // A different theme with the same name exists (e.g. "Default"). Use a name tied to the layout,
        // which later updates of the same layout overwrite instead of adding more copies.
        string layoutThemeName = $"{theme.Name} ({layoutName})";
        Theme? layoutTheme = settings.Themes.FirstOrDefault(t => t.Name == layoutThemeName);

        if (layoutTheme is null)
        {
            theme.Name = layoutThemeName;
            settings.Themes.Add(theme);
        }
        else
        {
            CopyThemeValues(theme, layoutTheme);
        }

        return layoutThemeName;
    }

    private static void CopyThemeValues(Theme source, Theme target)
    {
        target.PrimaryColor = source.PrimaryColor;
        target.SecondaryColor = source.SecondaryColor;
        target.BackgroundColor = source.BackgroundColor;
        target.Font = source.Font;
        target.CornerRadius = source.CornerRadius;
        target.Margin = source.Margin;
    }

    private static bool HasSameValues(Theme a, Theme b)
    {
        JsonObject nodeA = SerializeTheme(a);
        JsonObject nodeB = SerializeTheme(b);
        _ = nodeA.Remove(nameof(Theme.Name));
        _ = nodeB.Remove(nameof(Theme.Name));

        return JsonNode.DeepEquals(nodeA, nodeB);
    }

    private static JsonObject SerializeTheme(Theme theme)
    {
        JsonObject node = JsonSerializer.SerializeToNode(theme, jsonOptions)!.AsObject();
        _ = node.Remove(nameof(Theme.ModIo));
        return node;
    }

    private static string GetUniqueName(string name, IEnumerable<string> existingNames)
    {
        HashSet<string> names = existingNames.ToHashSet();
        if (!names.Contains(name))
        {
            return name;
        }

        int index = 2;
        while (names.Contains($"{name} ({index})"))
        {
            index++;
        }

        return $"{name} ({index})";
    }

    private static string ComputeHash(JsonObject root)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(root.ToJsonString()));
        return Convert.ToHexString(hash);
    }
}
