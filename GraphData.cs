using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CAccelerometer
{
    public class GraphData
    {
        public static int Cycle = 100;
        public static double dt = 1.0 / Cycle;
        public static double[] Xdata = new double[Cycle];
        public static double[] Ydata = new double[Cycle];
        public static double Amplitude, Intercept, PhaseShift;
        public static double Frequency = 5;
    }
}
