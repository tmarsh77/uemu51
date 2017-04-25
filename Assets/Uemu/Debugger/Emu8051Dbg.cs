using System;
using System.Collections.Generic;

using UTerm;
using Uterm.TextGUI;
using Uterm.Tools;
using Unity8051Emu.Wrapper;

namespace Unity8051Emu.Debugger
{
    public class Emu8051Dbg
    {
        private ITerminal _terminal;
        private Emu8051 _emu8051;
        private IClock _clock;
        private int _memoryFrameMode = 0;                       // selected memory type
        private CaretCoord _memCursorPos;                       // coordinates inside memory editor
        private CaretCoord _currentCaretPos;                    // caret pos in terminal screen space
        private UInt16 _memOffset = 0;                          // memory address
        private int _cursorPointedAddress = 0;                  // memory value
        private int _cursorPointedDisplayValue = 0;             // 0x0[0] - true, 0x[0]0 - false
        private bool _writeCursorH = false;
        private char _inputVal = (char)0x00;                    // modified byte value
        private ATUiElement _focusedElement;                    // current elements
        private int _focus = 0;                                 // focused element index
        private byte[] _addrInputVal = new byte[2];             // addres input field value stored here
        private int _programCounter = 0;                        // program counter
        private string _message;                                // current displayed message
        private long _cycles;
        public double _timer;

        private readonly Dictionary<string, ByteHexGrid> _dataelements =
            new Dictionary<string, ByteHexGrid>();

        private readonly Dictionary<string, Frame> _frames =
            new Dictionary<string, Frame>();

        private readonly string[] _memoryTypeTitles =
            new string[] { "SFR", "Lower", "Upper", "External", "ROM (Code)" };


        public Emu8051Dbg(Emu8051 emu, ITerminal term)
        {
            _emu8051 = emu;
            _terminal = term;

            // Create data grids

            _dataelements.Add("memgrid", new ByteHexGrid(1, 6, 8, 8, ' '));
            _dataelements.Add("memgrid_addr", new ByteHexGrid(1, 1, 8, 2, (char)0x00));
            _dataelements.Add("ports", new ByteHexGrid(6, 39, 1, 7, ' '));
            _dataelements.Add("ports_h", new ByteHexGrid(1, 39, 5, 7, ' '));
            _dataelements.Add("common_regs", new ByteHexGrid(22, 44, 1, 10, ' '));
            _dataelements.Add("common_regs_h", new ByteHexGrid(17, 44, 5, 10, ' '));
            _dataelements.Add("data_pointer", new ByteHexGrid(22, 74, 1, 2, (char)0x00));
            _dataelements.Add("data_pointer_h", new ByteHexGrid(17, 74, 5, 2, (char)0x00));
            _dataelements.Add("control_regs", new ByteHexGrid(14, 39, 1, 8, (char)0x00));
            _dataelements.Add("control_regs_h", new ByteHexGrid(9, 39, 5, 8, (char)0x00));
            _dataelements["control_regs"].LineOutputFormat =
                _dataelements["control_regs_h"].LineOutputFormat =
                    "{0}   {1}    {2}  {3}   {4}  {5}   {6}   {7}";
            _dataelements.Add("machine_status", new ByteHexGrid(6, 63, 1, 8, ' '));
            _dataelements.Add("machine_status_h", new ByteHexGrid(1, 63, 5, 8, ' '));
            _dataelements["machine_status"].DataOutputFormat = _dataelements["machine_status_h"].DataOutputFormat = "DEC";
            _dataelements.Add("stack", new ByteHexGrid(1, 33, 14, 1, (char)0x00));
            _dataelements["stack"].LineOutputFormat = "{0:X2}";
            _dataelements.Add("ascii", new ByteHexGrid(25, 51, 4, 27, (char)0x00));
            _dataelements["ascii"].Hidden = true;
            _dataelements["ascii"].DataOutputFormat = "STR";
            _dataelements.Add("addr_input", new ByteHexGrid(29, 6, 1, 2, (char)0x00));
            _dataelements.Add("code", new ByteHexGrid(22, 2, 1, 38, (char)0x00));
            _dataelements.Add("code_h", new ByteHexGrid(17, 2, 5, 38, (char)0x00));
            _dataelements["code"].DataOutputFormat = _dataelements["code_h"].DataOutputFormat = "STR";
            InitCodeFrame();

            // Create frames

            _frames.Add("memframe", new Frame(0, 0, "m)", 10, 31));
            _frames.Add("stackframe", new Frame(0, 31, "stck", 16, 6));
            _frames["stackframe"][8, 0] = ASCIICoDec.CodeASCII('>');
            _frames["stackframe"][8, 5] = ASCIICoDec.CodeASCII('<');
            _frames.Add("stateframe", new Frame(10, 0, "", 6, 31));
            _frames.Add("codeframe", new Frame(16, 0, "─PC────Opcodes───Assembly", 8, 42));
            _frames["codeframe"][6, 0] = (byte)'>';
            _frames["codeframe"][6, 41] = (byte)'<';
            _frames.Add("portsframe", new Frame(0, 37, "─SP─P0─P1─P2─P3─IP─IE", 8, 24));
            _frames["portsframe"][6, 0] = ASCIICoDec.CodeASCII('>');
            _frames["portsframe"][6, 23] = ASCIICoDec.CodeASCII('<');
            _frames.Add("commonregsframe", new Frame(16, 42, "─A──R0─R1─R2─R3─R4─R5─R6─R7─B──DPTR", 8, 38));
            _frames["commonregsframe"][6, 0] = ASCIICoDec.CodeASCII('>');
            _frames["commonregsframe"][6, 37] = ASCIICoDec.CodeASCII('<');
            _frames.Add("controlregsframe", new Frame(8, 37, "─TMOD─TCON──TH0─TH1──TL0─TL1──SCON─PCON", 8, 43));
            _frames["controlregsframe"][6, 0] = ASCIICoDec.CodeASCII('>');
            _frames["controlregsframe"][6, 42] = ASCIICoDec.CodeASCII('<');
            _frames.Add("pswframe", new Frame(0, 61, "─C─ACF0R1R0Ov──P", 8, 19));
            _frames["pswframe"][6, 0] = ASCIICoDec.CodeASCII('>');
            _frames["pswframe"][6, 18] = ASCIICoDec.CodeASCII('<');
            _frames.Add("asciiframe", new Frame(24, 49, "ASCII──[t]", 7, 31));
            
            // emulator init
            _emu8051.BreakpointCallback += () => ShowMessage("Breakpoint reached");
            // terminal init
            _terminal.InputEnabled = false;
            // debugger init
            _focusedElement = _dataelements["memgrid"];
            SwitchMemType();
        }

