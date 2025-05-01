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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using SharpDX.DirectWrite;	
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators.SmoothTrendStrategy
{
	public class RangeMarker : Indicator
	{
		private int				actualRange, barRange, digits, margin, penSize, rangeCount, rangeHighY, rangeLowY;
		private	float			bDist, rectHeight, rectWidth;
		private double			highPrice, lowPrice, width, height; // 12-27-2021 moved from OnRender so can use in OnCalculateMinMax
		const	float 			fontHeight	=	11f; // was 12
		private bool			isRangeChart, showPrices, volumetricBarType;
		private const string	noRangeMessage = "Solo funciona en grafico de Rango";

		private	SharpDX.Direct2D1.Brush			brushToUse;
		private	Dictionary<string, DXMediaMap>	dxmBrushes;
		private	SharpDX.DirectWrite.TextFormat	textFormat;
		private	Dictionary<string, Point>		renderPoints;
		private SharpDX.RectangleF				rect;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "RangeMarker";
				Calculate							= Calculate.OnPriceChange;
				IsOverlay							= true;
				DrawOnPricePanel					= true;
				PaintPriceMarkers					= false;
				IsSuspendedWhileInactive			= true;
				IsAutoScale							= true;
				dxmBrushes	= new Dictionary<string, DXMediaMap>();

				foreach (string brushName in new string[] { "DefaultColor", "WarningColor", "LockedColor", "TextColor" })
					dxmBrushes.Add(brushName, new DXMediaMap());
				
				DefaultColor						= Brushes.Gold;
				WarningColor						= Brushes.Blue;
				LockedColor							= Brushes.Red;
				TextColor							= Brushes.CornflowerBlue;
				ShowPrices							= true;
				
			}
			else if (State == State.DataLoaded)
			{
				if (ChartControl == null)
					return;

				isRangeChart		= false;
				volumetricBarType	= false;

				if (BarsPeriod.BarsPeriodType == BarsPeriodType.Range || (BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric
						&& BarsPeriod.BaseBarsPeriodType == BarsPeriodType.Range))  // 12/27/2021
				{
					isRangeChart	= true;

					// calc digits from TickSize
					digits			= 0;
					string s = ((decimal)TickSize).ToString(System.Globalization.CultureInfo.InvariantCulture);
					if (s.Contains("."))
						digits = s.Substring(s.IndexOf(".")).Length - 1;
				}

				if (!isRangeChart)      // 12-27-2021
					Draw.TextFixed(this, "error", noRangeMessage, TextPosition.BottomRight);

				if (BarsPeriod.BarsPeriodType == BarsPeriodType.Volumetric)
					volumetricBarType = true;

				textFormat	= new TextFormat(Core.Globals.DirectWriteFactory, "Arial", SharpDX.DirectWrite.FontWeight.Normal, SharpDX.DirectWrite.FontStyle.Normal, fontHeight);

				renderPoints	= new Dictionary<string, Point>();
				foreach (string pointName in new string[] { "endPoint", "endPoint1", "startPoint", "startPoint1", "upperTextPoint1", "upperTextPoint2", "upperTextPoint3", "upperTextPoint4" })
					renderPoints.Add(pointName, new Point());

				rect = new SharpDX.RectangleF();
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}
		
		public override void OnRenderTargetChanged()
		{
			try
			{
				foreach (KeyValuePair<string, DXMediaMap> item in dxmBrushes)
				{
					if (item.Value.DxBrush != null)
						item.Value.DxBrush.Dispose();

					if (RenderTarget != null)
						item.Value.DxBrush = item.Value.MediaBrush.ToDxBrush(RenderTarget);					
				}
			}
			catch (Exception exception)
			{
				Log(exception.ToString(), LogLevel.Error);
			}
		}

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if (!isRangeChart || Bars == null)      // 12-27-2021
			{
				base.OnRender(chartControl, chartScale);
				return;
			}
			
			renderPoints["startPoint"]		= UpdatePoint(renderPoints["startPoint"], ChartPanel.X, ChartPanel.Y);
			renderPoints["endPoint"]		= UpdatePoint(renderPoints["endPoint"], ChartPanel.X + ChartPanel.W, ChartPanel.Y + ChartPanel.H);
			renderPoints["startPoint1"]		= UpdatePoint(renderPoints["startPoint1"], ChartPanel.X, ChartPanel.Y + ChartPanel.H);
			renderPoints["endPoint1"]		= UpdatePoint(renderPoints["endPoint1"], ChartPanel.X + ChartPanel.W, ChartPanel.Y);			

			width		= renderPoints["endPoint"].X - renderPoints["startPoint"].X;
			height		= renderPoints["endPoint"].Y - renderPoints["startPoint"].Y;

			actualRange	= (int) Math.Round(Math.Max(Bars.GetClose(CurrentBar) - Bars.GetLow(CurrentBar), Bars.GetHigh(CurrentBar) - Bars.GetClose(CurrentBar)) / Bars.Instrument.MasterInstrument.TickSize);

			if (volumetricBarType)
				rangeCount	= BarsPeriod.BaseBarsPeriodValue - actualRange;
			else
				rangeCount	= BarsPeriod.Value - actualRange;
				
			// determine wiggle room in ticks
				
			barRange = (int) Math.Round( (Bars.GetHigh(CurrentBar) - Bars.GetLow(CurrentBar)) / Bars.Instrument.MasterInstrument.TickSize);

			if (volumetricBarType)
				margin = (BarsPeriod.BaseBarsPeriodValue - barRange);
			else
				margin = (BarsPeriod.Value - barRange);			

			// calc our rectangle properties
			highPrice	= Bars.GetHigh(CurrentBar) + (margin * TickSize);
			lowPrice	=  Bars.GetLow(CurrentBar)  - (margin * TickSize);

			bDist		= (volumetricBarType) ? 0 : chartControl.Properties.BarDistance < 12 ? 6 : 12; // make rectangle tighter if small bar spacing

			rangeHighY 	= (int) ((height + ChartPanel.Y) - (((highPrice - chartScale.MinValue) / chartScale.MaxMinusMin) * height) - 1); // modified 9/23/2020 to account for not being the top panel
			rangeLowY	= (int) ((height + ChartPanel.Y) - (((lowPrice - chartScale.MinValue) / chartScale.MaxMinusMin) * height) - 1);	 // modified 9/23/2020 to account for not being the top panel
			rectHeight	= rangeLowY - rangeHighY;					
			rectWidth 	= chartControl.GetBarPaintWidth(ChartControl.BarsArray[0]) + bDist;

			penSize = 1;
			
			if (margin > 1 )
			{
				brushToUse = dxmBrushes["DefaultColor"].DxBrush;
			}
			else if (margin == 1)
			{
				brushToUse = dxmBrushes["WarningColor"].DxBrush;
				penSize = 2;
			}
			else 
			{
				brushToUse = dxmBrushes["LockedColor"].DxBrush;
				penSize = 2;
			}

			UpdateRect(ref rect, (chartControl.GetXByBarIndex(ChartBars, CurrentBar) - (chartControl.GetBarPaintWidth(ChartBars) / 2) - bDist / 2), rangeHighY + 1, rectWidth, rectHeight);
			
			RenderTarget.DrawRectangle(rect, brushToUse, penSize);  // Draw the rectangle
			
			if (showPrices)
			{
				renderPoints["upperTextPoint1"] = UpdatePoint(renderPoints["upperTextPoint1"], (chartControl.GetXByBarIndex(ChartBars, CurrentBar)), (rangeHighY - 16));

				TextLayout textLayout1	= new TextLayout(Core.Globals.DirectWriteFactory, highPrice.ToString("F" + digits), textFormat, ChartPanel.X + ChartPanel.W, fontHeight);
						
				RenderTarget.DrawTextLayout(renderPoints["upperTextPoint1"].ToVector2(), textLayout1, dxmBrushes["TextColor"].DxBrush, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

				renderPoints["upperTextPoint2"]	= UpdatePoint(renderPoints["upperTextPoint2"], (chartControl.GetXByBarIndex(ChartBars, CurrentBar)), rangeLowY + 5);

				TextLayout textLayout2	= new TextLayout(Core.Globals.DirectWriteFactory, lowPrice.ToString("F" + digits), textFormat, ChartPanel.X + ChartPanel.W, fontHeight);

				RenderTarget.DrawTextLayout(renderPoints["upperTextPoint2"].ToVector2(), textLayout2, dxmBrushes["TextColor"].DxBrush, SharpDX.Direct2D1.DrawTextOptions.NoSnap);

				renderPoints["upperTextPoint3"]	= UpdatePoint(renderPoints["upperTextPoint3"], (chartControl.GetXByBarIndex(ChartBars, CurrentBar) + (rectWidth / 2) +3), rangeHighY);

				TextLayout textLayout3 = new TextLayout(Core.Globals.DirectWriteFactory, "R:" + rangeCount, textFormat, ChartPanel.X + ChartPanel.W, fontHeight);

				RenderTarget.DrawTextLayout(renderPoints["upperTextPoint3"].ToVector2(), textLayout3, TextColor.ToDxBrush(RenderTarget), SharpDX.Direct2D1.DrawTextOptions.NoSnap);

				renderPoints["upperTextPoint4"] = UpdatePoint(renderPoints["upperTextPoint4"], (chartControl.GetXByBarIndex(ChartBars, CurrentBar) + (rectWidth / 2) + 3), rangeLowY - 10);

				TextLayout textLayout4 = new TextLayout(Core.Globals.DirectWriteFactory, "C:" + barRange, textFormat, ChartPanel.X + ChartPanel.W, fontHeight);

				RenderTarget.DrawTextLayout(renderPoints["upperTextPoint4"].ToVector2(), textLayout4, TextColor.ToDxBrush(RenderTarget), SharpDX.Direct2D1.DrawTextOptions.NoSnap);

			} // if(showPrices)

			base.OnRender(chartControl, chartScale);
		}

		private Point UpdatePoint(Point point, double X, double Y)
		{
			point.X		= X;
			point.Y		= Y;
			return point;
		}

		private void UpdateRect(ref SharpDX.RectangleF updateRectangle, float x, float y, float width, float height)
		{
			updateRectangle.X		= x;
			updateRectangle.Y		= y;
			updateRectangle.Width	= width;
			updateRectangle.Height	= height;
		}

		private void UpdateRect(ref SharpDX.RectangleF rectangle, int x, int y, int width, int height)
		{
			UpdateRect(ref rectangle, (float)x, (float)y, (float)width, (float)height);
		}
		
		public override void OnCalculateMinMax()  // 12-27-2021 added for better visual near top and bottom to help chart autoscale
		{
			double tmpMin = double.MaxValue;
  			double tmpMax = double.MinValue;
			
			tmpMin = Math.Min(tmpMin, lowPrice);
    		tmpMax = Math.Max(tmpMax, highPrice);
			
			MinValue = tmpMin - 3 * TickSize;
			MaxValue = tmpMax + 3 * TickSize;			
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Show Prices", Description="Show price at high and low of box.", Order=1, GroupName="Visual")]
		public bool ShowPrices
        {
            get { return showPrices; }
            set { showPrices = value; }
        }

		[XmlIgnore]
		[Display(Name="Default Color", Description="Default Box Color", Order=3, GroupName="Visual")]
		public Brush DefaultColor
        {
            get { return dxmBrushes["DefaultColor"].MediaBrush; }
			set { dxmBrushes["DefaultColor"].MediaBrush = value; }
        }

		[Browsable(false)]
		public string DefaultColorSerializable
		{
			get { return Serialize.BrushToString(DefaultColor); }
			set { DefaultColor = Serialize.StringToBrush(value); }
		}			

		[XmlIgnore]
		[Display(Name="Locked Color", Description="Locked Box Color", Order=4, GroupName="Visual")]
		public Brush LockedColor
        {
            get { return dxmBrushes["LockedColor"].MediaBrush; }
            set { dxmBrushes["LockedColor"].MediaBrush = value; }
        }

		[Browsable(false)]
		public string LockedColorSerializable
		{
			get { return Serialize.BrushToString(LockedColor); }
			set { LockedColor = Serialize.StringToBrush(value); }
		}			

		[XmlIgnore]
		[Display(Name="Text Color", Description="Text Color", Order=2, GroupName="Visual")]
		public Brush TextColor
        {
            get { return dxmBrushes["TextColor"].MediaBrush; }
            set { dxmBrushes["TextColor"].MediaBrush = value; }
        }

		[Browsable(false)]
		public string TextColorSerializable
		{
			get { return Serialize.BrushToString(TextColor); }
			set { TextColor = Serialize.StringToBrush(value); }
		}			

		[XmlIgnore]
		[Display(Name="Warning Color", Description="Warning Box Color", Order=5, GroupName="Visual")]
		public Brush WarningColor
        {
            get { return dxmBrushes["WarningColor"].MediaBrush; }
            set { dxmBrushes["WarningColor"].MediaBrush = value; }
        }

		[Browsable(false)]
		public string WarningColorSerializable
		{
			get { return Serialize.BrushToString(WarningColor); }
			set { WarningColor = Serialize.StringToBrush(value); }
		}			
		#endregion
		
		[Browsable(false)]
		public class DXMediaMap
		{
			public SharpDX.Direct2D1.Brush		DxBrush;
			public System.Windows.Media.Brush	MediaBrush;
		}
		
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SmoothTrendStrategy.RangeMarker[] cacheRangeMarker;
		public SmoothTrendStrategy.RangeMarker RangeMarker(bool showPrices)
		{
			return RangeMarker(Input, showPrices);
		}

		public SmoothTrendStrategy.RangeMarker RangeMarker(ISeries<double> input, bool showPrices)
		{
			if (cacheRangeMarker != null)
				for (int idx = 0; idx < cacheRangeMarker.Length; idx++)
					if (cacheRangeMarker[idx] != null && cacheRangeMarker[idx].ShowPrices == showPrices && cacheRangeMarker[idx].EqualsInput(input))
						return cacheRangeMarker[idx];
			return CacheIndicator<SmoothTrendStrategy.RangeMarker>(new SmoothTrendStrategy.RangeMarker(){ ShowPrices = showPrices }, input, ref cacheRangeMarker);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SmoothTrendStrategy.RangeMarker RangeMarker(bool showPrices)
		{
			return indicator.RangeMarker(Input, showPrices);
		}

		public Indicators.SmoothTrendStrategy.RangeMarker RangeMarker(ISeries<double> input , bool showPrices)
		{
			return indicator.RangeMarker(input, showPrices);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SmoothTrendStrategy.RangeMarker RangeMarker(bool showPrices)
		{
			return indicator.RangeMarker(Input, showPrices);
		}

		public Indicators.SmoothTrendStrategy.RangeMarker RangeMarker(ISeries<double> input , bool showPrices)
		{
			return indicator.RangeMarker(input, showPrices);
		}
	}
}

#endregion
