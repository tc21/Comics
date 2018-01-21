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
        private Timer actionDelayTimer = new Timer(200);  

        public MainWindow()
        {
            InitializeComponent();
            DisableActions(null, null);

            actionDelayTimer.Elapsed += EnableActions;
            Activated += EnableActionsWithDelay;
            Deactivated += DisableActions;

            String serializedFile = "C:\\Users\\Lanxia\\Downloads\\comics.tmp";
            if (File.Exists(serializedFile))
            {
                
                using (Stream stream = File.Open(serializedFile, FileMode.Open))
                {
                    Debug.Write("Using serizlized file");
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    items = binaryFormatter.Deserialize(stream) as List<Comic>;
                    Debug.Write("Backend load completed");
                    Collection.ItemsSource = items;
                    Debug.Write("Succesfully set ItemsSource");
                }
                return;
            }
            Debug.Write("Not using serizlized file");
            // This will need to be changed
            List<String> rootPaths = new List<string>
            {
                "D:\\ACG\\S\\Images\\Comic\\Artists\\short",
                "D:\\ACG\\S\\Images\\Comic\\Artists\\long",
                "D:\\ACG\\S\\Images\\Comic\\Artists\\pictures",
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
                        String thumbnail = null;
                        foreach (FileInfo comicFile in comicFiles)
                        {
                            if (isImage(comicFile.Name))
                            {
                                thumbnail = comicFile.FullName;
                                break;
                            }
                        }
                        if (thumbnail != null)
                        {
                            items.Add(new Comic
                            {
                                Title = comicDirectory.Name,
                                Author = authorDirectory.Name,
                                ThumbnailPath = thumbnail
                            });
                        }
                    }
                }
            }

            //using (Stream stream = File.Open(serializedFile, FileMode.Create))
            //{
            //    BinaryFormatter binaryFormatter = new BinaryFormatter();
            //    binaryFormatter.Serialize(stream, items);
            //}
            Debug.Write("Backend load completed");
            Collection.ItemsSource = items;
            Debug.Write("Succesfully set ItemsSource");
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
            Debug.Write("Break\n");
        }

        private void ContextMenu_Open(object sender, RoutedEventArgs e)
        {
            temporaryComic?.openWithDefaultApplication();
            temporaryComic = null;
        }

        private void DockPanel_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Comic comic = VisualHelper.ComicAtMouseButtonEvent(sender, e);
            if (comic != null && comic.Equals(temporaryComic))
                comic.openWithDefaultApplication();
            temporaryComic = null;
        }

        private void DockPanel_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            temporaryComic = VisualHelper.ComicAtMouseButtonEvent(sender, e);
        }

        private void DockPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                    items.Sort((x, y) => x.ThumbnailPath.CompareTo(y.ThumbnailPath));
                    break;
            }
            Collection?.Items.Refresh();
        }
    }
}
