using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.Core.FloatingPoint;

namespace NinjaTrader.NinjaScript.Strategies
{
    public class MomentumScalper : Strategy
    {
        private MomentumSignal momentumSignal;
        private ATR atr;
        private VWAP vwap;
        private VOL volume;
        
        private double initialBalance;
        private double currentDrawdown;
        private int consecutiveLosses;
        private List<double> dailyPnL;
        private DateTime lastTradeTime;
        private bool inTradeWindow;
        private double lastVolatility;
        private double marketStrength;
        private int winningTrades;
        private int losingTrades;

        protected override void OnStateChange()
        {
            try
            {
                if (State == State.SetDefaults)
                {
                    Description = "Estratégia adaptativa de day trading com gestão de risco avançada";
                    Name = "MomentumScalper";
                    Calculate = Calculate.OnEachTick;
                    EntriesPerDirection = 1;
                    EntryHandling = EntryHandling.AllEntries;
                    IsExitOnSessionCloseStrategy = true;
                    ExitOnSessionCloseSeconds = 30;
                    IsFillLimitOnTouch = false;
                    MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                    OrderFillResolution = OrderFillResolution.Standard;
                    Slippage = 1;
                    StartBehavior = StartBehavior.WaitUntilFlat;
                    TimeInForce = TimeInForce.Gtc;
                    TraceOrders = false;
                    RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                    StopTargetHandling = StopTargetHandling.PerEntryExecution;
                    BarsRequiredToTrade = 20;

                    // Parâmetros de Risco
                    RiskPerTrade = 0.5;          // Risco por trade em %
                    MaxDailyLoss = 2.0;          // Máxima perda diária em %
                    UseAdaptiveRisk = true;      // Ajusta risco baseado no drawdown
                    
                    // Parâmetros de Entrada/Saída
                    ProfitTargetATR = 1.5;       // Alvo em múltiplos de ATR
                    StopLossATR = 1.0;           // Stop em múltiplos de ATR
                    UseTrailingStop = true;      // Usar stop móvel
                    TrailingStopATR = 0.5;       // Distância do stop móvel em ATR
                    
                    // Filtros de Tempo
                    UseTimeFilter = true;         // Usar filtro de horário
                    TradingStartTime = new TimeSpan(9, 30, 0);  // 9:30
                    TradingEndTime = new TimeSpan(16, 30, 0);   // 16:30
                    
                    // Filtros de Mercado
                    MinimumVolume = 1000;        // Volume mínimo
                    UseVolumeFilter = true;      // Filtro de volume
                    UseVolatilityFilter = true;  // Filtro de volatilidade
                    WaitAfterLoss = 5;           // Minutos de espera após perda
                }
                else if (State == State.Configure)
                {
                    // Configura indicadores
                    momentumSignal = MomentumSignal(9, 21, 14, 70, 30);
                    atr = ATR(14);
                    vwap = VWAP();
                    volume = VOL();

                    // Inicializa variáveis
                    dailyPnL = new List<double>();
                    initialBalance = Account.Get(AccountItem.CashValue, Currency.UsDollar);
                    currentDrawdown = 0;
                    consecutiveLosses = 0;
                    lastTradeTime = DateTime.MinValue;
                    winningTrades = 0;
                    losingTrades = 0;
                    
                    // Configura eventos
                    SetStopLoss(CalculationMode.Price, 0);
                    SetProfitTarget(CalculationMode.Price, 0);
                }
            }
            catch (Exception ex)
            {
                Print("Erro em OnStateChange: " + ex.Message);
                Print("StackTrace: " + ex.StackTrace);
            }
        }

