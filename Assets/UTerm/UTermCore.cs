using System;
using Uterm.Tools;

namespace UTerm
{
    class UTermCore
    {
        private enum CaretDir
        {
            KEEP = 0x00,
            PREV = 0x01,
            NEXT = 0x02,
        }

        const int HEIGHT = 30;
        const int WIDTH = 80;

        private byte[,] _buffer = new byte[HEIGHT, WIDTH];
        private CaretCoord _coord;

        public bool ShowCursor { get; set; }

        public bool Full
        {
            get
            {
                return _coord.Y == HEIGHT - 1 && _coord.X == WIDTH - 1;
            }
        }

        public bool ActiveChar
        {
            get
            {
                byte c = _buffer[_coord.Y, _coord.X];
                return c != (byte)ASCIICoDec.ControlCharacters.NULL
                    && c != (byte)ASCIICoDec.ControlCharacters.SPC;
            }
        }

        public UTermCore()
        {
            Flush();
        }

        // 0 - col increment/decrement completed
        // 1/2 row increment/decrement completed
        // -1/-2 row imcrement/decrement failed
        private int MoveCaret(CaretDir dir)
        {
            if (dir == CaretDir.NEXT)
            {
                if (_coord.X < WIDTH - 1)
                {
                    _coord.X++;
                }
                else if (_coord.Y < HEIGHT - 1)
                {
                    _coord.X = 0;
                    _coord.Y++;
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else if (dir == CaretDir.PREV)
            {
                if (_coord.X > 0)
                {
                    _coord.X--;
                }
                else if (_coord.Y > 0)
                {
                    _coord.X = WIDTH - 1;
                    _coord.Y--;
                    return 2;
                }
                else
                {
                    return -2;
                }
            }
            return 0;
        }

        public int PutChar(byte c)
        {
            if ((c >= 0 && c <= 0x20) || c == 0x7F/*DEL*/ || c == 0xFF/*nbsp*/)
            {
                if (c == (byte)ASCIICoDec.ControlCharacters.NULL)
                {
                    c = 0x20;
                }
                else if (c == (byte)ASCIICoDec.ControlCharacters.LF)
                {
                    if (_coord.Y < HEIGHT - 1)
                        while (Math.Abs(MoveCaret(CaretDir.NEXT)) != 1) ;
                    return 0;
                }
                else if (c == (byte)ASCIICoDec.ControlCharacters.BS)
                {
                    if (Full && ActiveChar)
                    {
                        _buffer[_coord.Y, _coord.X] = (byte)ASCIICoDec.ControlCharacters.NULL;
                        return 0;
                    }

                    MoveCaret(CaretDir.PREV);
                    _buffer[_coord.Y, _coord.X] = (byte)ASCIICoDec.ControlCharacters.NULL;
                    return 0;
                }
            }
            _buffer[_coord.Y, _coord.X] = c;
            return MoveCaret(CaretDir.NEXT);
        }

        public void PlaceCaret(CaretCoord coord)
        {
            if (coord.Y != -1)
            {
                if (coord.Y >= HEIGHT)
                    coord.Y = HEIGHT - 1;
                else if (coord.Y < 0)
                    coord.Y = 0;

                _coord.Y = coord.Y;
            }

            if (coord.X != -1)
            {
                if (coord.X >= WIDTH)
                    coord.X = WIDTH - 1;
                else if (coord.X < 0)
                    coord.X = 0;

                _coord.X = coord.X;
            }
        }

        public void Flush(byte fill = (byte)ASCIICoDec.ControlCharacters.NULL)
        {
            _coord.Y = _coord.X = 0;
            while (PutChar(fill) != -1) ;
            _coord.Y = _coord.X = 0;
        }
        
        public System.Collections.Generic.IEnumerable<char> ReadBuffer()
        {
            char cr = ASCIICoDec.DecodeASCII((byte)ASCIICoDec.ControlCharacters.LF);

            for (int h = 0; h < HEIGHT; h++)
            {
                for (int w = 0; w < WIDTH; w++)
                {
                    if (ShowCursor && h == _coord.Y && w == _coord.X && !(Full && ActiveChar))
                        yield return ASCIICoDec.DecodeASCII(0xDB);
                    else
                        yield return ASCIICoDec.DecodeASCII(_buffer[h, w]);
                }
                yield return cr;
            }
        }
    }
}