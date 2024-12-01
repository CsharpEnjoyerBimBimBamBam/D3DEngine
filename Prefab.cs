namespace DirectXEngine
{
    public class Prefab
    {
        public ulong ID { get; } = _ID++;
        private static ulong _ID = 0;
    }
}