        protected override void OnBarUpdate()
        {
            try
            {
                if (CurrentBar < BarsRequiredToTrade) return;

                // Verifica se está dentro da janela de trading
                inTradeWindow = IsWithinTradingHours();

                // Atualiza métricas
                UpdateMetrics();

                // Verifica condições de proteção
                if (!CanTrade()) return;

                // Calcula tamanho da posição baseado no risco adaptativo
                int quantity = CalculatePositionSize();

                // Gerencia posições existentes
                ManageExistingPositions();

                // Verifica sinais de entrada
                if (Position.MarketPosition == MarketPosition.Flat)
                {
                    // Sinal de Compra
                    if (momentumSignal != null && momentumSignal.IsBuySignal && IsValidSetup())
                    {
                        EnterLong(quantity, "MomentumLong");
                        lastTradeTime = Time[0];
                    }
                    // Sinal de Venda
                    else if (momentumSignal != null && momentumSignal.IsSellSignal && IsValidSetup())
                    {
                        EnterShort(quantity, "MomentumShort");
                        lastTradeTime = Time[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Print("Erro em OnBarUpdate: " + ex.Message);
                Print("StackTrace: " + ex.StackTrace);
            }
        }

        private bool IsWithinTradingHours()
        {
            if (!UseTimeFilter) return true;

            TimeSpan currentTime = Time[0].TimeOfDay;
            return currentTime >= TradingStartTime && currentTime <= TradingEndTime;
        }

        private void UpdateMetrics()
        {
            // Atualiza drawdown
            double currentBalance = Account.Get(AccountItem.CashValue, Currency.UsDollar);
            currentDrawdown = Math.Max(0, (initialBalance - currentBalance) / initialBalance * 100);

            // Atualiza PnL diário
            if (Time[0].Date != Time[1].Date)
            {
                dailyPnL.Add(currentBalance - initialBalance);
                initialBalance = currentBalance;
            }

            // Atualiza volatilidade
            if (CurrentBar > 0)
            {
                lastVolatility = Math.Abs(Close[0] - Close[1]) / Close[1] * 100;
            }

            // Atualiza força do mercado
            marketStrength = (Close[0] - Open[0]) / (High[0] - Low[0]);
        }

        private bool CanTrade()
        {
            // Verifica drawdown máximo
            if (currentDrawdown >= MaxDailyLoss) return false;

            // Verifica tempo mínimo após perda
            if (consecutiveLosses > 0 && 
                (Time[0] - lastTradeTime).TotalMinutes < (WaitAfterLoss * consecutiveLosses))
                return false;

            // Verifica janela de trading
            if (!inTradeWindow) return false;

            return true;
        }

        private bool IsValidSetup()
        {
            if (!UseVolumeFilter && !UseVolatilityFilter) return true;

            bool volumeOk = !UseVolumeFilter || Volume[0] >= MinimumVolume;
            bool volatilityOk = !UseVolatilityFilter || (atr[0] > atr[1]);

            return volumeOk && volatilityOk;
        }

        private int CalculatePositionSize()
        {
            double riskMultiplier = UseAdaptiveRisk ? 
                Math.Max(0.5, 1 - (currentDrawdown / MaxDailyLoss)) : 1.0;

            double riskAmount = Account.Get(AccountItem.CashValue, Currency.UsDollar) * 
                              (RiskPerTrade / 100) * riskMultiplier;

            double stopDistance = atr[0] * StopLossATR;
            if (stopDistance <= 0) return 1;

            int quantity = (int)(riskAmount / (stopDistance * SymbolInfo.TickSize * SymbolInfo.PointValue));
            return Math.Max(1, quantity);
        }

        private void ManageExistingPositions()
        {
            if (Position.MarketPosition == MarketPosition.Flat) return;

            double stopPrice, targetPrice;
            
            if (momentumSignal != null && momentumSignal.IsBuySignal && Position.MarketPosition != MarketPosition.Long)
            {
                stopPrice = UseTrailingStop ?
                    Math.Max(Position.AveragePrice - (atr[0] * StopLossATR),
                            Close[0] - (atr[0] * TrailingStopATR)) :
                    Position.AveragePrice - (atr[0] * StopLossATR);

                targetPrice = Position.AveragePrice + (atr[0] * ProfitTargetATR);
            }
            else // Short
            {
                stopPrice = UseTrailingStop ?
                    Math.Min(Position.AveragePrice + (atr[0] * StopLossATR),
                            Close[0] + (atr[0] * TrailingStopATR)) :
                    Position.AveragePrice + (atr[0] * StopLossATR);

                targetPrice = Position.AveragePrice - (atr[0] * ProfitTargetATR);
            }

            SetStopLoss(CalculationMode.Price, stopPrice);
            SetProfitTarget(CalculationMode.Price, targetPrice);
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, 
            int quantity, MarketPosition marketPosition, string orderId, DateTime time)
        {
            if (execution.ProfitLoss >= 0)
            {
                winningTrades++;
                consecutiveLosses = 0;
            }
            else
            {
                losingTrades++;
                consecutiveLosses++;
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(0.1, 5.0)]
        [Display(Name = "Risco por Trade %", Order = 1, GroupName = "Risco")]
        public double RiskPerTrade { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 10.0)]
        [Display(Name = "Máxima Perda Diária %", Order = 2, GroupName = "Risco")]
        public double MaxDailyLoss { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Risco Adaptativo", Order = 3, GroupName = "Risco")]
        public bool UseAdaptiveRisk { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 5.0)]
        [Display(Name = "Alvo de Lucro (ATR)", Order = 1, GroupName = "Alvos")]
        public double ProfitTargetATR { get; set; }

        [NinjaScriptProperty]
        [Range(0.5, 3.0)]
        [Display(Name = "Stop Loss (ATR)", Order = 2, GroupName = "Alvos")]
        public double StopLossATR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Stop Móvel", Order = 3, GroupName = "Alvos")]
        public bool UseTrailingStop { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, 2.0)]
        [Display(Name = "Distância Stop Móvel (ATR)", Order = 4, GroupName = "Alvos")]
        public double TrailingStopATR { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Filtro de Horário", Order = 1, GroupName = "Tempo")]
        public bool UseTimeFilter { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Hora Início", Order = 2, GroupName = "Tempo")]
        public TimeSpan TradingStartTime { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Hora Fim", Order = 3, GroupName = "Tempo")]
        public TimeSpan TradingEndTime { get; set; }

        [NinjaScriptProperty]
        [Range(100, 1000000)]
        [Display(Name = "Volume Mínimo", Order = 1, GroupName = "Filtros")]
        public int MinimumVolume { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Filtro de Volume", Order = 2, GroupName = "Filtros")]
        public bool UseVolumeFilter { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Usar Filtro de Volatilidade", Order = 3, GroupName = "Filtros")]
        public bool UseVolatilityFilter { get; set; }

        [NinjaScriptProperty]
        [Range(1, 30)]
        [Display(Name = "Espera Após Perda (min)", Order = 4, GroupName = "Filtros")]
        public int WaitAfterLoss { get; set; }
        #endregion
    }
}