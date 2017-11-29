using System;
using System.Text;
using Newtonsoft.Json;

namespace CustomUploader
{
    public class ExceptionReport
    {
        [JsonProperty]
        public readonly string Source;
        [JsonProperty]
        public readonly string Target;
        [JsonProperty]
        public readonly ConfigurationProvider ConfigurationProvider;
        [JsonProperty]
        public readonly Exception Exception;

        public ExceptionReport() { }

        public ExceptionReport(Exception exception, ConfigurationProvider configurationProvider, string source,
                               string target)
        {
            Source = source;
            Target = target;
            ConfigurationProvider = configurationProvider;
            Exception = exception;
        }

        public string GetExceptionText()
        {
            return GetExceptionText(Exception);
        }

        private static string GetExceptionText(Exception e, string prefix = "")
        {
            var sb = new StringBuilder();

            sb.AppendLine($"{prefix}{e.GetType()}: {e.Message}");
            if (!string.IsNullOrWhiteSpace(e.StackTrace))
            {
                sb.AppendLine($"{prefix}StackTrace:");
                sb.AppendLine($"{prefix}{e.StackTrace.Replace(Environment.NewLine, $"{Environment.NewLine}{prefix}")}");
            }

            if (e is AggregateException aggregateException)
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
    }
}
