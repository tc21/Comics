using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Comics.Support;

namespace Comics {
    public class MainViewModel : INotifyPropertyChanged {
        //private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // Properties conforming to INotifyPropertyChanged, so they automatically update the UI when changed
        // All loaded comics
        public const string AvailableComicsPropertyName = "AvailableComics";
        private ObservableCollection<Comic> availableComics = new ObservableCollection<Comic>();
        public ObservableCollection<Comic> AvailableComics {
            get => this.availableComics;
            set {
                if (this.availableComics == value) {
                    return;
                }

                this.availableComics = value;
                NotifyPropertyChanged(AvailableComicsPropertyName);
            }
        }

        // there has to be a better way than this, which is why I'm inlining this solution for now
        public class CheckBoxString : INotifyPropertyChanged {
            private readonly string stored;

            private bool isChecked = false;
            public bool IsChecked {
                get => isChecked;
                set {
                    isChecked = value;
                    NotifyPropertyChanged("IsChecked");
                }
            }

            public CheckBoxString(string s) {
                stored = s;
            }

            public override string ToString() {
                return stored;
            }

            public override bool Equals(object obj) {
                if (obj is CheckBoxString) {
                    return this.stored.Equals(((CheckBoxString)obj).stored);
                }

                return this.stored.Equals(obj);
            }

