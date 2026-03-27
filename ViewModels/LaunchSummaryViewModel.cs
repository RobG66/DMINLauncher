using System.Reactive;
using ReactiveUI;

namespace DMINLauncher.ViewModels;

public class LaunchSummaryViewModel : ReactiveObject
{
    public string SummaryText { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public LaunchSummaryViewModel(string summaryText)
    {
        SummaryText = summaryText;
        CloseCommand = ReactiveCommand.Create(() => { });
    }
}
