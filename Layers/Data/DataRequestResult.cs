namespace ChimpinOut.GoblinBot.Layers.Data
{
    public readonly struct DataRequestResult
    {
        public readonly bool Success;

        public readonly object Data;

        public DataRequestResult(bool success, object data)
        {
            Success = success;
            Data = data;
        }

        public bool TryGetData<T>(out T? data)
        {
            try
            {
                data = (T)Data;
                return true;
            }
            catch (Exception)
            {
                data = default;
                return false;
            }
        }
    }
}