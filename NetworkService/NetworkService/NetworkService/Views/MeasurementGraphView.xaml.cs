using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NetworkService.Model;
using NetworkService.ViewModel;

namespace NetworkService.Views
{
    /// <summary>
    /// Interaction logic for MeasurementGraphView.xaml
    /// G2 grafikon (barovi po vremenu, obe ose oznacene) i CG4 grafikon raspodele
    /// tipova crtaju se programski na Canvas-u (bez gotovih Chart kontrola).
    /// </summary>
    public partial class MeasurementGraphView : UserControl
    {
        private MeasurementGraphViewModel _vm;

        private static readonly Brush ValidBrush = new SolidColorBrush(Color.FromRgb(46, 160, 90));
        private static readonly Brush InvalidBrush = new SolidColorBrush(Color.FromRgb(214, 69, 69));
        private static readonly Brush AxisBrush = new SolidColorBrush(Color.FromRgb(110, 120, 135));
        private static readonly Brush GridBrush = new SolidColorBrush(Color.FromRgb(225, 230, 238));

        public MeasurementGraphView()
        {
            InitializeComponent();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
            {
                _vm.RedrawRequested -= Redraw;
            }

            _vm = DataContext as MeasurementGraphViewModel;
            if (_vm != null)
            {
                _vm.RedrawRequested += Redraw;
            }

            Dispatcher.BeginInvoke(new Action(Redraw), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => Redraw();
        private void OnGraphSizeChanged(object sender, SizeChangedEventArgs e) => Redraw();

        private void Redraw()
        {
            DrawBarChart();
            DrawDistribution();
        }

        // ===================== G2: BAROVI PO VREMENU =====================
        private void DrawBarChart()
        {
            if (_vm == null || GraphCanvas == null)
            {
                return;
            }

            GraphCanvas.Children.Clear();

            double width = GraphCanvas.ActualWidth;
            double height = GraphCanvas.ActualHeight;
            if (width < 40 || height < 40)
            {
                return;
            }

            // Margine za ose i oznake.
            const double leftMargin = 50;
            const double bottomMargin = 36;
            const double topMargin = 16;
            double plotWidth = width - leftMargin - 16;
            double plotHeight = height - topMargin - bottomMargin;

            // Y-osa i X-osa.
            DrawLine(leftMargin, topMargin, leftMargin, topMargin + plotHeight, AxisBrush, 1.5);
            DrawLine(leftMargin, topMargin + plotHeight, leftMargin + plotWidth, topMargin + plotHeight, AxisBrush, 1.5);

            // Naslovi osa (G2 zahteva da obe ose budu oznacene).
            AddText("Broj vozila", leftMargin - 44, topMargin - 2, 11, AxisBrush, true);
            AddText("Vreme", leftMargin + plotWidth - 36, topMargin + plotHeight + 18, 11, AxisBrush);

            List<MeasurementRecord> data = _vm.CurrentMeasurements.ToList();
            if (data.Count == 0)
            {
                AddText("Nema podataka za izabrani entitet.", leftMargin + 20, topMargin + plotHeight / 2, 13, AxisBrush);
                return;
            }

            double maxValid = _vm.MaxValidForSelected;
            double maxValue = Math.Max(data.Max(d => d.Value), maxValid);
            if (maxValue <= 0)
            {
                maxValue = 1;
            }
            maxValue *= 1.1; // malo prostora iznad najviseg bara

            // Y-osa: podeoci sa vrednostima.
            for (int i = 0; i <= 4; i++)
            {
                double frac = i / 4.0;
                double y = topMargin + plotHeight - frac * plotHeight;
                double value = frac * maxValue;
                DrawLine(leftMargin, y, leftMargin + plotWidth, y, GridBrush, 1);
                AddText(value.ToString("0", CultureInfo.InvariantCulture), 6, y - 8, 10, AxisBrush);
            }

            // Granica validnosti (npr. 15000 za IA, 7000 za IB).
            if (maxValid > 0 && maxValid <= maxValue)
            {
                double yLimit = topMargin + plotHeight - (maxValid / maxValue) * plotHeight;
                var limit = new Line
                {
                    X1 = leftMargin, Y1 = yLimit, X2 = leftMargin + plotWidth, Y2 = yLimit,
                    Stroke = new SolidColorBrush(Color.FromRgb(136, 136, 136)),
                    StrokeThickness = 1.4,
                    StrokeDashArray = new DoubleCollection { 4, 3 }
                };
                GraphCanvas.Children.Add(limit);
            }

            // Barovi.
            int count = data.Count;
            double slot = plotWidth / count;
            double barWidth = Math.Min(slot * 0.5, 60);

            for (int i = 0; i < count; i++)
            {
                MeasurementRecord record = data[i];
                double barHeight = (record.Value / maxValue) * plotHeight;
                double x = leftMargin + i * slot + (slot - barWidth) / 2;
                double y = topMargin + plotHeight - barHeight;

                var bar = new Rectangle
                {
                    Width = barWidth,
                    Height = Math.Max(barHeight, 1),
                    Fill = record.IsValid ? ValidBrush : InvalidBrush,
                    RadiusX = 3,
                    RadiusY = 3
                };
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, y);
                GraphCanvas.Children.Add(bar);

                // Vrednost iznad bara.
                AddText(record.Value.ToString("0", CultureInfo.InvariantCulture),
                    x, y - 16, 11, record.IsValid ? ValidBrush : InvalidBrush, true);

                // Vremenska oznaka na X-osi.
                AddText(record.TimeLabel, leftMargin + i * slot + 2, topMargin + plotHeight + 4, 10, AxisBrush);
            }
        }

        // ===================== CG4: RASPODELA TIPOVA =====================
        private void DrawDistribution()
        {
            if (_vm == null || DistributionCanvas == null)
            {
                return;
            }

            DistributionCanvas.Children.Clear();

            double width = DistributionCanvas.ActualWidth;
            double height = DistributionCanvas.ActualHeight;
            if (width < 40 || height < 10)
            {
                return;
            }

            var shares = _vm.TypeDistribution.Where(s => s.Count > 0).ToList();
            if (shares.Count == 0)
            {
                AddText("Nema entiteta.", 8, height / 2 - 8, 12, AxisBrush, false, DistributionCanvas);
                return;
            }

            double x = 0;
            int colorIndex = 0;
            Brush[] palette =
            {
                new SolidColorBrush(Color.FromRgb(54, 126, 210)),
                new SolidColorBrush(Color.FromRgb(127, 178, 232)),
                new SolidColorBrush(Color.FromRgb(46, 160, 90))
            };

            foreach (TypeShare share in shares)
            {
                double segWidth = width * (share.Percentage / 100.0);
                var rect = new Rectangle
                {
                    Width = Math.Max(segWidth, 1),
                    Height = height,
                    Fill = palette[colorIndex % palette.Length]
                };
                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, 0);
                DistributionCanvas.Children.Add(rect);

                if (segWidth > 40)
                {
                    AddText(share.TypeName + " " + share.Percentage.ToString("0") + "%",
                        x + 6, height / 2 - 9, 12, Brushes.White, true, DistributionCanvas);
                }

                x += segWidth;
                colorIndex++;
            }
        }

        // ===================== POMOCNE METODE ZA CRTANJE =====================
        private void DrawLine(double x1, double y1, double x2, double y2, Brush brush, double thickness)
        {
            GraphCanvas.Children.Add(new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = brush, StrokeThickness = thickness
            });
        }

        private void AddText(string text, double x, double y, double size, Brush brush, bool bold = false, Canvas target = null)
        {
            var block = new TextBlock
            {
                Text = text,
                FontSize = size,
                Foreground = brush,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal
            };
            Canvas.SetLeft(block, x);
            Canvas.SetTop(block, y);
            (target ?? GraphCanvas).Children.Add(block);
        }
    }
}
