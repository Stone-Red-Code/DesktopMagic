namespace DesktopMagic.Api;

/// <summary>
/// Specifies the severity level of a log message.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Informational message for general information.
    /// </summary>
    Info,

    /// <summary>
    /// Warning message indicating a potential issue.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message indicating a failure or critical issue.
    /// </summary>
    Error
}