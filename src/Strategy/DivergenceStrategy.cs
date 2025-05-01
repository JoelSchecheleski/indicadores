#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class DivergenceStrategy : Strategy
    {
        private DivergenceScanner divergenceScanner;
        private ATR atr;
        private VWAP vwap;
        
        private double initialBalance;
        private double currentDrawdown;
        private int consecutiveLosses;
        private DateTime lastTradeTime;
        private bool inTradeWindow;
        private double lastVolatility;
        private int winningTrades;
        private int losingTrades;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Estratégia baseada em divergências";
                Name = "DivergenceStrategy";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;

                // Plots
                AddPlot(new Stroke(Brushes.Green, 2), PlotStyle.Dot, "CompraDiv");
                AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Dot, "VendaDiv");
                BarsRequiredToTrade = 20;

                // Parâmetros do RSI
                RsiPeriod = 14;
                RsiSmoothing = 3;

                // Parâmetros de Risco
                RiskPerTrade = 0.5;          // Risco por trade em %
                MaxDailyLoss = 2.0;          // Máxima perda diária em %
                UseAdaptiveRisk = true;      // Ajusta risco baseado no drawdown
                
                // Parâmetros de Entrada/Saída
                ProfitTargetATR = 2.0;       // Alvo em múltiplos de ATR
                StopLossATR = 1.0;           // Stop em múltiplos de ATR
                UseTrailingStop = true;      // Usar stop móvel
                TrailingStopATR = 0.5;       // Distância do stop móvel em ATR
                MinimumVolume = 1000;        // Volume mínimo
                
                // Filtros de Tempo
                UseTimeFilter = false;         // Desabilitado para operar 24h
                
                // Filtros de Divergência
                RequireVwapConfirmation = true;  // Requer confirmação da VWAP
                MinimumRsiValue = 20;         // RSI mínimo para entradas
                MaximumRsiValue = 80;         // RSI máximo para entradas
                WaitBarsAfterSignal = 2;      // Barras de espera após sinal
            }
            else if (State == State.Configure)
            {
                // Configura indicadores
                divergenceScanner = new DivergenceScanner();
                AddChartIndicator(divergenceScanner);
                atr = ATR(14);
                vwap = VWAP();
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (CurrentBar < BarsRequiredToTrade) return;

                // Verifica se está em posição
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    double rsiValue = divergenceScanner.RSI(Close, RsiPeriod, RsiSmoothing)[0];

                    // Sinal de compra - Divergência de alta
                    if (divergenceScanner.Values[0][0] > 0 && rsiValue > MinimumRsiValue)
                    {
                        Values[0][0] = High[0];  // Plot sinal de compra
                        Print($"Sinal de COMPRA - RSI: {rsiValue}");
                        EnterLong(DefaultQuantity, "BullishDiv");
                    }
                    // Sinal de venda - Divergência de baixa
                    else if (divergenceScanner.Values[1][0] > 0 && rsiValue < MaximumRsiValue)
                    {
                        Values[1][0] = Low[0];  // Plot sinal de venda
                        Print($"Sinal de VENDA - RSI: {rsiValue}");
                        EnterShort(DefaultQuantity, "BearishDiv");
                    }
                }
            }
            catch (Exception ex)
            {
                Print("Erro em OnBarUpdate: " + ex.Message);
            }
        }

        // Métodos auxiliares
        private double GetCurrentProfit()
        {
            double profit = 0;
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                profit = Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]);
            }
            return SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit + profit;
        }

        private double GetRsiValue()
        {
            if (divergenceScanner == null) return 50;
            return divergenceScanner.Values[0][0];
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.Order.OrderState == OrderState.Filled)
            {
                if (execution.Order.Name.Contains("StopLoss") || execution.Order.Name.Contains("Erro"))
                {
                    consecutiveLosses++;
                    losingTrades++;
                    lastTradeTime = Time[0];
                }
                else if (execution.Order.Name.Contains("ProfitTarget"))
                {
                    consecutiveLosses = 0;
                    winningTrades++;
                    lastTradeTime = Time[0];
                }
            }
        }

        #region Properties
        [Range(0.1, 100.0), NinjaScriptProperty]
        [Display(Name = "Risco por Trade %", Description = "Risco por operação em porcentagem", Order = 1, GroupName = "Risco")]
        public double RiskPerTrade { get; set; }

        [Range(0.1, 100.0), NinjaScriptProperty]
        [Display(Name = "Máxima Perda Diária %", Description = "Limite de perda diária em porcentagem", Order = 2, GroupName = "Risco")]
        public double MaxDailyLoss { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Risco Adaptativo", Description = "Reduz tamanho após perdas consecutivas", Order = 3, GroupName = "Risco")]
        public bool UseAdaptiveRisk { get; set; }

        [Range(0.1, 10.0), NinjaScriptProperty]
        [Display(Name = "Alvo em ATR", Description = "Múltiplos de ATR para o alvo", Order = 1, GroupName = "Entrada/Saída")]
        public double ProfitTargetATR { get; set; }

        [Range(0.1, 10.0), NinjaScriptProperty]
        [Display(Name = "Stop em ATR", Description = "Múltiplos de ATR para o stop", Order = 2, GroupName = "Entrada/Saída")]
        public double StopLossATR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Stop Móvel", Description = "Ativa o stop móvel", Order = 3, GroupName = "Entrada/Saída")]
        public bool UseTrailingStop { get; set; }

        [Range(0.1, 10.0), NinjaScriptProperty]
        [Display(Name = "Stop Móvel em ATR", Description = "Distância do stop móvel em ATR", Order = 4, GroupName = "Entrada/Saída")]
        public double TrailingStopATR { get; set; }

        [Range(0, int.MaxValue), NinjaScriptProperty]
        [Display(Name = "Volume Mínimo", Description = "Volume mínimo para entrar na operação", Order = 5, GroupName = "Entrada/Saída")]
        public int MinimumVolume { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Filtro de Horário", Description = "Ativa o filtro de horário de operação", Order = 1, GroupName = "Tempo")]
        public bool UseTimeFilter { get; set; }

        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Horário de Início", Description = "Horário de início das operações", Order = 2, GroupName = "Tempo")]
        public TimeSpan TradingStartTime { get; set; }

        [PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [Display(Name = "Horário de Término", Description = "Horário de término das operações", Order = 3, GroupName = "Tempo")]
        public TimeSpan TradingEndTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Requer Confirmação VWAP", Description = "Requer que o preço esteja do lado correto da VWAP", Order = 1, GroupName = "Filtros")]
        public bool RequireVwapConfirmation { get; set; }

        [Range(0, 100), NinjaScriptProperty]
        [Display(Name = "RSI Mínimo", Description = "Valor mínimo do RSI para entradas", Order = 2, GroupName = "Filtros")]
        public int MinimumRsiValue { get; set; }

        [Range(0, 100), NinjaScriptProperty]
        [Display(Name = "RSI Máximo", Description = "Valor máximo do RSI para entradas", Order = 3, GroupName = "Filtros")]
        public int MaximumRsiValue { get; set; }

        [Range(0, 10), NinjaScriptProperty]
        [Display(Name = "Barras de Espera", Description = "Barras de espera após sinal", Order = 4, GroupName = "Filtros")]
        public int WaitBarsAfterSignal { get; set; }

        [Range(2, 50), NinjaScriptProperty]
        [Display(Name = "Período do RSI", Description = "Período para cálculo do RSI", Order = 5, GroupName = "Indicadores")]
        public int RsiPeriod { get; set; }

        [Range(1, 10), NinjaScriptProperty]
        [Display(Name = "Suavização do RSI", Description = "Período de suavização do RSI", Order = 6, GroupName = "Indicadores")]
        public int RsiSmoothing { get; set; }
        #endregion
    }
}

#region NinjaScript generated code. Neither change nor remove.
namespace NinjaTrader.NinjaScript.Strategies
{
    public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
    {

    }
}
#endregion
