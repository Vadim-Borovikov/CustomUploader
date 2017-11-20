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

            TextBox.Text = GetExceptionText(exception);
        }

        private static string GetExceptionText(Exception e, string prefix = "")
        {
            var sb = new StringBuilder();

            var aggregateException = e as AggregateException;
            if (aggregateException != null)
            {
                foreach (Exception ex in aggregateException.InnerExceptions)
                {
                    sb.Append(GetExceptionText(ex, prefix));
                }
                return sb.ToString();
            }

            sb.AppendLine($"{prefix}{e.Message}");
            sb.AppendLine($"{prefix}StackTrace:");
            sb.AppendLine($"{prefix}{e.StackTrace.Replace("\n", $"\n{prefix}")}");
            if (e.InnerException == null)
            {
                return sb.ToString();
            }

            sb.AppendLine($"{prefix}InnerException:");
            sb.Append(GetExceptionText(e.InnerException, $"\t{prefix}"));
            return sb.ToString();
        }

        private void ButtonCopyAndClose_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TextBox.Text);
            Close();
        }
    }
}
