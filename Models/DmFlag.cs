using ReactiveUI;

namespace DMINLauncher.Models;

public class DmFlag : ReactiveObject
{
    public string Label { get; set; } = "";
    public int BitValue { get; set; }
    
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}
