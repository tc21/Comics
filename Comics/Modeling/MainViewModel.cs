using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Comics
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Model
        public const string PropertyName = "VisibleComics";
        //private ObservableCollection<Comic> allComics;
        private ObservableCollection<Comic> visibleComics;
        public ObservableCollection<Comic> VisibleComics
        {
            get
            {
                return visibleComics;
            }
            set
            {
                if (visibleComics == value)
                    return;
                visibleComics = value;
                NotifyPropertyChanged(PropertyName);
            }
        }

        public MainViewModel()
        {
            VisibleComics = new ObservableCollection<Comic>();
            LoadComics();
        }

        // View Model Events
        public void LoadComics()
        {
            foreach (Defaults.CategorizedPath categorizedPath in Defaults.RootPaths)
            {
                DirectoryInfo rootDirectory = new DirectoryInfo(categorizedPath.Path);
                DirectoryInfo[] authorDirectories = rootDirectory.GetDirectories();

                foreach (DirectoryInfo authorDirectory in authorDirectories)
                {
                    if (Defaults.NameShouldBeIgnored(authorDirectory.Name))
                        continue;

                    LoadComicsForAuthor(authorDirectory, authorDirectory.Name, categorizedPath.Category, Defaults.profile.WorkTraversalDepth, null);
                }
            }
        }

        private void LoadComicsForAuthor(DirectoryInfo directory, string author, string category, int depth, string previousParts)
        {
            depth -= 1;
            DirectoryInfo[] comicDirectories = directory.GetDirectories();

            foreach (DirectoryInfo comicDirectory in comicDirectories)
            {
                if (Defaults.NameShouldBeIgnored(comicDirectory.Name))
                    continue;

                string currentName = comicDirectory.Name;
                if (previousParts != null)
                    currentName = previousParts + " - " + currentName;

                Comic comic = new Comic(currentName, author, category, comicDirectory.FullName);
                if (comic.ImagePath != null)
                    VisibleComics.Add(comic);

                if (depth > 0)
                    LoadComicsForAuthor(comicDirectory, author, category, depth, currentName);
            }
        }

        public void LoadComicThumbnails()
        {
            // We're supposed to account for display scaling here.
            //int width = Defaults.ThumbnailWidthForVisual(this);
            int width = Defaults.profile.ImageWidth;
            // Very primitive thumbnail caching being done here
            foreach (Comic comic in VisibleComics)
            {
                if (!(File.Exists(comic.ThumbnailPath)))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(comic.ImagePath);
                    image.DecodePixelWidth = width;
                    image.EndInit();

                    JpegBitmapEncoder bitmapEncoder = new JpegBitmapEncoder();
                    bitmapEncoder.Frames.Add(BitmapFrame.Create(image));
                    using (FileStream fileStream = new FileStream(comic.ThumbnailPath, FileMode.Create))
                        bitmapEncoder.Save(fileStream);
                }
            }
        }

        public void ReloadComics()
        {
            VisibleComics.Clear();
            LoadComics();
            LoadComicThumbnails();
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
