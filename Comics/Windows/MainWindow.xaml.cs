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

namespace Comics {
    public partial class MainWindow : Window {
        // These sets are in sync with the user's checked items in the sidebar
        // They are stored in a hashset for easy filtering
        // todo: take a look at ObservableHashSet
        private HashSet<string> selectedAuthors = new HashSet<string>();
        private HashSet<string> selectedCategories = new HashSet<string>();
        private HashSet<string> selectedTags = new HashSet<string>();
        // The two sidebar options
        private bool onlyShowLoved = false;
        private bool showDisliked = false;
        // The text currently inside the search bar
        private string searchText = null;

        // This property returns the view containing objects so the view can be updated 
        private ICollectionView ComicsView => CollectionViewSource.GetDefaultView(this.Collection?.ItemsSource);

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
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            RefreshComics();
            this.ComicsView.Filter = this.ComicFilter;
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
            this.ComicsView.Refresh();
            var count = this.Collection.Items.Count;
            this.Footer.Content = count.ToString() + " Item" + (count == 1 ? "" : "s");
        }
        
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) {
            this.searchText = this.SearchBox.Text;
            RefreshComics();
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

            return comic.MatchesSearchText(this.searchText)
                && comic.MatchesAuthors(this.selectedAuthors)
                && comic.MatchesCategories(this.selectedCategories)
                && comic.MatchesTags(this.selectedTags)
                && (!onlyShowLoved || comic.Loved)
                && (showDisliked || !comic.Disliked);
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
            OpenSelectedComics();
        }

        private void ContextMenu_EditInfo(object sender, RoutedEventArgs e) {
            if (this.Collection.SelectedItems.Count != 1) {
                // todo: implement the ability to edit multiple items
                return;
            }

            if (App.InfoWindow != null) {
                App.InfoWindow.EditingComics = this.Collection.SelectedItems.Cast<Comic>().ToList();
                //App.InfoWindow.Tags = this.availableTags.ToList();
                App.InfoWindow.Activate();
                return;
            }
            
            Window info = new InfoWindow {
                Owner = this,
                EditingComics = this.Collection.SelectedItems.Cast<Comic>().ToList(),
                //Tags = this.availableTags.ToList()
        };

            info.Show();
        }

        private void ContextMenu_Love(object sender, RoutedEventArgs e) {
            foreach (var c in this.Collection.SelectedItems) {
                Comic comic = (c as Comic);
                if (comic != null) {
                    comic.Loved = !comic.Loved;
                }
            }
        }

        private void ContextMenu_Dislike(object sender, RoutedEventArgs e) {
            foreach (var c in this.Collection.SelectedItems) {
                Comic comic = (c as Comic);
                if (comic != null) {
                    comic.Disliked = !comic.Disliked;
                }
            }
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

            foreach (Comic comic in App.ViewModel.AvailableComics) {
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
        private void SortOrderBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.ComicsView == null) {
                return;
            }

            UpdateSortDescriptions();

            Properties.Settings.Default.SelectedSortIndex = this.SortOrderBox.SelectedIndex;
        }

        public void UpdateSortDescriptions() {
            this.ComicsView.SortDescriptions.Clear();

            if (this.SortOrderBox.SelectedIndex == Comic.RandomSortIndex) {
                App.ViewModel.RandomizeComics();
            }

            foreach (string propertyName in Comic.SortDescriptionPropertyNamesForIndex(this.SortOrderBox.SelectedIndex)) {
                this.ComicsView.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Ascending));
            }
        }

        // Ways for the user to open a comic
        private void Collection_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            OpenSelectedComics();
        }

        private void Collection_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                OpenSelectedComics();
            }
        }

        // Ensures the collection is sorted when it is first loaded.
        private void Collection_Loaded(object sender, RoutedEventArgs e) {
            UpdateSortDescriptions();
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
            if (this.selectedAuthors.Count == 1) {
                this.RemoveSelectedAuthorsLink.Visibility = Visibility.Visible;
            }
            RefreshComics();
        }
        
        private void Author_Unchecked(object sender, RoutedEventArgs e) {
            this.selectedAuthors.Remove(((CheckBox)sender).Content.ToString());
            if (this.selectedAuthors.Count == 0) {
                this.RemoveSelectedAuthorsLink.Visibility = Visibility.Hidden;
            }
            RefreshComics();
        }

        private void Tag_Checked(object sender, RoutedEventArgs e) {
            this.selectedTags.Add(((CheckBox)sender).Content.ToString());
            RefreshComics();
        }
        
        private void Tag_Unchecked(object sender, RoutedEventArgs e) {
            this.selectedTags.Remove(((CheckBox)sender).Content.ToString());
            RefreshComics();
        }

        private void ShowLoved_Checked(object sender, RoutedEventArgs e) {
            this.onlyShowLoved = true;
            RefreshComics();
        }

        private void ShowLoved_Unchecked(object sender, RoutedEventArgs e) {
            this.onlyShowLoved = false;
            RefreshComics();
        }

        private void ShowDisliked_Checked(object sender, RoutedEventArgs e) {
            this.showDisliked = true;
            RefreshComics();
        }

        private void ShowDisliked_Unchecked(object sender, RoutedEventArgs e) {
            this.showDisliked = false;
            RefreshComics();
        }

        protected override void OnClosing(CancelEventArgs e) {
            Properties.Settings.Default.MainWindowTop = this.Top;
            Properties.Settings.Default.MainWindowLeft = this.Left;
            base.OnClosing(e);
        }

        // the following two functions allow for items to be dragged out of the program
        private Point dragStart;
        private List<Comic> selectedComics = null;

        private void Collection_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            var source = e.OriginalSource as DependencyObject;

            while (source is ContentElement) {
                source = LogicalTreeHelper.GetParent(source);
            }

            while (source != null && !(source is ListBoxItem)) {
                source = VisualTreeHelper.GetParent(source);
            }

            ListBoxItem item = source as ListBoxItem;

            if (item != null) {
                this.dragStart = e.GetPosition(null);
                if (this.Collection.SelectedItems.Count > 1) {
                    this.selectedComics = new List<Comic>(this.Collection.SelectedItems.Cast<Comic>());
                } else {
                    this.selectedComics = null;
                }
            }
        }

        private void Collection_MouseMove(object sender, MouseEventArgs e) {
            Point mouse = e.GetPosition(null);
            Vector difference = this.dragStart - mouse;
            
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

                string dataFormat = DataFormats.FileDrop;
                string[] files = new string[this.Collection.SelectedItems.Count];

                for (int i = 0; i < this.Collection.SelectedItems.Count; i++) {
                    files[i] = (this.Collection.SelectedItems[i] as Comic).ContainingPath;
                }

                DataObject dataObject = new DataObject(dataFormat, files);
                DragDrop.DoDragDrop(this.Collection, dataObject, DragDropEffects.Copy);
            }
        }

        public void ClearSelections() {
            this.RemoveSelectedAuthors();
            this.selectedCategories.Clear();
            this.selectedTags.Clear();
        }

        private void RemoveSelectedAuthors() {
            this.selectedAuthors.Clear();
            this.RemoveSelectedAuthorsLink.Visibility = Visibility.Hidden;
        }

        private void RemoveSelectedAuthorsLink_Click(object sender, RoutedEventArgs e) {
            RemoveSelectedAuthors();
            foreach (var item in App.ViewModel.AvailableAuthors) {
                item.IsChecked = false;
            }
            RefreshComics();
        }
    }
}
