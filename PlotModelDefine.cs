using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;
using OxyPlot.Axes;
using MathNet.Numerics;
using MathNet;
using MathNet.Numerics.LinearRegression;

namespace I2CAccelerometer
{
    public class PlotModelDefine
    {
        public static PlotModel GridLinesBoth()
        {
            var plotModel1 = new PlotModel();
            plotModel1.Title = "Both";
            var linearAxis1 = new LinearAxis();
            linearAxis1.MajorGridlineStyle = LineStyle.Solid;
            linearAxis1.MinorGridlineStyle = LineStyle.Dot;
            plotModel1.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis();
            linearAxis2.MajorGridlineStyle = LineStyle.Solid;
            linearAxis2.MinorGridlineStyle = LineStyle.Dot;
            linearAxis2.Position = AxisPosition.Bottom;
            plotModel1.Axes.Add(linearAxis2);
            plotModel1.Series.Add(new FunctionSeries(Math.Cos, 0, 100, 0.1, "cos(x)"));
            plotModel1.Series.Add(new FunctionSeries(Math.Sin, 0, 100, 0.1, "sin(x)"));
            return plotModel1;
        }


        public static PlotModel ZeroCrossing()
        {
            var plotModel = new PlotModel();
            plotModel.PlotAreaBorderThickness = new OxyThickness(0.0);
            plotModel.PlotMargins = new OxyThickness(10);
            var linearAxis = new LinearAxis();
            linearAxis.Maximum = 40;
            linearAxis.Minimum = -40;
            linearAxis.PositionAtZeroCrossing = true;
            linearAxis.TickStyle = TickStyle.Crossing;
            plotModel.Axes.Add(linearAxis);
            var secondLinearAxis = new LinearAxis();
            secondLinearAxis.Maximum = 40;
            secondLinearAxis.Minimum = -40;
            secondLinearAxis.PositionAtZeroCrossing = true;
            secondLinearAxis.Position = AxisPosition.Bottom;
            secondLinearAxis.TickStyle = TickStyle.Crossing;
            plotModel.Axes.Add(secondLinearAxis);
            return plotModel;
        }

        public static PlotModel InvisibleLineSeries()
        {
            var model = new PlotModel();
            var s1 = new LineSeries();
            s1.Points.Add(new DataPoint(0, 10));
            s1.Points.Add(new DataPoint(10, 40));
            var s2 = new LineSeries(); //{ IsVisible = false };
            s2.Points.Add(new DataPoint(40, 20));
            s2.Points.Add(new DataPoint(60, 30));
            model.Series.Add(s1);
            model.Series.Add(s2);
            return model;
        }

        public static PlotModel StairStepSeries()
        {
            var model = new PlotModel() { LegendSymbolLength = 24 };
            model.Title = "StairStepSeries";
            var s1 = new StairStepSeries()
            {
                Color = OxyColors.SkyBlue,
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerStroke = OxyColors.White,
                MarkerFill = OxyColors.SkyBlue,
                MarkerStrokeThickness = 1.5,
                Title = "sin(x)"
            };
            for (double x = 0; x < Math.PI * 2; x += 0.5)
                s1.Points.Add(new DataPoint(x, Math.Sin(x)));
            model.Series.Add(s1);

            return model;
        }

        public static PlotModel Scatter(double[] x, double[] y)
        {
            const int n = 1000;
            var model = new PlotModel() { LegendPosition = LegendPosition.LeftTop };
            model.Title = string.Format("Random data (n={0})", n);
            var s1 = new ScatterSeries
            {   Title = "Measurements",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = OxyColors.ForestGreen};
            for (int i = 0; i < n; i++)
            {
                var p = new ScatterPoint(x[i], y[i]);
                s1.Points.Add(p);
            }
            model.Series.Add(s1);
            return model;
        }

        public static PlotModel ScatterTry()
        {
            int cycle = 1000;
            const int n = 20;
            var model = new PlotModel() { LegendPosition = LegendPosition.LeftTop };
            model.Title = string.Format("Random data (n={0})", n);
            var s1 = new ScatterSeries { Title = "Measurements" };

            for (int i = 0; i < cycle; i++)
            {
                var p = new ScatterPoint(0,0);
                s1.Points.Add(p);
            }
            model.Series.Add(s1);
            return model;
        }

        public static PlotModel ChangingGraph(double[] x, double[] y, int m)
        {
            int n = m;
            var model = new PlotModel() { LegendPosition = LegendPosition.LeftTop };
            //model.Title = string.Format("Random data (n={0})", n);
            //model.Title = title;
            var s1 = new ScatterSeries
            {
                Title = "Measurements",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerFill = OxyColors.ForestGreen
            };
            for (int i = 0; i < n; i++)
            {
                try
                {
                    var p = new ScatterPoint(x[i], y[i]);
                    s1.Points.Add(p);

                }
                catch(Exception ex)
                {
                    break;
                }
                }

            model.Series.Add(s1);
            return model;
        }

        public static PlotModel AccToTime(double[] x, double[] y, double a)
        {
            int n = x.Length;
            var model = new PlotModel() { LegendPosition = LegendPosition.LeftTop };
            //model.Title = string.Format("Random data (n={0})", n);
            model.Title = "Byeetch";
            var s1 = new ScatterSeries
            {
                Title = "Measurements",
                MarkerType = MarkerType.Circle,
                MarkerSize = 2,
                MarkerFill = OxyColors.Red
            };
            //var s2 = new ScatterSeries
            //{
            //    Title = "FittingResult",
            //    MarkerType = MarkerType.Circle,
            //    MarkerSize = 2,
            //    MarkerFill = OxyColors.Orange
            //};
            
            for (int i = 0; i < n; i++)
            {
                var p = new ScatterPoint(x[i], y[i]);
                s1.Points.Add(p);
                //var f = new ScatterPoint(x[i], a * Math.Cos(2*Math.PI*GraphData.Frequency*x[i]));
            }
            model.Series.Add(s1);
            Func<double, double> s2 = (z) => a * Math.Sin((2 * Math.PI * GraphData.Frequency * z) + GraphData.PhaseShift) + GraphData.Intercept;
            model.Series.Add(new FunctionSeries(s2, 0, 1, GraphData.dt));
            //model.Series.Add(s2);
            return model;
        }
    }

    
}
