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
        public ObservableCollection<SortedString> visibleAuthors = new ObservableCollection<SortedString>();
        public ObservableCollection<SortedString> VisibleAuthors
        {
            get { return visibleAuthors; }
            set
            {
                if (visibleAuthors == value)
                    return;
                visibleAuthors = value;
                NotifyPropertyChanged(VisibleAuthorsPropertyName);
            }
        }

        public const string VisibleCategoriesPropertyName = "VisibleCategories";
        public ObservableCollection<SortedString> visibleCategories = new ObservableCollection<SortedString>();
        public ObservableCollection<SortedString> VisibleCategories
        {
            get { return visibleCategories; }
            set
            {
                if (visibleCategories == value)
                    return;
                visibleCategories = value;
                NotifyPropertyChanged(VisibleCategoriesPropertyName);
            }
        }

        public const string SelectedProfileCategoryName = "SelectedProfile";
        private int selectedProfile;
        public int SelectedProfile {
            get { return selectedProfile; }
            set
            {
                if (selectedProfile == value)
                    return;
                selectedProfile = value;
                NotifyPropertyChanged(SelectedProfileCategoryName);

                string profile = Profiles[selectedProfile];
                if (Defaults.Profile.ProfileName == profile)
                    return;

                if (Defaults.LoadProfile(profile))
                {
                    UpdateUIAfterProfileChanged();
                }
            }
        }

        public void UpdateUIAfterProfileChanged()
        {
            ReloadComics();
            App.SettingsWindow?.PopulateProfileSettings();
            App.ComicsWindow?.RefreshAll();
        }

        public const string ProfilesPropertyName = "Profiles";
        public ObservableCollection<string> profiles = new ObservableCollection<string>();
        public ObservableCollection<string> Profiles
        {
            get { return profiles; }
            set
            {
                if (profiles == value)
                    return;
                profiles = value;
                NotifyPropertyChanged(ProfilesPropertyName);
            }
        }

        public ObservableCollection<Defaults.CategorizedPath> RootPaths
        {
            get { return new ObservableCollection<Defaults.CategorizedPath>(Defaults.Profile.RootPaths); }
        }
        public ObservableCollection<string> SortPropertyDisplayName
        {
            get { return new ObservableCollection<string>(Comic.SortPropertyNames); }
        }

        public MainViewModel()
        {
            FileInfo[] files = new DirectoryInfo(Defaults.UserProfileFolder).GetFiles("*.xmlprofile");

            int index = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(files[i].Name);
                Profiles.Add(name);
                if (Defaults.Profile.ProfileName == name)
                    SelectedProfile = index;
                index++;
            }

            LoadComics();
        }

        // View Model Events
        public void LoadComics()
        {
            foreach (Defaults.CategorizedPath categorizedPath in Defaults.Profile.RootPaths)
            {
                DirectoryInfo rootDirectory = new DirectoryInfo(categorizedPath.Path);
                DirectoryInfo[] authorDirectories = rootDirectory.GetDirectories();

                foreach (DirectoryInfo authorDirectory in authorDirectories)
                {
                    if (Defaults.NameShouldBeIgnored(authorDirectory.Name))
                        continue;

                    LoadComicsForAuthor(authorDirectory, authorDirectory.Name, categorizedPath.Category, Defaults.Profile.WorkTraversalDepth, null);
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
                {
                    VisibleComics.Add(comic);
                    if (!VisibleAuthors.Contains(comic.Author))
                        VisibleAuthors.Add(comic.Author);
                    if (!VisibleCategories.Contains(comic.Category))
                        VisibleCategories.Add(comic.Category);
                }

                if (depth > 0)
                    LoadComicsForAuthor(comicDirectory, author, category, depth, currentName);
            }
        }

        public void LoadComicThumbnails()
        {
            // We're supposed to account for display scaling here.
            //int width = Defaults.ThumbnailWidthForVisual(this);
            int width = Defaults.Profile.ImageWidth;
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
