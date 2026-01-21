// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Windows only application")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "No need to")]
[assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "<Pending>")]
[assembly: SuppressMessage("Critical Code Smell", "S2696:Instance members should not write to \"static\" fields", Justification = "<Pending>", Scope = "member", Target = "~P:DesktopMagic.DataContexts.MainWindowDataContext.Settings")]
[assembly: SuppressMessage("Critical Code Smell", "S2696:Instance members should not write to \"static\" fields", Justification = "<Pending>", Scope = "member", Target = "~P:DesktopMagic.DataContexts.MainWindowDataContext.DialogService")]
[assembly: SuppressMessage("Major Code Smell", "S3885:\"Assembly.Load\" should be used", Justification = "Assembly.Load does not load dependencies", Scope = "member", Target = "~M:DesktopMagic.PluginWindow.ExecuteSource")]
[assembly: SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "<Pending>", Scope = "member", Target = "~M:DesktopMagic.MainWindow.OpenPluginsFolderButton_Click(System.Object,System.Windows.RoutedEventArgs)")]
[assembly: SuppressMessage("Minor Code Smell", "S2325:Methods and properties that don't access instance data should be static", Justification = "<Pending>", Scope = "member", Target = "~M:DesktopMagic.MainWindow.ScrollViewer_PreviewMouseWheel(System.Object,System.Windows.Input.MouseWheelEventArgs)")]
[assembly: SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "Breaks stuff", Scope = "member", Target = "~M:DesktopMagic.PluginWindow.UpdateTimer_Elapsed(System.Object,System.Timers.ElapsedEventArgs)")]
