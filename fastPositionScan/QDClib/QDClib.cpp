#define QDCEXPORT __declspec(dllexport)

#include <stdio.h>
#include <stdint.h>

#include <stdio.h>
#include <conio.h>
#include <time.h>
#include <sys/timeb.h>

typedef unsigned char byte;
typedef short VARIANT_BOOL;

#define VARIANT_TRUE (-1)
#define VARIANT_FALSE (0)

#define getch _getch

#include <CAENVMElib.h>
#include <CAENVMEoslib.h>
#include <CAENVMEtypes.h>

// Inicializa o dispositivo ou retorna uma mensagem de erro
extern "C" QDCEXPORT void QDC_Init();

// Encerra o dispositivo
extern "C" QDCEXPORT void QDC_End();

// Leitura
extern "C" QDCEXPORT int QDC_Read(int a);


#define MAX_BLT_SIZE		(256*1024)

#define DATATYPE_MASK		0x06000000
#define DATATYPE_HEADER		0x02000000
#define DATATYPE_CHDATA		0x00000000
#define DATATYPE_EOB		0x04000000
#define DATATYPE_FILLER		0x06000000


// --------------------------
// Global Variables
// --------------------------
// Base Addresses
uint32_t BaseAddress = 0x22220000;
uint32_t DiscrBaseAddr = 0;

// handle for the V1718/V2718 
int32_t handle = -1;

int VMEerror = 0;
char ErrorString[100];

CVBoardTypes ctype = cvV1718;
int pid = 0, bdnum = 0;
int brd_nch = 16;

long get_time()
{
	long time_ms;
	struct _timeb timebuffer;
	_ftime64_s(&timebuffer);
	time_ms = (long)timebuffer.time * 1000 + (long)timebuffer.millitm;
	return time_ms;
}


void write_reg(uint16_t reg_addr, uint16_t data)
{
	CVErrorCodes ret;
	ret = CAENVME_WriteCycle(handle, BaseAddress + reg_addr, &data, cvA32_U_DATA, cvD16);
	if (ret != cvSuccess) {
		printf(ErrorString, "Cannot write at address %08X\n", (uint32_t)(BaseAddress + reg_addr));
		VMEerror = 1;
	}
}


void QDC_Init() {

	uint16_t Iped = 100;			// pedestal of the QDC (or resolution of the TDC)
	int i;
	uint16_t QTP_LLD[32] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

	if (CAENVME_Init2(ctype, &pid, bdnum, &handle) != cvSuccess) {
		printf("Can't open VME controller\n");
		getch();
		return;
	}
	else {
		printf("Device initialized\n");
		printf("Handle = %d\n", handle);
	}

	// Reset QTP board
	write_reg(0x1016, 0);
	if (VMEerror) {
		printf("Error during QTP programming: ");
		printf(ErrorString);
		getch();
	}

	write_reg(0x1060, Iped);  // Set pedestal
	write_reg(0x1010, 0x60);  // enable BERR to close BLT at and of block

	// Set LLD (low level threshold for ADC data)
	write_reg(0x1034, 0x100);  // set threshold step = 16
	for (i = 0; i < brd_nch; i++) {
		if (brd_nch == 16)	write_reg(0x1080 + i * 4, QTP_LLD[i] / 16);
		else				write_reg(0x1080 + i * 2, QTP_LLD[i] / 16);
	}

	printf("QTP board programmed\n");
}

void QDC_End() {
	if (handle >= 0) {
		CAENVME_End(handle);
		printf("Encerrado");
	}
	else {
		printf("Não foi possível encerrar o dispositivo");
	}
}

