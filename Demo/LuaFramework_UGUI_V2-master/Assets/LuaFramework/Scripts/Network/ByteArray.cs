using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ByteArray
{
    public byte[] bytes;
    public int readIndex;
    private int writeIndex;
    public int length
    {
        get
        {
            return writeIndex - readIndex;
        }
    }
    public ByteArray(byte[] defaultByte)
    {
        bytes = defaultByte;
        readIndex = 0;
        writeIndex = defaultByte.Length;
    }
}
