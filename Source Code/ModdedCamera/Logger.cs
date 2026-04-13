using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ModdedCamera
{
    public static class Logger
    {
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModdedCamera.log");
        private static readonly object _lockObj = new object();

        // Buffer for batched log writes to reduce I/O overhead
        private static readonly Queue<string> _logBuffer = new Queue<string>(256);
        private static readonly object _bufferLock = new object();
        private static System.Threading.Timer _flushTimer;
        private const int FLUSH_INTERVAL_MS = 2000; // Flush every 2 seconds
        private const int MAX_BUFFER_SIZE = 100; // Flush when buffer reaches this size

        static Logger()
        {
            // Initialize timer for periodic flush
            _flushTimer = new System.Threading.Timer(FlushBuffer, null, FLUSH_INTERVAL_MS, FLUSH_INTERVAL_MS);
        }

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
#if DEBUG
            Write("DEBUG", message);
#endif
        }

        private static void Write(string level, string message)
        {
            try
            {
                string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + level + "] " + message;

                lock (_bufferLock)
                {
                    _logBuffer.Enqueue(line);

                    // Flush immediately if buffer is full
                    if (_logBuffer.Count >= MAX_BUFFER_SIZE)
                    {
                        FlushBufferInternal();
                    }
                }
            }
            catch
            {
                // Если не можем записать лог — ничего не делаем
            }
        }

        private static void FlushBuffer(object state)
        {
            FlushBufferInternal();
        }

        private static void FlushBufferInternal()
        {
            try
            {
                List<string> linesToFlush;

                lock (_bufferLock)
                {
                    if (_logBuffer.Count == 0) return;

                    linesToFlush = new List<string>(_logBuffer.Count);
                    while (_logBuffer.Count > 0)
                    {
                        linesToFlush.Add(_logBuffer.Dequeue());
                    }
                }

                if (linesToFlush.Count > 0)
                {
                    lock (_lockObj)
                    {
                        File.AppendAllLines(LogFile, linesToFlush);
                    }
                }
            }
            catch
            {
                // Если не можем записать лог — ничего не делаем
            }
        }

        /// <summary>
        /// Flush any pending log entries. Call before shutdown.
        /// </summary>
        public static void Flush()
        {
            FlushBufferInternal();
        }
    }
}
