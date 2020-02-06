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
        public const string ManuallyAddedComicCategoryName = "Manually Added";

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

                // it's on the main window to update sort descriptions, which has to happen after the main window knows
                // about the change in this property
                App.ComicsWindow?.UpdateComicSortDescriptions();
                UpdateFilterLists();
            }
        }

        public const string AvailableAuthorsPropertyName = "AvailableAuthors";
        public ObservableCollection<Checkable<string>> availableAuthors = new ObservableCollection<Checkable<string>>();
        public ObservableCollection<Checkable<string>> AvailableAuthors {
            get => this.availableAuthors;
            set {
                if (this.availableAuthors == value) {
                    return;
                }

                this.availableAuthors = value;
                NotifyPropertyChanged(AvailableAuthorsPropertyName);
                App.ComicsWindow?.UpdateAuthorSortDescriptions();
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
                App.ComicsWindow?.UpdateCategorySortDescriptions();
            }
        }

        // All loaded categories
        public const string AvailableTagsPropertyName = "AvailableTags";
        private ObservableCollection<Checkable<CustomSort<string>>> availableTags = new ObservableCollection<Checkable<CustomSort<string>>>();
        public ObservableCollection<Checkable<CustomSort<string>>> AvailableTags {
            get => this.availableTags;
            set {
                if (this.availableTags == value) {
                    return;
                }

                this.availableTags = value;
                NotifyPropertyChanged(AvailableTagsPropertyName);
                App.ComicsWindow?.UpdateTagSortDescriptions();
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
                App.ComicsWindow?.ClearSelections();
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

        /* Loads comics from database in a 3-step process:
         *  1. Retrieves all active comics from database
         *   -- this is when the program becomes usable --
         *  2. Validates all items exist on disk; deactivates those that don't
         *  3. Generate thumbnails if necessary
         */
        private async Task LoadComicsFromDatabase() {
            App.ComicsWindow?.PushFooter("LoadingIndicator", "Reloading...");
            App.ComicsWindow?.DisableInteractions();

            this.AvailableComics.Clear();
            this.AvailableAuthors.Clear();
            this.AvailableCategories.Clear();
            this.AvailableTags.Clear();
            await Task.Run(() => PopulateComicsFromDatabase());

            App.ComicsWindow?.PushFooter("LoadingIndicator", "Validating...");
            await Task.Run(() => ValidateLoadedComics());

            App.ComicsWindow?.PushFooter("LoadingIndicator", "Generating thumbnails...");
            await Task.Run(() => GenerateComicThumbnails());

            App.ComicsWindow?.PopFooter("LoadingIndicator");
            App.ComicsWindow?.EnableInteractions();
        }

        private void ValidateLoadedComics() {
            var validatedComics = new ObservableCollection<Comic>();
            var invalidComics = new List<Comic>();

            foreach (var comic in AvailableComics) {
                try {
                    var validatedComic = new Comic(comic.real_title, comic.real_author, comic.real_category, comic.path, comic.Metadata, dateAdded: comic.DateAdded);
                    validatedComics.Add(validatedComic);
                } catch (ComicLoadException) {
                    invalidComics.Add(comic);
                }
            }

            App.Current.Dispatcher.Invoke(() => {
                this.AvailableComics = validatedComics;
            });

            SQL.Database.Manager.RemoveComics(invalidComics);
        }

        // function that actually reloads comics
        private async Task ReloadComicsFromDisk() {
            
            App.ComicsWindow?.PushFooter("LoadingIndicator", "Reloading...");
            App.ComicsWindow?.DisableInteractions();

            var cachedComics = this.AvailableComics.Where(c => c.Category == ManuallyAddedComicCategoryName).ToArray();

            var newAvailableComics = new ObservableCollection<Comic>();

            await Task.Run(() => LoadComicsFromDisk(newAvailableComics));

            foreach (var comic in cachedComics) {
                AddComic(comic, newAvailableComics);
            }

            App.Current.Dispatcher.Invoke(() => {
                this.AvailableComics = newAvailableComics;
            });

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

        public void AddComicFromDisk(string title, string author, string category, string path, ObservableCollection<Comic> target) {
            AddComic(new Comic(title, author, category, path), target, true);
        }

        private void AddComic(Comic comic, ObservableCollection<Comic> target, bool recreateThumbnail = false) {
            foreach (var existingComic in target) {
                if (existingComic.UniqueIdentifier == comic.UniqueIdentifier) {
                    return;
                }
            }

            AddComicToAvailableComics(comic, target);

            if (recreateThumbnail || !(File.Exists(comic.ThumbnailPath))) {
                comic.GenerateThumbnail();
            }
           
            comic.Save();
        }

        public void RemoveComicFromDatabase(Comic comic) {
            SQL.Database.Manager.RemoveComic(comic);
            this.AvailableComics.Remove(comic);

            /* TODO: Currently, when a comic is removed its author/category/tag checkboxes aren't removed.
             * To do this, we need an efficient way of checking if an author/etc is the *only* author/etc remaining.
             * I recommend doing reference counting, perhaps taking advantage of a struct similar to CustomSort. */

            App.ComicsWindow?.ClearSelections();
            App.ComicsWindow?.RefreshComics();
        }

        private void PopulateComicsFromDatabase() {
            var comics = SQL.Database.Manager.AllComics();
            var comic_collection = new ObservableCollection<Comic>(comics);

            App.Current.Dispatcher.Invoke(() => {
                this.AvailableComics = comic_collection;
            });
        }

        public void UpdateFilterLists(bool updateAuthors = true, bool updateCategories = true, bool updateTags = true, 
                                      HashSet<string> excludedIds = null) {
            var authors_set = new HashSet<Checkable<string>>();
            var categories_set = new HashSet<string>();
            var tags_dict = new Dictionary<string, int>();

            foreach (var comic in this.AvailableComics) {
                if (excludedIds != null && excludedIds.Contains(comic.UniqueIdentifier)) {
                    continue;
                }

                authors_set.Add(comic.Author);
                categories_set.Add(comic.Category);
                foreach (var tag in comic.Tags) {
                    if (tags_dict.ContainsKey(tag)) {
                        tags_dict[tag] += 1;
                    } else {
                        tags_dict[tag] = 1;
                    }
                }
            }

            var authors = new ObservableCollection<Checkable<string>>(authors_set);
            var categories = new ObservableCollection<string>(categories_set);
            var tags = new ObservableCollection<Checkable<CustomSort<string>>>(
                tags_dict.Select(p => new Checkable<CustomSort<string>>(new CustomSort<string>(p.Key, p.Value)))
            );

            App.Current.Dispatcher.Invoke(() => {
                if (updateAuthors) {
                    this.AvailableAuthors = authors;
                    }
                if (updateCategories) {
                    this.AvailableCategories = categories;
                }
                if (updateTags) {
                    this.AvailableTags = tags;
                }
                App.ComicsWindow.RefreshFilter();
            });
        }

        // Loads all comics based on the current profile
        private void LoadComicsFromDisk(ObservableCollection<Comic> target) {
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
                    
                    LoadComicsForAuthorFromDisk(authorDirectory, authorDirectory.Name, categorizedPath.Category, Defaults.Profile.WorkTraversalDepth, null, target);

                    // for pdfs and such
                    FileInfo[] rootFiles = authorDirectory.GetFilesInNaturalOrder();
                    foreach (FileInfo file in rootFiles) {
                        if (Defaults.Profile.Extensions.Contains(file.Extension)) {
                            var comic = new Comic(file.Name, authorDirectory.Name, categorizedPath.Category, file.FullName);
                            AddComicToAvailableComics(comic, target);
                        }
                    }
                }
            }
        }

        // Given a directory corresponding to an author, adds subfolders in the directory as works by the author
        private void LoadComicsForAuthorFromDisk(DirectoryInfo directory, string author, string category, int depth, string previousParts, ObservableCollection<Comic> target) {
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
                    LoadComicsForAuthorFromDisk(comicDirectory, author, category, depth, currentName, target);
                }

                if (comic.FilePaths.Count() > 0) {
                    AddComicToAvailableComics(comic, target);
                }
            }
        }

        private void AddComicToAvailableComics(Comic comic, ObservableCollection<Comic> target) {
            target.Add(comic);
        }

        // Generates thumbnails for comics
        private void GenerateComicThumbnails() {
            foreach (Comic comic in this.AvailableComics) {
                if (!(File.Exists(comic.ThumbnailPath))) {
                    comic.GenerateThumbnail();
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

        // Used with "force" to generate a new database from scratch, or without "force" to
        // add new comics as well as update existing comics.
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
