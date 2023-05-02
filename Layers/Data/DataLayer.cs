using ChimpinOut.GoblinBot.Logging;

namespace ChimpinOut.GoblinBot.Layers.Data
{
    public class DataLayer : Layer
    {
        protected override string LogPrefix => "Data";
        
        public DataLayer(Logger logger) : base(logger)
        {
        }
        
        
    }
}