namespace DirectXEngine
{
    public interface IBinarySerializable
    {
        public void WriteToByteArray(byte[] buffer, int startIndex);
    }
}
