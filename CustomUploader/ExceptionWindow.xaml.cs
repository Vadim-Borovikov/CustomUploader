using System;
using System.Text;
using System.Windows;

namespace CustomUploader
{
    /// <summary>
    /// Interaction logic for SelectionWindow.xaml
    /// </summary>
    internal partial class ExceptionWindow
    {
        public ExceptionWindow(Exception exception)
        {
            InitializeComponent();

            Label.Content = exception.Message;

            TextBox.Text = GetExceptionText(exception);
        }

        private static string GetExceptionText(Exception e, string prefix = "")
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{prefix}{e.GetType()}: {e.Message}");
            sb.AppendLine($"{prefix}StackTrace:");
            sb.AppendLine($"{prefix}{e.StackTrace.Replace(Environment.NewLine, $"{Environment.NewLine}{prefix}")}");

            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                foreach (Exception ex in aggregateException.InnerExceptions)
                {
                    sb.Append(GetExceptionText(ex, $"\t{prefix}"));
                }
                return sb.ToString();
            }

            if (e.InnerException != null)
            {
                sb.AppendLine($"{prefix}InnerException:");
                sb.Append(GetExceptionText(e.InnerException, $"\t{prefix}"));
            }

            return sb.ToString();
        }

        private void ButtonCopyAndClose_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TextBox.Text);
            Close();
        }
    }
}
