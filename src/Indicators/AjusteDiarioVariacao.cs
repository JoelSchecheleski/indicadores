
#region Using declarations
using System;
using System.ComponentModel.DataAnnotations;      // <— DisplayAttribute
using System.Windows.Media;                       // <— Brushes
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Tools;                       // <— DisplayAttribute também
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;        // <— Draw.*
#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    public class AjusteDiarioVariacoes : Indicator
    {
        #region Parameters
        [NinjaScriptProperty]
        [Display(Name = "Ajuste Diário", Description = "Valor manual do ajuste diário", Order = 1, GroupName = "Parâmetros")]
        public double AjusteManual { get; set; }
        #endregion

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Name           = "AjusteDiarioVariacoes";
                Description    = "Traça linhas a cada 0,25% do ajuste manual até ±5%.";
                Calculate      = Calculate.OnBarClose;
                IsOverlay      = true;
                DisplayInDataBox = false;
                DrawOnPricePanel = true;
            }
        }

        protected override void OnBarUpdate()
        {
            if (CurrentBar < 1 || AjusteManual <= 0)
                return;

            // Apaga linhas da sessão anterior
            if (Bars.IsFirstBarOfSession)
                RemoveDrawObjects();

            // Desenha linhas na primeira barra da sessão
            if (Bars.IsFirstBarOfSession)
            {
                // linha base 0%
                Draw.HorizontalLine(this, "Base_"  + CurrentBar, AjusteManual, Brushes.White);

                // linhas de -5% a +5% em passos de 0,25%
                for (double pct = -5.0; pct <= 5.0; pct += 0.25)
                {
                    if (Math.Abs(pct) < 0.001) continue;
                    double nivel = AjusteManual * (1 + pct / 100.0);
                    string tag  = $"Var_{pct:0.00}_{CurrentBar}";
                    Brush  cor  = pct > 0 ? Brushes.LimeGreen : Brushes.Red;
                    Draw.HorizontalLine(this, tag, nivel, cor);
                }
            }
        }
    }
}
