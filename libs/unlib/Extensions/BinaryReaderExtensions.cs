using System.Text;

namespace System
{
    public static  class BinaryReaderExtensions
    {
        public static string ReadCString(this BinaryReader binaryReader)
        {
            StringBuilder result = new();
            byte b;
            while ((b = binaryReader.ReadByte()) != 0) result.Append((char)b);
            return result.ToString();
        }
    }
}
