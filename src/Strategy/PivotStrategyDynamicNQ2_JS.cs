using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class PivotStrategyDynamicNQ2_JS : Strategy
    {
        private double entryPrice = 0;
        private bool movedToBreakeven = false;
        private double highestSinceEntry = 0;
        private double lowestSinceEntry = double.MaxValue;
        private int consecutiveLosses = 0;
        private DateTime lastTradeTime = DateTime.MinValue;
        private PivotSignalSimpleDynamic pivotSignal;
        private bool buySignal = false;
        private bool sellSignal = false;

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

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Min Minutes Between Trades", Order = 6, GroupName = "Parameters")]
        public int MinMinutesBetweenTrades { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Max Consecutive Losses", Order = 7, GroupName = "Parameters")]
        public int MaxConsecutiveLosses { get; set; }

        [NinjaScriptProperty]
        [Range(0, 100)]
        [Display(Name = "Volume Filter Percentile", Order = 8, GroupName = "Parameters")]
        public int VolumeFilterPercentile { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "PivotStrategyDynamicNQ2_JS";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                BarsRequiredToTrade = 20; // Aumentado para ter mais dados históricos
                
                // Parâmetros padrão
                Contracts = 1;
                InitialStopTicks = 10;
                BreakEvenTicks = 20;
                TrailingDistanceTicks = 40;
                ProfitTargetTicks = 120;
                MinMinutesBetweenTrades = 5;
                MaxConsecutiveLosses = 3;
                VolumeFilterPercentile = 50;
            }
            else if (State == State.Configure)
            {
                pivotSignal = PivotSignalSimpleDynamic(Close);
                AddChartIndicator(pivotSignal);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade)
                return;

            // Verifica se estamos em horário de operação (09:30 - 16:30 ET)
            if (Time[0].Hour < 9 || (Time[0].Hour == 9 && Time[0].Minute < 30) ||
                Time[0].Hour > 16 || (Time[0].Hour == 16 && Time[0].Minute > 30))
                return;

            // Verifica volume mínimo
            if (!IsVolumeValid())
                return;

            // Verifica tempo mínimo entre operações
            if ((Time[0] - lastTradeTime).TotalMinutes < MinMinutesBetweenTrades)
                return;

            // Verifica máximo de perdas consecutivas
            if (consecutiveLosses >= MaxConsecutiveLosses)
                return;

            // Atualiza os sinais do indicador
            if (Low[2] > Low[1] && Low[0] > Low[1])
            {
                buySignal = true;
                sellSignal = false;
            }
            else if (High[2] < High[1] && High[0] < High[1])
            {
                sellSignal = true;
                buySignal = false;
            }
            else
            {
                buySignal = false;
                sellSignal = false;
            }

            if (Position.MarketPosition == MarketPosition.Flat)
            {
                movedToBreakeven = false;

                if (buySignal && IsValidBuySetup())
                {
                    entryPrice = Close[0];
                    highestSinceEntry = entryPrice;
                    lastTradeTime = Time[0];

                    EnterLong(Contracts, "LongEntry");
                    SetStopLoss("LongEntry", CalculationMode.Ticks, InitialStopTicks, false);
                    SetProfitTarget("LongEntry", CalculationMode.Ticks, ProfitTargetTicks);
                }
                else if (sellSignal && IsValidSellSetup())
                {
                    entryPrice = Close[0];
                    lowestSinceEntry = entryPrice;
                    lastTradeTime = Time[0];

                    EnterShort(Contracts, "ShortEntry");
                    SetStopLoss("ShortEntry", CalculationMode.Ticks, InitialStopTicks, false);
                    SetProfitTarget("ShortEntry", CalculationMode.Ticks, ProfitTargetTicks);
                }
            }
            else if (Position.MarketPosition == MarketPosition.Long)
            {
                ManageLongPosition();
            }
            else if (Position.MarketPosition == MarketPosition.Short)
            {
                ManageShortPosition();
            }
        }

        private bool IsVolumeValid()
        {
            if (CurrentBar < 20) return false;

            double[] volumes = new double[20];
            for (int i = 0; i < 20; i++)
                volumes[i] = Volume[i];

            Array.Sort(volumes);
            int percentileIndex = (int)Math.Ceiling(19 * (VolumeFilterPercentile / 100.0));
            double volumeThreshold = volumes[percentileIndex];

            return Volume[0] >= volumeThreshold;
        }

        private bool IsValidBuySetup()
        {
            // Confirmação de tendência usando médias móveis
            double sma20 = SMA(20)[0];
            double sma50 = SMA(50)[0];

            // Verifica se o preço está acima das médias móveis
            return Close[0] > sma20 && sma20 > sma50;
        }

        private bool IsValidSellSetup()
        {
            // Confirmação de tendência usando médias móveis
            double sma20 = SMA(20)[0];
            double sma50 = SMA(50)[0];

            // Verifica se o preço está abaixo das médias móveis
            return Close[0] < sma20 && sma20 < sma50;
        }

        private void ManageLongPosition()
        {
            if (Close[0] > highestSinceEntry)
                highestSinceEntry = Close[0];

            // Move para breakeven
            if (!movedToBreakeven && Close[0] >= entryPrice + (BreakEvenTicks * TickSize))
            {
                SetStopLoss("LongEntry", CalculationMode.Price, entryPrice + (1 * TickSize), false);
                movedToBreakeven = true;
            }

            // Trailing stop
            if (movedToBreakeven)
            {
                double trailStop = highestSinceEntry - (TrailingDistanceTicks * TickSize);
                if (trailStop > Position.AveragePrice)
                    SetStopLoss("LongEntry", CalculationMode.Price, trailStop, false);
            }

            // Saída por reversão
            if (sellSignal && Close[0] < Open[0])
            {
                ExitLong();
            }
        }

        private void ManageShortPosition()
        {
            if (Close[0] < lowestSinceEntry)
                lowestSinceEntry = Close[0];

            // Move para breakeven
            if (!movedToBreakeven && Close[0] <= entryPrice - (BreakEvenTicks * TickSize))
            {
                SetStopLoss("ShortEntry", CalculationMode.Price, entryPrice - (1 * TickSize), false);
                movedToBreakeven = true;
            }

            // Trailing stop
            if (movedToBreakeven)
            {
                double trailStop = lowestSinceEntry + (TrailingDistanceTicks * TickSize);
                if (trailStop < Position.AveragePrice)
                    SetStopLoss("ShortEntry", CalculationMode.Price, trailStop, false);
            }

            // Saída por reversão
            if (buySignal && Close[0] > Open[0])
            {
                ExitShort();
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.OrderState == OrderState.Filled)
            {
                if (execution.Order.OrderAction == OrderAction.BuyToCover || execution.Order.OrderAction == OrderAction.Sell)
                {
                    if (Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]) < 0)
                        consecutiveLosses++;
                    else
                        consecutiveLosses = 0;
                }
            }
        }
    }
}