            public override int GetHashCode() {
                return this.stored.GetHashCode();
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void NotifyPropertyChanged(string propertyName) {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public const string AvailableAuthorsPropertyName = "AvailableAuthors";
        public ObservableCollection<CheckBoxString> availableAuthors = new ObservableCollection<CheckBoxString>();
        public ObservableCollection<CheckBoxString> AvailableAuthors {
            get => this.availableAuthors;
            set {
                if (this.availableAuthors == value) {
                    return;
                }

                this.availableAuthors = value;
                NotifyPropertyChanged(AvailableAuthorsPropertyName);
            }
        }

        // All loaded categories
        public const string AvailableCategoriesPropertyName = "AvailableCategories";
        private ObservableCollection<string> availableCategories = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableCategories {
            get => this.availableCategories;
            set {
                if (this.availableCategories == value) {
                    return;
                }

                this.availableCategories = value;
                NotifyPropertyChanged(AvailableCategoriesPropertyName);
            }
        }

        // All loaded categories
        public const string AvailableTagsPropertyName = "AvailableTags";
        private ObservableCollection<string> availableTags = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableTags {
            get => this.availableTags;
            set {
                if (this.availableTags == value) {
                    return;
                }

                this.availableTags = value;
                NotifyPropertyChanged(AvailableTagsPropertyName);
            }
        }

        // The currently selected profile. Changing this updates internal settings and the UI
        public const string SelectedProfileCategoryName = "SelectedProfile";
        private int selectedProfile;
        public int SelectedProfile {
            get => this.selectedProfile;
            set {
                if (this.selectedProfile == value) {
                    return;
                }

                this.selectedProfile = value;
                NotifyPropertyChanged(SelectedProfileCategoryName);
                ProfileChanged();
            }
        }

        // List of profiles
        public const string ProfilesPropertyName = "Profiles";
        private ObservableCollection<string> profiles = new ObservableCollection<string>();
        public ObservableCollection<string> Profiles {
            get => this.profiles;
            set {
                if (this.profiles == value) {
                    return;
                }

                this.profiles = value;
                NotifyPropertyChanged(ProfilesPropertyName);
            }
        }

        // List of sort orders to display in ui
        public ObservableCollection<string> SortPropertyDisplayName => new ObservableCollection<string>(Comic.SortPropertyNames);

        // Initializer. Populates profiles and loads comics
        public MainViewModel() {
            LoadProfiles();
            UpdateComicsAfterProfileChanged();
        }

        private void LoadProfiles() {
            FileInfo[] files = new DirectoryInfo(Defaults.UserProfileFolder).GetFilesInNaturalOrder("*.profile.xml");

            int index = 0;
            for (int i = 0; i < files.Length; i++) {
                string name = files[i].Name.Substring(0, files[i].Name.Length - ".profile.xml".Length);
                this.Profiles.Add(name);
                if (Defaults.Profile.ProfileName == name) {
                    this.SelectedProfile = index;
                }

                index++;
            }
        }

        public void ReloadProfiles() {
            this.Profiles.Clear();
            LoadProfiles();
            ProfileChanged();
        }

        public void ProfileChanged() {
            if (this.selectedProfile < 0 || this.selectedProfile >= this.Profiles.Count) {
                return;
            }

            string profile = this.Profiles[this.selectedProfile];
            if (Defaults.Profile.ProfileName == profile) {
                return;
            }

            if (Defaults.LoadProfile(profile)) {
                UpdateComicsAfterProfileChanged();
            }
        }

        // Reloads the comics based on the new profile, and then notifies the windows to update their UI.
        public async void UpdateComicsAfterProfileChanged() {
            await LoadComicsFromDatabase();
            App.SettingsWindow?.PopulateProfileSettings();
            App.ComicsWindow?.RefreshComics();
        }

        public async void UpdateComicsAfterProfileUpdated() {
            // currently the same for debugging
            await LoadComicsFromDatabase();
            App.SettingsWindow?.PopulateProfileSettings();
            App.ComicsWindow?.RefreshComics();
        }


        // Public interface to reload all comics
        public async Task ReloadComics() {
            await ReloadComicsFromDisk();
            App.ComicsWindow?.ClearSelections();
            App.ComicsWindow?.RefreshComics();
        }

        private async Task LoadComicsFromDatabase() {
            App.ComicsWindow?.PushFooter("LoadingIndicator", "Loading...");
            App.ComicsWindow?.DisableInteractions();

            this.AvailableComics.Clear();
            this.AvailableAuthors.Clear();
            this.AvailableCategories.Clear();
            this.AvailableTags.Clear();
            this.AvailableTags.Add("Test Tag");

            await Task.Run(() => PopulateComicsFromDatabase());
            App.ComicsWindow?.UpdateSortDescriptions();
            await Task.Run(() => GenerateComicThumbnails());
            
            App.ComicsWindow?.PopFooter("LoadingIndicator");
            App.ComicsWindow?.EnableInteractions();
        }

        // function that actually reloads comics
        private async Task ReloadComicsFromDisk() {
            App.ComicsWindow?.PushFooter("LoadingIndicator", "Reloading...");
            App.ComicsWindow?.DisableInteractions();
            this.AvailableComics.Clear();
            this.AvailableAuthors.Clear();
            this.AvailableCategories.Clear();
            this.AvailableTags.Clear();
            await Task.Run(() => LoadComicsFromDisk());
            App.ComicsWindow?.UpdateSortDescriptions();
            await Task.Run(() => GenerateComicThumbnails());

            App.ComicsWindow?.PopFooter("LoadingIndicator");
            //App.ComicsWindow?.EnableInteractions();

            await UpdateDatabase();
        }

        // Public interface to reload (regenerate) all thumbnails
        public async Task ReloadComicThumbnails() {
            await Task.Run(() => GenerateComicThumbnails());
            App.ComicsWindow?.RefreshComics();
        }

        private void PopulateComicsFromDatabase() {
            var comics = SQL.Database.Manager.AllComics();
            var comic_collection = new ObservableCollection<Comic>(comics);

            App.Current.Dispatcher.Invoke(() => {
                this.AvailableComics = comic_collection;
            });

            UpdateFilterLists();
        }

        public void UpdateFilterLists() {
            var authors_set = new HashSet<CheckBoxString>();
            var categories_set = new HashSet<string>();
            var tags_set = new HashSet<string>();

            foreach (var comic in this.AvailableComics) {
                authors_set.Add(new CheckBoxString(comic.Author));
                categories_set.Add(comic.Category);
                tags_set.UnionWith(comic.Tags);
            }

            var authors = new ObservableCollection<CheckBoxString>(authors_set);
            var categories = new ObservableCollection<string>(categories_set);
            var tags = new ObservableCollection<string>(tags_set);

            App.Current.Dispatcher.Invoke(() => {
                this.AvailableAuthors = authors;
                this.AvailableCategories = categories;
                this.AvailableTags = tags;

                App.ComicsWindow.RefreshFilter();
            });
        }

        // Loads all comics based on the current profile
        private void LoadComicsFromDisk() {
            foreach (Defaults.CategorizedPath categorizedPath in Defaults.Profile.RootPaths) {
                if (!Directory.Exists(categorizedPath.Path)) {
                    continue;
                }

                DirectoryInfo rootDirectory = new DirectoryInfo(categorizedPath.Path);
                DirectoryInfo[] authorDirectories = rootDirectory.GetDirectoriesInNaturalOrder();

                foreach (DirectoryInfo authorDirectory in authorDirectories) {
                    if (Defaults.NameShouldBeIgnored(authorDirectory.Name)) {
                        continue;
                    }
                    
                    LoadComicsForAuthorFromDisk(authorDirectory, authorDirectory.Name, categorizedPath.Category, Defaults.Profile.WorkTraversalDepth, null);

                    // for pdfs and such
                    FileInfo[] rootFiles = authorDirectory.GetFilesInNaturalOrder();
                    foreach (FileInfo file in rootFiles) {
                        if (Defaults.Profile.Extensions.Contains(file.Extension)) {
                            var comic = new Comic(file.Name, authorDirectory.Name, categorizedPath.Category, file.FullName);
                            AddComicToAvailableComics(comic);
                        }
                    }
                }

            }
        }

        // Given a directory corresponding to an author, adds subfolders in the directory as works by the author
        private void LoadComicsForAuthorFromDisk(DirectoryInfo directory, string author, string category, int depth, string previousParts) {
            depth -= 1;
            DirectoryInfo[] comicDirectories = directory.GetDirectoriesInNaturalOrder();

            foreach (DirectoryInfo comicDirectory in comicDirectories) {
                if (Defaults.NameShouldBeIgnored(comicDirectory.Name)) {
                    continue;
                }

                string currentName = comicDirectory.Name;
                if (previousParts != null) {
                    currentName = previousParts + " - " + currentName;
                }

                Comic comic = new Comic(currentName, author, category, comicDirectory.FullName);

                if (depth > 0 && Defaults.Profile.SubdirectoryAction == Defaults.SubdirectoryAction.SEPARATE) {
                    LoadComicsForAuthorFromDisk(comicDirectory, author, category, depth, currentName);
                }

                if (comic.FilePaths.Count() > 0) {
                    AddComicToAvailableComics(comic);
                }
            }
        }

        private void AddComicToAvailableComics(Comic comic) {
            App.Current.Dispatcher.Invoke(() => {
                this.AvailableComics.Add(comic);
                if (!this.AvailableAuthors.Contains(new CheckBoxString(comic.Author))) {
                    this.AvailableAuthors.Add(new CheckBoxString(comic.Author));
                }

                if (!this.AvailableCategories.Contains(comic.Category)) {
                    this.AvailableCategories.Add(comic.Category);
                }

                foreach (var tag in comic.Tags) {
                    if (!this.AvailableTags.Contains(tag)) {
                        this.AvailableTags.Add(tag);
                    }
                }
            });
        }

        // Generates thumbnails for comics
        private void GenerateComicThumbnails() {
            foreach (Comic comic in this.AvailableComics) {
                if (!(File.Exists(comic.ThumbnailPath))) {
                    comic.RecreateThumbnail();
                }
            }
        }

        // Randomizes the .Random field for each comic
        public void RandomizeComics() {
            Random random = new Random();
            foreach (Comic comic in this.AvailableComics) {
                comic.Random = random.Next();
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Currently only generates a database from scratch
        public async Task UpdateDatabase(bool force = false) {
            App.ComicsWindow?.PushFooter("DatabaseIndicator", "Building database...");
            App.ComicsWindow?.DisableInteractions();

            await Task.Run(() => {
                var connection = SQL.Database.DatabaseConnection.ForCurrentProfile(force);

                if (!force) {
                    connection.InvalidateAllComics();
                }

                foreach (var comic in this.AvailableComics) {
                    if (force || !connection.HasComic(comic.UniqueIdentifier)) {
                        connection.AddComic(comic, false);
                    } else {
                        connection.UpdateComic(comic, false);
                    }
                }


            });

            App.ComicsWindow?.PopFooter("DatabaseIndicator");
            App.ComicsWindow?.EnableInteractions();
        }
    }
}
