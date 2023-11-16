// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows only application")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "No need to")]
[assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "<Pending>")]
[assembly: SuppressMessage("Minor Code Smell", "S3604:Member initializer values should not be redundant", Justification = "False postives")]