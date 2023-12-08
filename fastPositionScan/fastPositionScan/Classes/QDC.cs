using System;
using System.Runtime.InteropServices;

namespace fastPositionScan
{
    internal class QDC
    {
        [DllImport("QDClib.dll", EntryPoint = "QDC_Init")]
        public static extern void Init();

        [DllImport("QDClib.dll", EntryPoint = "QDC_End")]
        public static extern void End();

        [DllImport("QDClib.dll", EntryPoint = "QDC_Read")]
        public static extern int Read(int numeroCiclos);
    }
}
