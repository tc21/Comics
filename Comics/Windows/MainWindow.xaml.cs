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

namespace Comics {
    public partial class MainWindow : Window {
        // Used to disable left click actions when the application is out of focus
        // or within a set time (200ms by default) of gaining focus.
        //private Timer actionDelayTimer = new Timer(Defaults.Profile.ReactionTime);
        // These two sets are in sync with the user's checked items in the sidebar
        private HashSet<string> selectedCategories = new HashSet<string>();
        private HashSet<string> selectedAuthors = new HashSet<string>();
        // These three sets are in sync with all the visible items in the window
        private HashSet<string> availableAuthors = new HashSet<string>();
        private HashSet<string> availableCategories = new HashSet<string>();
        private HashSet<string> availableComics = new HashSet<string>();
        // The two sidebar options
        private bool onlyShowLoved = false;
        private bool showDisliked = false;
        // The text currently inside the search bar
        private string searchText = null;

        // These three private properties return the view containing objects so the view can be updated 
        private ICollectionView ComicsView => CollectionViewSource.GetDefaultView(this.Collection?.ItemsSource);
        private ICollectionView AuthorSelectorView => CollectionViewSource.GetDefaultView(this.AuthorSelector?.ItemsSource);
        private ICollectionView CategorySelectorView => CollectionViewSource.GetDefaultView(this.CategorySelector?.ItemsSource);

        public MainWindow() {
            InitializeComponent();
            App.ComicsWindow = this;
            this.DataContext = App.ViewModel;
            // Sets the windows size to its saved state
            this.Height = Properties.Settings.Default.MainWindowHeight;
            this.Width = Properties.Settings.Default.MainWindowWidth;
            this.Top = Properties.Settings.Default.MainWindowTop;
            this.Left = Properties.Settings.Default.MainWindowLeft;
            // Sets the selected sort to its saved state
            this.SortOrderBox.SelectedIndex = Properties.Settings.Default.SelectedSortIndex;
            // Sets the right sidebar visibility to its saved state
            this.RightSidebar.Loaded += ((u, v) => this.RightSidebar.Visibility =
                (Properties.Settings.Default.RightSidebarVisible) ? Visibility.Visible : Visibility.Collapsed);
            // Enables the left click delay
            //DisableActions(null, null);
            //actionDelayTimer.Elapsed += EnableActions;
            //Activated += EnableActionsWithDelay;
            //Deactivated += DisableActions;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            this.ComicsView.Filter = this.ComicFilter;
            this.AuthorSelectorView.Filter = this.AuthorSelectorFilter;
            this.CategorySelectorView.Filter = this.CategorySelectorFilter;
            // Actually loads the comics on startup
            RefreshAll();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            Properties.Settings.Default.Save();
            App.ComicsWindow = null;
        }

        // When the window changes size, the view must be refreshed to update the size of each item
        private void CollectionContainerSizeChanged(object sender, SizeChangedEventArgs e) {
            this.ComicsView?.Refresh();
            Properties.Settings.Default.MainWindowHeight = this.ActualHeight;
            Properties.Settings.Default.MainWindowWidth = this.ActualWidth;
        }

        // Asynchronously updates comics and refreshes the main view. 
        // Used when a category or author selection changes
        public async void RefreshComics() {
            await Task.Run(() => UpdateAvailableComics(this.searchText));
            this.ComicsView.Refresh();
        }

        // Asynchronously updates comics and refreshes all views
        // Used when a general selection / search term changes
        public async void RefreshAll() {
            await Task.Run(() => UpdateAvailableComics(this.searchText));
            this.ComicsView.Refresh();
            this.AuthorSelectorView.Refresh();
            this.CategorySelectorView.Refresh();
        }

        // A change in search actually requires looping through all comics, since the sidebar's author
        // list is generated dynamically from the visible comics. My library of ~700 comics already causes
        // lag when run on the UI thread., meaning we had to make this operation asynchronous. 
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) {
            this.searchText = this.SearchBox.Text;
            RefreshAll();
        }

