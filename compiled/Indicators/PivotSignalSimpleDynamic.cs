#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;

#endregion



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
					if ( cachePivotSignalSimpleDynamic[idx].EqualsInput(input))
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
