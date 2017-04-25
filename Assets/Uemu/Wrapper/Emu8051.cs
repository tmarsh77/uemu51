using System;
using System.Runtime.InteropServices;

namespace Unity8051Emu.Wrapper
{
    public class Emu8051
    {

        #region imports

        // common

        [DllImport("emu8051", EntryPoint = "create_instance")]
        private static extern IntPtr create_instance(int codeMem, int extMem, int upper);

        [DllImport("emu8051", EntryPoint = "emu_loadobj")]
        private static extern int emu_loadobj(IntPtr aCPU, string ihex, int len);

        [DllImport("emu8051", EntryPoint = "emu_reset")]
        private static extern void emu_reset(IntPtr aCPU, int wipe);

        [DllImport("emu8051", EntryPoint = "emu_tick")]
        private static extern void emu_tick(IntPtr aCPU);

        [DllImport("emu8051", EntryPoint = "delcore")]
        private static extern int delcore(IntPtr aCPU);

        // IO

        [DllImport("emu8051", EntryPoint = "setpin")]
        private static extern void setpin(IntPtr aCPU, int pin, byte val);
        
        // DEBUG

        [DllImport("emu8051", EntryPoint = "readmem")]
        private static extern byte readmem(IntPtr aCPU, int mem, int offset);

        [DllImport("emu8051", EntryPoint = "writemem")]
        private static extern void writemem(IntPtr aCPU, int mem, int offset, byte val);

        [DllImport("emu8051", EntryPoint = "readreg")]
        private static extern byte readreg(IntPtr aCPU, int pos);

        [DllImport("emu8051", EntryPoint = "writereg")]
        private static extern void writereg(IntPtr aCPU, int pos, int val);

        [DllImport("emu8051", EntryPoint = "getpc")]
        private static extern int getpc(IntPtr aCPU);

        [DllImport("emu8051", EntryPoint = "setpc")]
        private static extern void setpc(IntPtr aCPU, int value);

        [DllImport("emu8051", EntryPoint = "readsfr")]
        private static extern int readsfr(IntPtr aCPU, int aRegister);

        [DllImport("emu8051", EntryPoint = "decodeop")]
        private static extern int decodeop(IntPtr aCPU, int pos, IntPtr buffer);


        #endregion

        public event EventHandler<EventArgs> PortsUpdated;

        public long Cycles { get; private set; }
        public bool Breakpoint { get; private set; }
        public Action BreakpointCallback { get; set; }
        
        private IntPtr _emu;
        private bool _step;
        private int _breapointAddr = -1;
        private int _memUpper;
        private int _memROM;
        private int _memEXT;

        public Emu8051(int memROM, int memExt, int memUpper)
        {
            _memROM = memROM;
            _memEXT = memExt;
            _memUpper = memUpper;
            _emu = create_instance(_memROM, _memEXT, _memUpper);
        }

        #region ports

        private byte[] _ports = new byte[4];
        private byte[] _portsInput = new byte[4];

        public int this[int port, int pin]
        {
            get
            {
                return (_ports[port] & (1 << pin)) != 0 ? 1 : 0;
            }
            set
            {
                if (value == 0)
                    _portsInput[port] &= (byte)~(1 << pin);
                else
                    _portsInput[port] |= (byte)(1 << pin);
            }
        }

        public byte this[int port]
        {
            get
            {
                return _ports[port];
            }
            set
            {
                _portsInput[port] = value;
            }
        }

        public string PrintPort(int port)
        {
            string s = string.Empty;

            for (int i = 7; i >= 0; i--)
            {
                s += this[port, i];
                if (i != 0) s += ' ';
            }

            s += '\n';

            for (int i = 0; i < 8; i++)
            {
                s += _portsInput[port] & (1 << i);
                if (i != 7) s += ' ';
            }

            return s;
        }

        #endregion

        #region common

        public IClock Clock
        {
            set
            {
                value.Tick += TickHandle;
            }
        }

