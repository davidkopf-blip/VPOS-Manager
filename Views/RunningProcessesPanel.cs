using System;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using DumpLoader_2._0.ViewModels;

namespace DumpLoader_2._0.Views
{
    public sealed partial class RunningProcessesPanel : UserControl
    {
        private StackPanel _itemsHost = new StackPanel { Spacing = 10 };
        private TextBlock _emptyText;
        private Border _countBadge;
        private TextBlock _countText;
        private MainViewModel? _vm;

        public RunningProcessesPanel()
        {
            var accent = (SolidColorBrush)Application.Current.Resources["PrimaryAccent"];
            var primaryText = (SolidColorBrush)Application.Current.Resources["PrimaryText"];
            var secondaryText = (SolidColorBrush)Application.Current.Resources["SecondaryText"];

            var border = new Border
            {
                Background = (SolidColorBrush)Application.Current.Resources["CardBackground"],
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x0D, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
            };

            var root = new StackPanel { Spacing = 14 };

            var headerRow = new Grid();
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var header = new TextBlock
            {
                Text = "Running VPOS Instances",
                Foreground = primaryText,
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(header, 0);
            headerRow.Children.Add(header);

            _countText = new TextBlock
            {
                Text = "0",
                Foreground = accent,
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            _countBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x24, 0x00, 0xC8, 0x53)),
                CornerRadius = new CornerRadius(999),
                Padding = new Thickness(9, 2, 9, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = _countText,
            };
            Grid.SetColumn(_countBadge, 1);
            headerRow.Children.Add(_countBadge);

            root.Children.Add(headerRow);

            _itemsHost = new StackPanel { Spacing = 10 };
            root.Children.Add(_itemsHost);

            _emptyText = new TextBlock
            {
                Text = "More instances will appear here as they launch.",
                Foreground = secondaryText,
                FontSize = 12.5,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(8, 24, 8, 24)
            };
            root.Children.Add(_emptyText);

            border.Child = root;
            this.Content = border;

            UpdateCount();

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
            UpdateCount();
        }

        private void AddCard(TrackedProcessViewModel t)
        {
            var accent = (SolidColorBrush)Application.Current.Resources["PrimaryAccent"];
            var primaryText = (SolidColorBrush)Application.Current.Resources["PrimaryText"];
            var secondaryText = (SolidColorBrush)Application.Current.Resources["SecondaryText"];

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x09, 0xFF, 0xFF, 0xFF)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x0F, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14),
            };

            var sp = new StackPanel { Spacing = 8 };

            // Top row: version name (left) + status badge (right)
            var topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tbVersion = new TextBlock
            {
                Text = t.Version,
                Foreground = primaryText,
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Top,
            };
            Grid.SetColumn(tbVersion, 0);
            topRow.Children.Add(tbVersion);

            var statusPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };

            var haloGrid = new Grid { Width = 13, Height = 13, VerticalAlignment = VerticalAlignment.Center };
            haloGrid.Children.Add(new Ellipse { Width = 13, Height = 13, Fill = new SolidColorBrush(Color.FromArgb(0x2E, 0x00, 0xC8, 0x53)) });
            haloGrid.Children.Add(new Ellipse { Width = 7, Height = 7, Fill = accent, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            statusPanel.Children.Add(haloGrid);

            statusPanel.Children.Add(new TextBlock
            {
                Text = t.Status.ToUpperInvariant(),
                Foreground = accent,
                FontSize = 11,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                CharacterSpacing = 25,
                VerticalAlignment = VerticalAlignment.Center,
            });

            Grid.SetColumn(statusPanel, 1);
            topRow.Children.Add(statusPanel);

            sp.Children.Add(topRow);

            var metaPanel = new StackPanel { Spacing = 2 };
            metaPanel.Children.Add(new TextBlock { Text = $"PID: {t.ProcessId}", Foreground = secondaryText, FontSize = 12 });
            metaPanel.Children.Add(new TextBlock { Text = $"Started: {t.StartTime:T}", Foreground = secondaryText, FontSize = 12 });
            sp.Children.Add(metaPanel);

            var btnPanel = new Grid { Margin = new Thickness(0, 4, 0, 0), ColumnSpacing = 8 };
            btnPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // VPOS starts in two stages: the process is alive well before it has a real main
            // window. Calling "Bring To Front" before then crashed, so the button stays hidden
            // (and Stop takes the full row) until TrackedProcessViewModel confirms a window
            // exists.
            var btnFront = new Button { Content = "Bring To Front", Style = (Style)Application.Current.Resources["SecondaryButtonStyle"], HorizontalAlignment = HorizontalAlignment.Stretch };
            btnFront.Click += (_, _) => { _ = t.BringToFrontCommand.ExecuteAsync(null); };
            Grid.SetColumn(btnFront, 0);
            btnPanel.Children.Add(btnFront);

            var btnStop = new Button { Content = "Stop", Style = (Style)Application.Current.Resources["DangerButtonStyle"], HorizontalAlignment = HorizontalAlignment.Stretch };
            btnStop.Click += async (_, _) => { await t.StopProcessCommand.ExecuteAsync(null); };
            btnPanel.Children.Add(btnStop);

            void ApplyWindowReadyState()
            {
                if (t.IsWindowReady)
                {
                    btnFront.Visibility = Visibility.Visible;
                    Grid.SetColumn(btnStop, 1);
                    Grid.SetColumnSpan(btnStop, 1);
                }
                else
                {
                    btnFront.Visibility = Visibility.Collapsed;
                    Grid.SetColumn(btnStop, 0);
                    Grid.SetColumnSpan(btnStop, 2);
                }
            }

            ApplyWindowReadyState();
            t.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TrackedProcessViewModel.IsWindowReady))
                    ApplyWindowReadyState();
            };

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
            UpdateCount();
        }

        private void RemoveCard(TrackedProcessViewModel t)
        {
            var item = _itemsHost.Children.OfType<Border>().FirstOrDefault(b => ReferenceEquals(b.Tag, t));
            if (item != null)
                _itemsHost.Children.Remove(item);

            UpdateEmptyState();
            UpdateCount();
        }

        private void UpdateEmptyState()
        {
            _emptyText.Visibility = _itemsHost.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateCount()
        {
            var count = _itemsHost.Children.Count;
            _countText.Text = count.ToString();
            _countBadge.Visibility = count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
