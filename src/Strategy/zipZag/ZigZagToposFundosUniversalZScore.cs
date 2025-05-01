#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class ZigZagToposFundosUniversal_ZScore : Strategy
    {
        private ZigZag zigZag;
        private ZScorePrecoGauss zScoreInd;

        // Gestão
        private double entryPrice = 0;
        private double stopPrice = 0;
        private double trailAnchor = 0;
        private bool trailEnabled = false;
        private int lastOrderQty = 0;

        // Sinal
        private int lastSwingIdx = -1;
        private bool isLastSwingHigh = false;
        private bool isLastSwingLow = false;

        // Trava de reentrada
        private int barsSinceExit = 9999;
        private int barsAfterStop = 9999;
        private bool stopTaken = false;
        private MarketPosition lastPosition = MarketPosition.Flat;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "ZigZagToposFundosUniversal_ZScore";
                Description = @"Entradas por pivô e ZScore. Gestão idêntica à RenkoStrategy: trail só após X pontos/ticks e nunca retrocede. Trava de 5 barras para reentrada.";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = false;
                ExitOnSessionCloseSeconds = 30;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;

                ZigZagDeviation = 3;
                StopLossValue = 80;
                TrailDistanceTicks = 40;
                TrailUnlockTicks = 40;
                AllowLong = true;
                AllowShort = true;

                ZScorePeriodo = 34;
                ZScoreAlerta = false;
                ZScoreCompra = 0;
                ZScoreVenda = 0;
            }
            else if (State == State.DataLoaded)
            {
                zigZag = ZigZag(Close, DeviationType.Points, ZigZagDeviation, false);
                AddChartIndicator(zigZag);

                zScoreInd = ZScorePrecoGauss(Close, ZScorePeriodo, ZScoreAlerta);
                AddChartIndicator(zScoreInd);
            }
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.OrderType == OrderType.StopMarket || execution.Order.OrderType == OrderType.StopLimit)
            {
                stopTaken = true;
                Print("Stop Loss executado - Próxima entrada só após 4 barras");
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Math.Max(BarsRequiredToTrade, ZScorePeriodo))
                return;

            // Controle de barras após saída
            if (lastPosition != Position.MarketPosition)
            {
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    barsSinceExit = 0;
                    if (stopTaken)
                    {
                        barsAfterStop = 0;
                        Print("Stop Loss detectado - Aguardando 4 barras para nova entrada");
                    }
                }
                lastPosition = Position.MarketPosition;
            }
            if (barsSinceExit < 9999) barsSinceExit++;
            if (barsAfterStop < 9999) barsAfterStop++;

            double zz = zigZag[0];
            double zScore = zScoreInd[0];

            // Debug principal
            Print("Bar: " + CurrentBar
                + " | zz: " + zigZag[0]
                + " | High: " + High[0]
                + " | Low: " + Low[0]
                + " | lastSwingIdx: " + lastSwingIdx
                + " | isLastSwingLow: " + isLastSwingLow
                + " | isLastSwingHigh: " + isLastSwingHigh
                + " | ZScore: " + zScoreInd[0]
                + " | Pos: " + Position.MarketPosition
                + " | barsSinceExit: " + barsSinceExit);

            // Detecta pivôs
            if (!double.IsNaN(zz))
            {
                if (zz == High[0])
                {
                    lastSwingIdx = CurrentBar;
                    isLastSwingHigh = true;
                    isLastSwingLow = false;
                    Print("---- Pivô de topo detectado no bar " + CurrentBar + " ----");
                }
                else if (zz == Low[0])
                {
                    lastSwingIdx = CurrentBar;
                    isLastSwingLow = true;
                    isLastSwingHigh = false;
                    Print("---- Pivô de fundo detectado no bar " + CurrentBar + " ----");
                }
            }

            // Reset se flat
            if (Position.MarketPosition == MarketPosition.Flat)
            {
                entryPrice = 0;
                stopPrice = 0;
                trailAnchor = 0;
                trailEnabled = false;
                lastOrderQty = 0;
            }

            // ENTRADA só após 5 barras flat e 4 barras após stop
            if (Position.MarketPosition == MarketPosition.Flat && barsSinceExit > 5 && (!stopTaken || barsAfterStop > 4))
            {
                if (isLastSwingLow && AllowLong && zScore < ZScoreCompra)
                {
                    Print(">>> Tentando Entrar LONG no bar " + CurrentBar + " | ZScore: " + zScore);
                    EnterLong();
                }
                if (isLastSwingHigh && AllowShort && zScore > ZScoreVenda)
                {
                    Print(">>> Tentando Entrar SHORT no bar " + CurrentBar + " | ZScore: " + zScore);
                    EnterShort();
                }
                return;
            }

            // --- LONG ---
            if (Position.MarketPosition == MarketPosition.Long && Position.Quantity > 0)
            {
                if (entryPrice == 0 || lastOrderQty != Position.Quantity)
                {
                    entryPrice = Position.AveragePrice;
                    double novoStop = entryPrice - StopLossValue * TickSize;
                    if (novoStop < Close[0] - 0.01)
                    {
                        SetStopLoss(CalculationMode.Price, novoStop);
                        stopPrice = novoStop;
                        stopTaken = false;
                    }
                    else
                    {
                        Print("STOP LONG BLOQUEADO! novoStop=" + novoStop + " | Close=" + Close[0]);
                    }
                    lastOrderQty = Position.Quantity;
                    trailEnabled = false;
                    trailAnchor = entryPrice;
                }
                if (!trailEnabled && Close[0] >= entryPrice + TrailUnlockTicks * TickSize)
                {
                    trailEnabled = true;
                    trailAnchor = High[0];
                }
                if (trailEnabled && High[0] > trailAnchor)
                    trailAnchor = High[0];
                if (trailEnabled)
                {
                    double newStop = trailAnchor - TrailDistanceTicks * TickSize;
                    if (newStop > stopPrice && newStop > entryPrice && newStop < Close[0] - 0.01)
                    {
                        SetStopLoss(CalculationMode.Price, newStop);
                        stopPrice = newStop;
                    }
                    else
                    {
                        Print("TRAIL LONG BLOQUEADO! newStop=" + newStop + " | Close=" + Close[0]);
                    }
                }
            }

            // --- SHORT ---
            if (Position.MarketPosition == MarketPosition.Short && Position.Quantity > 0)
            {
                if (entryPrice == 0 || lastOrderQty != Position.Quantity)
                {
                    entryPrice = Position.AveragePrice;
                    double novoStop = entryPrice + StopLossValue * TickSize;
                    if (novoStop > Close[0] + 0.01)
                    {
                        SetStopLoss(CalculationMode.Price, novoStop);
                        stopPrice = novoStop;
                        stopTaken = false;
                    }
                    else
                    {
                        Print("STOP SHORT BLOQUEADO! novoStop=" + novoStop + " | Close=" + Close[0]);
                    }
                    lastOrderQty = Position.Quantity;
                    trailEnabled = false;
                    trailAnchor = entryPrice;
                }
                if (!trailEnabled && Close[0] <= entryPrice - TrailUnlockTicks * TickSize)
                {
                    trailEnabled = true;
                    trailAnchor = Low[0];
                }
                if (trailEnabled && Low[0] < trailAnchor)
                    trailAnchor = Low[0];
                if (trailEnabled)
                {
                    double newStop = trailAnchor + TrailDistanceTicks * TickSize;
                    if (newStop < stopPrice && newStop < entryPrice && newStop > Close[0] + 0.01)
                    {
                        SetStopLoss(CalculationMode.Price, newStop);
                        stopPrice = newStop;
                    }
                    else
                    {
                        Print("TRAIL SHORT BLOQUEADO! newStop=" + newStop + " | Close=" + Close[0]);
                    }
                }
            }
        }

        #region Properties

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "ZigZag Desvio (pontos)", Order = 1, GroupName = "ZigZag")]
        public double ZigZagDeviation { get; set; }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name = "Stop Loss Value (ticks)", Order = 2, GroupName = "Gestão")]
        public double StopLossValue { get; set; }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name = "Trail: distância (ticks)", Order = 3, GroupName = "Trail")]
        public int TrailDistanceTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, 1000)]
        [Display(Name = "Trail: ticks para ativar", Order = 4, GroupName = "Trail")]
        public int TrailUnlockTicks { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Allow Long Trades", Order = 5, GroupName = "Gestão")]
        public bool AllowLong { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Allow Short Trades", Order = 6, GroupName = "Gestão")]
        public bool AllowShort { get; set; }

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Período ZScore", Order = 7, GroupName = "ZScore")]
        public int ZScorePeriodo { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Alerta ZScore ±2", Order = 8, GroupName = "ZScore")]
        public bool ZScoreAlerta { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ZScore para Compra (<)", Order = 9, GroupName = "ZScore")]
        public double ZScoreCompra { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "ZScore para Venda (>)", Order = 10, GroupName = "ZScore")]
        public double ZScoreVenda { get; set; }

        #endregion
    }
}
