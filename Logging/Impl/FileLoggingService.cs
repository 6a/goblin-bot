using System.Diagnostics;

namespace ChimpinOut.GoblinBot.Logging.Impl
{
    public class FileLoggingService : ILoggingService
    {
        private const int WorkerThreadInterval = 1000;
        private const uint MaximumConsecutiveWriteErrors = 10;
        private const int LogRotationMaxRetention = 7;
        private static readonly string LogFolder = GetLogFolder();
        
        private readonly Logger _logger;
        
        private readonly ConcurrentQueue<TimedLogMessage> _messageQueue;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Thread _workerThread;

        private DateTime _currentDate;
        private StreamWriter _currentLogFile;

        private bool _disabled;

        private uint _consecutiveWriteErrorCount;

        public FileLoggingService(Logger logger)
        {
            _logger = logger;
            
            _messageQueue = new ConcurrentQueue<TimedLogMessage>();

            _cancellationTokenSource = new CancellationTokenSource();
            
            _workerThread = new Thread(() => ExecuteWorker(_cancellationTokenSource.Token))
            {
                Name = "FileLoggingServiceWorker",
                IsBackground = false,
            };
            
            _workerThread.Start();

            _currentDate = DateTime.Today;
            _currentLogFile = StreamWriter.Null;

            _disabled = false;
            _consecutiveWriteErrorCount = 0;

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            
            LogToStdOut("Initialized file logging service");
        }

        public void Enqueue(string message)
        {
            if (_disabled)
            {
                return;
            }
            
            _messageQueue.Enqueue(new TimedLogMessage(DateTime.Now, message));
        }

        private void ExecuteWorker(CancellationToken ct)
        {
            LogToStdOut("Started file logging service worker thread");
            
            if (!Directory.Exists(LogFolder))
            {
                try
                {
                    Directory.CreateDirectory(LogFolder);
                }
                catch (Exception exception)
                {
                    LogToStdErr($"Failed to create log folder [{LogFolder}] - disabled file logging");
                    LogToStdErr(exception);
                    _disabled = true;
                    
                    return;
                }
            }

            if (!OpenOrCreateLogFile(GetLogFilePath(DateTime.Today)))
            {
                return;
            }
            
            var workTimer = Stopwatch.StartNew();
            
            do
            {
                workTimer.Restart();
                TimedLogMessage currentLogMessage = default;
                
                try
                {
                    while (_messageQueue.TryDequeue(out currentLogMessage))
                    {
                        if (!RotateLogsIfRequired(currentLogMessage.Timestamp))
                        {
                            _disabled = true;
                            return;
                        }
                            
                        _currentLogFile.WriteLine(currentLogMessage.ToString());
                    }
                }
                catch (Exception exception)
                {
                    var logFileName = (_currentLogFile.BaseStream as FileStream)?.Name;
                    
                    if (currentLogMessage.IsValid)
                    {
                        LogToStdErr($"Failed to write the following message to the logfile [{logFileName}]:");
                        LogToStdErr(currentLogMessage.ToString(), true);
                    }
                    else
                    {
                        LogToStdErr($"Failed to write to the logfile [{logFileName}]:");
                    }
                    
                    LogToStdErr(exception);

                    if (++_consecutiveWriteErrorCount >= MaximumConsecutiveWriteErrors)
                    {
                        LogToStdErr($"Failed to write to logfile {_consecutiveWriteErrorCount} times - disabled file logging");
                        _disabled = true;

                        return;
                    }
                }

                _consecutiveWriteErrorCount = 0;
                ct.WaitHandle.WaitOne(Math.Max(WorkerThreadInterval - (int)workTimer.ElapsedMilliseconds, 0));
            } while (!ct.IsCancellationRequested);

            _currentLogFile.Dispose();
        }

        private bool RotateLogsIfRequired(DateTime logTimestamp)
        {
            var logDate = logTimestamp.Date;
            if (_currentDate >= logDate)
            {
                return true;
            }
            
            _currentDate = logDate;
            if (!OpenOrCreateLogFile(GetLogFilePath(logDate)))
            {
                return false;
            }

            try
            {
                foreach (var oldLogFilePath in Directory.EnumerateFiles(LogFolder, "*.log")
                    .OrderByDescending(path => path, StringComparer.Ordinal)
                    .Skip(LogRotationMaxRetention))
                {
                    File.Delete(oldLogFilePath);
                    LogToStdOut($"Deleted old log file: {oldLogFilePath}");
                }
            }
            catch (Exception exception)
            {
                LogToStdErr("Unable to delete old logfile(s) - disabled file logging");
                LogToStdErr(exception);
                _disabled = true;

                return false;
            }

            return true;
        }

        private void OnProcessExit(object? _, EventArgs? e)
        {
            _cancellationTokenSource.Cancel();
            _workerThread.Join();
        }

        private bool OpenOrCreateLogFile(string logFilePath)
        {
            try
            {
                var exists = File.Exists(logFilePath);
                
                _currentLogFile.Dispose();
                _currentLogFile = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true,
                };
                
                LogToStdOut($"{(exists ? "Opened" : "Created new")} log file: {logFilePath}");

                return true;
            }
            catch (Exception exception)
            {
                LogToStdErr($"Unable to create/open logfile [{logFilePath}] - disabled file logging");
                LogToStdErr(exception);
                _disabled = true;
                
                return false;
            }
        }

        private static string GetLogFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "ChimpinOut",
                    "GoblinBot");
        }

        private static string GetLogFilePath(DateTime day)
        {
            return Path.Combine(LogFolder, $"{day:yyyy-MM-dd}_out.log");
        }

        private void LogToStdErr(object? error, bool excludePrefix = false)
        {
            if (error == null)
            {
                return;
            }
            
            if (excludePrefix || error is Exception)
            {
                _logger.LogRaw(error.ToString() ?? string.Empty);
            }
            else
            {
                _logger.Log(new LogMessage(LogSeverity.Error, "Logging", error.ToString()));
            }
        }

        private void LogToStdOut(object? message)
        {
            _logger.Log(new LogMessage(LogSeverity.Info, "Logging", message?.ToString()));
        }
    }
}