int QDC_Read(int numeroCiclos) {
	int sum = 0;	//VALOR DA SOMA DOS EVENTOS

	int pnt = 0;  // word pointer
	int wcnt = 0; // num of lword read in the MBLT cycle
	int totnb = 0;
	uint32_t buffer[MAX_BLT_SIZE / 4];// readout buffer (raw data from the board)
	buffer[0] = DATATYPE_FILLER;
	int DataType = DATATYPE_HEADER;
	int DataError = 0;
	uint16_t ADCdata[32];			// ADC data (charge, peak or TAC)
	int nch = 0, chindex = 0, nev = 0, j, ns[32], bcnt;
	long CurrentTime, PrevPlotTime, ElapsedTime;	// time of the PC

	// clear Event Counter
	write_reg(0x1040, 0x0);
	// clear QTP
	write_reg(0x1032, 0x4);
	write_reg(0x1034, 0x4);

	PrevPlotTime = get_time();

	//int quit = 0;
	//while (!quit) {
	int evtcount = 0;
	while (evtcount < numeroCiclos) {

		CurrentTime = get_time(); // Time in milliseconds

		// Log statistics on the screen and plot histograms
		ElapsedTime = CurrentTime - PrevPlotTime;
		if (ElapsedTime > 1000) {
			//ClearScreen();
			nev = 0;
			totnb = 0;
			printf("Aguarde\n");
			PrevPlotTime = CurrentTime;
		}

		// if needed, read a new block of data from the board 
		if ((pnt == wcnt) || ((buffer[pnt] & DATATYPE_MASK) == DATATYPE_FILLER)) {
			CAENVME_FIFOMBLTReadCycle(handle, BaseAddress, (char*)buffer, MAX_BLT_SIZE, cvA32_U_MBLT, &bcnt);
			wcnt = bcnt / 4;
			totnb += bcnt;
			pnt = 0;
		}
		if (wcnt == 0)  // no data available
			continue;

		// header 
		switch (DataType) {
		case DATATYPE_HEADER:
			if ((buffer[pnt] & DATATYPE_MASK) != DATATYPE_HEADER) {
				//printf("Header not found: %08X (pnt=%d)\n", buffer[pnt], pnt);
				DataError = 1;
			}
			else {
				nch = (buffer[pnt] >> 8) & 0x3F;
				chindex = 0;
				nev++;
				memset(ADCdata, 0xFFFF, 32 * sizeof(uint16_t));
				if (nch > 0)
					DataType = DATATYPE_CHDATA;
				else
					DataType = DATATYPE_EOB;
			}
			break;

			// Channel data 
		case DATATYPE_CHDATA:
			if ((buffer[pnt] & DATATYPE_MASK) != DATATYPE_CHDATA) {
				//printf("Wrong Channel Data: %08X (pnt=%d)\n", buffer[pnt], pnt);
				DataError = 1;
			}
			else {
				if (brd_nch == 32)
					j = (int)((buffer[pnt] >> 16) & 0x3F);  // for V792 (32 channels)
				else
					j = (int)((buffer[pnt] >> 17) & 0x3F);  // for V792N (16 channels)
				ADCdata[j] = buffer[pnt] & 0xFFF;
				ns[j]++;
				if (chindex == (nch - 1))
					DataType = DATATYPE_EOB;
				chindex++;
			}
			break;

			// EOB 
		case DATATYPE_EOB:
			if ((buffer[pnt] & DATATYPE_MASK) != DATATYPE_EOB) {
				//printf("EOB not found: %08X (pnt=%d)\n", buffer[pnt], pnt);
				DataError = 1;
			}
			else {
				DataType = DATATYPE_HEADER;
				printf("Event Num. %d\n", buffer[pnt] & 0xFFFFFF);
				//printa apenas o canal 0
				if (ADCdata[0] != 0xFFFF) {
					printf("Ch %2d: %d\n", 0, ADCdata[0]);
					sum = sum + ADCdata[0];
					evtcount++;
				}
				//printa todos os canais
				/* for(i=0; i<32; i++) {
					if (ADCdata[i] != 0xFFFF)
						printf("Ch %2d: %d\n", i, ADCdata[i]);
				} */
			}
			break;
		}
		pnt++;

		if (DataError) {
			pnt = wcnt;
			write_reg(0x1032, 0x4);
			write_reg(0x1034, 0x4);
			DataType = DATATYPE_HEADER;
			DataError = 0;
		}
	}

	sum = sum / numeroCiclos;
	return sum;
	//printf("Carga média: %d", sum);
	//printf("Ciclos: %d", evtcount);
}