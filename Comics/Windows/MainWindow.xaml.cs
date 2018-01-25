using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Comics
{
    public partial class MainWindow : Window
    {
        // Used to disable left click actions when the application is out of focus
        // or within a set time (200ms by default) of gaining focus.
        private Timer actionDelayTimer = new Timer(Defaults.ActivationDelay);  

        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.ViewModel;

            DisableActions(null, null);
            actionDelayTimer.Elapsed += EnableActions;
            // These two lines collapse the right sidebar. The program should eventually remember its previous state
            RightSidebar.Loaded += ((u, v) => RightSidebar.Visibility = Visibility.Collapsed);
            RightSidebarButton.Loaded += ((u, v) => RightSidebarButton.Content = "◀❚");
            Activated += EnableActionsWithDelay;
            Deactivated += DisableActions;
            // This also needs to be moved to XAML
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(Collection.ItemsSource).Filter = ComicFilter;
            CollectionViewSource.GetDefaultView(Collection.ItemsSource).Refresh();
        }

        private void CollectionContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(Collection.ItemsSource)?.Refresh();
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(Collection.ItemsSource).Refresh();
        }

        private bool ComicFilter(object item)
        {
            if (String.IsNullOrWhiteSpace(SearchBox.Text))
                return true;
            return ((Comic)item).MatchesSearchText(SearchBox.Text.TrimStart());
        }
        
        // Handles a mouse event. By adding this handler to PreviewMouse..., 
        // the event is effectively disabled.
        private void EventHandled(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DisableActions(object sender, EventArgs e)
        {
            PreviewMouseLeftButtonDown += EventHandled;
            PreviewMouseLeftButtonUp += EventHandled;
        }

        private void EnableActionsWithDelay(object sender, EventArgs e)
        {
            actionDelayTimer.Start();
        }

        private void EnableActions(object sender, EventArgs e)
        {
            PreviewMouseLeftButtonDown -= EventHandled;
            PreviewMouseLeftButtonUp -= EventHandled;
            actionDelayTimer.Stop();
        }

        private void ToggleRightSidebar(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            if (RightSidebar.Visibility == Visibility.Collapsed)
            {
                RightSidebar.Visibility = Visibility.Visible;
                button.Content = "▶❚";
            }
            else
            {
                RightSidebar.Visibility = Visibility.Collapsed;
                button.Content = "◀❚";
            }
        }

        private void ShowSettings(object sender, RoutedEventArgs e)
        {
            if (!SettingsButton.ContextMenu.IsOpen)
            {
                e.Handled = true;

                var mouseRightClickEvent = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right)
                {
                    RoutedEvent = Mouse.MouseUpEvent,
                    Source = sender,
                };
                InputManager.Current.ProcessInput(mouseRightClickEvent);
            }
        }

        private void ContextMenu_Open(object sender, RoutedEventArgs e)
        {
            (Collection.SelectedItem as Comic)?.OpenWithDefaultApplication();
        }

        private void ContextMenu_Love(object sender, RoutedEventArgs e)
        {
            Comic comic = (Collection.SelectedItem as Comic);
            if (comic != null)
                comic.Loved = !comic.Loved;
        }

        private void ContextMenu_Dislike(object sender, RoutedEventArgs e)
        {
            Comic comic = (Collection.SelectedItem as Comic);
            if (comic != null)
                comic.Disliked = !comic.Disliked;
        }

        private void ContextMenu_ShowInExplorer(object sender, RoutedEventArgs e)
        {
            (Collection.SelectedItem as Comic)?.OpenContainingFolder();
        }

        private void ContextMenu_ReloadComics(object sender, RoutedEventArgs e)
        {
            App.ViewModel.ReloadComics();
        }

        private void ContextMenu_ReloadThumbnails(object sender, RoutedEventArgs e)
        {
            // Delete existing ones first, but warn the user that it'll take a long time
            // Currently does basically nothing (unless you accidentally deleted something)
            MessageBoxResult result = MessageBox.Show("Are you sure you want to reload thumbnails? All thumbnails (for items in this library) will be deleted and regenerated. This may take a long times.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
                return;

            foreach (Comic comic in App.ViewModel.VisibleComics)
            {
                if (File.Exists(comic.ThumbnailPath))
                    File.Delete(comic.ThumbnailPath);
            }

            App.ViewModel.LoadComicThumbnails();
        }

        private void ContextMenu_ShowSettings(object sender, RoutedEventArgs e)
        {
            Window settings = new SettingsWindow { Owner = this };
            settings.Show();
        }

        private void ContextMenu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ChangeSortOrder(object sender, SelectionChangedEventArgs e)
        {
            //switch (SortOrderBox.SelectedIndex)
            //{
            //    case 0:
            //        Items.Sort((x, y) => x.SortTitle.CompareTo(y.SortTitle));
            //        break;
            //    case 1:
            //        Items.Sort((x, y) => x.SortAuthor.CompareTo(y.SortAuthor));
            //        break;
            //    case 2:
            //        Items.Sort((x, y) => x.ImagePath.CompareTo(y.ImagePath));
            //        break;
            //}

            //Collection?.Items.Refresh();
        }

        private void Collection_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (Collection.SelectedItem as Comic)?.OpenWithDefaultApplication();
        }
    }
}
