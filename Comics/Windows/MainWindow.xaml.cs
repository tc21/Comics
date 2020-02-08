using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;

namespace Comics {
    public partial class MainWindow : Window {
        // These sets are in sync with the user's checked items in the sidebar
        // They are stored in a hashset for easy filtering
        // todo: take a look at ObservableHashSet
        private readonly HashSet<string> selectedAuthors = new HashSet<string>();
        private readonly HashSet<string> selectedCategories = new HashSet<string>();
        private readonly HashSet<string> selectedTags = new HashSet<string>();
        // The two sidebar options
        private bool onlyShowLoved = false;
        private bool showDisliked = false;
        // The text currently inside the search bar
        private string searchText = null;

        // Allow a queue of strings to be pushed to thefooter
        private readonly List<string> footerKeys = new List<string>();
        private readonly Dictionary<string, string> footerStrings = new Dictionary<string, string>();

        // This property returns the view containing objects so the view can be updated 
        private ICollectionView ComicsView => CollectionViewSource.GetDefaultView(this.Collection?.ItemsSource);
        private ICollectionView AuthorView => CollectionViewSource.GetDefaultView(this.AuthorSelector?.ItemsSource);
        private ICollectionView TagView => CollectionViewSource.GetDefaultView(this.TagSelector?.ItemsSource);
        private ICollectionView CategoryView => CollectionViewSource.GetDefaultView(this.CategorySelector?.ItemsSource);

        // this set contains the unique identifiers of comics that are filtered out (i.e. not shown)
        private HashSet<string> FilteredComics = new HashSet<string>();
        private CancellationTokenSource cts;

        public MainWindow() {
            this.InitializeComponent();
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
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            this.RefreshComics();
            this.RefreshFilter();
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
        public void RefreshComics() {
            this.ComicsView?.Refresh();
            var count = this.Collection.Items.Count;
            this.PushFooter("ItemCount", count.ToString() + " Item" + (count == 1 ? "" : "s"));
        }

        // we need to call this when we reassign things in the viewmodel
        public void RefreshFilter() {
            if (this.ComicsView != null) {
                this.ComicsView.Filter = this.ComicFilter;
            }
        }

        public void PushFooter(string key, string text) {
            if (this.footerKeys.Contains(key)) {
                this.footerStrings[key] = text;
            } else {
                this.footerKeys.Add(key);
                this.footerStrings.Add(key, text);
            }

            if (key == this.footerKeys.Last()) {
                this.FooterText.Text = this.footerStrings[key];
            }
        }

        public void PopFooter(string key) {
            var update = false;
            if (key == this.footerKeys.Last()) {
                update = true;
            }

            this.footerKeys.Remove(key);
            this.footerStrings.Remove(key);
            if (update) {
                var newFooter = "Comics";
                if (this.footerKeys.Count != 0) {
                    newFooter = this.footerStrings[this.footerKeys.Last()];
                }

                this.FooterText.Text = newFooter;
            }
        }

        public void DisableInteractions() {
            this.ProfileSelector.IsEnabled = false;
            this.SettingsButton.IsEnabled = false;

            if (App.SettingsWindow != null) {
                App.SettingsWindow.ProfileSelector.IsEnabled = false;
            }
        }

        public void EnableInteractions() {
            this.ProfileSelector.IsEnabled = true;
            this.SettingsButton.IsEnabled = true;

            if (App.SettingsWindow != null) {
                App.SettingsWindow.ProfileSelector.IsEnabled = true;
            }
        }
        
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e) {
            this.searchText = this.SearchBox.Text;

            await this.FilterComics();
        }
        
        // Detects the display scaling and stores it in settings
        public void UpdateDisplayScale() {
            var presentationSource = PresentationSource.FromVisual(this);
            var scale = presentationSource.CompositionTarget.TransformToDevice.M11;
            Properties.Settings.Default.DisplayScale = scale;
            Properties.Settings.Default.Save();
        }

        // reload author list, etc
        public void ComicInfoUpdated() {
            App.ViewModel.UpdateFilterLists();
        }

        // With the actual filtering done asynchronously, the filter imposed on the views are then quite simple.
        private bool ComicFilter(object item) {
            var comic = (Comic)item;
            return !this.FilteredComics.Contains(comic.UniqueIdentifier);
        }

        private async Task FilterComics() {
            /* note: I am not convinced the cancellation token actually improves performance, and it
             * looks like we'll need libraries much larger than my ~2000 items to test it */
            this.cts?.Cancel();

            this.PushFooter("FilterComics", "Filtering...");
            try {
                this.cts = new CancellationTokenSource();
                await this.FilterComics(this.cts.Token);
                this.RefreshComics();
            } catch (OperationCanceledException) {
                // do nothing
            }
            this.PopFooter("FilterComics");
        }

