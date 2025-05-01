#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class NadarayaWatsonEstimator : Indicator
    {
        private Series<double> estimatedValues;
        private Series<double> values;
        private Series<double> direction;
        private Series<bool> buySignal;
        private Series<bool> sellSignal;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Nadaraya-Watson Kernel Regression Estimator";
                Name = "NadarayaWatsonEstimator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;
                
                // Default values
                Length = 20;
                h = 2.0;
            }
            else if (State == State.Configure)
            {
                estimatedValues = new Series<double>(this);
                values = new Series<double>(this);
                direction = new Series<double>(this);
                buySignal = new Series<bool>(this);
                sellSignal = new Series<bool>(this);
                AddPlot(new Stroke(Brushes.DodgerBlue, 2), PlotStyle.Line, "NWE");
            }
            else if (State == State.DataLoaded)
            {
                // Ensure the indicator is properly initialized
                ClearOutputWindow();
            }
        }
        
        [Range(1, int.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Length", Description = "Period length for the estimation", Order = 1, GroupName = "Parameters")]
        public int Length { get; set; }

        [Range(0.1, double.MaxValue)]
        [NinjaScriptProperty]
        [Display(Name = "Bandwidth", Description = "Bandwidth parameter (h) for the kernel", Order = 2, GroupName = "Parameters")]
        public double h { get; set; }



        protected override void OnBarUpdate()
        {
            if (CurrentBar < Length)
                return;

            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < Length; i++)
            {
                double kernel = Math.Exp(-Math.Pow(i / (double)Length, 2) / (2 * Math.Pow(h, 2)));
                numerator += Input[i] * kernel;
                denominator += kernel;
            }

            if (denominator != 0)
                estimatedValues[0] = numerator / denominator;
            else
                estimatedValues[0] = Input[0];

            Values[0][0] = estimatedValues[0];

            // Calcular direção da tendência
            direction[0] = estimatedValues[0] - estimatedValues[1];

            // Detectar sinais de compra e venda
            buySignal[0] = direction[0] > 0 && direction[1] <= 0;  // Cruzamento para cima
            sellSignal[0] = direction[0] < 0 && direction[1] >= 0; // Cruzamento para baixo

            // Plotar sinais no gráfico
            if (buySignal[0])
            {
                Draw.Triangle(this, "Buy" + CurrentBar, true, 0, Low[0] - TickSize * 2, Brushes.LimeGreen);
            }
            else if (sellSignal[0])
            {
                Draw.Triangle(this, "Sell" + CurrentBar, true, 0, High[0] + TickSize * 2, Brushes.Red);
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		
		private NadarayaWatsonEstimator[] cacheNadarayaWatsonEstimator;

		public NadarayaWatsonEstimator NadarayaWatsonEstimator(int length, double h)
		{
			return NadarayaWatsonEstimator(Input, length, h);
		}

		public NadarayaWatsonEstimator NadarayaWatsonEstimator(ISeries<double> input, int length, double h)
		{
			if (cacheNadarayaWatsonEstimator != null)
				for (int idx = 0; idx < cacheNadarayaWatsonEstimator.Length; idx++)
					if (cacheNadarayaWatsonEstimator[idx].Length == length && cacheNadarayaWatsonEstimator[idx].h == h && cacheNadarayaWatsonEstimator[idx].EqualsInput(input))
						return cacheNadarayaWatsonEstimator[idx];
			return CacheIndicator<NadarayaWatsonEstimator>(new NadarayaWatsonEstimator(){ Length = length, h = h }, input, ref cacheNadarayaWatsonEstimator);
		}

	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		
		public NadarayaWatsonEstimator NadarayaWatsonEstimator(int length, double h)
		{
			return indicator.NadarayaWatsonEstimator(Input, length, h);
		}


		
		public NadarayaWatsonEstimator NadarayaWatsonEstimator(ISeries<double> input , int length, double h)
		{
			return indicator.NadarayaWatsonEstimator(input, length, h);
		}
	
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		
		public NadarayaWatsonEstimator NadarayaWatsonEstimator(int length, double h)
		{
			return indicator.NadarayaWatsonEstimator(Input, length, h);
		}


		
		public NadarayaWatsonEstimator NadarayaWatsonEstimator(ISeries<double> input , int length, double h)
		{
			return indicator.NadarayaWatsonEstimator(input, length, h);
		}

	}
}

#endregion
