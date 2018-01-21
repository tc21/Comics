﻿using System;
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
        private List<Comic> items = new List<Comic>();
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
            
            Collection.ItemsSource = items;

            LoadComics();
        }

        private void ApplicationDidLoad(object sender, EventArgs e)
        {
            LoadComicThumbnails();  // This can be very slow. Find a way to "background" it.
        }

        private void LoadComicThumbnails()
        {
            // Very primitive thumbnail caching being done here
            String thumbnailFolder = "C:\\Users\\Lanxia\\Downloads\\comics_thumbnails";
            foreach (Comic comic in items)
            {
                string thumbnailPath = System.IO.Path.Combine(thumbnailFolder, "[" + comic.Author + "]" + comic.Title + ".jpgthumbnail");

                if (!(File.Exists(thumbnailPath)))
                {

                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(comic.ImagePath);
                    image.DecodePixelHeight = Defaults.ThumbnailWidthForVisual(this);
                    image.EndInit();

                    JpegBitmapEncoder bitmapEncoder = new JpegBitmapEncoder();
                    bitmapEncoder.Frames.Add(BitmapFrame.Create(image));
                    using (FileStream fileStream = new FileStream(thumbnailPath, FileMode.Create))
                        bitmapEncoder.Save(fileStream);

                }

                comic.ThumbnailPath = thumbnailPath;
            }

        }

        private void LoadComics()
        {
            List<String> rootPaths = new List<string>
            {
                "D:\\ACG\\S\\Images\\Comic\\Artists\\long",
                "D:\\ACG\\S\\Images\\Comic\\Artists\\pictures",
                "D:\\ACG\\S\\Images\\Comic\\Artists\\short",
            };

            foreach (String rootPath in rootPaths)
            {
                DirectoryInfo rootDirectory = new DirectoryInfo(rootPath);
                DirectoryInfo[] authorDirectories = rootDirectory.GetDirectories();

                foreach (DirectoryInfo authorDirectory in authorDirectories)
                {
                    if (authorDirectory.Name.First() == '~')
                        continue;
                    DirectoryInfo[] comicDirectories = authorDirectory.GetDirectories();

                    foreach (DirectoryInfo comicDirectory in comicDirectories)
                    {
                        if (comicDirectory.Name.First() == '~')
                            continue;

                        FileInfo[] comicFiles = comicDirectory.GetFiles("*.*");
                        string firstImage = null;

                        foreach (FileInfo comicFile in comicFiles)
                        {
                            if (isImage(comicFile.Name))
                            {
                                firstImage = comicFile.FullName;
                                break;
                            }
                        }

                        if (firstImage != null)
                        {
                            Comic comic = new Comic
                            {
                                Title = comicDirectory.Name,
                                Author = authorDirectory.Name,
                                Path = comicDirectory.FullName,
                                ImagePath = firstImage,
                                ThumbnailPath = "blank.png"
                            };
                            items.Add(comic);
                        }
                    }
                }
            }
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
        
        private static readonly List<String> imageSuffixes = new List<String> { ".jpg", ".jpeg", ".png", ".tiff", ".bmp", ".gif" };
        private bool isImage(String filename)
        {
            string suffix = System.IO.Path.GetExtension(filename).ToLowerInvariant();
            return imageSuffixes.Contains(suffix);
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

        private void ContextMenu_ShowInExplorer(object sender, RoutedEventArgs e)
        {
            temporaryComic?.OpenContainingFolder();
            temporaryComic = null;
        }

        private void ContextMenu_ReloadComics(object sender, RoutedEventArgs e)
        {
            items.Clear();
            LoadComics();
            LoadComicThumbnails();
        }

        private void ContextMenu_ReloadThumbnails(object sender, RoutedEventArgs e)
        {
            // Delete existing ones first, but warn the user that it'll take a long time
            // Currently does basically nothing (unless you accidentally deleted something)
            LoadComicThumbnails();
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
            switch (((ComboBox)sender).SelectedIndex)
            {
                case 0:
                    items.Sort((x, y) => x.Title.CompareTo(y.Title));
                    break;
                case 1:
                    items.Sort((x, y) => x.Author.CompareTo(y.Author));
                    break;
                case 2:
                    items.Sort((x, y) => x.ImagePath.CompareTo(y.ImagePath));
                    break;
            }

            Collection?.Items.Refresh();
        }
    }
}
