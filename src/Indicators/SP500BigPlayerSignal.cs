using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using System;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class SP500BigPlayerSignal : Indicator
    {
        private SMA sma;
        private ATR atr;
        private MACD macd;
        private double volumeThreshold;
        private double atrThreshold;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Plots green triangles for buy signals and red triangles for sell signals based on big player activity.";
                Name = "SP500BigPlayerSignal";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = false;
                PaintPriceMarkers = false;

                // Parameters
                SmaPeriod = 50;
                AtrPeriod = 14;
                MacdFast = 12;
                MacdSlow = 26;
                MacdSignal = 9;
                VolumeLookback = 20;
                MinVolumeMultiplier = 1.5;
                AtrMultiplier = 1.0;
            }
            else if (State == State.Configure)
            {
                sma = SMA(SmaPeriod);
                atr = ATR(AtrPeriod);
                macd = MACD(MacdFast, MacdSlow, MacdSignal);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(SmaPeriod, VolumeLookback)) return;

            // Calculate average volume over lookback period
            double avgVolume = 0;
            for (int i = 0; i < VolumeLookback; i++)
                avgVolume += Volume[i];
            avgVolume /= VolumeLookback;

            // Define volume and ATR thresholds
            volumeThreshold = avgVolume * MinVolumeMultiplier;
            atrThreshold = atr[0] * AtrMultiplier;

            // Detect support/resistance via recent swing points
            double swingHigh = High[Math.Min(HighestBar(High, 20), CurrentBar)];
            double swingLow = Low[Math.Min(LowestBar(Low, 20), CurrentBar)];

            // Buy Signal Conditions
            bool isBuySignal = false;
            if (Close[0] > sma[0] && // Price above SMA (uptrend)
                Volume[0] > volumeThreshold && // High volume
                Close[0] <= swingLow * 1.01 && // Near support
                macd.Diff[0] > macd.Diff[1] && // Bullish MACD divergence
                atr[0] < atrThreshold) // Low volatility
            {
                isBuySignal = true;
                PlotBrushes[0][0] = Brushes.Green;
                Draw.TriangleUp(this, "BuySignal" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.Green);
            }

            // Sell Signal Conditions
            bool isSellSignal = false;
            if (Close[0] < sma[0] && // Price below SMA (downtrend)
                Volume[0] > volumeThreshold && // High volume
                Close[0] >= swingHigh * 0.99 && // Near resistance
                macd.Diff[0] < macd.Diff[1] && // Bearish MACD divergence
                atr[0] < atrThreshold) // Low volatility
            {
                isSellSignal = true;
                PlotBrushes[1][0] = Brushes.Red;
                Draw.TriangleDown(this, "SellSignal" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
            }

            // Expose signal values for strategy
            BuySignal[0] = isBuySignal ? 1 : 0;
            SellSignal[0] = isSellSignal ? 1 : 0;
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "SMA Period", Order = 1, GroupName = "Parameters")]
        public int SmaPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATR Period", Order = 2, GroupName = "Parameters")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "MACD Fast", Order = 3, GroupName = "Parameters")]
        public int MacdFast { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "MACD Slow", Order = 4, GroupName = "Parameters")]
        public int MacdSlow { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "MACD Signal", Order = 5, GroupName = "Parameters")]
        public int MacdSignal { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Volume Lookback", Order = 6, GroupName = "Parameters")]
        public int VolumeLookback { get; set; }

        [NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name = "Min Volume Multiplier", Order = 7, GroupName = "Parameters")]
        public double MinVolumeMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "ATR Multiplier", Order = 8, GroupName = "Parameters")]
        public double Atr露

        public double AtrMultiplier { get; set; }

        [Browsable(false)]
        [NinjaScriptProperty]
        [Display(Name = "Buy Signal", Order = 1, GroupName = "Outputs")]
        public Series<double> BuySignal { get; set; }

        [Browsable(false)]
        [NinjaScriptProperty]
        [Display(Name = "Sell Signal", Order = 2, GroupName = "Outputs")]
        public Series<double> SellSignal { get; set; }
        #endregion
    }
}