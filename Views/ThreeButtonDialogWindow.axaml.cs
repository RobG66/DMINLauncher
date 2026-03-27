using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;

namespace DMINLauncher.Views;

public record DialogButton(string Label, string Result, string Color, bool IsBold = false);

public partial class ThreeButtonDialogWindow : Window
{
    public string? Result { get; private set; }

    public ThreeButtonDialogWindow() { InitializeComponent(); }

    public ThreeButtonDialogWindow(
        string title,
        string message,
        IList<DialogButton> buttons,
        string? defaultResult = null)
    {
        InitializeComponent();

        Title = title;
        this.FindControl<TextBlock>("MessageText")!.Text = message;

        var panel = this.FindControl<StackPanel>("ButtonPanel")!;

        Button? defaultButton = null;

        foreach (var def in buttons)
        {
            var btn = new Button
            {
                Content    = def.Label,
                Background = SolidColorBrush.Parse(def.Color),
                Foreground = Brushes.White,
                FontWeight = def.IsBold ? FontWeight.Bold : FontWeight.Normal,
                FontSize   = 11,
                Padding    = new Avalonia.Thickness(7, 3)
            };

            var captured = def.Result;
            btn.Click += (_, _) => { Result = captured; Close(); };

            panel.Children.Add(btn);

            if (def.Result == defaultResult)
                defaultButton = btn;
        }

        if (defaultButton != null)
            Opened += (_, _) => defaultButton.Focus();
    }
}
