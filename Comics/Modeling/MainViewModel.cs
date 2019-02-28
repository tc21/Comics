﻿using System;
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

        // The loading of comics in a profile is done asynchronously. This is done to improve
        // the fluidity of the program. Loading thumbnails is also done asynchronously, because 
        // it takes a long time and the user still should be able to use the program. If we load
        // a new profile before any of this is done, we modify the collection of comics while the 
        // async operations are still looping over it. We can cancel the operations, ensure these
        // two operations are themselves happening synchronously, or do this.
        private void ComicLoadStarted() {
            App.ComicsWindow?.PushFooter("LoadingIndicator", "Loading...");
            App.ComicsWindow?.DisableInteractions();
        }

        private void ComicLoadEnded() {
            App.ComicsWindow?.PopFooter("LoadingIndicator");
            App.ComicsWindow?.EnableInteractions();
        }

        // Reloads the comics based on the new profile, and then notifies the windows to update their UI.
        public async void UpdateComicsAfterProfileChanged() {
            await ReloadComicSteps();
            App.SettingsWindow?.PopulateProfileSettings();
            App.ComicsWindow?.RefreshComics();
        }

        // Public interface to reload all comics
        public async Task ReloadComics() {
            await ReloadComicSteps();
            App.ComicsWindow?.ClearSelections();
            App.ComicsWindow?.RefreshComics();
        }

        // function that actually reloads comics
        private async Task ReloadComicSteps() {
            ComicLoadStarted();
            this.AvailableComics.Clear();
            this.AvailableAuthors.Clear();
            this.AvailableCategories.Clear();
            this.AvailableTags.Clear();
            await Task.Run(() => LoadComics());
            App.ComicsWindow?.UpdateSortDescriptions();
            await Task.Run(() => GenerateComicThumbnails());
            ComicLoadEnded();
        }

        // Public interface to reload (regenerate) all thumbnails
        public async Task ReloadComicThumbnails() {
            await Task.Run(() => GenerateComicThumbnails());
            App.ComicsWindow?.RefreshComics();
        }

        // Loads all comics based on the current profile
        private void LoadComics() {
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
                    
                    LoadComicsForAuthor(authorDirectory, authorDirectory.Name, categorizedPath.Category, Defaults.Profile.WorkTraversalDepth, null);
                    FileInfo[] rootFiles = authorDirectory.GetFilesInNaturalOrder();
                    foreach (FileInfo file in rootFiles) {
                        if (Defaults.Profile.Extensions.Contains(file.Extension)) {
                            AddComicToAvailableComics(new Comic(file.Name, authorDirectory.Name, categorizedPath.Category, file.FullName));
                        }
                    }
                }

            }
        }

        // Given a directory corresponding to an author, adds subfolders in the directory as works by the author
        private void LoadComicsForAuthor(DirectoryInfo directory, string author, string category, int depth, string previousParts) {
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
                    LoadComicsForAuthor(comicDirectory, author, category, depth, currentName);
                }

                if (comic.FilePaths.Count > 0) {
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
                    comic.CreateThumbnail();
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
        public void UpdateDatabase() {
            var connection = SQL.Database.DatabaseConnection.ForCurrentProfile(true);
            
            foreach (var comic in this.AvailableComics) {
                connection.AddComic(comic);
            }
        }
    }
}
