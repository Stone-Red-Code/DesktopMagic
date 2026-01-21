namespace DesktopMagic.Api.Settings;

/// <summary>
/// Represents a file selector control that allows the user to browse for a file.
/// </summary>
public sealed class FileSelector : Setting
{
    private string _value = string.Empty;

    /// <summary>
    /// Gets or sets the selected file path.
    /// </summary>
    public string Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                ValueChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the file filter for the file dialog (e.g., "Image Files|*.png;*.jpg|All Files|*.*").
    /// </summary>
    public string Filter { get; set; }

    /// <summary>
    /// Gets or sets the title of the file dialog.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the file selector should select folders instead of files.
    /// </summary>
    public bool SelectFolder { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSelector"/> class.
    /// </summary>
    /// <param name="defaultPath">The default file path.</param>
    /// <param name="filter">The file filter for the file dialog.</param>
    /// <param name="title">The title of the file dialog.</param>
    /// <param name="selectFolder">If true, selects folders instead of files.</param>
    public FileSelector(string defaultPath = "", string filter = "All Files|*.*", string title = "Select File", bool selectFolder = false)
    {
        _value = defaultPath;
        Filter = filter;
        Title = title;
        SelectFolder = selectFolder;
    }

    internal override string GetJsonValue()
    {
        return Value;
    }

    internal override void SetJsonValue(string value)
    {
        Value = value;
    }
}
