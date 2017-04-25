using System;
using Uterm.Tools;

namespace Uterm.TextGUI
{   
    static public class TGUIUtils
    {
        static public CaretCoord GetGlobalCoords(ATUiElement element, int row, int col)
        {
            return CaretCoord.New(row + element.CursorPos.Y * element.GlobalCursorStep.Y,
                col + element.CursorPos.X * element.GlobalCursorStep.X);
        }

        static public string ByteToHex(byte b)
        {
            return BitConverter.ToString(new byte[] { b });
        }

        static public char[] Get8bitHalfs(byte b)
        {
            string str = ByteToHex(b);
            return new char[] { str[0], str[1] };
        }

        static public byte[] Get16BitHalfs(UInt16 val)
        {
            byte h = (byte)(val >> 8);
            byte l = (byte)val;
            return new byte[] { h, l };
        }

        static public Int16 BytesTo16Bit(byte bl, byte bh)
        {
            return BitConverter.ToInt16(new byte[] { bl, bh }, 0);
        }

        private static int GetBit(byte b, int bitn)
        {
            return (b & (1 << bitn)) != 0 ? 1 : 0;
        }

        static public int[] GetBits(byte b)
        {
            int[] bits = new int[8];

            for (int i = 7; i >= 0; i--)
            {
                bits[7-i] = GetBit(b, i);
            }
            
            return bits;
        }
    }

    public enum CursorMoveDirection
    {
        LEFT,
        RIGHT,
        UP,
        DOWN
    }

    public abstract class ATUiElement
    {
        private CaretCoord _cursorPos = CaretCoord.New(0, 0);
        public CaretCoord GlobalCursorStep { get; set; }
        public CaretCoord Position = CaretCoord.New(0, 0);
        public bool Hidden { get; set; }

        public CaretCoord CursorPos
        {
            get
            {
                return _cursorPos;
            }
        }

        protected byte[,] data;

        public int Rows
        {
            get
            {
                return data.GetLength(0);
            }
        }

        public int Cols
        {
            get
            {
                return data.GetLength(1);
            }
        }

        public byte this[int row, int col]
        {
            get
            {
                return data[row, col];
            }
            set
            {
                data[row, col] = value;
            }
        }       
        
        public byte[,] GetData()
        {
            return data;
        }    
        
        public void Clear()
        {
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    data[i, j] = 0;
                }
            }
        }    

        public int MoveCursor(CursorMoveDirection dir)
        {
            switch (dir)
            {
                case CursorMoveDirection.LEFT:
                    {
                        if (_cursorPos.X > 0)
                        {
                            _cursorPos.X--;
                            return 0;
                        }
                        break;
                    }
                case CursorMoveDirection.RIGHT:
                    {
                        if (_cursorPos.X < data.GetLength(1) - 1)
                        {
                            _cursorPos.X++;
                            return 0;
                        }
                        break;
                    }
                case CursorMoveDirection.UP:
                    {
                        if (_cursorPos.Y > 0)
                        {
                            _cursorPos.Y--;
                            return 0;
                        }
                        break;
                    }
                case CursorMoveDirection.DOWN:
                    {
                        if (_cursorPos.Y < data.GetLength(0) - 1)
                        {
                            _cursorPos.Y++;
                            return 0;
                        }
                        break;
                    }
            }
            return -1;
        }

        protected void InitData(int rows, int cols)
        {
            data = new byte[rows, cols];
        }

        public abstract string[] GetLines();
    }

    public class Frame : ATUiElement
    {
        public string Title { get; set; }

        private byte _tl_corner_char = ASCIICoDec.CodeASCII('┌');
        private byte _tr_corner_char = ASCIICoDec.CodeASCII('┐');
        private byte _bl_corner_char = ASCIICoDec.CodeASCII('└');
        private byte _br_corner_char = ASCIICoDec.CodeASCII('┘');
        private byte _h_border_char = ASCIICoDec.CodeASCII('─');
        private byte _v_border_char = ASCIICoDec.CodeASCII('│');

        public Frame(int row, int col, string title, int rows, int cols)
        {
            Position = CaretCoord.New(row, col);
            Title = title;
            InitData(rows, cols);
            CreateFrame();
        }

        private void CreateFrame()
        {
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (i == 0 && j == 0)
                        data[i, j] = _tl_corner_char;
                    else if (i == 0 && j == data.GetLength(1) - 1)
                        data[i, j] = _tr_corner_char;
                    else if (i == data.GetLength(0) - 1 && j == 0)
                        data[i, j] = _bl_corner_char;
                    else if (i == data.GetLength(0) - 1 && j == data.GetLength(1) - 1)
                        data[i, j] = _br_corner_char;
                    else if (i == 0 || i == data.GetLength(0) - 1)
                        data[i, j] = _h_border_char;
                    else if (j == 0 || j == data.GetLength(1) - 1)
                        data[i, j] = _v_border_char;
                }
            }
        }

        public override string[] GetLines()
        {
            string[] lines = new string[data.GetLength(0)];

            for (int i = 0; i < data.GetLength(0); i++)
            {
                string line = string.Empty;
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (i == 0 && j > 0 && j <= Title.Length)
                        line += Title[j - 1];
                    else
                        line += ASCIICoDec.DecodeASCII(data[i, j]);
                }
                lines[i] = line;
            }
            return lines;
        }
    }

    public class ByteHexGrid : ATUiElement
    {
        private char _colSep;
        public string LineOutputFormat = null;
        public string DataOutputFormat = "HEX";

        public ByteHexGrid(int row, int col, int rows, int cols, char colSep)
        {
            Position = CaretCoord.New(row, col);
            int ccoordStep = colSep != 0x00 ? 1 : 0;
            GlobalCursorStep = CaretCoord.New(1, 2 + ccoordStep);
            _colSep = colSep;
            InitData(rows, cols);
        }

        public override string[] GetLines()
        {
            string[] lines = new string[data.GetLength(0)];

            for (int i = 0; i < data.GetLength(0); i++)
            {
                string line = string.Empty;
                object[] args = new object[data.GetLength(1)];

                for (int j = 0; j < data.GetLength(1); j++)
                {
                    if (LineOutputFormat == null)
                    {
                        if (DataOutputFormat == "HEX")
                            line += TGUIUtils.ByteToHex(data[i, j]);
                        else if (DataOutputFormat == "DEC")
                            line += data[i, j].ToString();
                        else if (DataOutputFormat == "STR")
                            line += ASCIICoDec.DecodeASCII(data[i, j]);

                        if (_colSep != 0x00 && j != data.GetLength(1))
                            line += _colSep;
                    }
                    else
                    {
                        args[j] = TGUIUtils.ByteToHex(data[i, j]);
                    }
                }

                if (LineOutputFormat == null)
                    lines[i] = line;
                else
                    lines[i] += string.Format(LineOutputFormat, args);

            }

            return lines;
        }
    }
}