using System;

namespace DesktopMagic.Settings;

/// <summary>
/// Connects a local layout or theme to the mod.io mod it was published as or downloaded from.
/// </summary>
public class ModIoLink
{
    public uint ModId { get; set; }

    /// <summary>
    /// The modfile (version) the local copy is based on.
    /// </summary>
    public uint ModfileId { get; set; }

    /// <summary>
    /// Hash of the shareable content at the last publish or download, used to detect local changes.
    /// </summary>
    public string? PublishedHash { get; set; }

    /// <summary>
    /// Whether the logged in mod.io user submitted the mod and is therefore allowed to upload new versions.
    /// </summary>
    public bool IsOwner { get; set; }

    public Uri? ProfileUri { get; set; }
}

public enum ModIoShareState
{
    NotShared,
    UpToDate,
    Modified,
    NotOwner
}

/// <summary>
/// A local item (layout or theme) that can be shared on mod.io.
/// </summary>
public interface IModIoShareable
{
    string Name { get; }

    ModIoLink? ModIo { get; set; }

    ModIoShareState ShareState { get; set; }
}
