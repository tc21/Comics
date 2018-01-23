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
            DisableActions(null, null);

            actionDelayTimer.Elapsed += EnableActions;
            Loaded += ApplicationDidLoad;
            RightSidebar.Loaded += ((u, v) => RightSidebar.Visibility = Visibility.Collapsed);
            RightSidebarButton.Loaded += ((u, v) => RightSidebarButton.Content = "◀❚");
            Activated += EnableActionsWithDelay;
            Deactivated += DisableActions;
            SearchBox.TextChanged += CommitSearch;

            LoadComics();
        }

        private void ApplicationDidLoad(object sender, EventArgs e)
        {
            LoadComicThumbnails();  // This can be very slow. Find a way to "background" it.
        }

        private void CollectionContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //Collection.ItemsPanel.
        }

        private void LoadComicThumbnails()
        {
            // Very primitive thumbnail caching being done here
            foreach (Comic comic in allItems)
            {
                if (!(File.Exists(comic.ThumbnailPath)))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(comic.ImagePath);
                    image.DecodePixelHeight = Defaults.ThumbnailWidthForVisual(this);
                    image.EndInit();

                    JpegBitmapEncoder bitmapEncoder = new JpegBitmapEncoder();
                    bitmapEncoder.Frames.Add(BitmapFrame.Create(image));
                    using (FileStream fileStream = new FileStream(comic.ThumbnailPath, FileMode.Create))
                        bitmapEncoder.Save(fileStream);

                }
            }

        }

        private void LoadComics()
        {
            foreach (Defaults.CategorizedPath categorizedPath in Defaults.RootPaths)
            {
                DirectoryInfo rootDirectory = new DirectoryInfo(categorizedPath.Path);
                DirectoryInfo[] authorDirectories = rootDirectory.GetDirectories();

                foreach (DirectoryInfo authorDirectory in authorDirectories)
                {
                    if (Defaults.NameShouldBeIgnored(authorDirectory.Name))
                        continue;

                    LoadComicsForAuthor(authorDirectory, authorDirectory.Name, categorizedPath.Category, 2, null);
                }
            }
            Items = allItems;
        }

        private void LoadComicsForAuthor(DirectoryInfo directory, string author, string category, int depth, string previousParts)
        {
            depth -= 1;
            DirectoryInfo[] comicDirectories = directory.GetDirectories();

            foreach(DirectoryInfo comicDirectory in comicDirectories)
            {
                if (Defaults.NameShouldBeIgnored(comicDirectory.Name))
                    continue;

                string currentName = comicDirectory.Name;
                if (previousParts != null)
                    currentName = previousParts + " - " + currentName;

                Comic comic = new Comic(currentName, author, category, comicDirectory.FullName);
                if (comic.ImagePath != null)
                    allItems.Add(comic);

                if (depth > 0)
                    LoadComicsForAuthor(comicDirectory, author, category, depth, currentName);
            }
        }
        
        private void CommitSearch(object sender, TextChangedEventArgs e)
        {
            string searchText = ((TextBox)sender).Text.Trim().ToLowerInvariant();

            if (searchText == null || searchText.Length == 0)
                Items = allItems;
            else
                Items = allItems.Where(comic => comic.MatchesSearchText(searchText)).ToList();
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
            allItems.Clear();
            LoadComics();
            LoadComicThumbnails();
        }

        private void ContextMenu_ReloadThumbnails(object sender, RoutedEventArgs e)
        {
            // Delete existing ones first, but warn the user that it'll take a long time
            // Currently does basically nothing (unless you accidentally deleted something)
            LoadComicThumbnails();
        }

        private void ContextMenu_ShowSettings(object sender, RoutedEventArgs e)
        {
            Window settings = new SettingsWindow();
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
