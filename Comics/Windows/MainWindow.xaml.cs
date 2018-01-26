using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
        private HashSet<string> availableAuthors = new HashSet<string>();
        private HashSet<string> availableComics = new HashSet<string>();
        private bool onlyShowLoved = false;
        private bool showDisliked = false;
        private string searchText;

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
            // Sets the windows size to its saved state
            Height = Properties.Settings.Default.MainWindowHeight;
            Width = Properties.Settings.Default.MainWindowWidth;
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

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ComicsView.Filter = ComicFilter;
            AuthorSelectorView.Filter = AuthorSelectorFilter;
            // Actually loads the comics on startup
            await Task.Run(() => UpdateAvailableComics(null));
            ComicsView.Refresh();
            AuthorSelectorView.Refresh();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void CollectionContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ComicsView?.Refresh();
            Properties.Settings.Default.MainWindowHeight = ActualHeight;
            Properties.Settings.Default.MainWindowWidth = ActualWidth;
        }
        
        // A change in search actually requires looping through all comics, since the sidebar's author
        // list is generated dynamically from the visible comics. My library of ~700 comics already causes
        // lag when run on the UI thread., meaning we had to make this operation asynchronous. 
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchText = SearchBox.Text;
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
            AuthorSelectorView.Refresh();
        }

        // This is the operation that had been causing lag.
        private async Task UpdateAvailableComics(string searchText)
        {
            availableAuthors.Clear();
            availableComics.Clear();
            foreach (Comic comic in App.ViewModel.VisibleComics)
            {
                if ((onlyShowLoved && !comic.Loved) || (!showDisliked && comic.Disliked))
                    continue;

                if (comic.MatchesSearchText(searchText))
                {
                    availableAuthors.Add(comic.Author.Display);
                    if (comic.MatchesCategories(selectedCategories) && comic.MatchesAuthors(selectedAuthors))
                        availableComics.Add(comic.UniqueIdentifier);
                }
            }
            
            string footer = availableComics.Count.ToString() + " Item" + (availableComics.Count == 1 ? "" : "s");
            Dispatcher.Invoke(() => Footer.Content = footer);
        }
        
        private bool ComicFilter(object item)
        {
            Comic comic = (Comic)item;
            return availableComics.Contains(comic.UniqueIdentifier);
        }

        private bool AuthorSelectorFilter(object item)
        {
            SortedString author = (SortedString)item;
            return availableAuthors.Contains(author.Display);
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
        }

        private void Collection_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (Collection.SelectedItem as Comic)?.OpenWithDefaultApplication();
        }

        private void Collection_Loaded(object sender, RoutedEventArgs e)
        {
            ChangeSortOrder(null, null);
        }

        // For why these functions are async, see SearchBox_TextChanged
        private async void Category_Checked(object sender, RoutedEventArgs e)
        {
            selectedCategories.Add(((CheckBox)sender).Content.ToString());
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
        }

        private async void Category_Unchecked(object sender, RoutedEventArgs e)
        {
            selectedCategories.Remove(((CheckBox)sender).Content.ToString());
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
        }

        private async void Author_Checked(object sender, RoutedEventArgs e)
        {
            selectedAuthors.Add(((CheckBox)sender).Content.ToString());
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
        }

        private async void Author_Unchecked(object sender, RoutedEventArgs e)
        {
            selectedAuthors.Remove(((CheckBox)sender).Content.ToString());
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
        }

        private async void ShowLoved_Checked(object sender, RoutedEventArgs e)
        {
            onlyShowLoved = true;
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
            AuthorSelectorView.Refresh();
        }

        private async void ShowLoved_Unchecked(object sender, RoutedEventArgs e)
        {
            onlyShowLoved = false;
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
            AuthorSelectorView.Refresh();
        }

        private async void ShowDisliked_Checked(object sender, RoutedEventArgs e)
        {
            showDisliked = true;
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
            AuthorSelectorView.Refresh();
        }

        private async void ShowDisliked_Unchecked(object sender, RoutedEventArgs e)
        {
            showDisliked = false;
            await Task.Run(() => UpdateAvailableComics(searchText));
            ComicsView.Refresh();
            AuthorSelectorView.Refresh();
        }
    }
}
