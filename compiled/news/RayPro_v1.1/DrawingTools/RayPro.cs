//////////////////// ZK_Moded by Zacarías Satrústegui. Telegram @FJ222   ////////////////////////////////////////////////////////////////////////////////////////////////////////////
///
///           Donaciones/Recompensas/Reward to THK2aGfnMLS7jx6n6pbeExu2P6hHhrMCSn (Tron Net)  |  0x72AFf83fbB071d2F2C9656De9F1ADE9d6D70d58a (Ethereum Net)     
///           
// 18-09-2024 - v1.0. 
// 02-12-2024 - v1.1. Arreglado fallo si no había texto y posText == PosicionTextoEnum.BordeDerechoEntreLinea, desaparecía la línea.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Core.FloatingPoint;
using SharpDX;
using Point = System.Windows.Point;
using System.Windows.Controls;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using System.Xml;
using System.Windows.Markup;
using System.Runtime.Remoting.Contexts;
using NinjaTrader.Gui.NinjaScript.Wizard;
#endregion

//This namespace holds Drawing tools in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.DrawingTools
{

    [Gui.CategoryOrder("NinjaScriptGeneral", 0)]
    [Gui.CategoryOrder("1. Texto", 1000010)]
    [Gui.CategoryOrder("2. Líneas internas", 1000020)]
    [Gui.CategoryOrder("Versión", 1000025)]
    [Gui.CategoryOrder("Data", 1000030)]

    public class RayPro : DrawingTool
    {
        public enum PosicionTextoEnum
        {
            BordeIzquierdo,
            Centrado,
            BordeDerecho,
            BordeDerechoEntreLinea
        }

        #region Propiedades
        protected enum ChartLineType
        {
            ArrowLine,
            ExtendedLine,
            HorizontalLine,
            Line,
            Ray,
            VerticalLine,
        }
        public override IEnumerable<ChartAnchor> Anchors { get { return new[] { StartAnchor, EndAnchor }; } }
        [Display(Order = 2)]
        public ChartAnchor EndAnchor { get; set; }
        [Display(Order = 1)]
        public ChartAnchor StartAnchor { get; set; }

        public override object Icon { get { return Gui.Tools.Icons.DrawLineTool; } }

        [CLSCompliant(false)]
        protected SharpDX.Direct2D1.PathGeometry ArrowPathGeometry;
        private const double cursorSensitivity = 15;
        private ChartAnchor editingAnchor;

        [Browsable(false)]
        [XmlIgnore]
        protected ChartLineType LineType { get; set; }

        [Display(ResourceType = typeof(Custom.Resource), GroupName = "NinjaScriptGeneral", Name = "Color de línea", Order = 99)]
        public Stroke Stroke { get; set; }

        public override bool SupportsAlerts { get { return true; } }

        //////////////////////////////////////////// 1. Texto ///////////////////////////////////////////////////////////////////////////


        [Display(ResourceType = typeof(Custom.Resource), Name = "Texto", GroupName = "1. Texto", Order = 10)]
        [PropertyEditor("NinjaTrader.Gui.Tools.MultilineEditor")]
        public string DisplayText
        {
            get { return text; }
            set
            {
                if (text == value)
                    return;
                text = value;
                needsLayoutUpdate = true;
            }
        }


        [Display(ResourceType = typeof(Custom.Resource), Name = "Fuente", GroupName = "1. Texto", Order = 20)]
        public Gui.Tools.SimpleFont Font
        {
            get { return font; }
            set
            {
                font = value;
                needsLayoutUpdate = true;
            }
        }


        [XmlIgnore]
        [Display(ResourceType = typeof(Custom.Resource), Name = "Color de letras", GroupName = "1. Texto", Order = 30)]
        public Brush TextBrush
        {
            get { return textBrush; }
            set
            {
                textBrush = value;
                if (textBrush != null && textBrush.CanFreeze)
                    textBrush.Freeze();
            }
        }

        [Browsable(false)]
        public string TextBrushSerialize
        {
            get { return Serialize.BrushToString(TextBrush); }
            set { TextBrush = Serialize.StringToBrush(value); }
        }

        [Range(1, 100), NinjaScriptProperty]
        [Display(Name = "Opacidad del texto %", Description = "1 - 100", GroupName = "1. Texto", Order = 35)]
        public int TextOpacity
        { get; set; }



        [Display(Name = "Posición del texto", GroupName = "1. Texto", Order = 35)]
        [RefreshProperties(RefreshProperties.All)]
        public PosicionTextoEnum PosicionTexto
        {
            get { return posText; }
            set { posText = value; }
        }

        [NinjaScriptProperty]
        [Display(Name = "Texto por encima de la línea", Description = "Texto encima o debajo de la línea", GroupName = "1. Texto", Order = 40)]
        public bool TextoEncimaDeLinea
        { get; set; }


        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Separación del texto respecto la línea", Description = "Separacó�n en píxeles", GroupName = "1. Texto", Order = 45)]
        public int SeparacionTextoLinea
        { get; set; }


        [NinjaScriptProperty]
        [Display(Name = "Versión:", Order = 5, GroupName = "Versión")]
        public string Version
        {
            get { return "1.1"; }
            set { }
        }

        #endregion


        ///////////////////////////////////////////////////

        private System.Windows.Media.Brush textBrush;
        private string text;
        private bool needsLayoutUpdate;
        private Gui.Tools.SimpleFont font;

        private List<Button> listaBotones = new List<Button>();
        private ChartControl chartControlx;
        private bool isButtonAdded = false;
        private StackPanel buttonPanel;
        private PosicionTextoEnum posText;

        private int alturaBotones = 20;
        private int anchuraBotones = 110;

        private TimeSpan initialTimeDifference;
        private DateTime initialDataPointTime;
        private bool isDragging = false; // bandera para controlar el arrastre

        ///////////////////////////////////////////////////

        protected override void OnStateChange()
        {

            if (State == State.SetDefaults)
            {
                LineType = ChartLineType.HorizontalLine;
                Name = "RayPro";
                DrawingState = DrawingState.Building;

                EndAnchor = new ChartAnchor
                {
                    IsEditing = true,
                    DrawingTool = this,
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorEnd,
                    IsBrowsable = true
                };

                StartAnchor = new ChartAnchor
                {
                    IsEditing = true,
                    DrawingTool = this,
                    DisplayName = Custom.Resource.NinjaScriptDrawingToolAnchorStart,
                    IsBrowsable = true
                };

                // a normal line with both end points has two anchors
                Stroke = new Stroke(Brushes.White, 1f);
                TextoEncimaDeLinea = true;
                Font = new Gui.Tools.SimpleFont() { Size = 14 };
                TextOpacity = 100;
                TextBrush = System.Windows.Media.Brushes.White;                
                text = "";
                SeparacionTextoLinea = 7;
                posText = PosicionTextoEnum.Centrado;
                

            }
            else if (State == State.Terminated)
            {
                // release any device resources
                EliminarBotones();
                Dispose();
            }
        }

        private void CreateButtons()
        {
            // Create a style to apply to the button
            Style s = new Style();
            s.TargetType = typeof(System.Windows.Controls.Button);
            s.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
            s.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.Orange));
            s.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.Black));
            s.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
            s.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));
            s.Setters.Add(new Setter(System.Windows.Controls.Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));

            int botonesACrear = 0;

            try
            {
                string folderPath = NinjaTrader.Core.Globals.UserDataDir + "\\templates\\DrawingTool\\RayPro";
                string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml");
                botonesACrear = Math.Min(15, xmlFiles.Length);
                string[] fileNamesWithoutExtension = new string[botonesACrear];



                for (int i = 0; i < botonesACrear; i++)
                {
                    // Obtener el nombre del archivo sin la extensión
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(xmlFiles[i]);
                    // Guardar el nombre en la matriz
                    fileNamesWithoutExtension[i] = fileNameWithoutExtension;
                }

                for (int i = 0; i < botonesACrear; i++)
                {
                    // Leer el archivo XML
                    XDocument doc = XDocument.Load(xmlFiles[i]);
                    // Buscar la etiqueta <DisplayText> y obtener su valor
                    string displayText = (doc.Descendants("DisplayText").FirstOrDefault() != null) ? doc.Descendants("DisplayText").FirstOrDefault().Value : null;
                    if (displayText != null)
                    {
                        Button boton = new Button
                        {
                            Content = fileNamesWithoutExtension[i],
                            Width = anchuraBotones,
                            Height = alturaBotones,
                            Background = Brushes.Violet,
                            Foreground = Brushes.Black,
                        };
                        boton.Style = s;
                        boton.Click += MyButton_Click;
                        listaBotones.Add(boton);
                    }

                }
            }
            catch (IOException ex)
            {
                Print("No se ha encontrado la carpeta con los templates. " + ex.Message);
            }
            catch (Exception ex)
            {
                Print("Error: " + ex.Message);
            }





            if (botonesACrear > 0)  //añadir tb el botón de cerrar al inicio de la lista
            {
                Style botonCerrarStyle = new Style();
                botonCerrarStyle.TargetType = typeof(System.Windows.Controls.Button);
                botonCerrarStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
                botonCerrarStyle.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.Orange));
                botonCerrarStyle.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.Black));
                botonCerrarStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
                botonCerrarStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));
                botonCerrarStyle.Setters.Add(new Setter(System.Windows.Controls.Button.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));

                Button boton = new Button
                {
                    Content = "X",
                    Width = anchuraBotones,
                    Height = alturaBotones,
                    Background = Brushes.Black,
                    Foreground = Brushes.Red,

                };
                boton.Style = botonCerrarStyle;
                boton.Click += MyButton_Click;
                listaBotones.Insert(0, boton);

            }
        }

        private void EliminarBotones()
        {
            if (chartControlx != null)
            {
                chartControlx.Dispatcher.InvokeAsync(() =>
                {
                    var chartWindow = System.Windows.Window.GetWindow(chartControlx);
                    var grid = chartWindow.Content as Grid;

                    if (grid != null)
                    {
                        for (int i = 0; i < listaBotones.Count; i++)
                        {
                            listaBotones[i].Click -= MyButton_Click;
                            listaBotones[i] = null;
                        }

                        if (buttonPanel != null)
                        {
                            grid.Children.Remove(buttonPanel);
                        }
                    }
                });
            }
        }

        private void MyButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button botonClickado = sender as Button;
            string textoBoton = "";
            if (botonClickado != null)
                textoBoton = (string)botonClickado.Content;

            string folderPath = NinjaTrader.Core.Globals.UserDataDir + "\\templates\\DrawingTool\\RayPro";
            string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml");

            if (textoBoton != "X")
            {
                foreach (string xmlFile in xmlFiles)
                {
                    try
                    {                        
                        if (Path.GetFileNameWithoutExtension(xmlFile) == textoBoton)
                        {
                            XDocument doc = XDocument.Load(xmlFile);
                            string displayText = (doc.Descendants("DisplayText").FirstOrDefault() != null) ? doc.Descendants("DisplayText").FirstOrDefault().Value : null;
                            text = displayText;
                            string separacionTextoLinea = (doc.Descendants("SeparacionTextoLinea").FirstOrDefault() != null) ? doc.Descendants("SeparacionTextoLinea").FirstOrDefault().Value : null;
                            SeparacionTextoLinea = Int32.Parse(separacionTextoLinea);
                            string textoEncimaDeLinea = (doc.Descendants("TextoEncimaDeLinea").FirstOrDefault() != null) ? doc.Descendants("TextoEncimaDeLinea").FirstOrDefault().Value : null;
                            TextoEncimaDeLinea = bool.Parse(textoEncimaDeLinea);
                            string posTexto = (doc.Descendants("PosicionTexto").FirstOrDefault() != null) ? doc.Descendants("PosicionTexto").FirstOrDefault().Value : null;
                            PosicionTextoEnum posicionTextoEnum;
                            if (!string.IsNullOrEmpty(posTexto) && Enum.TryParse(posTexto, true, out posicionTextoEnum))
                            {
                                PosicionTexto = posicionTextoEnum;
                            }

                            //Color de la línea (Stroke)
                            var strokeElement = doc.Descendants("Stroke").FirstOrDefault();
                            if (strokeElement != null)
                            {
                                var brushSerializeElement = strokeElement.Element("BrushSerialize");
                                string brushSerialize = brushSerializeElement != null ? brushSerializeElement.Value : null;

                                Brush strokeBrush = ConvertStringToBrush(brushSerialize);

                                var dashStyleHelperElement = strokeElement.Element("DashStyleHelper");
                                string dashStyleHelper = dashStyleHelperElement != null ? dashStyleHelperElement.Value : null;
                                DashStyleHelper DashStyle = (DashStyleHelper)Enum.Parse(typeof(DashStyleHelper), dashStyleHelper);

                                var opacityElement = strokeElement.Element("Opacity");
                                string opacity = opacityElement != null ? opacityElement.Value : null;
                                int Opacity = Int32.Parse(opacity);

                                var widthElement = strokeElement.Element("Width");
                                string width = widthElement != null ? widthElement.Value : null;
                                float Width = float.Parse(width, CultureInfo.InvariantCulture);

                                Stroke = new Stroke(strokeBrush, DashStyle, Width);
                                                               
                                
                            }
                            //Fuente
                            var fontElement = doc.Descendants("Font").FirstOrDefault();
                            if (fontElement != null)
                            {
                                var boldElement = fontElement.Element("Bold");
                                string bold = boldElement != null ? boldElement.Value : null;
                                bool Boldx = bool.Parse(bold);

                                var familySerializeElement = fontElement.Element("FamilySerialize");
                                string familySerialize = familySerializeElement != null ? familySerializeElement.Value : null;

                                var italicElement = fontElement.Element("Italic");
                                string italic = italicElement != null ? italicElement.Value : null;
                                bool Italicx = bool.Parse(italic);

                                var sizeElement = fontElement.Element("Size");
                                string size = sizeElement != null ? sizeElement.Value : null;
                                double Sizex = double.Parse(size, CultureInfo.InvariantCulture);

                                Font = new Gui.Tools.SimpleFont(familySerialize, Sizex) { Size = Sizex, Bold = Boldx, Italic = Italicx };
                            }
                            // Color del texto
                            var textBrushSerializeElement = doc.Descendants("TextBrushSerialize").FirstOrDefault();
                            string textBrushSerialize = textBrushSerializeElement != null ? textBrushSerializeElement.Value : null;

                            TextBrush = ConvertStringToBrush(textBrushSerialize);
                                                                                    

                            break;
                        }
                                                

                    }
                    catch (Exception ex)
                    {
                        Print("Error: " + ex.Message);
                    }
                }
            }

            //TryLoadFromTemplate(typeof(DrawingTool), "BOS_inf");
            //Print("-");

            ForceRefresh();
            EliminarBotones();
        }
        private Brush ConvertStringToBrush(string brushString) //Convierte String Brush serializado a Brush
        {
            using (var stringReader = new StringReader(brushString))
            using (var xmlReader = XmlReader.Create(stringReader))
            {
                return (Brush)XamlReader.Load(xmlReader);
            }
        }

        private ChartAnchor Anchor45(ChartAnchor starAnchort, ChartAnchor endAnchor, ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale)
        {
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                return endAnchor;

            Point startPoint = starAnchort.GetPoint(chartControl, chartPanel, chartScale);
            Point endPoint = endAnchor.GetPoint(chartControl, chartPanel, chartScale);

            double diffX = endPoint.X - startPoint.X;
            double diffY = endPoint.Y - startPoint.Y;

            double length = Math.Sqrt(diffX * diffX + diffY * diffY);

            double angle = Math.Atan2(diffY, diffX);

            double step = Math.PI / 8;
            double targetAngle = 0;

            if (angle > Math.PI - step || angle < -Math.PI + step) targetAngle = Math.PI;
            else if (angle > Math.PI - step * 3) targetAngle = Math.PI - step * 2;
            else if (angle > Math.PI - step * 5) targetAngle = Math.PI - step * 4;
            else if (angle > Math.PI - step * 7) targetAngle = Math.PI - step * 6;
            else if (angle < -Math.PI + step * 3) targetAngle = -Math.PI + step * 2;
            else if (angle < -Math.PI + step * 5) targetAngle = -Math.PI + step * 4;
            else if (angle < -Math.PI + step * 7) targetAngle = -Math.PI + step * 6;

            Point targetPoint = new Point(startPoint.X + Math.Cos(targetAngle) * length, startPoint.Y + Math.Sin(targetAngle) * length);
            ChartAnchor ret = new ChartAnchor();

            ret.UpdateFromPoint(targetPoint, chartControl, chartScale);

            if (startPoint.X == targetPoint.X)
            {
                ret.Time = starAnchort.Time;
                ret.SlotIndex = starAnchort.SlotIndex;
            }
            else if (startPoint.Y == targetPoint.Y)
                ret.Price = starAnchort.Price;

            return ret;
        }

        public override Cursor GetCursor(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, Point point)
        {
            switch (DrawingState)
            {
                case DrawingState.Building: return Cursors.Pen;
                case DrawingState.Moving: return IsLocked ? Cursors.No : Cursors.SizeAll;
                case DrawingState.Editing:
                    if (IsLocked)
                        return Cursors.No;
                    if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
                        return Cursors.SizeAll;
                    return editingAnchor == StartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
                default:
                    // draw move cursor if cursor is near line path anywhere
                    Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);

                    if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
                    {
                        // just go by single axis since we know the entire lines position
                        if (LineType == ChartLineType.VerticalLine && Math.Abs(point.X - startPoint.X) <= cursorSensitivity)
                            return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
                        if (LineType == ChartLineType.HorizontalLine && Math.Abs(point.Y - startPoint.Y) <= cursorSensitivity)
                            return IsLocked ? Cursors.Arrow : Cursors.SizeAll;
                        return null;
                    }

                    ChartAnchor closest = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);
                    if (closest != null)
                    {
                        if (IsLocked)
                            return Cursors.Arrow;
                        return closest == StartAnchor ? Cursors.SizeNESW : Cursors.SizeNWSE;
                    }

                    Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);
                    Point minPoint = startPoint;
                    Point maxPoint = endPoint;

                    // if we're an extended or ray line, we want to use min & max points in vector used for hit testing
                    if (LineType == ChartLineType.ExtendedLine)
                    {
                        // adjust vector to include min all the way to max points
                        minPoint = GetExtendedPoint(chartControl, chartPanel, chartScale, EndAnchor, StartAnchor);
                        maxPoint = GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);
                    }
                    else if (LineType == ChartLineType.Ray)
                        maxPoint = GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);

                    Vector totalVector = maxPoint - minPoint;
                    return MathHelper.IsPointAlongVector(point, minPoint, totalVector, cursorSensitivity) ?
                        IsLocked ? Cursors.Arrow : Cursors.SizeAll : null;
            }
        }

        public override IEnumerable<AlertConditionItem> GetAlertConditionItems()
        {
            yield return new AlertConditionItem
            {
                Name = Custom.Resource.NinjaScriptDrawingToolLine,
                ShouldOnlyDisplayName = true
            };
        }

        public sealed override Point[] GetSelectionPoints(ChartControl chartControl, ChartScale chartScale)
        {
            ChartPanel chartPanel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point startPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point endPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

            int totalWidth = chartPanel.W + chartPanel.X;
            int totalHeight = chartPanel.Y + chartPanel.H;

            if (LineType == ChartLineType.VerticalLine)
                return new[] { new Point(startPoint.X, chartPanel.Y), new Point(startPoint.X, chartPanel.Y + ((totalHeight - chartPanel.Y) / 2d)), new Point(startPoint.X, totalHeight) };
//          //if (LineType == ChartLineType.HorizontalLine)
                //return new[] { new Point(chartPanel.X, startPoint.Y), new Point(totalWidth / 2d, startPoint.Y), new Point(totalWidth, startPoint.Y) };

            //Vector strokeAdj = new Vector(Stroke.Width / 2, Stroke.Width / 2);
            Point midPoint = startPoint + ((endPoint - startPoint) / 2);
            return new[] { startPoint, midPoint, endPoint };
        }

        public override bool IsAlertConditionTrue(AlertConditionItem conditionItem, Condition condition, ChartAlertValue[] values, ChartControl chartControl, ChartScale chartScale)
        {
            if (values.Length < 1)
                return false;
            ChartPanel chartPanel = chartControl.ChartPanels[PanelIndex];
            // h line and v line have much more simple alert handling
            if (LineType == ChartLineType.HorizontalLine)
            {
                double barVal = values[0].Value;
                double lineVal = conditionItem.Offset.Calculate(StartAnchor.Price, AttachedTo.Instrument);

                switch (condition)
                {
                    case Condition.Equals: return barVal.ApproxCompare(lineVal) == 0;
                    case Condition.NotEqual: return barVal.ApproxCompare(lineVal) != 0;
                    case Condition.Greater: return barVal > lineVal;
                    case Condition.GreaterEqual: return barVal >= lineVal;
                    case Condition.Less: return barVal < lineVal;
                    case Condition.LessEqual: return barVal <= lineVal;
                    case Condition.CrossAbove:
                    case Condition.CrossBelow:
                        Predicate<ChartAlertValue> predicate = v =>
                        {
                            if (condition == Condition.CrossAbove)
                                return v.Value > lineVal;
                            return v.Value < lineVal;
                        };
                        return MathHelper.DidPredicateCross(values, predicate);
                }
                return false;
            }

            // get start / end points of what is absolutely shown for our vector
            Point lineStartPoint = StartAnchor.GetPoint(chartControl, chartPanel, chartScale);
            Point lineEndPoint = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

            if (LineType == ChartLineType.ExtendedLine || LineType == ChartLineType.Ray)
            {
                // need to adjust vector to rendered extensions
                Point maxPoint = GetExtendedPoint(chartControl, chartPanel, chartScale, StartAnchor, EndAnchor);
                if (LineType == ChartLineType.ExtendedLine)
                {
                    Point minPoint = GetExtendedPoint(chartControl, chartPanel, chartScale, EndAnchor, StartAnchor);
                    lineStartPoint = minPoint;
                }
                lineEndPoint = maxPoint;
            }

            double minLineX = double.MaxValue;
            double maxLineX = double.MinValue;

            foreach (Point point in new[] { lineStartPoint, lineEndPoint })
            {
                minLineX = Math.Min(minLineX, point.X);
                maxLineX = Math.Max(maxLineX, point.X);
            }

            // first thing, if our smallest x is greater than most recent bar, we have nothing to do yet.
            // do not try to check Y because lines could cross through stuff
            double firstBarX = values[0].ValueType == ChartAlertValueType.StaticValue ? minLineX : chartControl.GetXByTime(values[0].Time);
            double firstBarY = chartScale.GetYByValue(values[0].Value);

            // dont have to take extension into account as its already handled in min/max line x

            // bars completely passed our line
            if (maxLineX < firstBarX)
                return false;

            // bars not yet to our line
            if (minLineX > firstBarX)
                return false;

            // NOTE: normalize line points so the leftmost is passed first. Otherwise, our vector
            // math could end up having the line normal vector being backwards if user drew it backwards.
            // but we dont care the order of anchors, we want 'up' to mean 'up'!
            Point leftPoint = lineStartPoint.X < lineEndPoint.X ? lineStartPoint : lineEndPoint;
            Point rightPoint = lineEndPoint.X > lineStartPoint.X ? lineEndPoint : lineStartPoint;

            Point barPoint = new Point(firstBarX, firstBarY);
            // NOTE: 'left / right' is relative to if line was vertical. it can end up backwards too
            MathHelper.PointLineLocation pointLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, barPoint);
            // for vertical things, think of a vertical line rotated 90 degrees to lay flat, where it's normal vector is 'up'
            switch (condition)
            {
                case Condition.Greater: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove;
                case Condition.GreaterEqual: return pointLocation == MathHelper.PointLineLocation.LeftOrAbove || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.Less: return pointLocation == MathHelper.PointLineLocation.RightOrBelow;
                case Condition.LessEqual: return pointLocation == MathHelper.PointLineLocation.RightOrBelow || pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.Equals: return pointLocation == MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.NotEqual: return pointLocation != MathHelper.PointLineLocation.DirectlyOnLine;
                case Condition.CrossAbove:
                case Condition.CrossBelow:
                    Predicate<ChartAlertValue> predicate = v =>
                    {
                        double barX = chartControl.GetXByTime(v.Time);
                        double barY = chartScale.GetYByValue(v.Value);
                        Point stepBarPoint = new Point(barX, barY);
                        MathHelper.PointLineLocation ptLocation = MathHelper.GetPointLineLocation(leftPoint, rightPoint, stepBarPoint);
                        if (condition == Condition.CrossAbove)
                            return ptLocation == MathHelper.PointLineLocation.LeftOrAbove;
                        return ptLocation == MathHelper.PointLineLocation.RightOrBelow;
                    };
                    return MathHelper.DidPredicateCross(values, predicate);
            }

            return false;
        }

        public override bool IsVisibleOnChart(ChartControl chartControl, ChartScale chartScale, DateTime firstTimeOnChart, DateTime lastTimeOnChart)
        {
            if (DrawingState == DrawingState.Building)
                return true;

            DateTime minTime = Core.Globals.MaxDate;
            DateTime maxTime = Core.Globals.MinDate;

            if (LineType != ChartLineType.ExtendedLine && LineType != ChartLineType.Ray)
            {
                // make sure our 1 anchor is in time frame
                if (LineType == ChartLineType.VerticalLine)
                    return StartAnchor.Time >= firstTimeOnChart && StartAnchor.Time <= lastTimeOnChart;

                // check at least one of our anchors is in horizontal time frame
                foreach (ChartAnchor anchor in Anchors)
                {
                    if (anchor.Time < minTime)
                        minTime = anchor.Time;
                    if (anchor.Time > maxTime)
                        maxTime = anchor.Time;
                }
            }
            else
            {
                // extended line, rays: here we'll get extended point and see if they're on scale
                ChartPanel panel = chartControl.ChartPanels[PanelIndex];
                Point startPoint = StartAnchor.GetPoint(chartControl, panel, chartScale);

                Point minPoint = startPoint;
                Point maxPoint = GetExtendedPoint(chartControl, panel, chartScale, StartAnchor, EndAnchor);

                if (LineType == ChartLineType.ExtendedLine)
                    minPoint = GetExtendedPoint(chartControl, panel, chartScale, EndAnchor, StartAnchor);

                foreach (Point pt in new[] { minPoint, maxPoint })
                {
                    DateTime time = chartControl.GetTimeByX((int)pt.X);
                    if (time > maxTime)
                        maxTime = time;
                    if (time < minTime)
                        minTime = time;
                }
            }

            // check offscreen vertically. make sure to check the line doesnt cut through the scale, so check both are out
            if (LineType == ChartLineType.HorizontalLine && (StartAnchor.Price < chartScale.MinValue || StartAnchor.Price > chartScale.MaxValue) && !IsAutoScale)
                return false; // horizontal line only has one anchor to whiff

            // hline extends, but otherwise try to check if line horizontally crosses through visible chart times in some way
            if (LineType != ChartLineType.HorizontalLine && (minTime > lastTimeOnChart || maxTime < firstTimeOnChart))
                return false;

            return true;
        }

        public override void OnCalculateMinMax()
        {
            MinValue = double.MaxValue;
            MaxValue = double.MinValue;

            if (!IsVisible)
                return;

            // make sure to set good min/max values on single click lines as well, in case anchor left in editing
            if (LineType == ChartLineType.HorizontalLine)
                MinValue = MaxValue = Anchors.First().Price;
            else if (LineType != ChartLineType.VerticalLine)
            {
                // return min/max values only if something has been actually drawn
                if (Anchors.Any(a => !a.IsEditing))
                    foreach (ChartAnchor anchor in Anchors)
                    {
                        MinValue = Math.Min(anchor.Price, MinValue);
                        MaxValue = Math.Max(anchor.Price, MaxValue);
                    }
            }
        }

        public override void OnMouseDown(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            switch (DrawingState)
            {
                case DrawingState.Building:
                    if (StartAnchor.IsEditing)
                    {
                        dataPoint.CopyDataValues(StartAnchor);
                        StartAnchor.IsEditing = false;

                        // these lines only need one anchor, so stop editing end anchor too
                        if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
                            EndAnchor.IsEditing = false;

                        // give end anchor something to start with so we dont try to render it with bad values right away
                        dataPoint.CopyDataValues(EndAnchor);
                    }
                    else if (EndAnchor.IsEditing)
                    {
                        dataPoint.CopyDataValues(EndAnchor);
                        EndAnchor.IsEditing = false;
                    }

                    // is initial building done (both anchors set)
                    if (!StartAnchor.IsEditing && !EndAnchor.IsEditing)
                    {
                        DrawingState = DrawingState.Normal;
                        IsSelected = false;
                        
                        // Asegurarse de que el botón se añade una sola vez
                        if (!isButtonAdded && chartControl != null)
                        {
                            CreateButtons();

                            if (listaBotones != null && listaBotones.Count > 0)
                            {
                                chartControl.Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {

                                        var chartWindow = System.Windows.Window.GetWindow(chartControl);
                                        var mainGrid = chartWindow.Content as Grid;

                                        if (mainGrid != null)
                                        {
                                            // Creamos un StackPanel para contener los botones.
                                            buttonPanel = new StackPanel
                                            {
                                                Orientation = Orientation.Vertical
                                            };

                                            // Limpiamos cualquier botón previo para evitar duplicados y conflictos de posición.
                                            foreach (var button in listaBotones)
                                            {
                                                if (mainGrid.Children.Contains(button))
                                                {
                                                    mainGrid.Children.Remove(button);
                                                }
                                            }

                                            // Añadimos los botones al StackPanel.
                                            for (int i = 0; i < listaBotones.Count; i++)
                                            {
                                                buttonPanel.Children.Add(listaBotones[i]);
                                            }
                                            Point puntoFinLinea = EndAnchor.GetPoint(chartControl, chartPanel, chartScale);

                                            // Obtener los factores de DPI
                                            PresentationSource source = PresentationSource.FromVisual(chartControl);
                                            double dpiX = 96.0, dpiY = 96.0;
                                            if (source != null)
                                            {
                                                dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
                                                dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;
                                            }

                                            // Convertir las coordenadas del punto final a DIPs
                                            double margenIzquierdoX = puntoFinLinea.X * (96.0 / dpiX); // Ajusta este valor según sea necesario para posicionar el botón
                                            double margenSuperiorY = puntoFinLinea.Y * (96.0 / dpiY); // Ajusta este valor según sea necesario para posicionar el botón

                                            margenIzquierdoX -= chartPanel.ActualWidth - margenIzquierdoX - anchuraBotones + 20;


                                            if (margenIzquierdoX + anchuraBotones + 20 > chartPanel.ActualWidth)
                                            {
                                                margenIzquierdoX = chartPanel.ActualWidth - anchuraBotones * 2;
                                            }
                                            if (margenSuperiorY + alturaBotones * listaBotones.Count > chartPanel.ActualHeight)
                                            {
                                                margenSuperiorY = chartPanel.ActualHeight - alturaBotones * listaBotones.Count - 20;
                                            }


                                            // Debugging
                                            //Print(string.Format("Linea. Punto final: {0} | Margen Izquierdo X: {1} | Margen Superior Y: {2} | ChartPanel Actual Width: {3} | ChartPanel Actual Height: {4}",
                                               //puntoFinLinea, margenIzquierdoX, margenSuperiorY, chartPanel.ActualWidth, chartPanel.ActualHeight));

                                            buttonPanel.Margin = new Thickness(margenIzquierdoX, margenSuperiorY, 0, 0);

                                            // Añadimos el StackPanel al Grid principal si no está ya presente.
                                            if (!mainGrid.Children.Contains(buttonPanel))
                                            {
                                                mainGrid.Children.Add(buttonPanel);
                                            }

                                            isButtonAdded = true;
                                        }
                                        else
                                        {
                                            Print("No se pudo obtener el Grid del chart window.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Print("Error al modificar la ventana del gráfico: " + ex.Message);
                                    }
                                });
                            }
                        }

                        
                    }
                    break;
                case DrawingState.Normal:
                    Point point = dataPoint.GetPoint(chartControl, chartPanel, chartScale);
                    // see if they clicked near a point to edit, if so start editing
                    if (LineType == ChartLineType.HorizontalLine || LineType == ChartLineType.VerticalLine)
                    {
                        if (GetCursor(chartControl, chartPanel, chartScale, point) == null)
                            IsSelected = false;
                        else
                        {
                            // we dont care here, since we're moving just one anchor
                            editingAnchor = StartAnchor;
                        }
                    }
                    else
                        editingAnchor = GetClosestAnchor(chartControl, chartPanel, chartScale, cursorSensitivity, point);

                    if (editingAnchor != null)
                    {
                        editingAnchor.IsEditing = true;
                        DrawingState = DrawingState.Editing;                       
                    }
                    else
                    {
                        if (GetCursor(chartControl, chartPanel, chartScale, point) != null)
                            DrawingState = DrawingState.Moving;
                        else
                            // user whiffed.
                            IsSelected = false;
                    }
                    break;
            }
        }

        public override void OnMouseMove(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            if (IsLocked && DrawingState != DrawingState.Building)
                return;

            IgnoresSnapping = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            if (DrawingState == DrawingState.Building)
            {
                // start anchor will not be editing here because we start building as soon as user clicks, which
                // plops down a start anchor right away
                if (EndAnchor.IsEditing)
                    Anchor45(StartAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(EndAnchor);
            }
            else if (DrawingState == DrawingState.Editing && editingAnchor != null)
            {
                // if its a line with two anchors, update both x/y at once
                if (LineType != ChartLineType.HorizontalLine && LineType != ChartLineType.VerticalLine)
                {
                    ChartAnchor startAnchor = editingAnchor == StartAnchor ? EndAnchor : StartAnchor;
                    Anchor45(startAnchor, dataPoint, chartControl, chartPanel, chartScale).CopyDataValues(editingAnchor);
                }
                else if (LineType != ChartLineType.VerticalLine)  //Linea horizontal
                {
                    // Al arrastrar, simplemente actualizamos StartAnchor manteniendo la diferencia inicial
                    StartAnchor.Time = dataPoint.Time.Add((StartAnchor.Time - dataPoint.Time));

                    // Aquí también podrías ajustar el precio (Y) si es necesario
                    StartAnchor.Price = dataPoint.Price; // si deseas mantener la misma diferencia de precio

                    if (!isDragging)
                    {
                        // Calculamos la diferencia de tiempo inicial solo una vez, cuando empieza el arrastre
                        initialTimeDifference = StartAnchor.Time - dataPoint.Time;
                        initialDataPointTime = dataPoint.Time; // guardamos el tiempo del punto donde se hizo clic
                        isDragging = true;
                    }
                    // Actualizamos StartAnchor manteniendo la diferencia inicial
                    StartAnchor.Time = dataPoint.Time.Add(initialTimeDifference);
                    EndAnchor = editingAnchor;  
                }
                else
                {
                    // vertical line only needs X value updated
                    editingAnchor.Time = dataPoint.Time;
                    editingAnchor.SlotIndex = dataPoint.SlotIndex;
                }
            }
            else if (DrawingState == DrawingState.Moving)
                foreach (ChartAnchor anchor in Anchors)
                    // only move anchor values as needed depending on line type
                    if (LineType == ChartLineType.HorizontalLine)
                    {
                        anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
                        initialTimeDifference = StartAnchor.Time - dataPoint.Time;
                    }
                        //anchor.MoveAnchorTime(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this); //*****
                    else if (LineType == ChartLineType.VerticalLine)
                        anchor.MoveAnchorTime(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
                    else
                        anchor.MoveAnchor(InitialMouseDownAnchor, dataPoint, chartControl, chartPanel, chartScale, this);
            //lastMouseMovePoint.Value, point, chartControl, chartScale);
        }

        public override void OnMouseUp(ChartControl chartControl, ChartPanel chartPanel, ChartScale chartScale, ChartAnchor dataPoint)
        {
            isDragging = false;
            // simply end whatever moving
            if (DrawingState == DrawingState.Moving || DrawingState == DrawingState.Editing)
                DrawingState = DrawingState.Normal;
            if (editingAnchor != null)
                editingAnchor.IsEditing = false;
            editingAnchor = null;
        }

        public override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
            if (Stroke == null)
                return;

            chartControlx = chartControl;
            
            Stroke.RenderTarget = RenderTarget;

            SharpDX.Direct2D1.AntialiasMode oldAntiAliasMode = RenderTarget.AntialiasMode;
            RenderTarget.AntialiasMode = SharpDX.Direct2D1.AntialiasMode.PerPrimitive;
            ChartPanel panel = chartControl.ChartPanels[chartScale.PanelIndex];
            Point startPoint = StartAnchor.GetPoint(chartControl, panel, chartScale);
            Point endPoint = EndAnchor.GetPoint(chartControl, panel, chartScale);


            // align to full pixel to avoid unneeded aliasing
            double strokePixAdj = ((double)(Stroke.Width % 2)).ApproxCompare(0) == 0 ? 0.5d : 0d;
            Vector pixelAdjustVec = new Vector(strokePixAdj, strokePixAdj);


            // convert our start / end pixel points to directx 2d vectors
            Point endPointAdjusted = endPoint + pixelAdjustVec;
            SharpDX.Vector2 endVec = endPointAdjusted.ToVector2();
            Point startPointAdjusted = startPoint + pixelAdjustVec;
            SharpDX.Vector2 startVec = startPointAdjusted.ToVector2();
            SharpDX.Direct2D1.Brush tmpBrush = IsInHitTest ? chartControl.SelectionBrush : Stroke.BrushDX;

            if (LineType != ChartLineType.HorizontalLine)
            {
                RenderTarget.DrawLine(startVec, endVec, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
            }
            else
            {                
                Point finalPantalla = new Point(panel.X + panel.W, startPoint.Y);
                endPointAdjusted = finalPantalla + pixelAdjustVec;
                endVec = endPointAdjusted.ToVector2();
                if (posText != PosicionTextoEnum.BordeDerechoEntreLinea) 
                {
                    RenderTarget.DrawLine(startVec, endVec, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
                }
                else if (posText == PosicionTextoEnum.BordeDerechoEntreLinea)
                {
                    if (string.IsNullOrEmpty(text)) // Caso especial: sin texto
                    {
                        RenderTarget.DrawLine(startVec, endVec, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
                    }                    
                }
            }

            //////////////////////////////////////// Mod Texto ////////////////////////////////////////////////


            if (text != null && text != "")
            {
                SharpDX.Direct2D1.Brush textBrushDx;
                textBrushDx = textBrush.ToDxBrush(RenderTarget);
                NinjaTrader.Gui.Tools.SimpleFont simpleFont = chartControl.Properties.LabelFont ?? new NinjaTrader.Gui.Tools.SimpleFont("Arial", 16);
                SharpDX.DirectWrite.TextFormat textFormat1 = simpleFont.ToDirectWriteTextFormat();
                SharpDX.DirectWrite.TextFormat textFormat2 = Font.ToDirectWriteTextFormat();
                SharpDX.DirectWrite.TextLayout textLayout = new SharpDX.DirectWrite.TextLayout(NinjaTrader.Core.Globals.DirectWriteFactory, text, textFormat2, 400, textFormat1.FontSize);
                textFormat1.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;
                textFormat2.TextAlignment = SharpDX.DirectWrite.TextAlignment.Center;


                SharpDX.Vector2 delta = new SharpDX.Vector2(endVec.X - startVec.X, endVec.Y - startVec.Y);
                double angle = Math.Atan2(delta.Y, delta.X); //ángulo para rotar el texto de manera que sea perpendicular a la línea.
                // Calcular punto base de acuerdo a la posición del texto


                SharpDX.Vector2 normalVector; // Vector perpendicular a la línea
                if (TextoEncimaDeLinea)
                {
                    normalVector = new SharpDX.Vector2(delta.Y, -delta.X); // Vector de desplazamiento
                }
                else
                {
                    normalVector = new SharpDX.Vector2(-delta.Y, delta.X);
                }
                normalVector = SharpDX.Vector2.Normalize(normalVector);


                SharpDX.Vector2 textPosition;
                SharpDX.Vector2 adjustedTextPosition;

                if (posText == PosicionTextoEnum.BordeIzquierdo)
                {
                    textPosition = new SharpDX.Vector2(startVec.X, startVec.Y);
                    SharpDX.Vector2 offset = normalVector * SeparacionTextoLinea;
                    textPosition = textPosition + offset;
                    adjustedTextPosition = new SharpDX.Vector2(textPosition.X, textPosition.Y);
                }
                else
                {
                    if (posText == PosicionTextoEnum.BordeDerecho)
                    {
                        textPosition = new SharpDX.Vector2(endVec.X, endVec.Y);
                        SharpDX.Vector2 offset = normalVector * SeparacionTextoLinea;
                        textPosition = textPosition + offset;
                        adjustedTextPosition = new SharpDX.Vector2(textPosition.X - textLayout.Metrics.Width, textPosition.Y);
                    }
                    else
                    {
                        if (posText == PosicionTextoEnum.Centrado)
                        {
                            // Calcular punto medio de la línea
                            SharpDX.Vector2 midPoint = new SharpDX.Vector2((startVec.X + endVec.X) / 2, (startVec.Y + endVec.Y) / 2);

                            // Calcular ángulo y rotación
                            delta = new SharpDX.Vector2(endVec.X - startVec.X, endVec.Y - startVec.Y);
                            angle = Math.Atan2(delta.Y, delta.X); // Ángulo para rotar el texto de manera que sea perpendicular a la línea.                 
                            SharpDX.Vector2 offset = normalVector * SeparacionTextoLinea;
                            textPosition = midPoint + offset;
                            adjustedTextPosition = new SharpDX.Vector2(textPosition.X - textLayout.Metrics.Width / 2, textPosition.Y);
                        }
                        else //DerechoEnMedioDelaLinea
                        {
                            textPosition = new SharpDX.Vector2(endVec.X, endVec.Y);
                            SharpDX.Vector2 offset = normalVector * SeparacionTextoLinea;
                            adjustedTextPosition = new SharpDX.Vector2(textPosition.X - textLayout.Metrics.Width - 5, textPosition.Y - textLayout.Metrics.Height/2);

                            SharpDX.Vector2 lineStartPrimerTramo = new SharpDX.Vector2(startVec.X, endVec.Y);
                            SharpDX.Vector2 lineEndPrimerTramo = new SharpDX.Vector2(textPosition.X - textLayout.Metrics.Width - 7, endVec.Y);
                            SharpDX.Vector2 lineStartSegTramo = new SharpDX.Vector2(textPosition.X - 4, endVec.Y);
                            SharpDX.Vector2 lineEndSegTramo = new SharpDX.Vector2(endVec.X, endVec.Y);
                            RenderTarget.DrawLine(lineStartPrimerTramo, lineEndPrimerTramo, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
                            RenderTarget.DrawLine(lineStartSegTramo, lineEndSegTramo, tmpBrush, Stroke.Width, Stroke.StrokeStyle);
                        }
                    }
                }
                // Rotate and draw text

                RenderTarget.Transform = Matrix3x2.Rotation((float)angle, textPosition); ;
                RenderTarget.DrawTextLayout(adjustedTextPosition, textLayout, textBrushDx, SharpDX.Direct2D1.DrawTextOptions.NoSnap);
                RenderTarget.Transform = Matrix3x2.Identity; // Reset transformation

                // Cleanup
                textFormat1.Dispose();
                textFormat2.Dispose();
                textLayout.Dispose();
                textBrushDx.Dispose();
                // Rotate the RenderTarget back
                RenderTarget.Transform = Matrix3x2.Identity;
            }
        }
    }

    public static partial class Draw
    {
        private static T DrawLineTypeCoreModRay<T>(NinjaScriptBase owner, bool isAutoScale, string tag,
                                        int startBarsAgo, DateTime startTime, double startY, int endBarsAgo, DateTime endTime, double endY,
                                        Brush brush, DashStyleHelper dashStyle, int width, bool isGlobal, string templateName) where T : RayPro
        {
            if (owner == null)
                throw new ArgumentException("owner");

            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException(@"tag cant be null or empty", "tag");

            if (isGlobal && tag[0] != GlobalDrawingToolManager.GlobalDrawingToolTagPrefix)
                tag = string.Format("{0}{1}", GlobalDrawingToolManager.GlobalDrawingToolTagPrefix, tag);

            T lineT = DrawingTool.GetByTagOrNew(owner, typeof(T), tag, templateName) as T;

            if (lineT == null)
                return null;
                       
            DrawingTool.SetDrawingToolCommonValues(lineT, tag, isAutoScale, owner, isGlobal);

            // dont nuke existing anchor refs on the instance
            ChartAnchor startAnchor;

            // check if its one of the single anchor lines
            if (lineT is HorizontalLine || lineT is VerticalLine)
            {
                startAnchor = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
                startAnchor.CopyDataValues(lineT.StartAnchor);
            }
            else
            {
                startAnchor = DrawingTool.CreateChartAnchor(owner, startBarsAgo, startTime, startY);
                ChartAnchor endAnchor = DrawingTool.CreateChartAnchor(owner, endBarsAgo, endTime, endY);
                startAnchor.CopyDataValues(lineT.StartAnchor);
                endAnchor.CopyDataValues(lineT.EndAnchor);
            }

            if (brush != null)
                lineT.Stroke = new Stroke(brush, dashStyle, width) { RenderTarget = lineT.Stroke.RenderTarget };

            lineT.SetState(State.Active);
            return lineT;
        }
        // line overloads

        public static RayPro RayPro(NinjaScriptBase owner, string tag, bool isAutoScale, int startBarsAgo, double startY, int endBarsAgo,
           double endY, bool isGlobal, string templateName)
        {
            return DrawLineTypeCoreModRay<RayPro>(owner, isAutoScale, tag, startBarsAgo, Core.Globals.MinDate, startY, endBarsAgo, Core.Globals.MinDate, endY,
                null, DashStyleHelper.Solid, 0, isGlobal, templateName);
        }
        
    }
}
