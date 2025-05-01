#region Using declarations
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
#endregion

public enum TrendMAType1 { EMA, HMA, SMA, TMA, VMA, WMA, DEMA, TEMA, VWMA, ZLEMA, LinReg }

namespace NinjaTrader.NinjaScript.Indicators.SmoothTrendStrategy
{
    [Gui.CategoryOrder("Options", 1000001)]
    [Gui.CategoryOrder("Custom Colors", 1000002)]

    public class SmoothTrend : Indicator
    {				
        #region Globals

        private TrendMAType1             triggerMAType                   = TrendMAType1.EMA;
        private int                     triggerPeriod                   = 12;
        private TrendMAType1             averageMAType                   = TrendMAType1.EMA;
        private int                     averagePeriod                   = 12;

        private bool                    drawArrows                      = true;

        private bool                    colorRegion                     = true;
        private int                     regionOpacity                   = 30;
        private Brush                   regionUpColor                   = Brushes.Cyan;
        private Brush                   regionDownColor                 = Brushes.Indigo;
        private int                     StartIndex                      = 1;
        private int                     PriorIndex                      = 0;

        private Series<bool>            upTrend                         = null;
        private int                     MinBarsNeeded                   = 1;
        private double                  ArrowTickOffset                 = 0;

        private Brush 					triggerColor; //Color Plots Puntos
        private Brush 					averageColor; //Color Plots Puntos
		
		private double trendPlotValue;
		private Series<double> trendPlot;
		
		private string lastArrowTag = ""; // Guarda el tag de la última flecha dibujada

        #endregion

        /* --------------------------------------------------------------------------------------------------- */

        private void Initialize()
        {			
            AddPlot(new Stroke(Brushes.Cyan, 2), PlotStyle.Line, "Trigger");
            AddPlot(new Stroke(Brushes.Indigo, 2), PlotStyle.Line, "Average");
			
			AddPlot(new Stroke(Brushes.Transparent, 2), PlotStyle.Dot, "Buy Signal"); //Plot para compras
			AddPlot(new Stroke(Brushes.Transparent, 2), PlotStyle.Dot, "Sell Signal"); //Plot para compras
						
			AddPlot(Brushes.Black, "Trend"); //Identifica el cruce y mantiene el plot hasta un nuevo cruce
			
			AddPlot(new Stroke(Brushes.Beige, 4), PlotStyle.TriangleUp, "Cross Above"); //Plot para compras
			AddPlot(new Stroke(Brushes.Beige, 4), PlotStyle.TriangleDown, "Cross Below"); //Plot para compras
			
			
            IsOverlay 				= true;
            ArePlotsConfigurable 	= true;
            PaintPriceMarkers 		= false;
            Calculate 				= Calculate.OnEachTick;

            upTrend 				= new Series<bool>(this);
        }

        /* --------------------------------------------------------------------------------------------------- */

        protected override void OnStateChange()
        {
            switch (State)
            {
                case State.SetDefaults:
                    Name 						= "SmoothTrend";
                    Description 				= "Trigger Lines";
					Indicador					= "SmoothTrend";
					Version						= "1.0 | Mayo 2025";
					IsAutoScale					= false;
					IsSuspendedWhileInactive	= true;
					DrawOnPricePanel			= false;
					DrawSignalsPlot				= true;

                    Initialize();
                    break;

                case State.DataLoaded:
					
                    OnStartUp();  
                    break;
             }
        }

        /* --------------------------------------------------------------------------------------------------- */

        public override string DisplayName
        {
            get { return string.Format("{0}({1},{2},{3},{4})", this.Name,
                    TriggerMAType, TriggerPeriod, AverageMAType, AveragePeriod); }
        }

        /* --------------------------------------------------------------------------------------------------- */

        private void OnStartUp()
        {
            MinBarsNeeded = Math.Max(MinBarsNeeded, TriggerPeriod+AveragePeriod);
        }

        /* --------------------------------------------------------------------------------------------------- */

