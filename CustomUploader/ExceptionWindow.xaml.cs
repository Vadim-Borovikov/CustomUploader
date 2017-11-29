using System;
using System.Windows;
using Newtonsoft.Json;

namespace CustomUploader
{
    /// <summary>
    /// Interaction logic for SelectionWindow.xaml
    /// </summary>
    internal partial class ExceptionWindow
    {
        public ExceptionWindow(Exception exception, ConfigurationProvider configurationProvider, string source,
                               string target)
        {
            InitializeComponent();

            Label.Content = exception.Message;

            var report = new ExceptionReport(exception, configurationProvider, source, target);

            TextBox.Text = JsonConvert.SerializeObject(report);
        }

        private void ButtonCopyAndClose_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TextBox.Text);
            Close();
        }
    }
}
