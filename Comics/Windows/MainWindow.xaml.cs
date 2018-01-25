using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Comics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Stores all the comics.
        /// </summary>
        private List<Comic> allItems = new List<Comic>();
        private List<Comic> items = new List<Comic>();
        private List<Comic> Items
        {
            get
            {
                return items;
            }
            set
            {
                items = value;
                Collection.ItemsSource = items;
                ChangeSortOrder(null, null);
                string footer = (items.Count == 1) ? " Item" : " Items";
                Footer.Content = items.Count.ToString() + footer;
            }
        }

        /// <summary>
        /// Temporary storage before an action is confirmed
        /// </summary>
        private Comic temporaryComic;
        /// <summary>
        /// Used to disable left click actions when the application is out of focus
        /// or within a set time (200ms by default) of gaining focus.
        /// </summary>
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
        }

        private void CollectionContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Collection.ItemsPanel.
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

        /// <summary>
        /// Handles a mouse event. By adding this handler to PreviewMouse..., 
        /// the event is effectively disabled.
        /// </summary>
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

            if (RightSidebar.Visibility == System.Windows.Visibility.Collapsed)
            {
                RightSidebar.Visibility = System.Windows.Visibility.Visible;
                button.Content = "▶❚";
            }
            else
            {
                RightSidebar.Visibility = System.Windows.Visibility.Collapsed;
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
            temporaryComic?.OpenWithDefaultApplication();
            temporaryComic = null;
        }

        private void ContextMenu_Love(object sender, RoutedEventArgs e)
        {
            if (temporaryComic != null)
                temporaryComic.Loved = !temporaryComic.Loved;
            temporaryComic = null;
        }

        private void ContextMenu_Dislike(object sender, RoutedEventArgs e)
        {
            if (temporaryComic != null)
                temporaryComic.Disliked = !temporaryComic.Disliked;
            temporaryComic = null;
        }

        private void ContextMenu_ShowInExplorer(object sender, RoutedEventArgs e)
        {
            temporaryComic?.OpenContainingFolder();
            temporaryComic = null;
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

            foreach (Comic comic in allItems)
            {
                if (File.Exists(comic.ThumbnailPath))
                    File.Delete(comic.ThumbnailPath);
            }

            App.ViewModel.LoadComicThumbnails();
        }

        private void ContextMenu_ShowSettings(object sender, RoutedEventArgs e)
        {
            Window settings = new SettingsWindow();
            settings.Owner = this;
            settings.Show();
        }

        private void ContextMenu_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void EndedLeftClickOnComic(object sender, MouseButtonEventArgs e)
        {
            Comic comic = VisualHelper.ComicAtMouseButtonEvent(sender, e);
            if (comic != null && comic.Equals(temporaryComic))
                comic.OpenWithDefaultApplication();
            temporaryComic = null;
        }

        private void StartedRightClickOnComic(object sender, MouseButtonEventArgs e)
        {
            temporaryComic = VisualHelper.ComicAtMouseButtonEvent(sender, e);
        }

        private void StartedLeftClickOnComic(object sender, MouseButtonEventArgs e)
        {
            temporaryComic = VisualHelper.ComicAtMouseButtonEvent(sender, e);
        }

        private void ChangeSortOrder(object sender, SelectionChangedEventArgs e)
        {
            switch (SortOrderBox.SelectedIndex)
            {
                case 0:
                    Items.Sort((x, y) => x.SortTitle.CompareTo(y.SortTitle));
                    break;
                case 1:
                    Items.Sort((x, y) => x.SortAuthor.CompareTo(y.SortAuthor));
                    break;
                case 2:
                    Items.Sort((x, y) => x.ImagePath.CompareTo(y.ImagePath));
                    break;
            }

            Collection?.Items.Refresh();
        }
    }
}