        public bool IsRunning { get; set; }

        public bool HasBreakpoint
        {
            get { return _breapointAddr != -1; }
        }

        public void ResetEmulator(int wipe)
        {
            _breapointAddr = -1;
            emu_reset(_emu, wipe);
        }

        public int LoadIhex(string ihex)
        {
            return emu_loadobj(_emu, ihex, ihex.Length);
        }

        public void Step()
        {
            IsRunning = false;
            _step = true;
        }
        
        public void SetBreakpoint(int addr)
        {
            _breapointAddr = addr;
        }

        #endregion

        #region Internal
        
        private void TickHandle(object sender, EventArgs e)
        {
            if (IsRunning || _step)
            {
                if ((GetProgramCounter() == _breapointAddr) && !Breakpoint)
                {
                    if (!Breakpoint)
                    {
                        Breakpoint = true;
                        IsRunning = false;
                        if (BreakpointCallback != null)
                            BreakpointCallback();
                    }
                }
                else
                {
                    if (Breakpoint)
                        Breakpoint = false;

                    Tick();
                }
            }

            if (_step)
                _step = false;
        }

        private void Tick()
        {
            for (int i = 0; i < 4; i++)
                    setpin(_emu, i, _portsInput[i]);

            emu_tick(_emu);

            for (int i = 0; i < 4; i++)
            {
                    _ports[i] = ReadMem(0, 0x10 * i);
                    _portsInput[i] = (byte)readsfr(_emu, 0x80 + 0x10 * i);
            }

            if (PortsUpdated != null) PortsUpdated(this, EventArgs.Empty);
            
            Cycles += 12;
        }

        #endregion

        #region Debug

        public byte ReadMem(int mem, int offset)
        {
            return readmem(_emu, mem, offset);
        }

        public void WriteMem(int mem, int offset, byte val)
        {
            writemem(_emu, mem, offset, val);
        }

        public int GetMemVol(int mem)
        {
            switch (mem)
            {
                case 0: return 128;
                case 1: return 128;
                case 2: return _memUpper;
                case 3: return _memEXT;
                case 4: return _memROM;
            }
            return -1;
        }
      
        public int ReadReg(int pos)
        {
            return readreg(_emu, pos);
        }

        public void WriteReg(int pos, int val)
        {
            writereg(_emu, pos, val);
        }
             
        public int GetProgramCounter()
        {
            return getpc(_emu);
        }

        public void SetProgramCounter(int pc)
        {
            setpc(_emu, pc);
        }

        public byte[] ReadStack()
        {
            byte[] buf = new byte[14];

            int SP = ReadMem(MEM_TYPES.SFR, SFR_REGS.REG_SP - 0x80);

            for (int i = 0; i < 14; i++)
            {
                int offset = (i + SP - 7) & 0xFF;
                if (offset < 0x80)
                    buf[i] = ReadMem(MEM_TYPES.LOWER, offset);
                else
                    buf[i] = ReadMem(MEM_TYPES.LOWER, offset - 0x80);
            }

            return buf;
        }

        public Instruction DecodeOp(int addr)
        {
            int ASM_LEN = 64;

            // allocate memory for decoded instruction assembly string
            IntPtr assembly = Marshal.AllocHGlobal(ASM_LEN);
            int opCount = decodeop(_emu, addr, assembly);

            Instruction instr = new Instruction(addr, opCount, ASM_LEN);

            // opcodes
            for (int i = 0; i < opCount; i++)
                instr.Opcodes[i] = ReadMem(MEM_TYPES.ROM, addr + i);
            // assembly
            for (int i = 0; i < ASM_LEN; i++)
                instr.Assembly[i] = Marshal.ReadByte(assembly, i);

            // free allocated buffer
            Marshal.FreeHGlobal(assembly);

            return instr;
        }

        #endregion
        

        ~Emu8051()
        {
            delcore(_emu);
        }
    }
}