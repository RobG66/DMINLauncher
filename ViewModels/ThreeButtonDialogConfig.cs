namespace Gamelist_Manager.Views
{
    /// <summary>
    /// Configuration for a three-button dialog
    /// </summary>
    public class ThreeButtonDialogConfig
    {
        public string Title { get; set; } = "Confirm";
        public string Message { get; set; } = "";
        public string? DetailMessage { get; set; }
        public DialogIconTheme IconTheme { get; set; } = DialogIconTheme.Question;
        
        public string Button1Text { get; set; } = "Cancel";
        public string Button2Text { get; set; } = "No";
        public string Button3Text { get; set; } = "Yes";
        
        public ThreeButtonResult Button1Result { get; set; } = ThreeButtonResult.Button1;
        public ThreeButtonResult Button2Result { get; set; } = ThreeButtonResult.Button2;
        public ThreeButtonResult Button3Result { get; set; } = ThreeButtonResult.Button3;
    }

    /// <summary>
    /// Icon theme for the dialog
    /// </summary>
    public enum DialogIconTheme
    {
        Question,  // Blue
        Warning,   // Yellow/Orange
        Info,      // Blue
        Error      // Red
    }

    /// <summary>
    /// Result from the three-button dialog
    /// </summary>
    public enum ThreeButtonResult
    {
        Button1,
        Button2,
        Button3
    }
}
