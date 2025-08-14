namespace unforge
{
	public enum EDataType : ushort
    {
        varReference = 0x0310,
        varWeakPointer = 0x0210,
        varStrongPointer = 0x0110,
        varClass = 0x0010,
        varEnum = 0x000F,
        varGuid = 0x000E,
        varLocale = 0x000D,
        varDouble = 0x000C,
        varSingle = 0x000B,
        varString = 0x000A,
        varUInt64 = 0x0009,
        varUInt32 = 0x0008,
        varUInt16 = 0x0007,
        varByte = 0x0006,
        varInt64 = 0x0005,
        varInt32 = 0x0004,
        varInt16 = 0x0003,
        varSByte = 0x0002,
        varBoolean = 0x0001,
    }

    public enum EConversionType : ushort
    {
        varAttribute = 0x00,
        varComplexArray = 0x01,
        varSimpleArray = 0x02,
        varClassArray = 0x03,
    }

    public enum StringSizeEnum
    {
        Int8 = 1,
        Int16 = 2,
        Int32 = 4,
    }
}
