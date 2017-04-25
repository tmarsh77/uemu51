using System.Text;

namespace Uterm.Tools
{
    public struct CaretCoord
    {
        public int X, Y;

        static public CaretCoord New(int row, int col)
        {
            CaretCoord cc = new CaretCoord();
            cc.Y = row;
            cc.X = col;
            return cc;
        }

        public void SetTo(int val)
        {
            X = Y = val;
        }

        public bool IsNot(int val)
        {
            return X == val && Y == val;
        }
    }

    static class ASCIICoDec
    {
        public enum ControlCharacters
        {
            NULL = 0x00, SOH = 0x01,
            STX = 0x02, ETX = 0x03,
            EOT = 0x04, ENQ = 0x05,
            ACK = 0x06, BEL = 0x07,
            BS = 0x08, TAB = 0x09,
            LF = 0x0A, VT = 0x0B,
            FF = 0x0C, CR = 0x0D,
            SO = 0x0E, SI = 0x0F,
            DLE = 0x10, DC1 = 0x11,
            DC2 = 0x12, DC3 = 0x13,
            DC4 = 0x14, NAK = 0x15,
            SYN = 0x16, ETB = 0x17,
            CAN = 0x18, EM = 0x19,
            SUB = 0x1A, ESC = 0x1B,
            FS = 0x1C, GS = 0x1D,
            RS = 0x1E, US = 0x1F,
            SPC = 0x20, DEL = 0x7F,
            NBSP = 0xFF,
        }

        static private Encoding _enc = Encoding.GetEncoding(437);
        static private byte[] _byte_buf = new byte[1];
        static private char[] _char_buf = new char[1];

        static public char DecodeASCII(byte code)
        {
            _byte_buf[0] = code;
            return _enc.GetChars(_byte_buf)[0];
        }

        static public byte CodeASCII(char c)
        {
            _char_buf[0] = c;
            return _enc.GetBytes(_char_buf)[0];
        }

        static public byte[] CodeASCII(string s)
        {
            return _enc.GetBytes(s);
        }
    }
}