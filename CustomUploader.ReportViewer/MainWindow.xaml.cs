using System.Windows;
using Newtonsoft.Json;

namespace CustomUploader.ReportViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    internal partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonPasteAndConvert_Click(object sender, RoutedEventArgs e)
        {
            string json = Clipboard.GetText();
            var report = JsonConvert.DeserializeObject<ExceptionReport>(json);
            SourceTargetLabel.Content = $"{report.Source} → {report.Target ?? "…"}";
            TextBox.Text = report.GetExceptionText();
        }
    }
}
