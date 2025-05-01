#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.Gui;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class TrendFollowingStrategy : Strategy
    {
        private MovingAverageVolumeIndicator maVolumeIndicator;
        private bool isTradingHours;

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = "Estratégia de acompanhamento de tendência com médias móveis e volume";
                Name = "TrendFollowingStrategy";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.AllEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 20;

                FastPeriod = 10;
                SlowPeriod = 20;
                VolumeLookback = 20;
                VolumeMultiplier = 1.1; // Reduzido ainda mais para gerar mais sinais
                StopLossTicks = 20;
                ProfitTargetTicks = 40;
                TradingStartHour = 9;
                TradingEndHour = 16;
            }
            else if (State == State.Configure)
            {
                // Cria e configura o indicador
                maVolumeIndicator = MovingAverageVolumeIndicator(FastPeriod, SlowPeriod, VolumeLookback, VolumeMultiplier);
                
                // Adiciona o indicador ao gráfico
                AddChartIndicator(maVolumeIndicator);
                
                // Configura para calcular no fechamento da barra
                Calculate = Calculate.OnBarClose;
                
                // Configurações adicionais
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                TraceOrders = true; // Ativa o rastreamento de ordens para debug
                
                Print("Estratégia configurada com FastPeriod=" + FastPeriod + ", SlowPeriod=" + SlowPeriod + ", VolumeLookback=" + VolumeLookback + ", VolumeMultiplier=" + VolumeMultiplier);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < BarsRequiredToTrade) return;

            // Verifica se está dentro do horário de trading (ex.: 9h às 16h)
            isTradingHours = Time[0].Hour >= TradingStartHour && Time[0].Hour < TradingEndHour;

            if (!isTradingHours) return;

            // Verifica diretamente no indicador se houve cruzamento e volume alto
            // Isso é uma abordagem alternativa que não depende dos plots ou objetos de desenho
            bool bullishCross = CrossAbove(SMA(Close, FastPeriod), SMA(Close, SlowPeriod), 1);
            bool bearishCross = CrossBelow(SMA(Close, FastPeriod), SMA(Close, SlowPeriod), 1);
            
            // Calcula o limiar de volume (mesma lógica do indicador)
            double avgVolume = SMA(Volume, VolumeLookback)[0];
            double volumeThreshold = avgVolume * VolumeMultiplier;
            bool highVolume = Volume[0] > volumeThreshold;
            
            // Determina os sinais
            bool bullishSignal = bullishCross && highVolume;
            bool bearishSignal = bearishCross && highVolume;
            
            // Verifica também se há valores nos plots do indicador como backup
            bullishSignal = bullishSignal || maVolumeIndicator.Values[0][0] != 0;
            bearishSignal = bearishSignal || maVolumeIndicator.Values[1][0] != 0;
            
            // Debug para verificar os valores
            if (CurrentBar % 5 == 0 || bullishSignal || bearishSignal) // Imprime a cada 5 barras ou quando detectar sinais
            {
                Print("Barra: " + CurrentBar + ", Hora: " + Time[0].ToString());
                Print("BullishSignal: " + bullishSignal + ", BearishSignal: " + bearishSignal);
                Print("BullishCross: " + bullishCross + ", BearishCross: " + bearishCross + ", HighVolume: " + highVolume);
                Print("Volume atual: " + Volume[0] + ", Limiar: " + volumeThreshold);
                Print("Valor do indicador [0]: " + maVolumeIndicator.Values[0][0] + ", Valor do indicador [1]: " + maVolumeIndicator.Values[1][0]);
            }

            // Entrada longa
            if (bullishSignal && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterLong(1, "LongEntry");
                SetStopLoss("LongEntry", CalculationMode.Ticks, StopLossTicks, false);
                SetProfitTarget("LongEntry", CalculationMode.Ticks, ProfitTargetTicks);
                Print("### ENTRADA LONGA em " + Time[0].ToString() + " a " + Close[0] + " ###");
                Draw.ArrowUp(this, "LongEntry" + CurrentBar, false, 0, Low[0] - 4 * TickSize, Brushes.Lime);
                
                // Desenha uma linha horizontal no preço de entrada para melhor visualização
                Draw.Line(this, "EntryLine" + CurrentBar, false, 0, Close[0], 10, Close[0], Brushes.Yellow, DashStyleHelper.Solid, 2);
                
                // Força a atualização do gráfico
                ForceRefresh();
            }

            // Entrada curta
            if (bearishSignal && Position.MarketPosition == MarketPosition.Flat)
            {
                EnterShort(1, "ShortEntry");
                SetStopLoss("ShortEntry", CalculationMode.Ticks, StopLossTicks, false);
                SetProfitTarget("ShortEntry", CalculationMode.Ticks, ProfitTargetTicks);
                Print("### ENTRADA CURTA em " + Time[0].ToString() + " a " + Close[0] + " ###");
                Draw.ArrowDown(this, "ShortEntry" + CurrentBar, false, 0, High[0] + 4 * TickSize, Brushes.Crimson);
                
                // Força a atualização do gráfico
                ForceRefresh();
            }
        }

        #region Properties
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Período da Média Rápida", Order = 1, GroupName = "Parameters")]
        public int FastPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Período da Média Lenta", Order = 2, GroupName = "Parameters")]
        public int SlowPeriod { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Período de Lookback do Volume", Order = 3, GroupName = "Parameters")]
        public int VolumeLookback { get; set; }

        [NinjaScriptProperty]
        [Range(0.1, double.MaxValue)]
        [Display(Name = "Multiplicador de Volume", Order = 4, GroupName = "Parameters")]
        public double VolumeMultiplier { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Stop Loss (Ticks)", Order = 5, GroupName = "Parameters")]
        public int StopLossTicks { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Profit Target (Ticks)", Order = 6, GroupName = "Parameters")]
        public int ProfitTargetTicks { get; set; }

        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name = "Hora de Início do Trading", Order = 7, GroupName = "Trading Hours")]
        public int TradingStartHour { get; set; }

        [NinjaScriptProperty]
        [Range(0, 23)]
        [Display(Name = "Hora de Fim do Trading", Order = 8, GroupName = "Trading Hours")]
        public int TradingEndHour { get; set; }
        #endregion
    }
}