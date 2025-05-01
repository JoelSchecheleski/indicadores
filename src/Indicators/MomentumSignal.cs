#region Using declarations
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Gui;
using System.Windows.Media;
using NinjaTrader.NinjaScript.DrawingTools;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class MomentumSignal : Indicator
    {
        private EMA emaFast;
        private EMA emaSlow;
        private RSI rsi;
        private VWAP vwap;
        private VOL volume;
        private ADX adx;
        private ATR atr;
        private bool lastBuySignal;
        private bool lastSellSignal;
        private double lastVolatility;
        private double[] volumeProfile;
        private int volumeProfilePeriod = 20;
        private double marketStrength;
        private List<double> recentVolumes;

        public MomentumSignal()
        {
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Indicador adaptativo de momentum para day trading com análise multi-timeframe";
                Name = "MomentumSignal";
                Calculate = Calculate.OnEachTick;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                IsSuspendedWhileInactive = true;

                // Parâmetros de Tendência
                FastEmaPeriod = 9;
                SlowEmaPeriod = 21;
                RsiPeriod = 14;
                RsiOverbought = 70;
                RsiOversold = 30;
                
                // Parâmetros de Volatilidade
                AtrPeriod = 14;
                AdxPeriod = 14;
                AdxThreshold = 25;
                VolatilityFilter = true;
                
                // Parâmetros de Volume
                VolumeThreshold = 1.5;
                VolumeLookback = 20;
                
                // Parâmetros de Proteção
                UseVolatilityFilter = true;
                MaxSpread = 2;
                
                recentVolumes = new List<double>();

                // Plots
                AddPlot(new Stroke(Brushes.Green, 2), PlotStyle.Dot, "BuySignal");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Dot, "SellSignal");
                AddPlot(new Stroke(Brushes.Blue, 1), PlotStyle.Line, "MarketStrength");
                AddPlot(new Stroke(Brushes.Orange, 1), PlotStyle.Line, "Volatility");
            }
            else if (State == State.Configure)
            {
                // Configuração dos indicadores técnicos
                emaFast = EMA(Close, FastEmaPeriod);
                emaSlow = EMA(Close, SlowEmaPeriod);
                rsi = RSI(Close, RsiPeriod, 3);
                vwap = VWAP();
                volume = VOL();
                adx = ADX(AdxPeriod);
                atr = ATR(AtrPeriod);

                // Inicialização das variáveis
                lastBuySignal = false;
                lastSellSignal = false;
                lastVolatility = 0;
                volumeProfile = new double[volumeProfilePeriod];
                marketStrength = 0;
            }
        }

        protected void UpdateMarketStrength()
        {
            if (CurrentBar < 1) return;

            // Cálculo simplificado da força do mercado baseado no preço e volume
            double priceChange = Close[0] - Close[1];
            double volumeChange = Volume[0] - Volume[1];
            
            if (Math.Abs(volumeChange) > 0)
            {
                marketStrength = priceChange * (volumeChange / Volume[1]);
            }
            else
            {
                marketStrength = 0;
            }
        }

        protected void UpdateVolatility(double price)
        {
            if (CurrentBar < 1) return;
            
            double currentVolatility = Math.Abs(price - Close[1]) / Close[1] * 100;
            lastVolatility = (lastVolatility * 0.9) + (currentVolatility * 0.1); // Suavização exponencial
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(SlowEmaPeriod, Math.Max(RsiPeriod, AdxPeriod))) return;

            // Análise de Tendência
            bool aboveVwap = Close[0] > vwap[0];
            bool belowVwap = Close[0] < vwap[0];
            bool emaBullish = emaFast[0] > emaSlow[0];
            bool emaBearish = emaFast[0] < emaSlow[0];
            bool rsiValid = rsi[0] > RsiOversold && rsi[0] < RsiOverbought;

            // Análise de Volatilidade
            bool volatilityOk = !UseVolatilityFilter || (atr[0] > atr[1] && adx[0] > AdxThreshold);
            
            // Análise de Volume
            UpdateVolumeProfile();
            bool volumeOk = IsVolumeValid();

            // Atualiza métricas de mercado
            UpdateMarketStrength();
            UpdateVolatility(Close[0]);

            // Filtros de Proteção
            double spread = High[0] - Low[0];
            bool spreadOk = spread <= (MaxSpread * TickSize);

            // Sinal de Compra
            lastBuySignal = aboveVwap && emaBullish && rsiValid && 
                           CrossAbove(emaFast, emaSlow, 1) &&
                           volatilityOk && volumeOk && spreadOk &&
                           marketStrength > 0;

            if (lastBuySignal)
            {
                Draw.ArrowUp(this, "BuySignal" + CurrentBar, true, 0, Low[0] - 2 * TickSize, Brushes.Green);
                Values[0][0] = Low[0] - 2 * TickSize;
            }

            // Sinal de Venda
            lastSellSignal = belowVwap && emaBearish && rsiValid && 
                            CrossBelow(emaFast, emaSlow, 1) &&
                            volatilityOk && volumeOk && spreadOk &&
                            marketStrength < 0;

            if (lastSellSignal)
            {
                Draw.ArrowDown(this, "SellSignal" + CurrentBar, true, 0, High[0] + 2 * TickSize, Brushes.Red);
                Values[1][0] = High[0] + 2 * TickSize;
            }

            // Atualiza plots
            Values[2][0] = marketStrength * 100; // Market Strength
            Values[3][0] = lastVolatility;      // Volatility
        }

        private void UpdateVolumeProfile()
        {
            if (CurrentBar < VolumeLookback) return;

            // Atualiza o perfil de volume
            for (int i = volumeProfile.Length - 1; i > 0; i--)
            {
                volumeProfile[i] = volumeProfile[i - 1];
            }
            volumeProfile[0] = Volume[0];

            // Mantém uma lista dos volumes recentes
            recentVolumes.Add(Volume[0]);
            if (recentVolumes.Count > VolumeLookback)
            {
                recentVolumes.RemoveAt(0);
            }
        }

        private bool IsVolumeValid()
        {
            if (recentVolumes.Count < VolumeLookback) return false;

            double avgVolume = recentVolumes.Average();
            return Volume[0] >= (avgVolume * VolumeThreshold);
        }

        #region Properties
        // Parâmetros de Tendência
        [NinjaScriptProperty]
        [RangeAttribute(1, int.MaxValue)]
        [Display(Name = "Fast EMA Period", Order = 1, GroupName = "Tendência")]
        public int FastEmaPeriod { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(1, int.MaxValue)]
        [Display(Name = "Slow EMA Period", Order = 2, GroupName = "Tendência")]
        public int SlowEmaPeriod { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(1, int.MaxValue)]
        [Display(Name = "RSI Period", Order = 3, GroupName = "Tendência")]
        public int RsiPeriod { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(0, 100)]
        [Display(Name = "RSI Overbought", Order = 4, GroupName = "Tendência")]
        public double RsiOverbought { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(0, 100)]
        [Display(Name = "RSI Oversold", Order = 5, GroupName = "Tendência")]
        public double RsiOversold { get; set; }

        // Parâmetros de Volatilidade
        [NinjaScriptProperty]
        [RangeAttribute(1, int.MaxValue)]
        [Display(Name = "ATR Period", Order = 1, GroupName = "Volatilidade")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(1, int.MaxValue)]
        [Display(Name = "ADX Period", Order = 2, GroupName = "Volatilidade")]
        public int AdxPeriod { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(0, 100)]
        [Display(Name = "ADX Threshold", Order = 3, GroupName = "Volatilidade")]
        public double AdxThreshold { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Use Volatility Filter", Order = 4, GroupName = "Volatilidade")]
        public bool UseVolatilityFilter { get; set; }

        // Parâmetros de Volume
        [NinjaScriptProperty]
        [RangeAttribute(1.0, 10.0)]
        [Display(Name = "Volume Threshold", Order = 1, GroupName = "Volume")]
        public double VolumeThreshold { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(5, 100)]
        [Display(Name = "Volume Lookback", Order = 2, GroupName = "Volume")]
        public int VolumeLookback { get; set; }

        // Parâmetros de Proteção
        [NinjaScriptProperty]
        [Display(Name = "Use Volatility Filter", Order = 1, GroupName = "Proteção")]
        public bool VolatilityFilter { get; set; }

        [NinjaScriptProperty]
        [RangeAttribute(1, 10)]
        [Display(Name = "Max Spread (Ticks)", Order = 2, GroupName = "Proteção")]
        public int MaxSpread { get; set; }
        #endregion

        #region Signal Properties
        public bool IsBuySignal
        {
            get { return lastBuySignal; }
        }

        public bool IsSellSignal
        {
            get { return lastSellSignal; }
        }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private MomentumSignal[] cacheMomentumSignal;
        public MomentumSignal MomentumSignal(int fastEmaPeriod, int slowEmaPeriod, int rsiPeriod, double rsiOverbought, double rsiOversold)
        {
            return MomentumSignal(Input, fastEmaPeriod, slowEmaPeriod, rsiPeriod, rsiOverbought, rsiOversold);
        }

        public MomentumSignal MomentumSignal(ISeries<double> input, int fastEmaPeriod, int slowEmaPeriod, int rsiPeriod, double rsiOverbought, double rsiOversold)
        {
            if (cacheMomentumSignal != null)
                for (int idx = 0; idx < cacheMomentumSignal.Length; idx++)
                    if (cacheMomentumSignal[idx] != null && cacheMomentumSignal[idx].FastEmaPeriod == fastEmaPeriod && cacheMomentumSignal[idx].SlowEmaPeriod == slowEmaPeriod && cacheMomentumSignal[idx].RsiPeriod == rsiPeriod && cacheMomentumSignal[idx].RsiOverbought == rsiOverbought && cacheMomentumSignal[idx].RsiOversold == rsiOversold && cacheMomentumSignal[idx].EqualsInput(input))
                        return cacheMomentumSignal[idx];
            return CacheIndicator<MomentumSignal>(new MomentumSignal(){ FastEmaPeriod = fastEmaPeriod, SlowEmaPeriod = slowEmaPeriod, RsiPeriod = rsiPeriod, RsiOverbought = rsiOverbought, RsiOversold = rsiOversold }, input, ref cacheMomentumSignal);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.MomentumSignal MomentumSignal(int fastEmaPeriod, int slowEmaPeriod, int rsiPeriod, double rsiOverbought, double rsiOversold)
        {
            return indicator.MomentumSignal(Input, fastEmaPeriod, slowEmaPeriod, rsiPeriod, rsiOverbought, rsiOversold);
        }

        public Indicators.MomentumSignal MomentumSignal(ISeries<double> input , int fastEmaPeriod, int slowEmaPeriod, int rsiPeriod, double rsiOverbought, double rsiOversold)
        {
            return indicator.MomentumSignal(input, fastEmaPeriod, slowEmaPeriod, rsiPeriod, rsiOverbought, rsiOversold);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.MomentumSignal MomentumSignal(int fastEmaPeriod, int slowEmaPeriod, int rsiPeriod, double rsiOverbought, double rsiOversold)
        {
            return indicator.MomentumSignal(Input, fastEmaPeriod, slowEmaPeriod, rsiPeriod, rsiOverbought, rsiOversold);
        }

        public Indicators.MomentumSignal MomentumSignal(ISeries<double> input , int fastEmaPeriod, int slowEmaPeriod, int rsiPeriod, double rsiOverbought, double rsiOversold)
        {
            return indicator.MomentumSignal(input, fastEmaPeriod, slowEmaPeriod, rsiPeriod, rsiOverbought, rsiOversold);
        }
    }
}

#endregion
