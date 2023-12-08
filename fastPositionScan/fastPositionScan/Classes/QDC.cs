using System.Runtime.InteropServices;

namespace fastPositionScan
{
    internal class QDC
    {
        [DllImport("QDClibrary.dll")]
        public static extern void helloWorld();

        [DllImport("QDClibrary.dll")]
        public static extern int add(int a, int b);

        [DllImport("QDClibrary.dll")]
        public static extern void QDC_Init();

        [DllImport("QDClibrary.dll")]
        public static extern void QDC_End();

        [DllImport("QDClibrary.dll")]
        public static extern int QDC_Read();

        [DllImport("QDClibrary.dll")]
        public static extern int Read(int numeroCiclos);
    }
}
