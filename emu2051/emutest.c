// emu2051.cpp : Defines the entry point for the console application.
//
/*
#include <stdio.h>
#include "efolib.h"

#if !defined(ARRAY_SIZE)
	#define ARRAY_SIZE(x) (sizeof((x)) / sizeof((x)[0]))
#endif

struct em8051 *test_core;

void print_stack_test()
{
	unsigned char *stack = dbg_get_stack(test_core);
	for (int i = 0; i < 14; i++)
		printf("%02X ", *(stack + i));
	printf("\n");
}

void load_object_test()
{
	char *ihex = ":03000000020800F3\n\
				:0C080000787FE4F6D8FD75810702080C33\n\
				:05080C0063900280FB77\n\
				:00000001FF\0";

	int loadres = load_ihex(test_core, ihex, strlen(ihex));

	printf("IHEX load result: %d\n\n", loadres);
}


int main()
{
	test_core = create_instance();
	load_object_test();

	print_stack_test();
	getchar();

	int i = 0;
	while (1)
	{
		// Program instructions executing starts at ~260*12 clock cycles
		i+=12;
		emu_tick(test_core);
		if (i < 260 * 12)
			continue;
		printf("at %d P1 0x%x\n\n", i, get_port_value(test_core, 0x90));
		getchar();
	}

    return 0;
}
*/