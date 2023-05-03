using ChimpinOut.GoblinBot.Common;
using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers
{
    public abstract class Layer : Component
    {
        protected Layer(Logger logger) : base(logger)
        {
        }

        public abstract Task<bool> InitializeAsync();
    }
}