        public IClock Clock
        {
            get
            {
                return _clock;
            }
            set
            {
                _clock = value;
                _clock.Tick += Clock_Tick;
            }
        }

        private void Clock_Tick(object sender, EventArgs e)
        {
            if (_emu8051.IsRunning)
                _timer += Clock.MsPerTick;
        }

        /// <summary>
        /// Call from Update() method
        /// </summary>
        public void Tick()
        {
            bool history = _emu8051.Cycles > _cycles;
            _cycles = _emu8051.Cycles;
            RefreshDebuggerState(history);
        }


        #region output, display


        private void RefreshDebuggerState(bool history)
        {
            _programCounter = _emu8051.GetProgramCounter();
            RedrawData(history);
            _terminal.PlaceCaret(_currentCaretPos);
            _terminal.Tick();

            if (_terminal.HasInput)
            {
                byte inpData = _terminal.ReadLastInput();
                HandleTerminalInput(inpData);
            }

            if (_inputVal != 0x00)
            {
                WriteVal(_inputVal);
                _inputVal = (char)0x00;
                _memCursorPos.X++;
            }
        }

        private void RedrawStatic()
        {
            foreach (ATUiElement frame in _frames.Values)
            {
                if (!frame.Hidden)
                    DrawUTUiElement(frame);
            }

            // hints

            WriteAt(CaretCoord.New(25, 0), "[spc] step [r] run [xX] reset/wipe [+-]clk spd ");
            WriteAt(CaretCoord.New(26, 0), "[m]switch mem [tab]editor focus [0-9af] input  ");

            if (_focus != 3)
                WriteAt(CaretCoord.New(27, 0), "[i]nteractive mode ");
            else
                WriteAt(CaretCoord.New(27, 0), "                   ");

            if (_focus == 3)
                WriteAt(CaretCoord.New(28, 0), "[g]o [p] breakpoint");
            else
                WriteAt(CaretCoord.New(28, 0), "                   ");

            WriteAt(CaretCoord.New(29, 0),
                "addr$ 0000                                       ");
        }

