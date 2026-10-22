using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using NetworkService.Model;
using NetworkService.ViewModel;

namespace NetworkService.Views
{
    /// <summary>
    /// Interaction logic for NetworkDisplayView.xaml
    /// Drag&Drop i iscrtavanje linija su prirodno deo View sloja, pa se obavljaju
    /// u code-behind-u, dok se SVE stanje (raspored, veze) cuva u ViewModel-u.
    /// </summary>
    public partial class NetworkDisplayView : UserControl
    {
        private NetworkDisplayViewModel _vm;
        private Point _dragStart;
        private Entity _draggedEntity;

        public NetworkDisplayView()
        {
            InitializeComponent();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Otkacimo se sa starog VM-a i zakacimo na novi (zbog ponovnog iscrtavanja linija).
            if (_vm != null)
            {
                _vm.RedrawRequested -= RedrawLines;
            }

            _vm = DataContext as NetworkDisplayViewModel;
            if (_vm != null)
            {
                _vm.RedrawRequested += RedrawLines;
            }

            Dispatcher.BeginInvoke(new System.Action(RedrawLines), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RedrawLines();
        }

        private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawLines();
        }

        // ===================== DRAG IZ TREEVIEW-A =====================
        private void Tree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStart = e.GetPosition(null);
            _draggedEntity = (e.OriginalSource as FrameworkElement)?.DataContext as Entity;
        }

        private void Tree_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedEntity == null)
            {
                return;
            }

            Vector diff = _dragStart - e.GetPosition(null);
            if (System.Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                System.Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // Drag iz TreeView-a: izvor nije sa mreze (FromSlot = -1).
                var data = new DataObject(typeof(DragPayload), new DragPayload(_draggedEntity, -1));
                DragDrop.DoDragDrop(EntitiesTree, data, DragDropEffects.Move);
                _draggedEntity = null;
            }
        }

        // ===================== DRAG SA POLJA NA POLJE (premestanje) =====================
        private void Slot_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _vm == null || _vm.IsConnectMode)
            {
                return; // u rezimu povezivanja ne premestamo prevlacenjem
            }

            var slot = (sender as FrameworkElement)?.DataContext as DisplaySlot;
            if (slot == null || !slot.IsOccupied)
            {
                return;
            }

            var data = new DataObject(typeof(DragPayload), new DragPayload(slot.Entity, slot.Index));
            DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
        }

        private void Slot_DragOver(object sender, DragEventArgs e)
        {
            var slot = (sender as FrameworkElement)?.DataContext as DisplaySlot;
            // Dozvoljen drop samo na prazno polje.
            e.Effects = (slot != null && slot.IsEmpty && e.Data.GetDataPresent(typeof(DragPayload)))
                ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void Slot_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(DragPayload)) || _vm == null)
            {
                return;
            }

            var payload = (DragPayload)e.Data.GetData(typeof(DragPayload));
            var targetSlot = (sender as FrameworkElement)?.DataContext as DisplaySlot;
            if (targetSlot == null)
            {
                return;
            }

            if (payload.FromSlotIndex >= 0)
            {
                _vm.MoveEntity(payload.FromSlotIndex, targetSlot.Index); // premestanje na mrezi
            }
            else
            {
                _vm.PlaceEntity(payload.Entity, targetSlot.Index);        // postavljanje iz TreeView-a
            }
        }

        // ===================== KLIK (povezivanje) i DVOKLIK (uklanjanje) =====================
        private void Slot_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_vm == null || !_vm.IsConnectMode)
            {
                return;
            }

            var slot = (sender as FrameworkElement)?.DataContext as DisplaySlot;
            if (slot != null && slot.IsOccupied)
            {
                _vm.HandleConnectClick(slot.Entity);
            }
        }

        private void Slot_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Border nema MouseDoubleClick (to ima samo Control), pa dupli klik
            // detektujemo preko ClickCount na MouseLeftButtonDown dogadjaju.
            if (_vm == null || _vm.IsConnectMode || e.ClickCount < 2)
            {
                return;
            }

            var slot = (sender as FrameworkElement)?.DataContext as DisplaySlot;
            if (slot != null && slot.IsOccupied)
            {
                _vm.RemoveFromGrid(slot.Index); // vraca entitet u TreeView
            }
        }

        // ===================== ISCRTAVANJE LINIJA =====================
        private void RedrawLines()
        {
            if (_vm == null || LinesCanvas == null || SlotsHost == null)
            {
                return;
            }

            LinesCanvas.Children.Clear();

            foreach (Connection connection in _vm.Connections)
            {
                Border first = FindBorderForEntity(connection.First);
                Border second = FindBorderForEntity(connection.Second);
                if (first == null || second == null)
                {
                    continue;
                }

                Point p1 = CenterOf(first);
                Point p2 = CenterOf(second);

                var line = new Line
                {
                    X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y,
                    StrokeThickness = 3,
                    Stroke = new SolidColorBrush(Color.FromRgb(54, 126, 210)),
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                LinesCanvas.Children.Add(line);
            }
        }

        private Point CenterOf(Border border)
        {
            GeneralTransform transform = border.TransformToVisual(LinesCanvas);
            return transform.Transform(new Point(border.ActualWidth / 2, border.ActualHeight / 2));
        }

        // Pronalazi Border (polje) koji trenutno prikazuje dati entitet.
        private Border FindBorderForEntity(Entity entity)
        {
            DisplaySlot slot = _vm.Slots.FirstOrDefault(s => s.Entity == entity);
            if (slot == null)
            {
                return null;
            }

            var presenter = SlotsHost.ItemContainerGenerator.ContainerFromItem(slot) as FrameworkElement;
            if (presenter == null)
            {
                return null;
            }

            return FindVisualChild<Border>(presenter);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                {
                    return typed;
                }
                T result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        // Pomocna klasa za prenos podataka tokom Drag&Drop-a.
        private class DragPayload
        {
            public Entity Entity { get; }
            public int FromSlotIndex { get; } // -1 ako dolazi iz TreeView-a

            public DragPayload(Entity entity, int fromSlotIndex)
            {
                Entity = entity;
                FromSlotIndex = fromSlotIndex;
            }
        }
    }
}
