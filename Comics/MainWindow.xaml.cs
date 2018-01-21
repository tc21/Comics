using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Comics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ComicItem> items = new List<ComicItem>();

        public MainWindow()
        {
            InitializeComponent();

            Collection.SelectionChanged += Collection_SelectionChanged;

            String rootPath = "C:\\Users\\Lanxia\\Downloads\\sc\\abgrund (さいかわゆさ)";
            DirectoryInfo rootDirectory = new DirectoryInfo(rootPath);

            DirectoryInfo[] comicDirectories = rootDirectory.GetDirectories();
            foreach (DirectoryInfo comicDirectory in comicDirectories)
            {
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
                    items.Add(new ComicItem
                    {
                        Title = comicDirectory.Name,
                        Author = rootDirectory.Name,
                        ThumbnailPath = thumbnail
                    });
                }
            }

            Collection.ItemsSource = items;
        }

        public class ComicItem
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public string ThumbnailPath { get; set; }
        }

        private static readonly List<String> imageSuffixes = new List<String> { ".jpg", ".jpeg", ".png", ".tiff", ".bmp", ".gif" };
        private bool isImage(String filename)
        {
            string suffix = System.IO.Path.GetExtension(filename).ToLowerInvariant();
            return imageSuffixes.Contains(suffix);
        }

        private void openComic(ComicItem comic)
        {
            Process.Start(comic.ThumbnailPath);
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

        private void Collection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            openComic(Collection.SelectedItem as ComicItem);
            Collection.select
        }

        private void ContextMenu_Open(object sender, RoutedEventArgs e)
        {
            openComic(Collection.SelectedItem as ComicItem);
        }
    }
}