        // This is the operation that was causing lag.
        private void UpdateAvailableComics(string searchText) {
            this.availableCategories.Clear();
            this.availableAuthors.Clear();
            this.availableComics.Clear();

            var visibleComics = new ObservableCollection<Comic>(App.ViewModel.VisibleComics);

            foreach (Comic comic in visibleComics) {
                if ((this.onlyShowLoved && !comic.Loved) || (!this.showDisliked && comic.Disliked)) {
                    continue;
                }

                if (comic.MatchesSearchText(searchText)) {
                    this.availableAuthors.Add(comic.Author.Display);
                    this.availableCategories.Add(comic.Category.Display);
                    if (comic.MatchesCategories(this.selectedCategories) && comic.MatchesAuthors(this.selectedAuthors)) {
                        this.availableComics.Add(comic.UniqueIdentifier);
                    }
                }
            }

            string footer = this.availableComics.Count.ToString() + " Item" + (this.availableComics.Count == 1 ? "" : "s");
            this.Dispatcher.Invoke(() => this.Footer.Content = footer);
        }

        // Detects the display scaling and stores it in settings
        public void UpdateDisplayScale() {
            PresentationSource presentationSource = PresentationSource.FromVisual(this);
            double scale = presentationSource.CompositionTarget.TransformToDevice.M11;
            Properties.Settings.Default.DisplayScale = scale;
            Properties.Settings.Default.Save();
        }

        // With the actual filtering done asynchronously, the filter imposed on the views are then quite simple.
        private bool ComicFilter(object item) {
            Comic comic = (Comic)item;
            return this.availableComics.Contains(comic.UniqueIdentifier);
        }

        private bool AuthorSelectorFilter(object item) {
            SortedString author = (SortedString)item;
            return this.availableAuthors.Contains(author.Display);
        }

        private bool CategorySelectorFilter(object item) {
            SortedString category = (SortedString)item;
            return this.availableCategories.Contains(category.Display);
        }

        // Handles a mouse event. By adding this handler to PreviewMouse..., 
        // the event is effectively disabled.
        private void MouseEventHandled(object sender, MouseButtonEventArgs e) {
            e.Handled = true;
        }

        // These functions enable the "left click delay". Since nothing that happens in the 
        // program actually only requires 1 left click anymore, I'm considering disabling it
        // by default.
        private void DisableActions(object sender, EventArgs e) {
            //PreviewMouseLeftButtonDown += MouseEventHandled;
            //PreviewMouseLeftButtonUp += MouseEventHandled;
        }

        private void EnableActionsWithDelay(object sender, EventArgs e) {
            //actionDelayTimer.Start();
        }

        private void EnableActions(object sender, EventArgs e) {
            //PreviewMouseLeftButtonDown -= MouseEventHandled;
            //PreviewMouseLeftButtonUp -= MouseEventHandled;
            //actionDelayTimer.Stop();
        }

        // Happens when the "toggle right sidebar" footer button is pressed
        private void ToggleRightSidebar(object sender, RoutedEventArgs e) {
            Button button = sender as Button;

            if (this.RightSidebar.Visibility == Visibility.Collapsed) {
                this.RightSidebar.Visibility = Visibility.Visible;
            } else {
                this.RightSidebar.Visibility = Visibility.Collapsed;
            }

            Properties.Settings.Default.RightSidebarVisible = !Properties.Settings.Default.RightSidebarVisible;
        }

