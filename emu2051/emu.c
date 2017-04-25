#include<stdlib.h>
#include "emu.h"

#define NULL ((void *)0)

struct em8051 *create_instance(int codeMem, int extMem, int upper)
{
	long size = sizeof(struct em8051);
	struct em8051 *emu = malloc(size);
	memset(emu, 0, size);
	emu->mCodeMem = malloc(codeMem);
	emu->mCodeMemSize = codeMem;
	emu->mExtData = malloc(extMem);
	emu->mExtDataSize = extMem;
	emu->mLowerData = malloc(128);
	if (upper != 0)
		emu->mUpperData = malloc(upper);
	emu->mSFR = malloc(128);
	emu->except = &emu_exception;
	emu->sfrread = &readsfr;
	emu->xread = NULL;
	emu->xwrite = NULL;
	reset(emu, 1);
	return emu;
}

int emu_loadobj(struct em8051 *aCPU, char *ihex, int len)
{
	return load_obj(aCPU, ihex, len);
}

void emu_reset(struct em8051 *aCPU, int wipe)
{
	reset(aCPU, wipe);
}

void emu_tick(struct em8051 *aCPU)
{
	tick(aCPU);
}

void delcore(struct em8051 *aCPU)
{
	free(aCPU->mSFR);
	free(aCPU->mLowerData);
	free(aCPU->mUpperData);
	free(aCPU->mExtData);
	free(aCPU->mCodeMem);
	free(aCPU);
}

// IO

void setpin(struct em8051 *aCPU, int pin, unsigned char val)
{
	aCPU->pins[pin] = val;
}

// System

int readsfr(struct em8051 *aCPU, int aRegister)
{
	unsigned char pout = -1;

	if (aRegister == REG_P0 + 0x80)
		pout = aCPU->pins[0];
	else if (aRegister == REG_P1 + 0x80)
		pout = aCPU->pins[1];
	else if (aRegister == REG_P2 + 0x80)
		pout = aCPU->pins[2];
	else if (aRegister == REG_P3 + 0x80)
		pout = aCPU->pins[3];

	if (pout != -1)
		return aCPU->mSFR[aRegister - 0x80] & pout;

	return aCPU->mSFR[aRegister - 0x80];
}

void emu_exception(struct em8051 *aCPU, int aCode)
{

}

// Debug

unsigned char readmem(struct em8051 *aCPU, int mem, int offset)
{
	switch (mem)
	{
	case 0: return aCPU->mSFR[offset];
	case 1: return aCPU->mLowerData[offset];
	case 2: return aCPU->mUpperData[offset];
	case 3: return aCPU->mExtData[offset];
	case 4: return aCPU->mCodeMem[offset];
	}
	return 0;
}

void writemem(struct em8051 *aCPU, int mem, int offset, unsigned char val)
{
	switch (mem)
	{
		case 0: aCPU->mSFR[offset] = val;
		case 1: aCPU->mLowerData[offset] = val;
		case 2: aCPU->mUpperData[offset] = val;
		case 3: aCPU->mExtData[offset] = val;
		case 4: aCPU->mCodeMem[offset] = val;
	}
}

unsigned char readreg(struct em8051 *aCPU, int pos)
{
	int rx = 8 * ((aCPU->mSFR[REG_PSW] & (PSWMASK_RS0 | PSWMASK_RS1)) >> PSW_RS0);
	switch (pos)
	{
	case 0:
		return aCPU->mSFR[REG_ACC];
	case 1:
		return aCPU->mLowerData[rx + 0];
	case 2:
		return aCPU->mLowerData[rx + 1];
	case 3:
		return aCPU->mLowerData[rx + 2];
	case 4:
		return aCPU->mLowerData[rx + 3];
	case 5:
		return aCPU->mLowerData[rx + 4];
	case 6:
		return aCPU->mLowerData[rx + 5];
	case 7:
		return aCPU->mLowerData[rx + 6];
	case 8:
		return aCPU->mLowerData[rx + 7];
	case 9:
		return aCPU->mSFR[REG_B];
	case 10:
		return aCPU->mSFR[REG_DPH] << 8 | aCPU->mSFR[REG_DPL];
	}
	return 0;
}

void writereg(struct em8051 *aCPU, int pos, int val)
{
	int rx = 8 * ((aCPU->mSFR[REG_PSW] & (PSWMASK_RS0 | PSWMASK_RS1)) >> PSW_RS0);
	switch (pos)
	{
	case 0:
		aCPU->mSFR[REG_ACC] = val;
		break;
	case 1:
		aCPU->mLowerData[rx + 0] = val;
		break;
	case 2:
		aCPU->mLowerData[rx + 1] = val;
		break;
	case 3:
		aCPU->mLowerData[rx + 2] = val;
		break;
	case 4:
		aCPU->mLowerData[rx + 3] = val;
		break;
	case 5:
		aCPU->mLowerData[rx + 4] = val;
		break;
	case 6:
		aCPU->mLowerData[rx + 5] = val;
		break;
	case 7:
		aCPU->mLowerData[rx + 6] = val;
		break;
	case 8:
		aCPU->mLowerData[rx + 7] = val;
		break;
	case 9:
		aCPU->mSFR[REG_B] = val;
		break;
	case 10:
		aCPU->mSFR[REG_DPH] = (val >> 8) & 0xff;
		aCPU->mSFR[REG_DPL] = val & 0xff;
		break;
	}
}

int getpc(struct em8051 *aCPU)
{
	return aCPU->mPC;
}

void setpc(struct em8051 *aCPU, int val)
{
	aCPU->mPC = val;
}

int decodeop(struct em8051 *aCPU, int pos, unsigned char *buffer)
{
	return decode(aCPU, pos, buffer);
}