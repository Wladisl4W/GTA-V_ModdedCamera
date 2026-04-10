using System;
using System.IO;
using System.Text;

namespace ModdedCamera
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModdedCamera.log");
        private static readonly object _lockObj = new object();

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warn(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        /// <summary>
        /// FIXED: Now includes stack traces and inner exceptions for better debugging.
        /// </summary>
        public static void Error(Exception ex, string context = "")
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(context))
            {
                sb.Append(context);
                sb.Append(": ");
            }
            
            sb.Append(ex.GetType().Name);
            sb.Append(": ");
            sb.Append(ex.Message);

            // Add stack trace
            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                sb.Append("\nStack Trace: ");
                sb.Append(ex.StackTrace);
            }

            // Add inner exceptions recursively
            Exception inner = ex.InnerException;
            while (inner != null)
            {
                sb.Append("\n  Inner Exception: ");
                sb.Append(inner.GetType().Name);
                sb.Append(": ");
                sb.Append(inner.Message);
                
                if (!string.IsNullOrEmpty(inner.StackTrace))
                {
                    sb.Append("\n  Stack Trace: ");
                    sb.Append(inner.StackTrace);
                }
                
                inner = inner.InnerException;
            }

            Write("ERROR", sb.ToString());
        }

        public static void Debug(string message)
        {
            Write("DEBUG", message);
        }

        private static void Write(string level, string message)
        {
            try
            {
                string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + level + "] " + message;
                lock (_lockObj)
                {
                    File.AppendAllText(LogFile, line + Environment.NewLine);
                }
            }
            catch
            {
                // Если не можем записать лог — ничего не делаем
            }
        }
    }
}
