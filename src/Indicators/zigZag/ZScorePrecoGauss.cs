// Indicador NinjaTrader 8: ZScore com Peso Gaussiano
// Desenvolvido por Ezequiel Schechleski

using System;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;

namespace NinjaTrader.NinjaScript.Indicators
{
    public class ZScorePrecoGauss : Indicator
    {
        private Series<double> pesos;
        private Series<double> media;
        private Series<double> desvio;
        private Series<double> zscore;

        [NinjaScriptProperty]
        [Range(5, 100)]
        [Display(Name = "Período da Janela", Order = 1, GroupName = "Parâmetros")]
        public int Periodo { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Ativar Alerta Z ±2", Order = 2, GroupName = "Parâmetros")]
        public bool AlertaZ { get; set; }

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name = "ZScorePrecoGauss";
                Periodo = 34;
                AlertaZ = true;
                AddPlot(Brushes.White, "ZScore");
            }
            else if (State == State.Configure)
            {
                pesos = new Series<double>(this);
                media = new Series<double>(this);
                desvio = new Series<double>(this);
                zscore = new Series<double>(this);
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < Periodo)
                return;

            double somaPeso = 0;
            double mediaPonderada = 0;
            double varianciaPonderada = 0;

            for (int i = 0; i < Periodo; i++)
            {
                double peso = Math.Exp(-Math.Pow(i, 2) / (2.0 * Math.Pow(Periodo / 2.0, 2)));
                double preco = Input[i];
                somaPeso += peso;
                mediaPonderada += preco * peso;
                pesos[i] = peso;
            }

            mediaPonderada /= somaPeso;
            media[0] = mediaPonderada;

            for (int i = 0; i < Periodo; i++)
            {
                double preco = Input[i];
                varianciaPonderada += Math.Pow(preco - mediaPonderada, 2) * pesos[i];
            }

            varianciaPonderada /= somaPeso;
            desvio[0] = Math.Sqrt(varianciaPonderada);

            if (desvio[0] == 0)
            {
                zscore[0] = 0;
            }
            else
            {
                zscore[0] = (Input[0] - media[0]) / desvio[0];
            }

            Values[0][0] = zscore[0];

            // Alerta visual e sonoro
            if (AlertaZ && Math.Abs(zscore[0]) > 2)
            {
                Draw.Dot(this, "ZAlert" + CurrentBar, false, 0, High[0] + TickSize * 10, Brushes.Red);
                Alert("ZScoreAlert", Priority.Medium, "Z-Score passou de ±2", NinjaTrader.Core.Globals.InstallDir + "\\sounds\\Alert3.wav", 0, Brushes.Red, Brushes.White);
            }
        }
    }
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ZScorePrecoGauss[] cacheZScorePrecoGauss;
		public ZScorePrecoGauss ZScorePrecoGauss(int periodo, bool alertaZ)
		{
			return ZScorePrecoGauss(Input, periodo, alertaZ);
		}

		public ZScorePrecoGauss ZScorePrecoGauss(ISeries<double> input, int periodo, bool alertaZ)
		{
			if (cacheZScorePrecoGauss != null)
				for (int idx = 0; idx < cacheZScorePrecoGauss.Length; idx++)
					if (cacheZScorePrecoGauss[idx] != null && cacheZScorePrecoGauss[idx].Periodo == periodo && cacheZScorePrecoGauss[idx].AlertaZ == alertaZ && cacheZScorePrecoGauss[idx].EqualsInput(input))
						return cacheZScorePrecoGauss[idx];
			return CacheIndicator<ZScorePrecoGauss>(new ZScorePrecoGauss(){ Periodo = periodo, AlertaZ = alertaZ }, input, ref cacheZScorePrecoGauss);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ZScorePrecoGauss ZScorePrecoGauss(int periodo, bool alertaZ)
		{
			return indicator.ZScorePrecoGauss(Input, periodo, alertaZ);
		}

		public Indicators.ZScorePrecoGauss ZScorePrecoGauss(ISeries<double> input , int periodo, bool alertaZ)
		{
			return indicator.ZScorePrecoGauss(input, periodo, alertaZ);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ZScorePrecoGauss ZScorePrecoGauss(int periodo, bool alertaZ)
		{
			return indicator.ZScorePrecoGauss(Input, periodo, alertaZ);
		}

		public Indicators.ZScorePrecoGauss ZScorePrecoGauss(ISeries<double> input , int periodo, bool alertaZ)
		{
			return indicator.ZScorePrecoGauss(input, periodo, alertaZ);
		}
	}
}

#endregion
