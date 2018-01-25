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
        public const string VisibleComicsPropertyName = "VisibleComics";
        private ObservableCollection<Comic> allComics = new ObservableCollection<Comic>();
        private ObservableCollection<Comic> visibleComics = new ObservableCollection<Comic>();
        public ObservableCollection<Comic> VisibleComics
        {
            get { return visibleComics; }
            set
            {
                if (visibleComics == value)
                    return;
                visibleComics = value;
                NotifyPropertyChanged(VisibleComicsPropertyName);
            }
        }
        public const string VisibleAuthorsPropertyName = "VisibleAuthors";
        public ObservableCollection<Comic> VisibleAuthors
        {
            get {
                ObservableCollection<Comic> visibleAuthors = new ObservableCollection<Comic>();
                HashSet<string> displayed = new HashSet<string>();
                foreach (Comic comic in visibleComics)
                {
                    if (!displayed.Contains(comic.DisplayAuthor))
                    {
                        displayed.Add(comic.DisplayAuthor);
                        visibleAuthors.Add(comic);
                    }
                }
                return visibleAuthors;
            }
        }

        public int ImageHeight => Defaults.DefaultHeight;
        public int ImageWidth => Defaults.DefaultHeight;

        public ObservableCollection<Defaults.CategorizedPath> RootPaths
        {
            get { return new ObservableCollection<Defaults.CategorizedPath>(Defaults.profile.RootPaths); }
        }
        public ObservableCollection<string> SortPropertyDisplayName
        {
            get { return new ObservableCollection<string>(Comic.SortPropertyDisplayNames); }
        }

        public MainViewModel()
        {
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
            UpdateSearch(null);
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
                    allComics.Add(comic);

                if (depth > 0)
                    LoadComicsForAuthor(comicDirectory, author, category, depth, currentName);
            }
        }

        public void UpdateSearch(string searchText)
        {
            visibleComics.Clear();

            foreach (Comic comic in allComics)
            {
                if (!String.IsNullOrWhiteSpace(searchText) && !comic.MatchesSearchText(searchText))
                    continue;
                visibleComics.Add(comic);
            }
            NotifyPropertyChanged(VisibleComicsPropertyName);
            NotifyPropertyChanged(VisibleAuthorsPropertyName);
        }

        public void LoadComicThumbnails()
        {
            // We're supposed to account for display scaling here.
            //int width = Defaults.ThumbnailWidthForVisual(this);
            int width = Defaults.profile.ImageWidth;
            // Very primitive thumbnail caching being done here
            foreach (Comic comic in allComics)
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
            allComics.Clear();
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