        private void RedrawData(bool updateHistory)
        {
            int row = 0;

            for (UInt16 addr = _memOffset; addr < _memOffset + 8 * 8; addr += 8)
            {
                byte[] addr_halfs = TGUIUtils.Get16BitHalfs(addr);

                for (int j = 0; j < 2; j++)
                    _dataelements["memgrid_addr"][row, j] = addr_halfs[j];

                for (int j = 0; j < 8; j++)
                {
                    byte b = _emu8051.ReadMem(_memoryFrameMode, addr + j);
                    _dataelements["memgrid"][row, j] = b;
                }
                row++;
            }

            // ASCII view

            if (!_dataelements["ascii"].Hidden)
            {
                int asciiRow = 0;
                int asciiCol = 5;
                byte padd = ASCIICoDec.CodeASCII('.');
                UInt16 memaddr = _memOffset;
                for (int i = 0; i < 8; i++)
                {
                    if (i == 4)
                    {
                        asciiRow = 0;
                        asciiCol = 19;
                    }

                    string saddr = memaddr.ToString("X4");

                    int sp = 0;
                    for (int k = asciiCol - 5; k < asciiCol - 1; k++)
                    {
                        _dataelements["ascii"][asciiRow, k] = ASCIICoDec.CodeASCII(saddr[sp]);
                        sp++;
                    }

                    for (int j = 0; j < 8; j++)
                    {
                        byte val = _emu8051.ReadMem(_memoryFrameMode, memaddr);

                        if (val > 32 && val < 127)
                            _dataelements["ascii"][asciiRow, asciiCol + j] = val;
                        else
                            _dataelements["ascii"][asciiRow, asciiCol + j] = padd;

                        memaddr++;
                    }
                    asciiRow++;
                }
            }

            if (updateHistory)
            {
                UpdateHistory(_dataelements["machine_status_h"], _dataelements["machine_status"], 5, 7);
                UpdateHistory(_dataelements["ports_h"], _dataelements["ports"], 5, 7);
                UpdateHistory(_dataelements["common_regs_h"], _dataelements["common_regs"], 5, 10);
                UpdateHistory(_dataelements["data_pointer_h"], _dataelements["data_pointer"], 5, 2);
                UpdateHistory(_dataelements["code_h"], _dataelements["code"], 5, 38);
            }

            // Stack

            byte[] stackData = _emu8051.ReadStack();
            for (int i = 0; i < 14; i++)
                _dataelements["stack"][i, 0] = stackData[i];

            // Common registers

            for (int i = 0; i < 10; i++)
                _dataelements["common_regs"][0, i] = (byte)_emu8051.ReadReg(i);

            // Data pointer

            byte[] dptr = TGUIUtils.Get16BitHalfs((ushort)_emu8051.ReadReg(10));
            for (int j = 0; j < 2; j++)
                _dataelements["data_pointer"][0, j] = dptr[j];

            // Code

            Instruction instr = _emu8051.DecodeOp(_programCounter);
            string format = "{0:X4}  {1} {2} {3}  {4}";
            string asm = string.Empty;
            for (int i = 0; i < instr.Assembly.Length; i++)
            {
                byte b = instr.Assembly[i];
                if (b == 0x00) // end of cstring
                    break;
                asm += ASCIICoDec.DecodeASCII(b);
            }

            string outstr = string.Format(format, _programCounter,
                TGUIUtils.ByteToHex(instr.Opcodes[0]),
                instr.Opcodes.Length > 1 ? TGUIUtils.ByteToHex(instr.Opcodes[1]) : "  ",
                instr.Opcodes.Length > 2 ? TGUIUtils.ByteToHex(instr.Opcodes[2]) : "  ",
                asm
            );

            if (outstr.Length < 38)
                outstr += new string(' ', 38 - outstr.Length);

            for (int i = 0; i < outstr.Length; i++)
                _dataelements["code"][0, i] = ASCIICoDec.CodeASCII(outstr[i]);

            // Ports

            _dataelements["ports"][0, 0] = _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_SP - 0x80);
            _dataelements["ports"][0, 1] = _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_P0 - 0x80);
            _dataelements["ports"][0, 2] = _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_P1 - 0x80);
            _dataelements["ports"][0, 3] = _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_P2 - 0x80);
            _dataelements["ports"][0, 4] = _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_P3 - 0x80);
            _dataelements["ports"][0, 5] = _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_IP - 0x80);
            _dataelements["ports"][0, 6] = _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_IE - 0x80);

            // Machine status word

            int[] machine_status = TGUIUtils.GetBits(
                _emu8051.ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_PSW - 0x80));

            for (int i = 0; i < 8; i++)
                _dataelements["machine_status"][0, i] = (byte)machine_status[i];

            // Address input

            _dataelements["addr_input"][0, 0] = _addrInputVal[0];
            _dataelements["addr_input"][0, 1] = _addrInputVal[1];

            // Cursor info

            RefreshCursorInfo();

            if (!_writeCursorH)
                _currentCaretPos = TGUIUtils.GetGlobalCoords(
                    _focusedElement, _focusedElement.Position.Y, _focusedElement.Position.X);

            // Info display

            int[] bits = TGUIUtils.GetBits((byte)_cursorPointedDisplayValue);

            WriteAt(CaretCoord.New(11, 2),
                string.Format("Sel     : {0} {1} {2} {3} {4} {5} {6} {7}",
                bits[0], bits[1], bits[2], bits[3], bits[4], bits[5], bits[6], bits[7]));

            WriteAt(CaretCoord.New(12, 2),
                string.Format("Time    : {0}ms", _timer.ToString("1.000")));

            WriteAt(CaretCoord.New(13, 2),
                string.Format("Cycles  : {0}", _emu8051.Cycles));

            string spd = string.Empty;

            if (Clock.SpeedMode == 0)
                spd = "MAX   ";
            else if (Clock.SpeedMode == 1)
                spd = "HIGH  ";
            else if (Clock.SpeedMode == 2)
                spd = "MIDDLE";
            else if (Clock.SpeedMode == 3)
                spd = "LOW   ";
            else if (Clock.SpeedMode == 4)
                spd = "LOWEST";

            WriteAt(CaretCoord.New(14, 2),
                string.Format("Clk spd : {0}      ", spd));

            foreach (ATUiElement element in _dataelements.Values)
            {
                if (!element.Hidden)
                    DrawUTUiElement(element);
            }
        }

        private void DrawUTUiElement(ATUiElement element)
        {
            string[] lines = element.GetLines();

            for (int i = 0; i < lines.Length; i++)
            {
                _terminal.PlaceCaret(CaretCoord.New(i + element.Position.Y, element.Position.X));
                _terminal.WriteLine(lines[i]);
            }
        }

        private void ShowMessage(string msg)
        {
            if (!string.IsNullOrEmpty(_message))
                WriteAt(CaretCoord.New(28, 46 - _message.Length),
                    new string(' ', _message.Length + 2));

            if (msg == null)
                return;

            _message = msg;
            WriteAt(CaretCoord.New(28, 47), "<");
            WriteAt(CaretCoord.New(28, 46 - msg.Length), msg);
        }

        private void WriteAt(CaretCoord coord, string str)
        {
            _terminal.PlaceCaret(coord);
            _terminal.WriteLine(str);
        }
        
        private void UpdateHistory(ATUiElement historyElement,
            ATUiElement element, int rows, int cols)
        {
            byte[,] history = ShiftHistory(historyElement.GetData(), element.GetData());

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    historyElement[i, j] = history[i, j];
        }

        private void InitCodeFrame()
        {
            string code_data_init = "0000  00 00 00  NOP                   ";

            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 38; j++)
                {
                    byte chr = ASCIICoDec.CodeASCII(code_data_init[j]);

                    if (i < 5)
                        _dataelements["code_h"][i, j] = chr;
                    else
                        _dataelements["code"][0, j] = chr;
                }
            }
        }


        #endregion


        #region input


        private void HandleTerminalInput(byte data)
        {
            bool cursorState = _writeCursorH;
            _writeCursorH = false;

            if (ASCIICoDec.DecodeASCII(data).ToString().ToLower() == "m")
            {
                SwitchMemType();
                return;
            }
            else if (ASCIICoDec.DecodeASCII(data).ToString().ToLower() == "r")
            {
                if (_focus == 3)
                    SwitchFocus(0);

                ShowMessage(null);

                if (!_emu8051.IsRunning)
                {
                    _emu8051.IsRunning = true;
                }
                else
                {
                    _emu8051.IsRunning = false;
                }
                return;
            }
            else if (ASCIICoDec.DecodeASCII(data).ToString().ToLower() == "i")
            {
                if (_focus != 3)
                {
                    _emu8051.IsRunning = false;
                    SwitchFocus(3);
                }
                else
                    SwitchFocus(0);
                return;
            }
            else if (ASCIICoDec.DecodeASCII(data).ToString().ToLower() == "g")
            {
                if (_focus == 3)
                {
                    _emu8051.SetProgramCounter(GetInputAddr());
                    ShowMessage("PC set");
                }
                return;
            }
            else if (ASCIICoDec.DecodeASCII(data).ToString().ToLower() == "p")
            {
                if (_focus == 3)
                {
                    if (!_emu8051.HasBreakpoint)
                    {
                        _emu8051.SetBreakpoint(GetInputAddr());
                        ShowMessage("Breakpoint set");
                    }
                    else
                    {
                        _emu8051.SetBreakpoint(-1);
                        ShowMessage("Breakpoint clear");
                    }
                }
                return;
            }
            else if (data == (byte)ASCIICoDec.ControlCharacters.SPC)
            {
                _emu8051.Step();
                Clock.Impulse();

                _timer += Clock.MsPerTick;

                return;
            }
            else if (data == (byte)ASCIICoDec.ControlCharacters.FS)
            {
                Clock.AdjustSpeed('+');
            }
            else if (data == (byte)ASCIICoDec.ControlCharacters.RS)
            {
                Clock.AdjustSpeed('-');
            }
            else if (ASCIICoDec.DecodeASCII(data).ToString() == "x")
            {
                Reset(0);
            }
            else if (ASCIICoDec.DecodeASCII(data).ToString() == "X")
            {
                Reset(1);
            }
            else if (ASCIICoDec.DecodeASCII(data).ToString() == "t")
            {
                _dataelements["ascii"].Hidden = !_dataelements["ascii"].Hidden;
                RedrawStatic();
            }

            else if (data == (byte)ASCIICoDec.ControlCharacters.DC1)
            {
                int moveOp = _focusedElement.MoveCursor(CursorMoveDirection.LEFT);

                if (_focus == 2 && moveOp == -1)
                    SwitchFocus(1);
            }
            else if (data == (byte)ASCIICoDec.ControlCharacters.DC2)
            {
                int moveOp = _focusedElement.MoveCursor(CursorMoveDirection.RIGHT);

                if (_focus == 1 && moveOp == -1)
                    SwitchFocus(2);

            }
            else if (data == (byte)ASCIICoDec.ControlCharacters.DC3)
            {
                int moveOp = _focusedElement.MoveCursor(CursorMoveDirection.UP);

                if (_focus == 0 && moveOp == -1)
                {
                    if (_memOffset > 0)
                        _memOffset -= 8;
                }
            }
            else if (data == (byte)ASCIICoDec.ControlCharacters.DC4)
            {
                int moveOp = _focusedElement.MoveCursor(CursorMoveDirection.DOWN);
                if (_focus == 0 && moveOp == -1)
                {
                    if (_memOffset < _emu8051.GetMemVol(_memoryFrameMode) - 8 * 8)
                        _memOffset += 8;
                }
            }
            else if (data == (byte)ASCIICoDec.ControlCharacters.TAB)
            {
                SwitchFocus();
            }
            else
            {
                char inpChar = ASCIICoDec.DecodeASCII(data).ToString().ToLower()[0];
                int code = ASCIICoDec.CodeASCII(inpChar);
                if ((code > 47 && code <= 57) || (code > 96 && code <= 102)) // 0-9 A-F
                {
                    _inputVal = inpChar;
                    _writeCursorH = cursorState;
                }
            }
        }

        private int GetCursorPointedMemAddress()
        {
            CaretCoord dataPos = _focusedElement.CursorPos;
            if (_focus == 0)
                return (dataPos.Y * 8) + _memOffset + dataPos.X;
            else
                return dataPos.X;
        }

        private int GetCursorPointedMemValue()
        {
            if (_focus == 0)
            {
                return _emu8051.ReadMem(_memoryFrameMode, GetCursorPointedMemAddress());
            }
            else if (_focus == 1)
            {
                return _emu8051.ReadReg(_cursorPointedAddress);
            }
            else if (_focus == 2)
            {
                return TGUIUtils.Get16BitHalfs((ushort)_emu8051.ReadReg(10))[_cursorPointedAddress];
            }
            else if (_focus == 3)
            {
                return _addrInputVal[_cursorPointedAddress];
            }

            return -1;
        }

        private void RefreshCursorInfo()
        {
            _cursorPointedAddress = GetCursorPointedMemAddress();
            _cursorPointedDisplayValue = GetCursorPointedMemValue();
        }

        private short GetInputAddr()
        {
            return TGUIUtils.BytesTo16Bit(
                _addrInputVal[1], _addrInputVal[0]);
        }

        private void SwitchMemType()
        {
            _memoryFrameMode = _memoryFrameMode < 4 ? _memoryFrameMode + 1 : 0;
            _frames["memframe"].Title = _memoryTypeTitles[_memoryFrameMode];
            RedrawStatic();
            _memCursorPos = _focusedElement.Position;
            _memOffset = 0;
        }

        private void SwitchFocus(int setTo = -1)
        {
            if (setTo == -1)
                _focus = _focus < 1 ? _focus + 1 : 0;
            else
                _focus = setTo;

            if (_focus == 0)
                _focusedElement = _dataelements["memgrid"];
            else if (_focus == 1)
                _focusedElement = _dataelements["common_regs"];
            else if (_focus == 2)
                _focusedElement = _dataelements["data_pointer"];
            else if (_focus == 3)
                _focusedElement = _dataelements["addr_input"];

            _cursorPointedAddress = 0;

            RedrawStatic();
        }

        private void WriteVal(char val)
        {
            string hex = string.Empty;
            char[] halfs = TGUIUtils.Get8bitHalfs((byte)_cursorPointedDisplayValue);

            if (_writeCursorH)
            {
                hex = halfs[0].ToString() + val.ToString();
                _writeCursorH = false;
                _currentCaretPos.X--;
            }
            else
            {
                hex = val.ToString() + halfs[1].ToString();
                _currentCaretPos.X++;
                _writeCursorH = true;
            }

            byte b = (byte)int.Parse(hex, System.Globalization.NumberStyles.HexNumber);

            if (_focus == 0)
            {
                _emu8051.WriteMem(_memoryFrameMode, _cursorPointedAddress, b);
            }
            else if (_focus == 1)
            {
                _emu8051.WriteReg(_cursorPointedAddress, b);
            }
            else if (_focus == 2)
            {
                _dataelements["data_pointer"][0, _cursorPointedAddress] = b;
                short dptr = TGUIUtils.BytesTo16Bit(_dataelements["data_pointer"][0, 1], _dataelements["data_pointer"][0, 0]);
                _emu8051.WriteReg(10, dptr);
            }
            else if (_focus == 3)
            {
                _addrInputVal[_cursorPointedAddress] = b;
            }
        }


        #endregion


        #region util

        private byte[,] ShiftHistory(byte[,] history,
            byte[,] data, int dataRow = 0)
        {
            byte[,] temp = new byte[history.GetLength(0), history.GetLength(1)];

            for (int i = 1; i < history.GetLength(0); i++)
            {
                for (int j = 0; j < history.GetLength(1); j++)
                {
                    temp[i - 1, j] = history[i, j];
                }
            }

            for (int j = 0; j < data.GetLength(1); j++)
            {
                temp[temp.GetLength(0) - 1, j] = data[dataRow, j];
            }

            return temp;
        }


        #endregion


        #region emulator, system


        private void Reset(int wipe)
        {
            _emu8051.IsRunning = false;

            if (wipe == 1)
            {
                foreach (ATUiElement element in _dataelements.Values)
                    element.Clear();

                InitCodeFrame();
            }

            _emu8051.ResetEmulator(wipe);
        }


        #endregion
    }
}