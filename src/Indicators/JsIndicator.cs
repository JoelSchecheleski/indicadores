#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Gui;
using NinjaTrader.Cbi;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui.Chart;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class JsIndicator : Indicator
    {
        private Series<double> upperBand;
        private Series<double> lowerBand;
        private Series<double> smaFast;
        private Series<double> smaSlow;
        private Series<double> buffer1;
        private Series<double> buffer2;
        private Series<double> ema100;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Indicador combinado Target Thor 2.0";
                Name = "JsIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                DrawHorizontalGridLines = true;
                DrawVerticalGridLines = true;
                PaintPriceMarkers = true;
                ScaleJustification = NinjaTrader.Gui.Chart.ScaleJustification.Right;

                // Parâmetros padrão
                MaFastPeriod = 1;
                MaSlowPeriod = 30;
                SignalPeriod = 4;
                BollingerPeriod = 20;
                BollingerMultiplier = 3.0;
                ShowBollingerBands = true;

                // Cores padrão
                BuyColor = Brushes.Green;
                SellColor = Brushes.Red;
                UpperBandColor = Brushes.Silver;
                LowerBandColor = Brushes.Silver;
                EmaColor = Brushes.Blue;
            }
            else if (State == State.Configure)
            {
                // Inicializa o array Values com 5 plots (3 linhas + 2 sinais)
                Values = new Series<double>[5];
                for (int i = 0; i < Values.Length; i++)
                    Values[i] = new Series<double>(this);
            }
            else if (State == State.DataLoaded)
            {
                upperBand = new Series<double>(this);
                lowerBand = new Series<double>(this);
                smaFast = new Series<double>(this);
                smaSlow = new Series<double>(this);
                buffer1 = new Series<double>(this);
                buffer2 = new Series<double>(this);
                ema100 = new Series<double>(this);

                // Configuração dos plots
                AddPlot(new Stroke(UpperBandColor, 2), PlotStyle.Line, "Upper Band");
                AddPlot(new Stroke(LowerBandColor, 2), PlotStyle.Line, "Lower Band");
                AddPlot(new Stroke(EmaColor, 2), PlotStyle.Line, "EMA100");
                AddPlot(new Stroke(BuyColor, 2), PlotStyle.Dot, "Buy Signal");
                AddPlot(new Stroke(SellColor, 2), PlotStyle.Dot, "Sell Signal");
            }
        }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Ma Fast Period", Order = 1, GroupName = "Parameters")]
        public int MaFastPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Ma Slow Period", Order = 2, GroupName = "Parameters")]
        public int MaSlowPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Signal Period", Order = 3, GroupName = "Parameters")]
        public int SignalPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Bollinger Period", Order = 4, GroupName = "Parameters")]
        public int BollingerPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Bollinger Multiplier", Order = 5, GroupName = "Parameters")]
        public double BollingerMultiplier { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Bollinger Bands", Order = 6, GroupName = "Parameters")]
        public bool ShowBollingerBands { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Buy Color", Order = 7, GroupName = "Colors")]
        public Brush BuyColor
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Sell Color", Order = 8, GroupName = "Colors")]
        public Brush SellColor
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Upper Band Color", Order = 9, GroupName = "Colors")]
        public Brush UpperBandColor
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Lower Band Color", Order = 10, GroupName = "Colors")]
        public Brush LowerBandColor
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "EMA Color", Order = 11, GroupName = "Colors")]
        public Brush EmaColor
        { get; set; }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(Math.Max(BollingerPeriod, MaSlowPeriod), 100))
                return;

            // Cálculo das Bandas de Bollinger
            double sma20 = SMA(Close, BollingerPeriod)[0];
            double stdDev = StdDev(Close, BollingerPeriod)[0];
            upperBand[0] = sma20 + (BollingerMultiplier * stdDev);
            lowerBand[0] = sma20 - (BollingerMultiplier * stdDev);

            // Cálculo do EMA 100
            ema100[0] = EMA(Close, 100)[0];

            // Cálculo das médias móveis e sinais
            smaFast[0] = SMA(Close, MaFastPeriod)[0];
            smaSlow[0] = SMA(Close, MaSlowPeriod)[0];
            buffer1[0] = smaFast[0] - smaSlow[0];
            buffer2[0] = WMA(buffer1, SignalPeriod)[0];

            // Plotagem das Bandas de Bollinger e EMA
            if (ShowBollingerBands)
            {
                Values[0][0] = upperBand[0];
                Values[1][0] = lowerBand[0];
                Values[2][0] = ema100[0];
            }
            else
            {
                Values[0][0] = double.NaN;
                Values[1][0] = double.NaN;
                Values[2][0] = double.NaN;
            }

            // Sinais de compra e venda
            if (buffer1[0] > buffer2[0] && buffer1[1] <= buffer2[1])
            {
                Values[3][0] = Low[0] - TickSize;
                Values[4][0] = double.NaN;
            }
            else if (buffer1[0] < buffer2[0] && buffer1[1] >= buffer2[1])
            {
                Values[3][0] = double.NaN;
                Values[4][0] = High[0] + TickSize;
            }
            else
            {
                Values[3][0] = double.NaN;
                Values[4][0] = double.NaN;
            }
        }
    }
}
#endregion
