using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Comics {
    public class MainViewModel : INotifyPropertyChanged {
        //private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // Properties conforming to INotifyPropertyChanged, so they automatically update the UI when changed
        // All loaded comics
        public const string VisibleComicsPropertyName = "VisibleComics";
        private ObservableCollection<Comic> visibleComics = new ObservableCollection<Comic>();
        public ObservableCollection<Comic> VisibleComics {
            get => this.visibleComics;
            set {
                if (this.visibleComics == value) {
                    return;
                }

                this.visibleComics = value;
                NotifyPropertyChanged(VisibleComicsPropertyName);
            }
        }

        // All loaded authors
        public const string VisibleAuthorsPropertyName = "VisibleAuthors";
        public ObservableCollection<SortedString> visibleAuthors = new ObservableCollection<SortedString>();
        public ObservableCollection<SortedString> VisibleAuthors {
            get => this.visibleAuthors;
            set {
                if (this.visibleAuthors == value) {
                    return;
                }

                this.visibleAuthors = value;
                NotifyPropertyChanged(VisibleAuthorsPropertyName);
            }
        }

        // All loaded categories
        public const string VisibleCategoriesPropertyName = "VisibleCategories";
        public ObservableCollection<SortedString> visibleCategories = new ObservableCollection<SortedString>();
        public ObservableCollection<SortedString> VisibleCategories {
            get => this.visibleCategories;
            set {
                if (this.visibleCategories == value) {
                    return;
                }

                this.visibleCategories = value;
                NotifyPropertyChanged(VisibleCategoriesPropertyName);
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
        public ObservableCollection<string> profiles = new ObservableCollection<string>();
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
            FileInfo[] files = new DirectoryInfo(Defaults.UserProfileFolder).GetFiles("*.xmlprofile");

            int index = 0;
            for (int i = 0; i < files.Length; i++) {
                string name = Path.GetFileNameWithoutExtension(files[i].Name);
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

        // The loading of comics in a profile is done asynchronously. This is done to improve
        // the fluidity of the program. Loading thumbnails is also done asynchronously, because 
        // it takes a long time and the user still should be able to use the program. If we load
        // a now profile before any of this is done, we modify the collection of comics while the 
        // async operations are still looping over it. We can cancel the operations, ensure these
        // two operations are themselves happening synchronously, or do this.
        private void ProfileLoadStarted() {
            if (App.ComicsWindow != null) {
                App.ComicsWindow.Footer.Content = "Loading...";
                App.ComicsWindow.ProfileSelector.IsEnabled = false;
                App.ComicsWindow.SettingsButton.IsEnabled = false;
            }
            if (App.SettingsWindow != null) {
                App.SettingsWindow.ProfileSelector.IsEnabled = false;
            }
        }

        private void ProfileLoadEnded() {
            if (App.ComicsWindow != null) {
                App.ComicsWindow.ProfileSelector.IsEnabled = true;
                App.ComicsWindow.SettingsButton.IsEnabled = true;
            }
            if (App.SettingsWindow != null) {
                App.SettingsWindow.ProfileSelector.IsEnabled = true;
            }
        }

        // Reloads the comics based on the new profile, and then notifies the windows to update their UI.
        public async void UpdateComicsAfterProfileChanged() {
            //cancellationTokenSource.Cancel();
            //cancellationTokenSource = new CancellationTokenSource();
            ProfileLoadStarted();
            this.VisibleComics.Clear();
            await Task.Run(() => LoadComics(/*cancellationTokenSource.Token*/));
            await Task.Run(() => GenerateComicThumbnails(/*cancellationTokenSource.Token*/));
            ProfileLoadEnded();
            App.SettingsWindow?.PopulateProfileSettings();
            App.ComicsWindow?.RefreshAll();
        }

        // Public interface to reload all comics
        public async Task ReloadComics() {
            //cancellationTokenSource.Cancel();
            //cancellationTokenSource = new CancellationTokenSource();
            ProfileLoadStarted();
            this.VisibleComics.Clear();
            await Task.Run(() => LoadComics(/*cancellationTokenSource.Token*/));
            await Task.Run(() => GenerateComicThumbnails(/*cancellationTokenSource.Token*/));
            ProfileLoadEnded();
            App.ComicsWindow?.RefreshComics();
        }

        // Public interface to reload (regenerate) all thumbnails
        public async Task ReloadComicThumbnails() {
            await Task.Run(() => GenerateComicThumbnails(/*cancellationTokenSource.Token*/));
            App.ComicsWindow?.RefreshComics();
        }

        // Loads all comics based on the current profile
        private void LoadComics(/*CancellationToken cancellationToken*/) {
            Debug.Print("> lc");
            foreach (Defaults.CategorizedPath categorizedPath in Defaults.Profile.RootPaths) {
                if (!Directory.Exists(categorizedPath.Path)) {
                    continue;
                }

                DirectoryInfo rootDirectory = new DirectoryInfo(categorizedPath.Path);
                DirectoryInfo[] authorDirectories = rootDirectory.GetDirectories();

                foreach (DirectoryInfo authorDirectory in authorDirectories) {
                    if (Defaults.NameShouldBeIgnored(authorDirectory.Name)) {
                        continue;
                    }

                    //try
                    //{
                    LoadComicsForAuthor(authorDirectory, authorDirectory.Name, categorizedPath.Category, Defaults.Profile.WorkTraversalDepth, null/*, cancellationToken*/);
                    FileInfo[] rootFiles = authorDirectory.GetFiles();
                    foreach (FileInfo file in rootFiles) {
                        if (Defaults.Profile.Extensions.Contains(file.Extension)) {
                            AddComicToVisibleComics(new Comic(file.Name, authorDirectory.Name, categorizedPath.Category, file.FullName)/*, cancellationToken*/);
                        }
                    }
                    //}
                    //catch (OperationCanceledException)
                    //{
                    //    return;
                    //}
                }

            }
            Debug.Print("< lc");
        }

        // Given a directory corresponding to an author, adds subfolders in the directory as works by the author
        private void LoadComicsForAuthor(DirectoryInfo directory, string author, string category, int depth, string previousParts/*, CancellationToken cancellationToken*/) {
            depth -= 1;
            DirectoryInfo[] comicDirectories = directory.GetDirectories();

            foreach (DirectoryInfo comicDirectory in comicDirectories) {
                if (Defaults.NameShouldBeIgnored(comicDirectory.Name)) {
                    continue;
                }

                string currentName = comicDirectory.Name;
                if (previousParts != null) {
                    currentName = previousParts + " - " + currentName;
                }

                Comic comic = new Comic(currentName, author, category, comicDirectory.FullName);

                if (depth > 0) {
                    if (Defaults.Profile.TreatSubdirectoriesAsSeparateWorks) {
                        LoadComicsForAuthor(comicDirectory, author, category, depth, currentName/*, cancellationToken*/);
                    } else {
                        DirectoryInfo[] subdirectories = comicDirectory.GetDirectories();
                        foreach (DirectoryInfo subdirectory in subdirectories) {
                            AddFolderToExistingComic(subdirectory, comic, depth);
                        }
                    }
                }

                if (comic.FilePaths.Count > 0) {
                    AddComicToVisibleComics(comic/*, cancellationToken*/);
                }
            }
        }

        // Adds a comic to the visible comics list
        private void AddComicToVisibleComics(Comic comic/*, CancellationToken cancellationToken*/) {
            //cancellationToken.ThrowIfCancellationRequested();
            App.Current.Dispatcher.Invoke(() => {
                this.VisibleComics.Add(comic);
                if (!this.VisibleAuthors.Contains(comic.Author)) {
                    this.VisibleAuthors.Add(comic.Author);
                }

                if (!this.VisibleCategories.Contains(comic.Category)) {
                    this.VisibleCategories.Add(comic.Category);
                }
            });
        }

        // Given a folder and a comic, adds contents of the folder to the comic
        private void AddFolderToExistingComic(DirectoryInfo directory, Comic comic, int depth) {
            comic.AddDirectory(directory);
            depth -= 1;
            if (depth > 0) {
                DirectoryInfo[] subdirectories = directory.GetDirectories();
                foreach (DirectoryInfo subdirectory in subdirectories) {
                    AddFolderToExistingComic(subdirectory, comic, depth);
                }
            }
        }

        // Generates thumbnails for comics
        private void GenerateComicThumbnails(/*CancellationToken cancellationToken*/) {
            Debug.Print("> gct");
            foreach (Comic comic in this.VisibleComics) {
                if (!(File.Exists(comic.ThumbnailPath))) {
                    comic.CreateThumbnail();
                }
                //if (cancellationToken.IsCancellationRequested)
                //    return;
            }
            Debug.Print("< gct");
        }

        // Randomizes the .Random field for each comic
        public void RandomizeComics() {
            Random random = new Random();
            foreach (Comic comic in this.VisibleComics) {
                comic.Random = random.Next();
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
