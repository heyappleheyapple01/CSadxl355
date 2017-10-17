// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Threading;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using System.Numerics;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using MathNet.Numerics;
using System.Threading.Tasks;
//using MathNet.Numerics.LinearRegression;
//using MathNet.Numerics.LinearAlgebra.Complex;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace I2CAccelerometer
{
    struct Acceleration
    {
        public double X;
        public double Y;
        public double Z;
    };


    /// <summary>
    /// Sample app that reads data over I2C from an attached ADXL345 accelerometer
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const byte ACCEL_I2C_ADDR = 0x53;           /* 7-bit I2C address of the ADXL345 with SDO pulled low */
        private const byte ACCEL_REG_POWER_CONTROL = 0x2D;  /* Address of the Power Control register */
        private const byte ACCEL_REG_DATA_FORMAT = 0x2C;    /* Address of the Data Format register   */
        private const byte ACCEL_REG_X = 0x08;              /* Address of the X Axis data register   */
        private const byte ACCEL_REG_Y = 0x0B;              /* Address of the Y Axis data register   */
        private const byte ACCEL_REG_Z = 0x0E;              /* Address of the Z Axis data register   */

        private I2cDevice I2CAccel;
        private Timer periodicTimer;
        private Timer plottimer;

        int cycle = GraphData.Cycle;
        int acc, pre_acc;    //for refreshing the data
        double[] acceleration = new double[GraphData.Cycle];     //Y for fitting, cycle = 100
        double[] time = new double[GraphData.Cycle];     //X for fitting
        //double frequency = 5;  //5hz

        public MainPage()
        {
            this.InitializeComponent();

            /* Register for the unloaded event so we can clean up upon exit */
            Unloaded += MainPage_Unloaded;

            /* Initialize the I2C bus, accelerometer, and timer */
            InitI2CAccel();
        }

        

        private async void InitI2CAccel()
        {

            var settings = new I2cConnectionSettings(ACCEL_I2C_ADDR); 
            settings.BusSpeed = I2cBusSpeed.FastMode;     /* 400KHz bus speed*/
            var controller = await I2cController.GetDefaultAsync();
            I2CAccel = controller.GetDevice(settings);    /* Create an I2cDevice with our selected bus controller and I2C settings */
           

            /* 
             * Initialize the accelerometer:
             *
             * For this device, we create 2-byte write buffers:
             * The first byte is the register address we want to write to.
             * The second byte is the contents that we want to write to the register. 
             */
            byte[] WriteBuf_DataFormat = new byte[] { ACCEL_REG_DATA_FORMAT, 0x82 };        /* 0x81 +- 2; 0x82 +- 4; 0x83 +- 8;*/

            byte[] WriteBuf_PowerControl = new byte[] { ACCEL_REG_POWER_CONTROL, 0x00 };    /* 0x00 puts the accelerometer into measurement mode */

            /* Write the register settings */
            try
            {
                I2CAccel.Write(WriteBuf_DataFormat);
                I2CAccel.Write(WriteBuf_PowerControl);
            }
            /* If the write fails display the error and stop running */
            catch (Exception ex)
            {
                Text_Status.Text = "Failed to communicate with device: " + ex.Message;
            }

            /* Now that everything is initialized, create a timer so we read data every 10mS */
            periodicTimer = new Timer(this.TimerCallback, null, 0, 1000/GraphData.Cycle);
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            /* Cleanup */
            I2CAccel.Dispose();
        }

        


        private void TimerCallback(object state)
        {
            string xText, yText, zText; 
            string Fitting_Result;  
            string statusText;
            double dt = 1 / GraphData.Cycle;
            /* Read and format accelerometer data */
            for (int t = 0; t < cycle; t++)
            {
                time[t] = t * GraphData.dt;
            }
            try
            {
                Acceleration accel = ReadI2CAccel();
                
                xText = String.Format("X Axis: {0:F3}G", accel.X);
                yText = String.Format("Y Axis: {0:F3}G", accel.Y);
                zText = String.Format("Z Axis: {0:F3}G", accel.Z);

                acceleration[cycle - 1] = accel.X;
                /*refresh the data in the fitting chunk*/
                for (acc = 1; acc < cycle; acc++)
                {
                    pre_acc = acc - 1;
                    acceleration[pre_acc] = acceleration[acc];
                }

                GraphData.Xdata = time;
                GraphData.Ydata = acceleration;
                //xdata = time;
                //ydata = acceleration;
                double[] p = Fit.LinearCombination(
                        //y = p0 + p1cos(wt) + p2sin(wt); Amplitude = (p1^2+p2^2)^1/2
                        time,
                        acceleration,
                        x => 1.0,
                        x => Math.Sin(2 * Math.PI * GraphData.Frequency * x),
                        x => Math.Cos(2 * Math.PI * GraphData.Frequency * x));
                GraphData.Amplitude = Math.Sqrt(Math.Pow(p[1],2)+ Math.Pow(p[2], 2));
                GraphData.Intercept = p[0];
                GraphData.PhaseShift = Math.Acos(p[2]/GraphData.Amplitude);
                Fitting_Result = String.Format("Fitting Result: {0:F3}G", GraphData.Amplitude);
               
                statusText = "Status: Running";

                //foreach (double acc in acceleration)
                //    Debug.WriteLine(acc);
            }
            catch (Exception ex)
            {
                xText = "X Axis: Error";
                yText = "Y Axis: Error";
                zText = "Z Axis: Error";
                Fitting_Result = "Byeeeeeetch";
                statusText = "Failed to read from Accelerometer: " + ex.Message;
            }

            /* UI updates must be invoked on the UI thread */
            var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                Text_X_Axis.Text = xText;
                Text_Y_Axis.Text = yText;
                Text_Z_Axis.Text = zText;
                Text_Fitting_Result.Text = Fitting_Result;
                Text_Status.Text = statusText;
                //plotView.Model = PlotModelDefine.ScatterTry();
            });
        }

        private Acceleration ReadI2CAccel()
        {
            //const int ACCEL_RES = 1024;         /* The ADXL345 has 10 bit resolution giving 1024 unique values 
            const int ACCEL_RES = 1048576;         /*20bit*/
            const int ACCEL_DYN_RANGE_G = 8;    /* The ADXL345 had a total dynamic range of 8G, since we're configuring it to +-4G */
            const int UNITS_PER_G = ACCEL_RES / ACCEL_DYN_RANGE_G;  /* Ratio of raw int values to G units                          */

            /* 
             * Read from the accelerometer 
             * We call WriteRead() so we first write the address of the X-Axis I2C register, then read 3 axes separately
             */

            byte[] RegAddrBuf_X = new byte[] { ACCEL_REG_X }; /* Register address we want to read from                                         */
            byte[] ReadBuf_X = new byte[10];                   /* We read 4 bytes sequentially */
            I2CAccel.WriteRead(RegAddrBuf_X, ReadBuf_X);
            int AccelerationRawX = BitConverter.ToInt32(ReadBuf_X, 0); //Raw data is 20bit, so 32 is the minimum Bitconverter amount
            int AccelerationShiftedX = AccelerationRawX >> 12; //cancel out the reserve bit and the byte caused by extra read

            //byte[] RegAddrBuf_Y = new byte[] { ACCEL_REG_Y };
            //byte[] ReadBuf_Y = new byte[4];
            //I2CAccel.WriteRead(RegAddrBuf_Y, ReadBuf_Y);
            int AccelerationRawY = BitConverter.ToInt32(ReadBuf_X, 3);
            int AccelerationShiftedY = AccelerationRawY >> 12;

            //byte[] RegAddrBuf_Z = new byte[] { ACCEL_REG_Z };
            //byte[] ReadBuf_Z = new byte[4];
            //I2CAccel.WriteRead(RegAddrBuf_Z, ReadBuf_Z);
            int AccelerationRawZ = BitConverter.ToInt32(ReadBuf_X, 6);
            int AccelerationShiftedZ = AccelerationRawZ >> 12;

            /* Convert raw values to G's */
            Acceleration accel;
            accel.X = (double)AccelerationShiftedX / UNITS_PER_G; 
            accel.Y = (double)AccelerationShiftedY / UNITS_PER_G;
            accel.Z = (double)AccelerationShiftedZ / UNITS_PER_G;

            return accel;

        }

        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //double[] x = new double[1000];
            //double[] y = new double[1000];
            //for (int i = 0; i < 1000; i++)
            //{
            //    x[i] = 0.1 * i;
            //    y[i] = Math.Sin(x[i]);
            //    //plotView.Model = PlotModelDefine.AccToTime(x, y, string.Format("Random data (n={0})", i), i);
            //}
            var model = new PlotModel();
            model.Title = string.Format("Osas");
            //plotView.Model = PlotModelDefine.AccToTime2(x, y);
            plottimer = new Timer(this.GraphTimer, null, 1000, 3000);
            
            
            
            
            //plottimer_2 = new Timer(this.GraphNumberTimer, null, 0, 3000);

            //plotView.Model = PlotModelDefine.InvisibleLineSeries();
            //plotView.Model = PlotModelDefine.GridLinesBoth();
            //plotView.Model = FunctionSeriesExamples.CreateSquareWave();
        }

        int n = 1;
        void GraphTimer(object state)
        {
            double[] x = GraphData.Xdata;
            double[] y = GraphData.Ydata;
            double a = GraphData.Amplitude;
            var task = this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,()=> {
                var m = plotView.Model;
                m = PlotModelDefine.AccToTime(x, y, a);
                plotView.Model = m;
                plotView.InvalidatePlot();
                });

            var OutputAcc = new Task(() =>
            {
                foreach (double acc in y)
                    Debug.WriteLine(acc);
                Debug.WriteLine("--------------End Patch---------------");
                
            });
            n++;
            if (n % 50 == 0)
            {
                OutputAcc.Start();
            }
            
            
        }

    }
}
