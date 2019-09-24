using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Comics {
    public class Defaults {
        // The default settings currently being used
        public static UserDefaults Profile { get; set; }

        // some default values
        public static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".tiff", ".bmp", ".gif" };
        private const string defaultProfileName = "New Profile (Automatically Generated)";
        private const int defaultImageHeight = 254;
        private const int defaultTitleFontSize = 12;
        private const int defaultSubtitleFontSize = 10;
        private const string defaultFontFamily = "Segoe UI";
        private const int defaultWidth = 180;  // Using the "recommended" value of height / sqrt(2)
        private const int defaultMargin = 3;
        private const int defaultReactionTime = 140;
        private const int defaultWorkTraversalDepth = 1;  // starts at 1

        // may be able to customize in the future
        public static int TitleFontSize => defaultTitleFontSize;
        public static int SubtitleFontSize => defaultSubtitleFontSize;
        public static int TileMargin => defaultMargin;
        public static string ApplicationFontFamily => defaultFontFamily;

        // Margin applied to the right hand side of the collection area to prevent the user 
        // resizing faster than the application can resize tiles, which can cause the application to
        // quickly alternate between the right number of columns and one less.
        public static int SafetyMargin => 2;

        // The height of the title and subtitle, combined
        public static int LabelHeight => LineHeightForFontWithSize(TitleFontSize) + LineHeightForFontWithSize(SubtitleFontSize);



        public enum SubdirectoryAction {
            COMBINE,  // combine all files into one comic
            SEPARATE  // treat subdirectories as separate comics
        }

        public enum IgnoredPrefixAction {
            IGNORE,  // pretend the folder doesn't exist
            INVISIBLE,  // pretend everything in the folder is in the parent folder
            EXTEND_AUTHOR  // pretend the folder name is part of its child folder's names
        }

        // Default settings for each instance
        public class UserDefaults {
            public string ProfileName { get; set; }
            public int ImageHeight { get; set; }
            public int ImageWidth { get; set; }
            public List<CategorizedPath> RootPaths { get; set; }
            public List<string> Extensions { get; set; }
            public List<string> IgnoredPrefixes { get; set; }

            // Used for automatic naming naming, etc. Perhaps we'll eventually have better ways of doing it
            public int WorkTraversalDepth { get; set; }
            
            public SubdirectoryAction SubdirectoryAction { get; set; }
            public IgnoredPrefixAction IgnoredPrefixAction { get; set; }

            public StartupApplication DefaultApplication { get; set; }
            public string ExecutionArguments { get; set; }



            public int ItemHeight => ImageHeight + LabelHeight + 2 * TileMargin;
            public int ItemWidth => ImageWidth + 2 * TileMargin;
            public string DatabaseFile => Path.Combine(UserDatabaseFolder, Profile.ProfileName + ".library.db");
        }

        // Searializable "tuple"
        public class CategorizedPath {
            public string Category { get; set; }
            public string Path { get; set; }
        }

        public class StartupApplication {
            public enum Application {
                Viewer, Custom
            }

            public const string ViewerIndicator = "{Viewer}";

            public Application Type;
            public string Path;

            public string Name => this.Type == Application.Viewer ? ViewerIndicator : this.Path;

            public void StartComic(Comic comic) {
                var arguments = Comic.ExecutionString.CreateExecutionArguments(Profile.ExecutionArguments, comic);

                if (this.Type == Application.Viewer) {
                    var viewer = new Viewer.MainWindow(arguments.ToArray()) {
                        Top = Support.Helper.RestrictToRange(Properties.Settings.Default.ViewerTop, 0, null),
                        Left = Support.Helper.RestrictToRange(Properties.Settings.Default.ViewerLeft, 0, null),
                        Height = Support.Helper.RestrictToRange(Properties.Settings.Default.ViewerHeight, 200, null),
                        Width = Support.Helper.RestrictToRange(Properties.Settings.Default.ViewerWidth, 200, null)
                    };

                    var conn = SQL.Database.DatabaseConnection.ForCurrentProfile();
                    viewer.GoToIndex(conn.GetProgress(comic));

                    var customContextMenuItems = new List<object>();
                    var loveItem = new System.Windows.Controls.MenuItem { Header = "Love" };
                    loveItem.Click += ((o, e) => comic.Loved = !comic.Loved);
                    var dislikeItem = new System.Windows.Controls.MenuItem() { Header = "Dislike" };
                    dislikeItem.Click += ((o, e) => comic.Disliked = !comic.Disliked);
                    var thumbnailItem = new System.Windows.Controls.MenuItem() { Header = "Set Thumbnail as Current Image" };
                    thumbnailItem.Click += ((o, e) => {
                        // This also needs to be more generic
                        comic.Metadata.ThumbnailSource = viewer.CurrentImage;
                        comic.Save();

                        File.Delete(comic.ThumbnailPath);
                        comic.RecreateThumbnail();
                        App.ComicsWindow.RefreshComics();
                    });
                    customContextMenuItems.Add(loveItem);
                    customContextMenuItems.Add(dislikeItem);
                    customContextMenuItems.Add(thumbnailItem);
                    viewer.CustomContextMenuItems = customContextMenuItems;

                    viewer.Closing += ((sender, e) => {
                        Properties.Settings.Default.ViewerTop = viewer.Top;
                        Properties.Settings.Default.ViewerLeft = viewer.Left;
                        Properties.Settings.Default.ViewerHeight = viewer.ActualHeight;
                        Properties.Settings.Default.ViewerWidth = viewer.ActualWidth;

                        conn.SetProgress(comic, viewer.CurrentImageIndex);
                    });

                    viewer.Show();
                } else {
                    if (!File.Exists(this.Path)) {
                        MessageBox.Show("Startup application doesn't exist!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    Process.Start(this.Path, string.Join(" ", arguments.Select(p => "\"" + p + "\"")));
                }
            }

            public static StartupApplication Viewer() {
                return new StartupApplication { Type = Application.Viewer };
            }

            public static StartupApplication Custom(string path) {
                if (!File.Exists(path)) {
                    throw new FileNotFoundException("Not a valid file", path);
                }
                return new StartupApplication { Type = Application.Custom, Path = path };
            }

            public static StartupApplication Interpolate(string name) {
                if (name == ViewerIndicator) {
                    return Viewer();
                } else {
                    return Custom(name);
                }
            }
        }

        // Initialized a "default" defaults which is probably quickly replaced
        static Defaults() {
            if (!LoadProfile(Properties.Settings.Default.CurrentProfile)) {
                CreateNewProfile(null);
            }
        }


        // Calculates the height of a label containing a font with this size. 
        public static int LineHeightForFontWithSize(int fontsize) {
            return (int)Math.Ceiling(fontsize * 4.0 / 3.0);
        }

        public static bool NameShouldBeIgnored(string name) {
            foreach (string prefix in Profile.IgnoredPrefixes) {
                if (name.StartsWith(prefix)) {
                    return true;
                }
            }

            return false;
        }

        // How the application was first coded

        // Provides dynamically-sized widths and heights to the wrap panel
        private static int MaximumDynamicWidth => (int)(16.301 / 10 * Profile.ItemWidth + 1);
        public static Size DynamicSize(double viewPortWidth) {
            if (viewPortWidth < Profile.ItemWidth) {
                return new Size(Profile.ItemWidth, Profile.ItemHeight);
            }

            viewPortWidth -= SafetyMargin;
            int numberOfColumns = EstimateNumberOfColumns(viewPortWidth / Profile.ItemWidth);
            int dynamicWidth = (int)(viewPortWidth / numberOfColumns);
            int dynamicImageWidth = dynamicWidth - 2 * TileMargin;
            int dynamicImageHeight = (int)Math.Round((double)Profile.ImageHeight * dynamicImageWidth / Profile.ItemWidth);
            return new Size(dynamicImageWidth + 2 * TileMargin, dynamicImageHeight + LabelHeight + 2 * TileMargin);
        }

        // Some weird formula
        private static int EstimateNumberOfColumns(double n) {
            double[] magic_numbers = { 0.857, 2.037, 3.427, 4.973, 6.644, 8.418, 12.221, 14.23, 16.301 };
            for (int i = 0; i < 9; i++) {
                if (n < magic_numbers[i]) {
                    return i + 1;
                }
            }
            return 10;
        }

        // Probably not that efficient - blame the wrap panel
        public static int DynamicHeight(double viewPortWidth) {
            return (int)DynamicSize(viewPortWidth).Height;
        }

        public static int DynamicWidth(double viewPortWidth) {
            return (int)DynamicSize(viewPortWidth).Width;
        }

        // Only one thumbnail is generated, which needs to account for display scaling
        public static int ThumbnailWidthForVisual() {
            double scale = Properties.Settings.Default.DisplayScale;
            return (int)Math.Ceiling(scale * MaximumDynamicWidth);
        }

        // Where to store user data (hint: it's Username\AppData\Local\TC-C7)
        public static string UserDataFolder {
            get {
                string userDataFolder = Properties.Settings.Default.StorageFullPath;
                if (!Path.IsPathRooted(userDataFolder)) {
                    string parentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    userDataFolder = Path.Combine(parentFolder, "TC-C7", "Comics");
                    Properties.Settings.Default.StorageFullPath = userDataFolder;
                    Properties.Settings.Default.Save();
                }

                if (!Directory.Exists(userDataFolder)) {
                    Directory.CreateDirectory(userDataFolder);
                }

                return userDataFolder;
            }
        }


        private static string UserFolderFor(string name) {
            var userProfileFolder = Path.Combine(UserDataFolder, name);

            if (!Directory.Exists(userProfileFolder)) {
                Directory.CreateDirectory(userProfileFolder);
            }

            return userProfileFolder;
        }

        public static string UserProfileFolder => UserFolderFor("profiles");
        public static string UserThumbnailsFolder => UserFolderFor("thumbnails");
        public static string UserMetadataFolder => UserFolderFor("metadata");
        public static string UserDatabaseFolder => UserFolderFor("database");

        public static void SaveProfile() {
            XmlSerializer writer = new XmlSerializer(typeof(UserDefaults));
            string path = Path.Combine(UserProfileFolder, Profile.ProfileName + ".profile.xml");
            string tempPath = path + ".tmp";

            using (FileStream tempFile = File.Create(tempPath)) {
                writer.Serialize(tempFile, Profile);
            }

            if (File.Exists(path)) {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }

        public static bool LoadProfile(string name) {
            UserDefaults profile;
            XmlSerializer reader = new XmlSerializer(typeof(UserDefaults));
            string path = Path.Combine(UserProfileFolder, name + ".profile.xml");

            if (!File.Exists(path)) {
                return false;
            }

            using (StreamReader file = new StreamReader(path)) {
                profile = (UserDefaults)reader.Deserialize(file);
            }

            Profile = profile;
            Properties.Settings.Default.CurrentProfile = Defaults.Profile.ProfileName;
            Properties.Settings.Default.Save();

            return true;
        }

        public static string FormatExtension(string extension) {
            if (string.IsNullOrWhiteSpace(extension)) {
                return null;
            }

            extension = extension.Trim();
            if (extension.Any(char.IsWhiteSpace) || extension.Substring(1).Contains('.')) {
                return null;
            }

            if (extension[0] == '.') {
                return extension;
            }

            return "." + extension;
        }

        public static string FormatText(string prefix) {
            if (string.IsNullOrWhiteSpace(prefix)) {
                return prefix;
            }

            return prefix.Trim();
        }

        public static string FormatDirectory(string directory) {
            if (Directory.Exists(directory)) {
                return Path.GetFullPath(directory);
            }

            return null;
        }

        public static bool NameIsValidNameForNewProfile(string name) {
            string path = Path.Combine(UserProfileFolder, name + ".profle.xml");
            return !File.Exists(path);
        }

        public static string GenerateValidNameForNewProfile(string suggestedName) {
            if (NameIsValidNameForNewProfile(suggestedName)) {
                return suggestedName;
            }

            for (int i = 1; i <= 65536; i++) {
                string name = suggestedName + " (" + i.ToString() + ")";
                if (NameIsValidNameForNewProfile(name)) {
                    return name;
                }
            }
            throw new Exception("Could not generate new name for a profile: all names in use");
        }

        public static void RenameCurrentProfile(string newname) {
            if (Profile.ProfileName == newname) {
                return;
            }

            string previousName = Profile.ProfileName;
            Profile.ProfileName = newname;
            SaveProfile();
            if (File.Exists(Path.Combine(UserProfileFolder, Profile.ProfileName + ".profle.xml"))) {
                File.Delete(Path.Combine(UserProfileFolder, previousName + ".profle.xml"));
            }

            Properties.Settings.Default.CurrentProfile = newname;
        }

        public static bool IsValidFileName(string name) {
            foreach (char invalid in Path.GetInvalidFileNameChars()) {
                if (name.Contains(invalid)) {
                    return false;
                }
            }
            return true;
        }

        public static void DeleteCurrentProfile() {
            File.Delete(Path.Combine(UserProfileFolder, Profile.ProfileName + ".profle.xml"));
        }

        public static void CreateNewProfile(string name) {
            // Used internally for generating a temp profile
            if (name is null) {
                Profile = new UserDefaults() {
                    ProfileName = defaultProfileName,
                    ImageHeight = defaultImageHeight,
                    ImageWidth = defaultWidth,
                    RootPaths = new List<CategorizedPath>(),
                    Extensions = new List<string>(),
                    IgnoredPrefixes = new List<string>(),
                    WorkTraversalDepth = defaultWorkTraversalDepth,
                    SubdirectoryAction = SubdirectoryAction.SEPARATE,
                    IgnoredPrefixAction = IgnoredPrefixAction.IGNORE
                };
                return;
            }

            // Display scaling thing
            if (name == defaultProfileName) {
                App.ComicsWindow.UpdateDisplayScale();
            }

            if (Profile != null) {
                Profile.ProfileName = name;
                Properties.Settings.Default.CurrentProfile = name;
                SaveProfile();
            } else {
                Profile = new UserDefaults() {
                    ProfileName = name,
                    ImageHeight = defaultImageHeight,
                    ImageWidth = defaultWidth,
                    RootPaths = new List<CategorizedPath>()
                    {
                        new CategorizedPath() {Category="Pictures", Path=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}
                    },
                    Extensions = new List<string>() { ".jpg", ".jpeg", ".png", ".tiff", ".bmp", ".gif" },
                    IgnoredPrefixes = new List<string>() { "~", "(" },
                    WorkTraversalDepth = defaultWorkTraversalDepth,
                    SubdirectoryAction = SubdirectoryAction.SEPARATE,
                    IgnoredPrefixAction = IgnoredPrefixAction.IGNORE
                };
            }
        }
    }
}
