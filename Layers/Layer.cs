using ChimpinOut.GoblinBot.Common;
using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers
{
    public abstract class Layer : Component
    {
        protected Layer(Logger logger) : base(logger)
        {
        }

        public virtual async Task<bool> InitializeAsync()
        {
            await LogAsync(LogSeverity.Info, $"{GetFormattedName()} initializing...");
            return true;
        }

        protected async Task<bool> LogAndReturnInitializationResult(bool success)
        {
            var severity = success ? LogSeverity.Info : LogSeverity.Error;
            var suffix = success ? "successfully initialized" : "failed to initialize";
  
            await LogAsync(severity, $"{GetFormattedName()} {suffix}");
            
            return success;
        }

        protected async Task<string> GetEnvironmentVariable(string environmentVariable)
        {
            try
            {
                var token = Environment.GetEnvironmentVariable(environmentVariable);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    await LogAsync(LogSeverity.Info, $"Environment variable [{environmentVariable}] read successfully");
                    return token;
                }
                
                await LogAsync(LogSeverity.Error, $"Failed to read environment variable [{environmentVariable}]: value was null or empty");
                return string.Empty;
            }
            catch (SecurityException securityException)
            {
                await LogAsync(LogSeverity.Error, $"Failed to read environment variable [{environmentVariable}]: {securityException}");
                return string.Empty; 
            }
        }

        protected string GetFormattedName()
        {
            var className = GetType().Name;
            var sb = new StringBuilder(className, className.Length + 8);
            for (var i = sb.Length - 1; i >= 1; i--)
            {
                if (!char.IsUpper(sb[i]))
                {
                    continue;
                }

                sb.Insert(i, ' ');
            }
            
            return sb.ToString();
        }
    }
}