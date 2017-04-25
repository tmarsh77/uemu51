#define DllExport __declspec( dllexport )
#include "emu8051.h"

DllExport struct em8051 *create_instance(int codeMem, int extMem, int upper);
DllExport int emu_loadobj(struct em8051 *aCPU, char *ihex, int len);
DllExport void emu_tick(struct em8051 *aCPU);
DllExport void emu_reset(struct em8051 *aCPU, int wipe);

DllExport void setpin(struct em8051 *aCPU, int pin, unsigned char val);
DllExport int decodeop(struct em8051 *aCPU, int pos, unsigned char *buffer);
DllExport int getpc(struct em8051 *aCPU);
DllExport void setpc(struct em8051 *aCPU, int value);
DllExport unsigned char readmem(struct em8051 *aCPU, int mem, int offset);
DllExport void writemem(struct em8051 *aCPU, int mem, int offset, unsigned char val);
DllExport unsigned char readreg(struct em8051 *aCPU, int pos);
DllExport void writereg(struct em8051 *aCPU, int pos, int val);
DllExport void delcore(struct em8051 *aCPU);

DllExport int readsfr(struct em8051 *aCPU, int aRegister);
void emu_exception(struct em8051 *aCPU, int aCode);