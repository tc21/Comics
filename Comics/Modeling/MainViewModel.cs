using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
                string name = Path.GetFileNameWithoutExtension(files[i].Name);
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
                    FileInfo[] rootFiles = authorDirectory.GetFiles();
                    foreach (FileInfo file in rootFiles)
                    {
                        if (Defaults.Profile.Extensions.Contains(file.Extension))
                            AddComicToVisibleComics(new Comic(file.Name, authorDirectory.Name, categorizedPath.Category, file.FullName));
                    }
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

                if (depth > 0)
                {
                    if (Defaults.Profile.TreatSubdirectoriesAsSeparateWorks)
                    {
                        LoadComicsForAuthor(comicDirectory, author, category, depth, currentName);
                    }
                    else
                    {
                        DirectoryInfo[] subdirectories = comicDirectory.GetDirectories();
                        foreach (DirectoryInfo subdirectory in subdirectories)
                            AddFolderToExistingComic(subdirectory, comic, depth);
                    }
                }

                if (comic.FilePaths.Count > 0)
                    AddComicToVisibleComics(comic);
            }
        }

        private void AddComicToVisibleComics(Comic comic)
        {
            VisibleComics.Add(comic);
            if (!VisibleAuthors.Contains(comic.Author))
                VisibleAuthors.Add(comic.Author);
            if (!VisibleCategories.Contains(comic.Category))
                VisibleCategories.Add(comic.Category);
        }

        private void AddFolderToExistingComic(DirectoryInfo directory, Comic comic, int depth)
        {
            comic.AddDirectory(directory);
            depth -= 1;
            if (depth > 0)
            {
                DirectoryInfo[] subdirectories = directory.GetDirectories();
                foreach (DirectoryInfo subdirectory in subdirectories)
                    AddFolderToExistingComic(subdirectory, comic, depth);
            }
        }

        public void LoadComicThumbnails()
        {
            // We're supposed to account for display scaling here.
            //int width = Defaults.ThumbnailWidthForVisual(this);
            // Very primitive thumbnail caching being done here
            foreach (Comic comic in VisibleComics)
            {
                if (!(File.Exists(comic.ThumbnailPath)))
                    comic.CreateThumbnail();
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