       protected override void OnBarUpdate()
        {
			#region Indicadores
	            switch (TriggerMAType)
	            {
	                case TrendMAType1.EMA: Trigger[0] = EMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.HMA: Trigger[0] = HMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.SMA: Trigger[0] = SMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.TMA: Trigger[0] = TMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.WMA: Trigger[0] = WMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.DEMA: Trigger[0] = DEMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.TEMA: Trigger[0] = TEMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.VWMA: Trigger[0] = VWMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.ZLEMA: Trigger[0] = ZLEMA(Input, TriggerPeriod)[0]; break;
	                case TrendMAType1.LinReg: Trigger[0] = LinReg(Input, TriggerPeriod)[0]; break;
	            }
	
	            switch (AverageMAType)
	            {
	                case TrendMAType1.EMA: Average[0] = EMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.HMA: Average[0] = HMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.SMA: Average[0] = SMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.TMA: Average[0] = TMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.WMA: Average[0] = WMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.DEMA: Average[0] = DEMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.TEMA: Average[0] = TEMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.VWMA: Average[0] = VWMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.ZLEMA: Average[0] = ZLEMA(Trigger, AveragePeriod)[0]; break;
	                case TrendMAType1.LinReg: Average[0] = LinReg(Trigger, AveragePeriod)[0]; break;
	            }
				#endregion						
	
	        if (CurrentBar < MinBarsNeeded)
				return;
			
			#region Plots	
			// Definir colores de plots
	        triggerColor = Trigger[0] > Average[0] ? RegionUpColor : RegionDownColor; // Ajusta los colores según tu necesidad
	        averageColor = Trigger[0] < Average[0] ? RegionDownColor : RegionUpColor; // Ajusta los colores según tu necesidad
			
			if(DrawSignalsPlot)
			{
				if (Low[0] > Average[0]	&& Low[0] < Trigger[0] && High[0] > Trigger[0])
		        {
		         	Values[2][0] = High[0];
					PlotBrushes[2][0] = triggerColor; // Color de plot alcista basado en Trigger
					
					// Eliminar flecha anterior si existe
				    if (!string.IsNullOrEmpty(lastArrowTag))
				    {
				        RemoveDrawObject(lastArrowTag);
				    }
				    
				    // Dibujar flecha alcista 
				    lastArrowTag = "BullishSignal_" + CurrentBar;
				    Draw.ArrowUp(this, lastArrowTag, true, Time[0], Low[0] - TickSize * 2, regionUpColor);
		        }
		        else
		        {
		          	Values[2][0] = double.NaN;
		        }
					
		        if (High[0] < Average[0] && High[0] > Trigger[0] && Low[0] < Trigger[0])
		        {
		           	Values[3][0] = Low[0];
					PlotBrushes[3][0] = averageColor; // Color de plot bajista basado en Average
					
					// Eliminar flecha anterior si existe
				    if (!string.IsNullOrEmpty(lastArrowTag))
				    {
				        RemoveDrawObject(lastArrowTag);
				    }
				    
				    // Dibujar flecha bajista 
				    lastArrowTag = "BearishSignal_" + CurrentBar;
				    Draw.ArrowDown(this, lastArrowTag, true, Time[0], High[0] + TickSize * 2, regionDownColor);
		        }
		        else
		        {
		           	Values[3][0] = double.NaN;
		        }           
			}	       	
			#endregion //Plots alcistas y bajistas (señales)
				
			#region Cruces
	            if (CrossAbove(Trigger, Average, 1))
	            {
	                if (DrawArrows)
	                {
						Values[5][0] = Average[0] - 2;
						PlotBrushes[5][0] = triggerColor;
	                }
					trendPlotValue = 1; // Establecer valor para el plot
					PlotBrushes[4][0] = triggerColor;
	            }
	            else if (CrossBelow(Trigger, Average, 1))
	            {
	                if (DrawArrows)
	                {
						Values[6][0] = Average[0] + 2;
						PlotBrushes[6][0] = averageColor;
	                }
					trendPlotValue = -1; // Establecer valor para el plot
					PlotBrushes[4][0] = averageColor;
	            }
				Values[4][0] = trendPlotValue; // Asignar el valor al plot
				#endregion
					
			#region Color Region
	            if (Trigger[0] > Average[0])
	            {
	                    PlotBrushes[0][-Displacement] = UpTrend[1] ? RegionUpColor : RegionDownColor;
	                    PlotBrushes[1][-Displacement] = UpTrend[1] ? RegionUpColor : RegionDownColor;
	                if (ColorRegion && RegionOpacity != 0)
	                {
	                    if (IsFirstTickOfBar)
	                        PriorIndex = StartIndex;
	                    int CountBars = CurrentBar - PriorIndex + 1 - Displacement;
	                    if (UpTrend[1])
	                    {
	                        if (StartIndex == CurrentBar)
	                            RemoveDrawObject("Region"+CurrentBar);
	                        if (CountBars <= CurrentBar)
	                            Draw.Region(this, "Region"+PriorIndex, CountBars, -Displacement, Trigger, Average, null, RegionUpColor, RegionOpacity);
	                        StartIndex = PriorIndex;
	                    }
	                    else
	                    {
	                        if (CountBars <= CurrentBar && StartIndex == PriorIndex)
	                            Draw.Region(this, "Region"+PriorIndex, CountBars, 1-Displacement, Trigger, Average, null, RegionDownColor, RegionOpacity);
	                        	Draw.Region(this, "Region"+CurrentBar, 1-Displacement, -Displacement, Trigger, Average, null, RegionUpColor, RegionOpacity);
	                       		StartIndex = CurrentBar;
	                    }
	                }
	                UpTrend[0] = true;
	            }
	            else if (Trigger[0] < Average[0])
	            {
	                    PlotBrushes[0][-Displacement] = UpTrend[1] ? RegionUpColor : RegionDownColor;
	                    PlotBrushes[1][-Displacement] = UpTrend[1] ? RegionUpColor : RegionDownColor;
	
	                if (ColorRegion && RegionOpacity != 0)
	                {
	                    if (IsFirstTickOfBar)
	                        PriorIndex = StartIndex;
	                    int CountBars = CurrentBar - PriorIndex + 1 - Displacement;
	                    if (!UpTrend[1])
	                    {
	                        if (StartIndex == CurrentBar)
	                            RemoveDrawObject("Region"+CurrentBar);
	                        if (CountBars <= CurrentBar)
	                            Draw.Region(this, "Region"+PriorIndex, CurrentBar-PriorIndex+1-Displacement, -Displacement, Trigger, Average, null, RegionDownColor, RegionOpacity);
	                        	StartIndex = PriorIndex;
	                    }
	                    else
	                    {
	                        if (CountBars <= CurrentBar && StartIndex == PriorIndex)
	                            Draw.Region(this, "Region"+PriorIndex, CurrentBar-PriorIndex+1-Displacement, 1-Displacement, Trigger, Average, null, RegionUpColor, RegionOpacity);
	                        	Draw.Region(this, "Region"+CurrentBar, 1-Displacement, -Displacement, Trigger, Average, null, RegionDownColor, RegionOpacity);
	                       		StartIndex = CurrentBar;
	                    }
	                }
	                UpTrend[0] = false;
	            }
				#endregion
		}