        private async Task FilterComics(CancellationToken ct) {
            var count = 0;
            var filtered = new HashSet<string>();

            await Task.Run(() => {
                foreach (var comic in App.ViewModel.AvailableComics) {
                    if (!this.ComicMatchesFilter(comic)) {
                        filtered.Add(comic.UniqueIdentifier);
                    }

                    count++;
                    if (count % 100 == 0) {
                        ct.ThrowIfCancellationRequested();
                    }
                }

            }, ct);

            this.FilteredComics = filtered;
        }

        private bool ComicMatchesFilter(Comic comic) {
            return comic.MatchesSearchText(this.searchText)
                && comic.MatchesAuthors(this.selectedAuthors)
                && comic.MatchesCategories(this.selectedCategories)
                && comic.MatchesTags(this.selectedTags)
                && (!this.onlyShowLoved || comic.Loved)
                && (this.showDisliked || !comic.Disliked);
        }
        
        // Happens when the "toggle right sidebar" footer button is pressed
        private void ToggleRightSidebar(object sender, RoutedEventArgs e) {
            if (this.RightSidebar.Visibility == Visibility.Collapsed) {
                this.RightSidebar.Visibility = Visibility.Visible;
            } else {
                this.RightSidebar.Visibility = Visibility.Collapsed;
            }

            Properties.Settings.Default.RightSidebarVisible = !Properties.Settings.Default.RightSidebarVisible;
        }

        // Opens currently selected comic
        private void OpenSelectedComics() {
            foreach (var comic in this.Collection.SelectedItems) {
                (comic as Comic)?.Open();
            }
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
            this.OpenSelectedComics();
        }

        private async void ContextMenu_Love(object sender, RoutedEventArgs e) {
            foreach (var c in this.Collection.SelectedItems) {
                var comic = (c as Comic);
                if (comic != null) {
                    comic.Loved = !comic.Loved;
                }
            }
            await this.FilterComics();
        }

        private async void ContextMenu_Dislike(object sender, RoutedEventArgs e) {
            foreach (var c in this.Collection.SelectedItems) {
                var comic = (c as Comic);
                if (comic != null) {
                    comic.Disliked = !comic.Disliked;
                }
            }
            await this.FilterComics();
        }

        private void ContextMenu_ShowInExplorer(object sender, RoutedEventArgs e) {
            foreach (var comic in this.Collection.SelectedItems) {
                (comic as Comic)?.OpenContainingFolder();
            }
        }

        private void ContextMenu_RedefineThumbnail(object sender, RoutedEventArgs e) {
            if (this.Collection.SelectedItems.Count != 1) {
                // todo: we should disable the option
                return;
            }

            var comic = (this.Collection.SelectedItem as Comic);
            if (comic is null) {
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog {
                InitialDirectory = comic.path
            };

            if (openFileDialog.ShowDialog() != true) {
                return;
            }

            comic.ThumbnailSource = openFileDialog.FileName;
            comic.Save();

            File.Delete(comic.ThumbnailPath);
            comic.GenerateThumbnail();

            this.RefreshComics();
        }

        private async void ContextMenu_ReloadComics(object sender, RoutedEventArgs e) {
            await App.ViewModel.ReloadComics();
        }

        private async void ContextMenu_ReloadThumbnails(object sender, RoutedEventArgs e) {
            // Delete existing ones first, but warn the user that it'll take a long time
            // Currently does basically nothing (unless you accidentally deleted something)
            var result = MessageBox.Show(
                "Are you sure you want to reload thumbnails? All thumbnails (for items in this library) will be"
                    + " deleted and regenerated. This may take a long times.", "Warning", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes) {
                return;
            }

            foreach (var comic in App.ViewModel.AvailableComics) {
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

        private async void ContextMenu_UpdateDatabase(object sender, RoutedEventArgs e) {
            await App.ViewModel.UpdateDatabase(force: true);
        }


        private void ContextMenu_RemoveFromDatabase(object sender, RoutedEventArgs e) {
            var comicsToRemove =this.Collection.SelectedItems.OfType<Comic>().ToArray();

            foreach (var comic_ in comicsToRemove) {
                if (comic_ is Comic comic) {
                    App.ViewModel.RemoveComicFromDatabase(comic);
                }
            }
        }

        // When the user changes the sort order, we update the sort descriptions on the comics view.
        // When the user selects "random", we have to randomize a field inside the comic object.
        private void SortOrderBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.ComicsView is null) {
                return;
            }

            this.UpdateComicSortDescriptions();

            Properties.Settings.Default.SelectedSortIndex = this.SortOrderBox.SelectedIndex;
        }

        public void UpdateSortDescriptions(bool randomize = true) {
            this.UpdateComicSortDescriptions(randomize);
            this.UpdateAuthorSortDescriptions();
            this.UpdateCategorySortDescriptions();
            this.UpdateTagSortDescriptions();
        }

        public void UpdateComicSortDescriptions(bool randomize = true) {
            if (this.ComicsView == null) {
                return;
            }

            this.ComicsView.SortDescriptions.Clear();

            if (this.SortOrderBox.SelectedIndex == Comic.RandomSortIndex && randomize) {
                App.ViewModel.RandomizeComics();
            }

            foreach (var property in Comic.SortDescriptionPropertiesForIndex(this.SortOrderBox.SelectedIndex)) {
                this.ComicsView.SortDescriptions.Add(new SortDescription(property.Name, property.ListSortDirection));
            }
        }

        public void UpdateAuthorSortDescriptions() {
            this.AuthorView.SortDescriptions.Clear();
            this.AuthorView.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
        }

        public void UpdateCategorySortDescriptions() {
            this.CategoryView.SortDescriptions.Clear();
            this.CategoryView.SortDescriptions.Add(new SortDescription("", ListSortDirection.Ascending));
        }

        public void UpdateTagSortDescriptions() {
            // TODO switch between count and name
            this.TagView.SortDescriptions.Clear();
            this.TagView.SortDescriptions.Add(new SortDescription("", ListSortDirection.Descending));
        }

        // Ways for the user to open a comic
        private void Collection_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            this.OpenSelectedComics();
        }

