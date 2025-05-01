// PivotStrategyDynamicNQ2 - Estratégia com Alvo Condicional, Stop e Trailing Dinâmico Corrigido
// Desenvolvido por Tom - O Cara da NinjaTrader 🚀

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class PivotStrategyDynamicNQ2 : Strategy
    {
        private PivotSignalSimpleDynamic pivotSignal;
        private int contracts = 1;
        private int breakEvenTicks = 20; // 5 pontos
        private int trailingDistanceTicks = 40; // 10 pontos
        private int initialStopTicks = 10;
        private int profitTargetTicks = 120; // 30 pontos
        private double entryPrice = 0;
        private bool movedToBreakeven = false;
        private double highestSinceEntry = 0;
        private double lowestSinceEntry = double.MaxValue;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Contracts", Order = 1, GroupName = "Parameters")]
        public int Contracts
        {
            get { return contracts; }
            set { contracts = value; }
        }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Initial Stop (Ticks)", Order = 2, GroupName = "Parameters")]
        public int InitialStopTicks
        {
            get { return initialStopTicks; }
            set { initialStopTicks = value; }
        }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Estratégia automática baseada no PivotSignalSimpleDynamic com trailing stop e alvo condicional.";
                Name = "PivotStrategyDynamicNQ2";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsInstantiatedOnEachOptimizationIteration = false;
                Contracts = 1;
                InitialStopTicks = 10;
                BarsRequiredToTrade = 3;
            }
            else if (State == State.Configure)
            {
                pivotSignal = PivotSignalSimpleDynamic();
                AddChartIndicator(pivotSignal);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 3)
                return;

            bool buySignal = Low[2] > Low[1] && Low[0] > Low[1];
            bool sellSignal = High[2] < High[1] && High[0] < High[1];

            if (Position.MarketPosition == MarketPosition.Flat)
            {
                movedToBreakeven = false;

                if (buySignal)
                {
                    entryPrice = Close[0];
                    highestSinceEntry = entryPrice;
                    EnterLong(Contracts, "LongEntry");
                    SetStopLoss("LongEntry", CalculationMode.Price, entryPrice - (InitialStopTicks * TickSize), false);
                }
                else if (sellSignal)
                {
                    entryPrice = Close[0];
                    lowestSinceEntry = entryPrice;
                    EnterShort(Contracts, "ShortEntry");
                    SetStopLoss("ShortEntry", CalculationMode.Price, entryPrice + (InitialStopTicks * TickSize), false);
                }
            }

            // Long
            if (Position.MarketPosition == MarketPosition.Long)
            {
                if (Close[0] > highestSinceEntry)
                    highestSinceEntry = Close[0];

                if (!movedToBreakeven && Close[0] >= entryPrice + (breakEvenTicks * TickSize))
                {
                    SetStopLoss("LongEntry", CalculationMode.Price, entryPrice, false);
                    movedToBreakeven = true;
                }

                if (movedToBreakeven)
                {
                    double trailStop = highestSinceEntry - (trailingDistanceTicks * TickSize);
                    if (trailStop > entryPrice)
                        SetStopLoss("LongEntry", CalculationMode.Price, trailStop, false);
                }

                bool oppositeSignal = High[2] < High[1] && High[0] < High[1];
                bool reachedTarget = Close[0] >= entryPrice + (profitTargetTicks * TickSize);

                if (oppositeSignal || reachedTarget)
                {
                    ExitLong("TargetExit", "LongEntry");
                }
            }

            // Short
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                if (Close[0] < lowestSinceEntry)
                    lowestSinceEntry = Close[0];

                if (!movedToBreakeven && Close[0] <= entryPrice - (breakEvenTicks * TickSize))
                {
                    SetStopLoss("ShortEntry", CalculationMode.Price, entryPrice, false);
                    movedToBreakeven = true;
                }

                if (movedToBreakeven)
                {
                    double trailStop = lowestSinceEntry + (trailingDistanceTicks * TickSize);
                    if (trailStop < entryPrice)
                        SetStopLoss("ShortEntry", CalculationMode.Price, trailStop, false);
                }

                bool oppositeSignal = Low[2] > Low[1] && Low[0] > Low[1];
                bool reachedTarget = Close[0] <= entryPrice - (profitTargetTicks * TickSize);

                if (oppositeSignal || reachedTarget)
                {
                    ExitShort("TargetExit", "ShortEntry");
                }
            }
        }
    }
}