        // Happens when the "settings" footer button is pressed
        private void ShowSettingsContextMenu(object sender, RoutedEventArgs e) {
            if (!this.SettingsButton.ContextMenu.IsOpen) {
                e.Handled = true;

                var mouseRightClickEvent = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) {
                    RoutedEvent = Mouse.MouseUpEvent,
                    Source = sender,
                };
                InputManager.Current.ProcessInput(mouseRightClickEvent);
            }
        }

        // Handlers for context menu items. This includes the right click and settings menus
        private void ContextMenu_Open(object sender, RoutedEventArgs e) {
            (this.Collection.SelectedItem as Comic)?.Open();
        }

        private void ContextMenu_Love(object sender, RoutedEventArgs e) {
            Comic comic = (this.Collection.SelectedItem as Comic);
            if (comic != null) {
                comic.Loved = !comic.Loved;
            }
        }

        private void ContextMenu_Dislike(object sender, RoutedEventArgs e) {
            Comic comic = (this.Collection.SelectedItem as Comic);
            if (comic != null) {
                comic.Disliked = !comic.Disliked;
            }
        }

        private void ContextMenu_ShowInExplorer(object sender, RoutedEventArgs e) {
            (this.Collection.SelectedItem as Comic)?.OpenContainingFolder();
        }

        private void ContextMenu_RedefineThumbnail(object sender, RoutedEventArgs e) {
            Comic comic = (this.Collection.SelectedItem as Comic);
            if (comic == null) {
                return;
            }

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog {
                InitialDirectory = comic.ContainingPath
            };
            if (openFileDialog.ShowDialog() != true) {
                return;
            }

            comic.Metadata.ThumbnailSource = openFileDialog.FileName;
            comic.SaveMetadata();

            File.Delete(comic.ThumbnailPath);
            comic.CreateThumbnail();
            RefreshComics();
        }

        private async void ContextMenu_ReloadComics(object sender, RoutedEventArgs e) {
            await App.ViewModel.ReloadComics();
        }

        private async void ContextMenu_ReloadThumbnails(object sender, RoutedEventArgs e) {
            // Delete existing ones first, but warn the user that it'll take a long time
            // Currently does basically nothing (unless you accidentally deleted something)
            MessageBoxResult result = MessageBox.Show("Are you sure you want to reload thumbnails? All thumbnails (for items in this library) will be deleted and regenerated. This may take a long times.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes) {
                return;
            }

            foreach (Comic comic in App.ViewModel.VisibleComics) {
                if (File.Exists(comic.ThumbnailPath)) {
                    File.Delete(comic.ThumbnailPath);
                }
            }

            await App.ViewModel.ReloadComicThumbnails();
        }

        private void ContextMenu_ShowSettings(object sender, RoutedEventArgs e) {
            if (App.SettingsWindow != null) {
                App.SettingsWindow.Activate();
                return;
            }

            Window settings = new SettingsWindow { Owner = this };
            settings.Show();
        }

        private void ContextMenu_Exit(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        // When the user changes the sort order, we update the sort descriptions on the comics view.
        // When the user selects "random", we have to randomize a field inside the comic object.
        private async void SortOrderBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.ComicsView == null) {
                return;
            }
            Debug.Print("sort changed");
            await App.ViewModel.SetSortIndex(this.SortOrderBox.SelectedIndex);
        }

        public void UpdateSortDescriptions(IEnumerable<string> sortDescriptionPropertyNames) {
            this.ComicsView.SortDescriptions.Clear();

            foreach (string propertyName in Comic.SortDescriptionPropertyNamesForIndex(this.SortOrderBox.SelectedIndex)) {
                this.ComicsView.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
            }
        }

        // Ways for the user to open a comic
        private void Collection_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            (this.Collection.SelectedItem as Comic)?.Open();
        }

        private void Collection_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                (this.Collection.SelectedItem as Comic)?.Open();
            }
        }

        // TODO: Ensures the collection is sorted when it is first loaded.
        // 30 April 2018: This never worked properly, so it now sets sort order to default to ensure consistency between ui and behavior
        private async void Collection_Loaded(object sender, RoutedEventArgs e) {
            //IEnumerable<string> sortDescriptions = await App.ViewModel.SortDescriptionsForIndex(Properties.Settings.Default.SelectedSortIndex);
            //UpdateSortDescriptions(sortDescriptions);
            //Properties.Settings.Default.SelectedSortIndex = Comic.DefaultSortIndex;
            //this.SortOrderBox.SelectedIndex = Comic.DefaultSortIndex;
            //SortOrderBox_SelectionChanged(this, null);
        }

        // Handlers for when the user checks and unchecks sidebar options
        private void Category_Checked(object sender, RoutedEventArgs e) {
            this.selectedCategories.Add(((CheckBox)sender).Content.ToString());
            RefreshComics();
        }

        private void Category_Unchecked(object sender, RoutedEventArgs e) {
            this.selectedCategories.Remove(((CheckBox)sender).Content.ToString());
            RefreshComics();
        }

        private void Author_Checked(object sender, RoutedEventArgs e) {
            this.selectedAuthors.Add(((CheckBox)sender).Content.ToString());
            RefreshComics();
        }

        private void Author_Unchecked(object sender, RoutedEventArgs e) {
            this.selectedAuthors.Remove(((CheckBox)sender).Content.ToString());
            RefreshComics();
        }

        private void ShowLoved_Checked(object sender, RoutedEventArgs e) {
            this.onlyShowLoved = true;
            RefreshAll();
        }

        private void ShowLoved_Unchecked(object sender, RoutedEventArgs e) {
            this.onlyShowLoved = false;
            RefreshAll();
        }

        private void ShowDisliked_Checked(object sender, RoutedEventArgs e) {
            this.showDisliked = true;
            RefreshAll();
        }

        private void ShowDisliked_Unchecked(object sender, RoutedEventArgs e) {
            this.showDisliked = false;
            RefreshAll();
        }

        protected override void OnClosing(CancelEventArgs e) {
            Properties.Settings.Default.MainWindowTop = this.Top;
            Properties.Settings.Default.MainWindowLeft = this.Left;
            base.OnClosing(e);
        }
    }
}
