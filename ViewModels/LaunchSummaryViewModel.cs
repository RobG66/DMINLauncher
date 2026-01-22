using System;
using System.Reactive;
using ReactiveUI;

namespace DMINLauncher.ViewModels;

public class LaunchSummaryViewModel : ReactiveObject
{
    public string SummaryText { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    public event EventHandler? CloseRequested;

    public LaunchSummaryViewModel(string summaryText)
    {
        SummaryText = summaryText;
        CloseCommand = ReactiveCommand.Create(() => CloseRequested?.Invoke(this, EventArgs.Empty));
    }
}
