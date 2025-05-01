#region Using declarations
using System;
using System.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MovingAverageVolumeIndicator : Indicator
    {
        private SMA smaFast;
        private SMA smaSlow;
        private double volumeThreshold;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicador de cruzamento de médias móveis com confirmação de volume";
                Name = "MovingAverageVolumeIndicator";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                PaintPriceMarkers = true;
                IsAutoScale = true;

                FastPeriod = 10;
                SlowPeriod = 20;
                VolumeLookback = 20;
                VolumeMultiplier = 1.5;
                
                // Adiciona plots para sinais
                AddPlot(new Stroke(Brushes.Green, 2), PlotStyle.Dot, "Sinal de Compra");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Dot, "Sinal de Venda");
            }
            else if (State == State.Configure)
            {
                smaFast = SMA(FastPeriod);
                smaSlow = SMA(SlowPeriod);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < SlowPeriod) return;

            // Calcula a média de volume
            double avgVolume = SMA(Volume, VolumeLookback)[0];
            volumeThreshold = avgVolume * VolumeMultiplier;

            // Verifica cruzamento de médias
            bool bullishCross = CrossAbove(smaFast, smaSlow, 1);
            bool bearishCross = CrossBelow(smaFast, smaSlow, 1);

            // Confirmação de volume
            bool highVolume = Volume[0] > volumeThreshold;

            // Sinal de compra (bullish)
            if (bullishCross && highVolume)
            {
                // Sinal de compra
                Values[0][0] = Low[0] - 2 * TickSize;
                Draw.ArrowUp(this, "BuySignal" + CurrentBar, false, 0, Low[0] - 2 * TickSize, Brushes.Green);
                Print("Sinal de compra detectado na barra " + CurrentBar + " em " + Time[0].ToString());
            }

            // Sinal de venda (bearish)
            if (bearishCross && highVolume)
            {
                // Sinal de venda
                Values[1][0] = High[0] + 2 * TickSize;
                Draw.ArrowDown(this, "SellSignal" + CurrentBar, false, 0, High[0] + 2 * TickSize, Brushes.Red);
                Print("Sinal de venda detectado na barra " + CurrentBar + " em " + Time[0].ToString());
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Período da Média Rápida", Order = 1, GroupName = "Parameters")]
        public int FastPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Período da Média Lenta", Order = 2, GroupName = "Parameters")]
        public int SlowPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Período de Lookback do Volume", Order = 3, GroupName = "Parameters")]
        public int VolumeLookback { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Multiplicador de Volume", Order = 4, GroupName = "Parameters")]
        public double VolumeMultiplier { get; set; }
        #endregion
    }
}