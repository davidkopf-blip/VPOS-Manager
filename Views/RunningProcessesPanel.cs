using System;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Text;
using DumpLoader_2._0.ViewModels;

namespace DumpLoader_2._0.Views
{
    public sealed partial class RunningProcessesPanel : UserControl
    {
        private StackPanel _itemsHost = new StackPanel();
        private TextBlock _emptyText;
        private MainViewModel? _vm;

        public RunningProcessesPanel()
        {
            var border = new Border
            {
                Background = (SolidColorBrush)Application.Current.Resources["CardBackground"],
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(16),
                Margin = new Thickness(12, 0, 0, 0)
            };

            var root = new StackPanel();

            var header = new TextBlock
            {
                Text = "Running VPOS Instances",
                Foreground = (SolidColorBrush)Application.Current.Resources["PrimaryText"],
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            root.Children.Add(header);

            _itemsHost = new StackPanel();
            root.Children.Add(_itemsHost);

            _emptyText = new TextBlock
            {
                Text = "No VPOS instances currently running.",
                Foreground = (SolidColorBrush)Application.Current.Resources["SecondaryText"],
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 24, 0, 0)
            };
            root.Children.Add(_emptyText);

            border.Child = root;
            this.Content = border;

            this.DataContextChanged += RunningProcessesPanel_DataContextChanged;
        }

        private void RunningProcessesPanel_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (_vm != null)
            {
                if (_vm.TrackedProcesses is INotifyCollectionChanged oldColl)
                {
                    oldColl.CollectionChanged -= TrackedProcesses_CollectionChanged;
                }
            }

            _vm = args.NewValue as MainViewModel;

            _itemsHost.Children.Clear();

            if (_vm == null)
            {
                UpdateEmptyState();
                return;
            }

            if (_vm.TrackedProcesses is INotifyCollectionChanged coll)
            {
                coll.CollectionChanged += TrackedProcesses_CollectionChanged;
            }

            foreach (var t in _vm.TrackedProcesses)
            {
                AddCard(t);
            }

            UpdateEmptyState();
        }

        private void TrackedProcesses_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TrackedProcessViewModel t in e.NewItems!)
                    AddCard(t);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (TrackedProcessViewModel t in e.OldItems!)
                    RemoveCard(t);
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _itemsHost.Children.Clear();
            }

            UpdateEmptyState();
        }

        private void AddCard(TrackedProcessViewModel t)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.DarkGray) { Opacity = 0.17 },
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var sp = new StackPanel();

            var tbVersion = new TextBlock { Text = t.Version, Foreground = (Brush)Application.Current.Resources["PrimaryText"], FontWeight = Microsoft.UI.Text.FontWeights.SemiBold };
            sp.Children.Add(tbVersion);

            var tbPid = new TextBlock { Text = $"PID: {t.ProcessId}", Foreground = (Brush)Application.Current.Resources["SecondaryText"] };
            sp.Children.Add(tbPid);

            var tbStarted = new TextBlock { Text = $"Started: {t.StartTime:T}", Foreground = (Brush)Application.Current.Resources["SecondaryText"] };
            sp.Children.Add(tbStarted);

            var tbStatus = new TextBlock { Text = $"Status: {t.Status}", Foreground = (Brush)Application.Current.Resources["SecondaryText"] };
            sp.Children.Add(tbStatus);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

            var btnFront = new Button { Content = "Bring To Front", Style = (Style)Application.Current.Resources["SecondaryButtonStyle"] };
            btnFront.Click += (_, _) => { _ = t.BringToFrontCommand.ExecuteAsync(null); };
            btnPanel.Children.Add(btnFront);

            var btnStop = new Button { Content = "Stop", Style = (Style)Application.Current.Resources["DangerButtonStyle"] };
            btnStop.Click += async (_, _) => { await t.StopProcessCommand.ExecuteAsync(null); };
            btnPanel.Children.Add(btnStop);

            sp.Children.Add(btnPanel);

            border.Child = sp;



            // store reference on the viewmodel (using Tag) to find later
            border.Tag = t;

            // Note: do NOT subscribe to t.RequestRemove here. TrackedProcessViewModel raises it
            // from Process.Exited, which fires on a thread-pool thread, so touching _itemsHost
            // (a UI element) directly from that handler throws RPC_E_WRONG_THREAD. Removal is
            // already handled safely via TrackedProcesses_CollectionChanged below, which only
            // reacts to changes MainViewModel makes after dispatching to the UI thread.

            _itemsHost.Children.Add(border);
        }

        private void RemoveCard(TrackedProcessViewModel t)
        {
            var item = _itemsHost.Children.OfType<Border>().FirstOrDefault(b => ReferenceEquals(b.Tag, t));
            if (item != null)
                _itemsHost.Children.Remove(item);

            UpdateEmptyState();
        }

        private void UpdateEmptyState()
        {
            _emptyText.Visibility = _itemsHost.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