        /* --------------------------------------------------------------------------------------------------- */
		
		#region EstrategyInfo
		[NinjaScriptProperty]
		[ReadOnly(true)]
		[Display(Name="Indicador", Order=1, GroupName="0. Información Indicador")]
		public string Indicador
		{ get; set; }
		
		[NinjaScriptProperty]
		[ReadOnly(true)]
		[Display(Name="Versión", Order=2, GroupName="0. Información Indicador")]
		public string Version
		{ get; set; }
		#endregion
		
		#region Plots
        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> Trigger
        {
            get { return Values[0]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> Average
        {
            get { return Values[1]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<double> BuySignal
        {
            get { return Values[2]; }
        }
		
		[Browsable(false)]
        [XmlIgnore()]
        public Series<double> SellSignal
        {
            get { return Values[3]; }
        }
		
		[Browsable(false)]
        [XmlIgnore()]
        public Series<double> Trend
        {
            get { return Values[4]; }
        }
		
		[Browsable(false)]
        [XmlIgnore()]
        public Series<double> TriangleUpSignal
        {
            get { return Values[5]; }
        }

        [Browsable(false)]
        [XmlIgnore()]
        public Series<bool> UpTrend
        {
            get { return upTrend; }
        }
		#endregion

		#region Propiedades
        [NinjaScriptProperty]
        [Display(Name = "TriggerMAType", GroupName = "Configuración", Order = 1)]
        public TrendMAType1 TriggerMAType
        {
            get { return triggerMAType; }
            set { triggerMAType = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "TriggerPeriod", GroupName = "Configuración", Order = 2)]
        public int TriggerPeriod
        {
            get { return triggerPeriod; }
            set { triggerPeriod = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "AverageMAType", GroupName = "Configuración", Order = 3)]
        public TrendMAType1 AverageMAType
        {
            get { return averageMAType; }
            set { averageMAType = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "AveragePeriod", GroupName = "Configuración", Order = 4)]
        public int AveragePeriod
        {
            get { return averagePeriod; }
            set { averagePeriod = Math.Max(1, value); }
        }

        [NinjaScriptProperty]
        [Display(Name = "Color Región", GroupName = "Opciones", Order = 7)]
        public bool ColorRegion
        {
            get { return colorRegion; }
            set { colorRegion = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Opacidad Región", GroupName = "Opciones", Order = 8)]
        public int RegionOpacity
        {
            get { return regionOpacity; }
            set { regionOpacity = Math.Min(100, Math.Max(0, value)); }
        }

        [XmlIgnore()]
        [NinjaScriptProperty]
        [Display(Name = "Color Alcista", GroupName = "Opciones", Order = 9)]
        public Brush RegionUpColor
        {
            get { return regionUpColor; }
            set { regionUpColor = value; }
        }

        [Browsable(false)]
        public string RegionUpColorSerialize
        {
            get { return Serialize.BrushToString(regionUpColor); }
            set { regionUpColor = Serialize.StringToBrush(value); }
        }

        [XmlIgnore()]
        [NinjaScriptProperty]
        [Display(Name = "Color Bajista", GroupName = "Opciones", Order = 10)]
        public Brush RegionDownColor
        {
            get { return regionDownColor; }
            set { regionDownColor = value; }
        }

        [Browsable(false)]
        public string RegionDownColorSerialize
        {
            get { return Serialize.BrushToString(regionDownColor); }
            set { regionDownColor = Serialize.StringToBrush(value); }
        }
		
		[NinjaScriptProperty]
        [Display(Name = "Dibujar Cruces", GroupName = "Opciones", Order = 11)]
        public bool DrawArrows
        {
            get { return drawArrows; }
            set { drawArrows = value; }
        }
		
		[NinjaScriptProperty]
        [Display(Name = "Dibujar Puntos de Entrada", GroupName = "Opciones", Order = 12)]
        public bool DrawSignalsPlot
        { get; set; }
        #endregion

    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SmoothTrendStrategy.SmoothTrend[] cacheSmoothTrend;
		public SmoothTrendStrategy.SmoothTrend SmoothTrend(string indicador, string version, TrendMAType1 triggerMAType, int triggerPeriod, TrendMAType1 averageMAType, int averagePeriod, bool colorRegion, int regionOpacity, Brush regionUpColor, Brush regionDownColor, bool drawArrows, bool drawSignalsPlot)
		{
			return SmoothTrend(Input, indicador, version, triggerMAType, triggerPeriod, averageMAType, averagePeriod, colorRegion, regionOpacity, regionUpColor, regionDownColor, drawArrows, drawSignalsPlot);
		}

		public SmoothTrendStrategy.SmoothTrend SmoothTrend(ISeries<double> input, string indicador, string version, TrendMAType1 triggerMAType, int triggerPeriod, TrendMAType1 averageMAType, int averagePeriod, bool colorRegion, int regionOpacity, Brush regionUpColor, Brush regionDownColor, bool drawArrows, bool drawSignalsPlot)
		{
			if (cacheSmoothTrend != null)
				for (int idx = 0; idx < cacheSmoothTrend.Length; idx++)
					if (cacheSmoothTrend[idx] != null && cacheSmoothTrend[idx].Indicador == indicador && cacheSmoothTrend[idx].Version == version && cacheSmoothTrend[idx].TriggerMAType == triggerMAType && cacheSmoothTrend[idx].TriggerPeriod == triggerPeriod && cacheSmoothTrend[idx].AverageMAType == averageMAType && cacheSmoothTrend[idx].AveragePeriod == averagePeriod && cacheSmoothTrend[idx].ColorRegion == colorRegion && cacheSmoothTrend[idx].RegionOpacity == regionOpacity && cacheSmoothTrend[idx].RegionUpColor == regionUpColor && cacheSmoothTrend[idx].RegionDownColor == regionDownColor && cacheSmoothTrend[idx].DrawArrows == drawArrows && cacheSmoothTrend[idx].DrawSignalsPlot == drawSignalsPlot && cacheSmoothTrend[idx].EqualsInput(input))
						return cacheSmoothTrend[idx];
			return CacheIndicator<SmoothTrendStrategy.SmoothTrend>(new SmoothTrendStrategy.SmoothTrend(){ Indicador = indicador, Version = version, TriggerMAType = triggerMAType, TriggerPeriod = triggerPeriod, AverageMAType = averageMAType, AveragePeriod = averagePeriod, ColorRegion = colorRegion, RegionOpacity = regionOpacity, RegionUpColor = regionUpColor, RegionDownColor = regionDownColor, DrawArrows = drawArrows, DrawSignalsPlot = drawSignalsPlot }, input, ref cacheSmoothTrend);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SmoothTrendStrategy.SmoothTrend SmoothTrend(string indicador, string version, TrendMAType1 triggerMAType, int triggerPeriod, TrendMAType1 averageMAType, int averagePeriod, bool colorRegion, int regionOpacity, Brush regionUpColor, Brush regionDownColor, bool drawArrows, bool drawSignalsPlot)
		{
			return indicator.SmoothTrend(Input, indicador, version, triggerMAType, triggerPeriod, averageMAType, averagePeriod, colorRegion, regionOpacity, regionUpColor, regionDownColor, drawArrows, drawSignalsPlot);
		}

		public Indicators.SmoothTrendStrategy.SmoothTrend SmoothTrend(ISeries<double> input , string indicador, string version, TrendMAType1 triggerMAType, int triggerPeriod, TrendMAType1 averageMAType, int averagePeriod, bool colorRegion, int regionOpacity, Brush regionUpColor, Brush regionDownColor, bool drawArrows, bool drawSignalsPlot)
		{
			return indicator.SmoothTrend(input, indicador, version, triggerMAType, triggerPeriod, averageMAType, averagePeriod, colorRegion, regionOpacity, regionUpColor, regionDownColor, drawArrows, drawSignalsPlot);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SmoothTrendStrategy.SmoothTrend SmoothTrend(string indicador, string version, TrendMAType1 triggerMAType, int triggerPeriod, TrendMAType1 averageMAType, int averagePeriod, bool colorRegion, int regionOpacity, Brush regionUpColor, Brush regionDownColor, bool drawArrows, bool drawSignalsPlot)
		{
			return indicator.SmoothTrend(Input, indicador, version, triggerMAType, triggerPeriod, averageMAType, averagePeriod, colorRegion, regionOpacity, regionUpColor, regionDownColor, drawArrows, drawSignalsPlot);
		}

		public Indicators.SmoothTrendStrategy.SmoothTrend SmoothTrend(ISeries<double> input , string indicador, string version, TrendMAType1 triggerMAType, int triggerPeriod, TrendMAType1 averageMAType, int averagePeriod, bool colorRegion, int regionOpacity, Brush regionUpColor, Brush regionDownColor, bool drawArrows, bool drawSignalsPlot)
		{
			return indicator.SmoothTrend(input, indicador, version, triggerMAType, triggerPeriod, averageMAType, averagePeriod, colorRegion, regionOpacity, regionUpColor, regionDownColor, drawArrows, drawSignalsPlot);
		}
	}
}

#endregion
