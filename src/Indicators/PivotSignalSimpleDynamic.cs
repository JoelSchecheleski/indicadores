// PivotSignalSimpleDynamic - Indicador de Pivôs com suporte para Estratégia
// Desenvolvido por Tom - O Cara da NinjaTrader 🚀

#region Using declarations
using System;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media; // Necessário para Brushes
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class PivotSignalSimpleDynamic : Indicator
    {
        public bool BuySignal { get; private set; }
        public bool SellSignal { get; private set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "PivotSignalSimpleDynamic";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                DisplayInDataBox = true;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 2)
                return;

            BuySignal = false;
            SellSignal = false;

            // Pivô de Alta
            if (Low[2] > Low[1] && Low[0] > Low[1])
            {
                BuySignal = true;
                Draw.Dot(this, "PivotLow" + CurrentBar, true, 0, Low[0] - TickSize, Brushes.Green);
            }

            // Pivô de Baixa
            if (High[2] < High[1] && High[0] < High[1])
            {
                SellSignal = true;
                Draw.Dot(this, "PivotHigh" + CurrentBar, true, 0, High[0] + TickSize, Brushes.Red);
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PivotSignalSimpleDynamic[] cachePivotSignalSimpleDynamic;
		public PivotSignalSimpleDynamic PivotSignalSimpleDynamic()
		{
			return PivotSignalSimpleDynamic(Input);
		}

		public PivotSignalSimpleDynamic PivotSignalSimpleDynamic(ISeries<double> input)
		{
			if (cachePivotSignalSimpleDynamic != null)
				for (int idx = 0; idx < cachePivotSignalSimpleDynamic.Length; idx++)
					if (cachePivotSignalSimpleDynamic[idx] != null &&  cachePivotSignalSimpleDynamic[idx].EqualsInput(input))
						return cachePivotSignalSimpleDynamic[idx];
			return CacheIndicator<PivotSignalSimpleDynamic>(new PivotSignalSimpleDynamic(), input, ref cachePivotSignalSimpleDynamic);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PivotSignalSimpleDynamic PivotSignalSimpleDynamic()
		{
			return indicator.PivotSignalSimpleDynamic(Input);
		}

		public Indicators.PivotSignalSimpleDynamic PivotSignalSimpleDynamic(ISeries<double> input )
		{
			return indicator.PivotSignalSimpleDynamic(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PivotSignalSimpleDynamic PivotSignalSimpleDynamic()
		{
			return indicator.PivotSignalSimpleDynamic(Input);
		}

		public Indicators.PivotSignalSimpleDynamic PivotSignalSimpleDynamic(ISeries<double> input )
		{
			return indicator.PivotSignalSimpleDynamic(input);
		}
	}
}

#endregion