        private void Collection_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.OpenSelectedComics();
            }
        }

        // Ensures the collection is sorted when it is first loaded.
        private void Collection_Loaded(object sender, RoutedEventArgs e) {
            this.UpdateSortDescriptions();
        }

        // Handlers for when the user checks and unchecks sidebar options
        private async void Category_Changed(object sender, RoutedEventArgs e) {
            if (sender as CheckBox is CheckBox checkbox &&
                checkbox.IsChecked is bool isChecked) {
                if (isChecked) {
                    this.selectedCategories.Add(checkbox.Content.ToString());
                } else {
                    this.selectedCategories.Remove(checkbox.Content.ToString());
                }

                await this.FilterComics();
                App.ViewModel.UpdateFilterLists(updateCategories: false, excludedIds: this.FilteredComics);
            }
        }
        private async void Author_Changed(object sender, RoutedEventArgs e) {
            if (sender as CheckBox is CheckBox checkbox &&
                checkbox.IsChecked is bool isChecked) {
                if (isChecked) {
                    this.selectedAuthors.Add(checkbox.Content.ToString());
                    this.RemoveSelectedAuthorsLink.Visibility = Visibility.Visible;
                } else {
                    this.selectedAuthors.Remove(checkbox.Content.ToString());
                    if (this.selectedAuthors.Count == 0) {
                        this.RemoveSelectedAuthorsLink.Visibility = Visibility.Hidden;
                    }
                }

                await this.FilterComics();
                App.ViewModel.UpdateFilterLists(updateCategories: false, updateAuthors: false, excludedIds: this.FilteredComics);
            }
        }

        private async void Tag_Changed(object sender, RoutedEventArgs e) {

            if (sender as CheckBox is CheckBox checkbox &&
                checkbox.IsChecked is bool isChecked) {
                if (isChecked) {
                    this.selectedTags.Add(checkbox.Content.ToString());
                    this.RemoveSelectedTagsLink.Visibility = Visibility.Visible;
                } else {
                    this.selectedTags.Remove(checkbox.Content.ToString());
                    if (this.selectedTags.Count == 0) {
                        this.RemoveSelectedTagsLink.Visibility = Visibility.Hidden;
                    }
                }

                await this.FilterComics();
                App.ViewModel.UpdateFilterLists(updateCategories: false, updateTags: false, excludedIds: this.FilteredComics);
            }
        }

        private async void ShowLoved_Changed(object sender, RoutedEventArgs e) {
            if ((sender as CheckBox).IsChecked is bool isChecked) {
                this.onlyShowLoved = isChecked;

                await this.FilterComics();
                App.ViewModel.UpdateFilterLists(updateCategories: false, excludedIds: this.FilteredComics);
            }
        }

        private async void ShowDisliked_Changed(object sender, RoutedEventArgs e) {
            if ((sender as CheckBox).IsChecked is bool isChecked) {
                this.showDisliked = isChecked;

                await this.FilterComics();
                App.ViewModel.UpdateFilterLists(updateCategories: false, excludedIds: this.FilteredComics);
            }
        }

        protected override void OnClosing(CancelEventArgs e) {
            Properties.Settings.Default.MainWindowTop = this.Top;
            Properties.Settings.Default.MainWindowLeft = this.Left;
            base.OnClosing(e);
        }

        // the following two functions allow for items to be dragged out of the program
        private Point? dragStart;
        private List<Comic> selectedComics = null;

        private void Collection_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var source = e.OriginalSource as DependencyObject;

            while (source is ContentElement) {
                source = LogicalTreeHelper.GetParent(source);
            }

            while (source != null && !(source is ListBoxItem)) {
                source = VisualTreeHelper.GetParent(source);
            }

            if (source is ListBoxItem) {
                this.dragStart = e.GetPosition(null);
                if (this.Collection.SelectedItems.Count > 1) {
                    this.selectedComics = new List<Comic>(this.Collection.SelectedItems.Cast<Comic>());
                } else {
                    this.selectedComics = null;
                }
            }
        }
        
        private void Collection_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            // note: this function is not called if a DragDrop event has already started:
            // see end of Collection_MouseMove
            this.dragStart = null;
        }

        private void Collection_MouseMove(object sender, MouseEventArgs e) {
            if (this.dragStart is Point start) {
                var mouse = e.GetPosition(null);
                var difference = start - mouse;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance) {

                    if (this.Collection.SelectedItems.Count == 0) {
                        return;
                    }

                    if (this.selectedComics != null) {
                        foreach (var comic in this.selectedComics) {
                            if (!this.Collection.SelectedItems.Contains(comic)) {
                                this.Collection.SelectedItems.Add(comic);
                            }
                        }
                        this.selectedComics = null;
                    }

                    var dataFormat = DataFormats.FileDrop;
                    var files = new string[this.Collection.SelectedItems.Count];

                    for (var i = 0; i < this.Collection.SelectedItems.Count; i++) {
                        files[i] = (this.Collection.SelectedItems[i] as Comic).path;
                    }

                    var dataObject = new DataObject(dataFormat, files);
                    DragDrop.DoDragDrop(this.Collection, dataObject, DragDropEffects.Copy);
                    // DoDragDrop takes over control, and at this point the mouse is released
                    this.dragStart = null;
                }
            }
        }

        public void ClearSelections() {
            this.RemoveSelectedAuthors();
            this.RemoveSelectedTags();
            this.selectedTags.Clear();
        }

        private void RemoveSelectedAuthors() {
            this.selectedAuthors.Clear();
            this.RemoveSelectedAuthorsLink.Visibility = Visibility.Hidden;
        }

        private void RemoveSelectedTags() {
            this.selectedTags.Clear();
            this.RemoveSelectedTagsLink.Visibility = Visibility.Hidden;
        }

        private async void RemoveSelectedAuthorsLink_Click(object sender, RoutedEventArgs e) {
            this.RemoveSelectedAuthors();
            foreach (var item in App.ViewModel.AvailableAuthors) {
                item.IsChecked = false;
            }

            await this.FilterComics();
            App.ViewModel.UpdateFilterLists(updateCategories: false, excludedIds: this.FilteredComics);

            this.RefreshComics();
        }

        private async void RemoveSelectedTagsLink_Click(object sender, RoutedEventArgs e) {
            this.RemoveSelectedTags();
            foreach (var item in App.ViewModel.AvailableTags) {
                item.IsChecked = false;
            }

            await this.FilterComics();
            App.ViewModel.UpdateFilterLists(updateCategories: false, excludedIds: this.FilteredComics);

            this.RefreshComics();
        }
        private void Sidebar_SizeChanged(object sender, SizeChangedEventArgs e) {
            this.UpdateTagSelectorMaxHeight(e.NewSize.Height);
        }

        private void Sidebar_Loaded(object sender, RoutedEventArgs e) {
            this.UpdateTagSelectorMaxHeight((e.Source as DockPanel).ActualHeight);
        }

        private void UpdateTagSelectorMaxHeight(double containerHeight) {
            // 120 is a magic number limiting the max size of the tag selector such that
            // the tag and author selectors fill up the same amount of space
            this.TagSelector.MaxHeight = (containerHeight - this.CategorySelector.ActualHeight - 120) / 2;
        }

        private void Collection_DragEnter(object sender, DragEventArgs e) {
            e.Effects = DragDropEffects.Copy;
        }

        private void Collection_Drop(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var file in files) {
                    if (Directory.Exists(file)) {
                        var names = file.Split('\\');

                        var author = names.Length > 1 ? names[names.Length - 2] : "Unknown Author";
                        var title = names[names.Length - 1];

                        App.ViewModel.AddComicFromDisk(title, author, MainViewModel.ManuallyAddedComicCategoryName, file, App.ViewModel.AvailableComics);
                    }
                }
            }
        }
    }
}
