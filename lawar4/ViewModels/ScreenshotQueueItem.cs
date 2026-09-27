using CommunityToolkit.Mvvm.ComponentModel;

namespace lawar4.ViewModels;

/// <summary>Review-list wrapper around a queued screenshot path, with multi-select support for removal.</summary>
public sealed class ScreenshotQueueItem : ObservableObject
{
    public ScreenshotQueueItem(string fullPath)
    {
        FullPath = fullPath;
        FileName = Path.GetFileName(fullPath);
    }

    public string FullPath { get; }
    public string FileName { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
