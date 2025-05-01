using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using System;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class SP500BigPlayerStrategy : Strategy
    {
        private SP500BigPlayerSignal signalIndicator;
        private ATR atr;
        private double initialStop;
        private bool isBreakeven;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Automated strategy based on SP500BigPlayerSignal indicator.";
                Name = "SP500BigPlayerStrategy";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionClose = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumChase = 0;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                BarsRequiredToTrade = 20;
                StopLossDistance = 20; // Ticks
                RiskPercentage = 1.0; // 1% of account equity
                AtrPeriod = 14;
                TrailingStopTrigger = 30; // Ticks
            }
            else if (State == State.Configure)
            {
                signalIndicator = SP500BigPlayerSignal();
                atr = ATR(AtrPeriod);
                SetTrailStop(CalculationMode.Ticks, StopLossDistance);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade) return;

            // Calculate position size based on risk percentage
            double accountEquity = Account.GetAccountValue(AccountItem.Equity, Currency.UsDollar);
            double riskAmount = accountEquity * (RiskPercentage / 100);
            double atrValue = atr[0];
            int positionSize = (int)Math.Floor(riskAmount / (atrValue * Instrument.MasterInstrument.PointValue));

            // Entry Logic
            if (signalIndicator.BuySignal[0] == 1 && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterLong(positionSize, "BuySignal");
                initialStop = Low[Math.Min(LowestBar(Low, 10), CurrentBar)] - StopLossDistance * TickSize;
                isBreakeven = false;
            }
            else if (signalIndicator.SellSignal[0] == 1 && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterShort(positionSize, "SellSignal");
                initialStop = High[Math.Min(HighestBar(High, 10), CurrentBar)] + StopLossDistance * TickSize;
                isBreakeven = false;
            }

            // Stop-Loss and Breakeven Logic
            if (Position.MarketPosition == MarketPosition.Long)
            {
                double currentPrice = Close[0];
                double entryPrice = Position.AveragePrice;
                if (!isBreakeven && currentPrice >= entryPrice + TrailingStopTrigger * TickSize)
                {
                    SetStopLoss("BuySignal", CalculationMode.Price, entryPrice, false);
                    isBreakeven = true;
                }
                else if (!isBreakeven)
                {
                    SetStopLoss("BuySignal", CalculationMode.Price, initialStop, false);
                }
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                double currentPrice = Close[0];
                double entryPrice = Position.AveragePrice;
                if (!isBreakeven && currentPrice <= entryPrice - TrailingStopTrigger * TickSize)
                {
                    SetStopLoss("SellSignal", CalculationMode.Price, entryPrice, false);
                    isBreakeven = true;
                }
                else if (!isBreakeven)
                {
                    SetStopLoss("SellSignal", CalculationMode.Price, initialStop, false);
                }
            }

            // Exit on Opposite Signal
            if (Position.MarketPosition == MarketPosition.Long && signalIndicator.SellSignal[0] == 1)
            {
                ExitLong("ExitLong", "BuySignal");
            }
            else if (Position.MarketPosition == MarketPosition.Short && signalIndicator.BuySignal[0] == 1)
            {
                ExitShort("ExitShort", "SellSignal");
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss Distance (Ticks)", Order = 1, GroupName = "Parameters")]
        public int StopLossDistance { get; set; }

        [NinjaScriptProperty]
        [Range(0, double.MaxValue)]
        [Display(Name = "Risk Percentage", Order = 2, GroupName = "Parameters")]
        public double RiskPercentage { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ATR Period", Order = 3, GroupName = "Parameters")]
        public int AtrPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Trailing Stop Trigger (Ticks)", Order = 4, GroupName = "Parameters")]
        public int TrailingStopTrigger { get; set; }
        #endregion
    }
}