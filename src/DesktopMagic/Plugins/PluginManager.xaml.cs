using DesktopMagic.DataContexts;

using Modio.NET;
using Modio.NET.Models;

using System;
using System.Collections.Generic;
using System.Windows;

namespace DesktopMagic.Plugins;
/// <summary>
/// Interaction logic for PluginManager.xaml
/// </summary>
public partial class PluginManager : Window
{
    private readonly Client client = new Client(new Credentials("88e6ea774c3a502b06114e7fee0829ac"));

    private readonly PluginManagerDataContext pluginManagerDataContext = new();

    public PluginManager()
    {
        InitializeComponent();

        DataContext = pluginManagerDataContext;
    }

    protected override async void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        IReadOnlyList<Mod> mods = await client.Games[5665].Mods.Search().ToListAsync();
        foreach (Mod mod in mods)
        {
            pluginManagerDataContext.Mods.Add(new ModEntryDataContext(mod));
        }

        pluginManagerDataContext.Mods.Add(new ModEntryDataContext(new Mod() { Name = "yes", DescriptionPlaintext = "treoith34895z93" }));
    }
}
