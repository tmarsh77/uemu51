using System;

namespace Unity8051Emu.Wrapper
{
    public static class MEM_TYPES
    {
        public const int SFR = 0;
        public const int LOWER = 1;
        public const int UPPER = 2;
        public const int EXT = 3;
        public const int ROM = 4;
    }

    public static class SFR_REGS
    {
        public const int REG_ACC = 0xE0;
        public const int REG_B = 0xF0;
        public const int REG_PSW = 0xD0;
        public const int REG_SP = 0x81;
        public const int REG_DPL = 0x82;
        public const int REG_DPH = 0x83;
        public const int REG_P0 = 0x80;
        public const int REG_P1 = 0x90;
        public const int REG_P2 = 0xA0;
        public const int REG_P3 = 0xB0;
        public const int REG_IP = 0xB8;
        public const int REG_IE = 0xA8;
        public const int REG_TMOD = 0x89;
        public const int REG_TCON = 0x88;
        public const int REG_TH0 = 0x8C;
        public const int REG_TL0 = 0x8A;
        public const int REG_TH1 = 0x8D;
        public const int REG_TL1 = 0x8B;
        public const int REG_SCON = 0x98;
        public const int REG_PCON = 0x87;
    }

    public interface IClock
    {
        event EventHandler<EventArgs> Tick;
        int SpeedMode { get; }
        double MsPerTick { get; }
        void AdjustSpeed(char mode);
        void Impulse();
    }

    public struct Instruction
    {
        public int Address { get; private set; }
        public byte[] Opcodes { get; private set; }
        public byte[] Assembly { get; private set; }

        public Instruction(int address, int opCount, int assemblyLength)
        {
            Address = address;
            Opcodes = new byte[opCount];
            Assembly = new byte[assemblyLength];
        }
    }
}