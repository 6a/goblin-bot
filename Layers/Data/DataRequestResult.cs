namespace ChimpinOut.GoblinBot.Layers.Data
{
    public readonly struct DataRequestResult<TData>
    {
        public readonly bool Success;

        public readonly TData Data;

        public DataRequestResult(bool success, TData data)
        {
            Success = success;
            Data = data;
        }
    }
}