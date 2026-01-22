using System;
using DMINLauncher.Services;
using ReactiveUI;

namespace DMINLauncher.Models;

public class WadFile : ReactiveObject
{
    public string RelativePath { get; set; } = "";
    public string FullPath { get; set; } = "";
    public DateTime LastModified { get; set; }
    
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    private int _loadOrder;
    public int LoadOrder
    {
        get => _loadOrder;
        set => this.RaiseAndSetIfChanged(ref _loadOrder, value);
    }

    private WadInfo? _wadInfo;
    public WadInfo? WadInfo
    {
        get
        {
            // Lazy load WAD info on first access
            if (_wadInfo == null && !string.IsNullOrEmpty(FullPath))
            {
                _wadInfo = WadParser.Parse(FullPath);
            }
            return _wadInfo;
        }
    }

    public string DisplayName => RelativePath;
    
    public string Stats => WadInfo?.Summary ?? "";
    
    public string MapList => WadInfo?.MapListSummary ?? "";
}
