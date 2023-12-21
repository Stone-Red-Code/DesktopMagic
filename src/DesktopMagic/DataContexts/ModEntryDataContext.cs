using Modio.NET.Models;

using System;
using System.Windows.Input;

namespace DesktopMagic.DataContexts;
internal class ModEntryDataContext(Mod mod, ICommand installCommand)
{
    public string Name => mod.Name!;

    public string Description => mod.DescriptionPlaintext!;

    public string? Logo => mod.Logo?.Thumb320x180?.ToString();

    public DateTime FormattedDateAdded => DateTimeOffset.FromUnixTimeSeconds(mod.DateAdded).LocalDateTime;
    public DateTime FormattedDateUpdated => DateTimeOffset.FromUnixTimeSeconds(mod.DateUpdated).LocalDateTime;

    public ICommand InstallCommand => installCommand;
}
