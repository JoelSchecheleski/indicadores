#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class ZigZagToposFundosUniversal_ZScore : Strategy
    {
        private ZigZag           zigZag;
        private ZScorePrecoGauss zScoreInd;

        private int  lastSwingIdx     = -1;
        private bool isLastSwingHigh  = false;
        private bool isLastSwingLow   = false;
        private bool printedType      = false;
        private int  lastEntryBar     = -999;

        private double trailStopPrice = 0;
        private bool   trailActive    = false;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "ZigZag + ZScore com trail manual interno";
                Name = "ZigZagToposFundosUniversal_ZScore_Manual";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = false;
                ExitOnSessionCloseSeconds = 30;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                BarsRequiredToTrade = 20;

                ZigZagDeviation = 2;
                StopLossTicks = 20;
                TrailUnlockTicks = 15;
                TrailDistanceTicks = 20;

                AllowLong = true;
                AllowShort = true;

                ZScorePeriodo = 34;
                ZScoreAlerta = true;
                ZScoreCompra = -1.5;
                ZScoreVenda = 1.5;
            }
            else if (State == State.DataLoaded)
            {
                zigZag = ZigZag(Close, DeviationType.Points, ZigZagDeviation, false);
                zScoreInd = ZScorePrecoGauss(Close, ZScorePeriodo, ZScoreAlerta);

                AddChartIndicator(zigZag);
                AddChartIndicator(zScoreInd);
            }
        }

        protected override void OnBarUpdate()
        {
            if (!printedType)
            {
                printedType = true;
                Print("Tipo gráfico: " + BarsPeriod.BarsPeriodType + " | ZigZagDeviation: " + ZigZagDeviation);
            }

            if (CurrentBar < Math.Max(BarsRequiredToTrade, ZScorePeriodo))
                return;

            double zzValue = zigZag[0];
            double zScore  = zScoreInd[0];

            if (!double.IsNaN(zzValue))
            {
                if (zzValue == High[0])
                {
                    lastSwingIdx     = CurrentBar;
                    isLastSwingHigh  = true;
                    isLastSwingLow   = false;
                }
                else if (zzValue == Low[0])
                {
                    lastSwingIdx    = CurrentBar;
                    isLastSwingLow  = true;
                    isLastSwingHigh = false;
                }
            }

            bool canEnterAgain = (CurrentBar - lastEntryBar) > 2;

            // ENTRADAS
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                trailActive = false;
                trailStopPrice = 0;

                if (isLastSwingLow && AllowLong && zScore < ZScoreCompra && canEnterAgain)
                {
                    EnterLong("Long");
                    lastEntryBar = CurrentBar;
                    isLastSwingLow = false;
                    Print(">>> COMPRA em " + Time[0] + " | ZScore: " + zScore.ToString("F2"));
                }

                if (isLastSwingHigh && AllowShort && zScore > ZScoreVenda && canEnterAgain)
                {
                    EnterShort("Short");
                    lastEntryBar = CurrentBar;
                    isLastSwingHigh = false;
                    Print(">>> VENDA em " + Time[0] + " | ZScore: " + zScore.ToString("F2"));
                }
            }

            // GESTÃO LONG
            if (Position.MarketPosition == MarketPosition.Long)
            {
                double stopInicial = Position.AveragePrice - StopLossTicks * TickSize;

                if (!trailActive && Close[0] >= Position.AveragePrice + TrailUnlockTicks * TickSize)
                {
                    trailActive = true;
                    trailStopPrice = Close[0] - TrailDistanceTicks * TickSize;
                }

                if (trailActive)
                {
                    double novoTrail = Close[0] - TrailDistanceTicks * TickSize;
                    if (novoTrail > trailStopPrice)
                        trailStopPrice = novoTrail;
                }

                double stop = trailActive ? trailStopPrice : stopInicial;

                if (Close[0] <= stop)
                {
                    ExitLong("StopManualLong", "Long");
                    Print("<<< STOP LONG em " + Time[0] + " | Preço: " + Close[0]);
                    trailActive = false;
                }
            }

            // GESTÃO SHORT
            if (Position.MarketPosition == MarketPosition.Short)
            {
                double stopInicial = Position.AveragePrice + StopLossTicks * TickSize;

                if (!trailActive && Close[0] <= Position.AveragePrice - TrailUnlockTicks * TickSize)
                {
                    trailActive = true;
                    trailStopPrice = Close[0] + TrailDistanceTicks * TickSize;
                }

                if (trailActive)
                {
                    double novoTrail = Close[0] + TrailDistanceTicks * TickSize;
                    if (novoTrail < trailStopPrice)
                        trailStopPrice = novoTrail;
                }

                double stop = trailActive ? trailStopPrice : stopInicial;

                if (Close[0] >= stop)
                {
                    ExitShort("StopManualShort", "Short");
                    Print("<<< STOP SHORT em " + Time[0] + " | Preço: " + Close[0]);
                    trailActive = false;
                }
            }
        }

        #region Properties

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ZigZag Desvio (pontos)", Order = 1)]
        public double ZigZagDeviation { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Stop (ticks)", Order = 2)]
        public int StopLossTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Trail ativa após (ticks)", Order = 3)]
        public int TrailUnlockTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 100)]
        [Display(Name = "Distância do Trail (ticks)", Order = 4)]
        public int TrailDistanceTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Permitir Long", Order = 5)]
        public bool AllowLong { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Permitir Short", Order = 6)]
        public bool AllowShort { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Período ZScore", Order = 7)]
        public int ZScorePeriodo { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Alerta ZScore ±2", Order = 8)]
        public bool ZScoreAlerta { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ZScore para Compra (<)", Order = 9)]
        public double ZScoreCompra { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ZScore para Venda (>)", Order = 10)]
        public double ZScoreVenda { get; set; }

        #endregion
    }
}
