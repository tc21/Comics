using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private HashSet<string> selectedCategories = new HashSet<string>();
        private HashSet<string> selectedAuthors = new HashSet<string>();

        private ICollectionView ComicsView
        {
            get {
                if (Collection == null)
                    return null;
                return CollectionViewSource.GetDefaultView(Collection.ItemsSource); }
        }

        private ICollectionView AuthorSelectorView
        {
            get
            {
                if (Collection == null)
                    return null;
                return CollectionViewSource.GetDefaultView(AuthorSelector.ItemsSource);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.ViewModel;

            // Sets the selected sort to its saved state
            SortOrderBox.SelectedIndex = Properties.Settings.Default.SelectedSortIndex;
            // Sets the right sidebar visibility to its saved state
            RightSidebar.Loaded += ((u, v) => RightSidebar.Visibility = 
                (Properties.Settings.Default.RightSidebarVisible) ? Visibility.Visible : Visibility.Collapsed);
            // Enables the left click delay
            DisableActions(null, null);
            actionDelayTimer.Elapsed += EnableActions;
            Activated += EnableActionsWithDelay;
            Deactivated += DisableActions;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ComicsView.Filter = ComicFilter;
            ComicsView.Refresh();
        }

        private void CollectionContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ComicsView?.Refresh();
        }
        
        // This function handles search via the search box. The view model updates the comics 
        // to be shown by passing it the search text.
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            App.ViewModel.UpdateSearch(SearchBox.Text);
        }

        // This function handles filtering via the side panel. Thus it only affects the main
        // area and not the side panel entries.
        private bool ComicFilter(object item)
        {
            Comic comic = (Comic)item;
            return ((selectedCategories.Count == 0 || selectedCategories.Contains(comic.DisplayCategory)) &&
                    (selectedAuthors.Count == 0 || selectedAuthors.Contains(comic.DisplayAuthor)));
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
                RightSidebar.Visibility = Visibility.Visible;
            else
                RightSidebar.Visibility = Visibility.Collapsed;

            Properties.Settings.Default.RightSidebarVisible = !Properties.Settings.Default.RightSidebarVisible;
            Properties.Settings.Default.Save();
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
            if (ComicsView == null)
                return;

            ComicsView.SortDescriptions.Clear();
            foreach (string propertyName in Comic.SortDescriptionPropertyNamesForIndex(SortOrderBox.SelectedIndex))
            {
                ComicsView.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
            }
            ComicsView.Refresh();

            Properties.Settings.Default.SelectedSortIndex = SortOrderBox.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void Collection_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (Collection.SelectedItem as Comic)?.OpenWithDefaultApplication();
        }

        private void Collection_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeSortOrder(null, null);
        }

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            selectedCategories.Add((string)((CheckBox)sender).Content);
            ComicsView.Refresh();
        }

        private void Category_Unchecked(object sender, RoutedEventArgs e)
        {
            selectedCategories.Remove((string)((CheckBox)sender).Content);
            ComicsView.Refresh();
        }

        private void Author_Checked(object sender, RoutedEventArgs e)
        {
            selectedAuthors.Add((string)((CheckBox)sender).Content);
            ComicsView.Refresh();
        }

        private void Author_Unchecked(object sender, RoutedEventArgs e)
        {
            selectedAuthors.Remove((string)((CheckBox)sender).Content);
            ComicsView.Refresh();
        }
    }
}
