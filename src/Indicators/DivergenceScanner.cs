#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class DivergenceScanner : Indicator
    {
        private RSI rsi;
        private List<PricePoint> swingHighs;
        private List<PricePoint> swingLows;
        private const int SWING_LOOKBACK = 5;

        private class PricePoint
        {
            public int BarIndex { get; set; }
            public double Price { get; set; }
            public double Rsi { get; set; }
            public bool IsValid { get; set; }

            public PricePoint(int barIndex, double price, double rsi)
            {
                BarIndex = barIndex;
                Price = price;
                Rsi = rsi;
                IsValid = true;
            }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Scanner de Divergências Regulares e Escondidas";
                Name = "DivergenceScanner";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
                DrawOnPricePanel = true;
                IsSuspendedWhileInactive = true;

                // Parâmetros
                RsiPeriod = 14;
                RsiSmoothing = 3;
                SwingStrength = 3;
                MinimumDivergenceStrength = 5;

                // Plots para divergências
                AddPlot(new Stroke(Brushes.Green, 2), PlotStyle.Line, "BullishDivergence");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Line, "BearishDivergence");
                AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Dot, "HiddenBullishDivergence");
                AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Dot, "HiddenBearishDivergence");

                // Plots para sinais
                AddPlot(new Stroke(Brushes.LimeGreen, 2), PlotStyle.Cross, "BullishSignal");
                AddPlot(new Stroke(Brushes.Crimson, 2), PlotStyle.Cross, "BearishSignal");
            }
            else if (State == State.Configure)
            {
                rsi = RSI(Close, RsiPeriod, RsiSmoothing);
                swingHighs = new List<PricePoint>();
                swingLows = new List<PricePoint>();
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < RsiPeriod + SwingStrength) return;

            // Identificar Swing Points
            if (IsSwingHigh(SwingStrength))
            {
                swingHighs.Add(new PricePoint(CurrentBar, High[0], rsi[0]));
                if (swingHighs.Count > SWING_LOOKBACK) swingHighs.RemoveAt(0);
            }

            if (IsSwingLow(SwingStrength))
            {
                swingLows.Add(new PricePoint(CurrentBar, Low[0], rsi[0]));
                if (swingLows.Count > SWING_LOOKBACK) swingLows.RemoveAt(0);
            }

            // Limpar valores anteriores
            Values[0][0] = 0;  // BullishDivergence
            Values[1][0] = 0;  // BearishDivergence
            Values[2][0] = 0;  // HiddenBullishDivergence
            Values[3][0] = 0;  // HiddenBearishDivergence
            Values[4][0] = 0;  // BullishSignal
            Values[5][0] = 0;  // BearishSignal

            // Verificar Divergências
            CheckRegularDivergences();
            CheckHiddenDivergences();
        }

        private bool IsSwingHigh(int strength)
        {
            if (CurrentBar < strength * 2) return false;

            for (int i = 1; i <= strength; i++)
            {
                if (High[0] <= High[i] || High[0] <= High[-i])
                    return false;
            }
            return true;
        }

        private bool IsSwingLow(int strength)
        {
            if (CurrentBar < strength * 2) return false;

            for (int i = 1; i <= strength; i++)
            {
                if (Low[0] >= Low[i] || Low[0] >= Low[-i])
                    return false;
            }
            return true;
        }

        private void CheckRegularDivergences()
        {
            if (swingLows.Count >= 2)
            {
                var current = swingLows[swingLows.Count - 1];
                var previous = swingLows[swingLows.Count - 2];
                
                // Divergência de alta: preço faz mínimo mais baixo mas RSI faz mínimo mais alto
                if (current.Price < previous.Price && current.Rsi > previous.Rsi)
                {
                    Values[0][0] = 1;  // Sinal bullish
                    Values[4][0] = High[0];  // Plot triângulo verde
                    Draw.Line(this, "BullishDiv" + CurrentBar, false, 
                             previous.BarIndex, previous.Price, 
                             current.BarIndex, current.Price, 
                             Brushes.Green, DashStyleHelper.Solid, 2);
                }
            }

            if (swingHighs.Count >= 2)
            {
                var current = swingHighs[swingHighs.Count - 1];
                var previous = swingHighs[swingHighs.Count - 2];
                
                // Divergência de baixa: preço faz máximo mais alto mas RSI faz máximo mais baixo
                if (current.Price > previous.Price && current.Rsi < previous.Rsi)
                {
                    Values[1][0] = 1;  // Sinal bearish
                    Values[5][0] = Low[0];  // Plot triângulo vermelho
                    Draw.Line(this, "BearishDiv" + CurrentBar, false, 
                             previous.BarIndex, previous.Price, 
                             current.BarIndex, current.Price, 
                             Brushes.Red, DashStyleHelper.Solid, 2);
                }
            }
        }

        private void CheckHiddenDivergences()
        {
            // Divergência de Alta Escondida (Hidden Bullish)
            if (swingLows.Count >= 2)
            {
                var current = swingLows[swingLows.Count - 1];
                var previous = swingLows[swingLows.Count - 2];

                if (current.Price.ApproxCompare(previous.Price) > 0 && current.Rsi.ApproxCompare(previous.Rsi) < 0)
                {
                    if (CurrentBar - previous.BarIndex <= MinimumDivergenceStrength)
                    {
                        Values[2][0] = 1; // Sinal Hidden Bullish
                        Draw.Line(this, "HidBullDiv" + CurrentBar, false, previous.BarIndex, previous.Price, 
                                CurrentBar, Low[0], Brushes.Blue, DashStyleHelper.Dash, 2);
                    }
                }
            }

            // Divergência de Baixa Escondida (Hidden Bearish)
            if (swingHighs.Count >= 2)
            {
                var current = swingHighs[swingHighs.Count - 1];
                var previous = swingHighs[swingHighs.Count - 2];

                if (current.Price.ApproxCompare(previous.Price) < 0 && current.Rsi.ApproxCompare(previous.Rsi) > 0)
                {
                    if (CurrentBar - previous.BarIndex <= MinimumDivergenceStrength)
                    {
                        Values[3][0] = 1; // Sinal Hidden Bearish
                        Draw.Line(this, "HidBearDiv" + CurrentBar, false, previous.BarIndex, previous.Price, 
                                CurrentBar, High[0], Brushes.Orange, DashStyleHelper.Dash, 2);
                    }
                }
            }
        }

        #region Properties
        [Range(2, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Período RSI", Description = "Período para cálculo do RSI", Order = 1, GroupName = "Parâmetros")]
        public int RsiPeriod { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Suavização RSI", Description = "Período de suavização do RSI", Order = 2, GroupName = "Parâmetros")]
        public int RsiSmoothing { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Força do Swing", Description = "Número de barras para confirmar um swing point", Order = 3, GroupName = "Parâmetros")]
        public int SwingStrength { get; set; }

        [Range(1, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Força Mínima da Divergência", Description = "Número máximo de barras entre os pontos de divergência", Order = 4, GroupName = "Parâmetros")]
        public int MinimumDivergenceStrength { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.
namespace NinjaTrader.NinjaScript.Indicators
{
    public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
    {
        private DivergenceScanner[] cacheDivergenceScanner;
        public DivergenceScanner DivergenceScanner()
        {
            return DivergenceScanner(Input);
        }

        public DivergenceScanner DivergenceScanner(ISeries<double> input)
        {
            if (cacheDivergenceScanner != null)
                for (int idx = 0; idx < cacheDivergenceScanner.Length; idx++)
                    if (cacheDivergenceScanner[idx] != null &&  cacheDivergenceScanner[idx].EqualsInput(input))
                        return cacheDivergenceScanner[idx];
            return CacheIndicator<DivergenceScanner>(new DivergenceScanner(), input, ref cacheDivergenceScanner);
        }
    }
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
    public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
    {
        public Indicators.DivergenceScanner DivergenceScanner()
        {
            return indicator.DivergenceScanner(Input);
        }

        public Indicators.DivergenceScanner DivergenceScanner(ISeries<double> input)
        {
            return indicator.DivergenceScanner(input);
        }
    }
}

namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {
        public Indicators.DivergenceScanner DivergenceScanner()
        {
            return indicator.DivergenceScanner(Input);
        }

        public Indicators.DivergenceScanner DivergenceScanner(ISeries<double> input)
        {
            return indicator.DivergenceScanner(input);
        }
    }
}
#endregion
