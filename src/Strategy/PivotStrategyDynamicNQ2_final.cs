using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class PivotStrategyDynamicNQ2 : Strategy
    {
        private double entryPrice = 0;
        private bool movedToBreakeven = false;
        private double highestSinceEntry = 0;
        private double lowestSinceEntry = double.MaxValue;

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Contracts", Order = 1, GroupName = "Parameters")]
        public int Contracts { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Initial Stop (Ticks)", Order = 2, GroupName = "Parameters")]
        public int InitialStopTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "BreakEven Ticks", Order = 3, GroupName = "Parameters")]
        public int BreakEvenTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trailing Distance (Ticks)", Order = 4, GroupName = "Parameters")]
        public int TrailingDistanceTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Profit Target (Ticks)", Order = 5, GroupName = "Parameters")]
        public int ProfitTargetTicks { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "PivotStrategyDynamicNQ2";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                BarsRequiredToTrade = 3;
                Contracts = 1;
                InitialStopTicks = 10;
                BreakEvenTicks = 20;
                TrailingDistanceTicks = 40;
                ProfitTargetTicks = 120;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade)
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
                    SetStopLoss("LongEntry", CalculationMode.Ticks, InitialStopTicks, false);
                    SetProfitTarget("LongEntry", CalculationMode.Ticks, ProfitTargetTicks);
                }
                else if (sellSignal)
                {
                    entryPrice = Close[0];
                    lowestSinceEntry = entryPrice;

                    EnterShort(Contracts, "ShortEntry");
                    SetStopLoss("ShortEntry", CalculationMode.Ticks, InitialStopTicks, false);
                    SetProfitTarget("ShortEntry", CalculationMode.Ticks, ProfitTargetTicks);
                }
            }

            if (Position.MarketPosition == MarketPosition.Long)
            {
                if (Close[0] > highestSinceEntry)
                    highestSinceEntry = Close[0];

                if (!movedToBreakeven && Close[0] >= entryPrice + (BreakEvenTicks * TickSize))
                {
                    SetStopLoss("LongEntry", CalculationMode.Price, entryPrice, false);
                    movedToBreakeven = true;
                }

                if (movedToBreakeven)
                {
                    double trailStop = highestSinceEntry - (TrailingDistanceTicks * TickSize);
                    if (trailStop > entryPrice)
                        SetStopLoss("LongEntry", CalculationMode.Price, trailStop, false);
                }
            }

            if (Position.MarketPosition == MarketPosition.Short)
            {
                if (Close[0] < lowestSinceEntry)
                    lowestSinceEntry = Close[0];

                if (!movedToBreakeven && Close[0] <= entryPrice - (BreakEvenTicks * TickSize))
                {
                    SetStopLoss("ShortEntry", CalculationMode.Price, entryPrice, false);
                    movedToBreakeven = true;
                }

                if (movedToBreakeven)
                {
                    double trailStop = lowestSinceEntry + (TrailingDistanceTicks * TickSize);
                    if (trailStop < entryPrice)
                        SetStopLoss("ShortEntry", CalculationMode.Price, trailStop, false);
                }
            }
        }
    }
}
