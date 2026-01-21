using System;

namespace DesktopMagic.Api;

/// <summary>
/// Marks a field or property to be automatically persisted between plugin sessions.
/// </summary>
/// <remarks>
/// The field or property must be JSON serializable. Complex types should have parameterless constructors.
/// State is saved when the plugin stops and loaded when it starts.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PersistStateAttribute : Attribute
{
}
