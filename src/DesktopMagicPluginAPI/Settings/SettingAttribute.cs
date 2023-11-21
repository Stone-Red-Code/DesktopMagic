using System;

namespace DesktopMagic.Api.Settings;

/// <summary>
/// Marks a Property as element.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SettingAttribute : Attribute
{
    /// <summary>
    /// The name of the element.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The order index of the element.
    /// </summary>
    public int OrderIndex { get; set; }

    /// <summary>
    /// The id of the element.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Marks a Property as element with the provided <paramref name="name"/> and <paramref name="orderIndex"/>.
    /// </summary>
    /// <param name="id">The id of the element.</param>
    /// <param name="name">The name of the element.</param>
    /// <param name="orderIndex">The order index of the element.</param>
    public SettingAttribute(string id, string name, int orderIndex = 0)
    {
        Id = id;
        Name = name;
        OrderIndex = orderIndex;
    }

    /// <summary>
    /// Marks a Property as element with the provided <paramref name="orderIndex"/>.
    /// </summary>
    /// <param name="id">The id of the element.</param>
    /// <param name="orderIndex">The order index of the element.</param>
    public SettingAttribute(string id, int orderIndex)
    {
        Id = id;
        OrderIndex = orderIndex;
    }

    /// <inheritdoc cref="SettingAttribute"/>
    public SettingAttribute(string id)
    {
        Id = id;
    }
}