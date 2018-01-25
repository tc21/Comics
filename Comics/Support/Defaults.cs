using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Comics
{
    public class Defaults
    {
        // The default settings currently being used
        public static UserDefaults profile;

        // Default settings for each instance
        public class UserDefaults
        {
            public string ProfileName { get; set; }
            public int ImageHeight { get; set; }
            public int ImageWidth { get; set; }
            public int TitleFontSize { get; set; }
            public int SubtitleFontSize { get; set; }
            public int TileMargin { get; set; }
            // The all-important time between application is activated and application starts accepting left clicks
            public int ReactionTime { get; set; }
            // This user-specified font family will fall back to default (probably Segoe UI) if it doesn't exist
            public string ApplicationFontFamily { get; set; }
            public List<CategorizedPath> RootPaths { get; set; }
            public List<string> Extensions { get; set; }
            public List<string> IgnoredPrefixes { get; set; }
            // Used for automatic naming naming, etc. Perhaps we'll eventually have better ways of doing it
            public int WorkTraversalDepth { get; set; }

            // Margin applied to the right hand side of the collection area to prevent the user 
            // resizing faster than the application can resize tiles, which can cause the application to
            // quickly alternate between the right number of columns and one less.
            public int SafetyMargin
            {
                get
                {
                    return 16 + 2 * defaultMargin;
                }
            }

            // The height of the title and subtitle, combined
            public int LabelHeight
            {
                get
                {
                    return LineHeightForFontWithSize(TitleFontSize) + LineHeightForFontWithSize(SubtitleFontSize);
                }
            }
        }

        // Searializable "tuple"
        public class CategorizedPath
        {
            public string Category { get; set; }
            public string Path { get; set; }
        }

        // Initialized a "default" defaults which is probably quickly replaced
        static Defaults()
        {
            if (!LoadProfile(Properties.Settings.Default.CurrentProfile))
            {
                string automaticallyGeneratedProfileName = "New Profile (Automatically Generated)";
                profile = new UserDefaults()
                {
                    ProfileName = automaticallyGeneratedProfileName,
                    ImageHeight = defaultImageHeight,
                    ImageWidth = defaultWidth,
                    TitleFontSize = defaultTitleFontSize,
                    SubtitleFontSize = defaultSubtitleFontSize,
                    ApplicationFontFamily = defaultFontFamily,
                    TileMargin = defaultMargin,
                    ReactionTime = defaultReactionTime,
                    RootPaths = new List<CategorizedPath>()
                    {
                        new CategorizedPath() {Category="Pictures", Path=Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)}
                    },
                    Extensions = new List<string>() { ".jpg", ".jpeg", ".png", ".tiff", ".bmp", ".gif" },
                    IgnoredPrefixes = new List<string>() { "~", "(" },
                    WorkTraversalDepth = defaultWorkTraversalDepth,
            };
                Properties.Settings.Default.CurrentProfile = automaticallyGeneratedProfileName;
                Properties.Settings.Default.Save();
                SaveProfile();
            }
        }
        
        private const int defaultImageHeight = 254;
        private const int defaultTitleFontSize = 12;
        private const int defaultSubtitleFontSize = 10;
        private const string defaultFontFamily = "Segoe UI";
        private const int defaultWidth = 180;  // Using the recommended value of height / sqrt(2)
        private const int defaultMargin = 3;
        private const int defaultReactionTime = 140;
        private const int defaultWorkTraversalDepth = 1;

        // Calculates the height of a label containing a font with this size. 
        public static int LineHeightForFontWithSize(int fontsize)
        {
            return (int)Math.Ceiling(fontsize * 4.0 / 3.0);
        }

        public static bool NameShouldBeIgnored(string name)
        {
            foreach (string prefix in profile.IgnoredPrefixes)
                if (name.StartsWith(prefix))
                    return true;
            return false;
        }

        // The one thing that needs to wait to be user-defined
        public static List<CategorizedPath> RootPaths { get { return profile.RootPaths; } }
        public static List<string> ImageSuffixes { get { return profile.Extensions; } }

        // How the application was first coded
        public static int SafetyMargin { get { return 16 + 2 * profile.TileMargin; } }
        public static int DefaultHeight { get { return profile.ImageHeight + profile.LabelHeight + 2 * profile.TileMargin; } }
        public static int DefaultWidth { get { return profile.ImageWidth + 2 * profile.TileMargin; } }
        public static int ActivationDelay { get { return profile.ReactionTime; } }

        // Provides dynamically-sized widths and heights to the wrap panel
        private static int MaximumDynamicWidth { get { return 2 * DefaultWidth - 1; } }
        public static Size DynamicSize (double viewPortWidth)
        {
            if (viewPortWidth < DefaultWidth)
                return new Size(DefaultWidth, DefaultHeight);

            viewPortWidth -= 16;
            int numberOfColumns = EstimateNumberOfColumns(viewPortWidth / DefaultWidth);
            int dynamicWidth = (int)(viewPortWidth / numberOfColumns);
            int dynamicImageWidth = dynamicWidth - 2 * profile.TileMargin;
            int dynamicImageHeight = (int) Math.Round((double)profile.ImageHeight * dynamicImageWidth / DefaultWidth);
            return new Size(dynamicImageWidth + 2 * profile.TileMargin,
                dynamicImageHeight + profile.LabelHeight + 2 * profile.TileMargin);
        }

        // Some weird formula
        private static int EstimateNumberOfColumns(double n)
        {
            double[] magic_numbers = { 0.857, 2.037, 3.427, 4.973, 6.644, 8.418, 12.221, 14.23, 16.301 };
            for (int i = 0; i < 9; i++)
            {
                if (n < magic_numbers[i])
                    return i + 1;
            }
            return 10;
        }

        // Probably not that efficient - blame the wrap panel
        public static int DynamicHeight(double viewPortWidth)
        {
            return (int)DynamicSize(viewPortWidth).Height;
        }

        public static int DynamicWidth(double viewPortWidth)
        {
            return (int)DynamicSize(viewPortWidth).Width;
        }

        // Only one thumbnail is generated, which needs to account for display scaling
        public static int ThumbnailWidthForVisual(Visual visual)
        {
            double scale = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice.M11;
            return (int)Math.Ceiling(scale * MaximumDynamicWidth);
        }
        
        // Where to store user data (hint: it's Username\AppData\Local\TC-C7)
        public static string UserDataFolder
        {
            get
            {
                string userDataFolder = Properties.Settings.Default.StorageFullPath;
                if (!Path.IsPathRooted(userDataFolder))
                {
                    string parentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    userDataFolder = Path.Combine(parentFolder, "TC-C7", "Comics");
                    Properties.Settings.Default.StorageFullPath = userDataFolder;
                    Properties.Settings.Default.Save();
                }

                if (!Directory.Exists(userDataFolder))
                    Directory.CreateDirectory(userDataFolder);

                return userDataFolder;
            }
        }

        public static string UserProfileFolder
        {
            get
            {
                string userProfileFolder = Properties.Settings.Default.ProfilePath;
                if (!Path.IsPathRooted(userProfileFolder))
                {
                    userProfileFolder = Path.Combine(UserDataFolder, "profiles");
                    Properties.Settings.Default.ProfilePath = userProfileFolder;
                    Properties.Settings.Default.Save();
                }

                if (!Directory.Exists(userProfileFolder))
                    Directory.CreateDirectory(userProfileFolder);

                return userProfileFolder;
            }
        }

        public static string UserThumbnailsFolder
        {
            get
            {
                string userThumbnailsFolder = Properties.Settings.Default.ThumbnailsPath;
                if (!Path.IsPathRooted(userThumbnailsFolder))
                {
                    userThumbnailsFolder = Path.Combine(UserDataFolder, "thumbnails");
                    Properties.Settings.Default.ThumbnailsPath = userThumbnailsFolder;
                    Properties.Settings.Default.Save();
                }

                if (!Directory.Exists(userThumbnailsFolder))
                    Directory.CreateDirectory(userThumbnailsFolder);

                return userThumbnailsFolder;
            }
        }

        public static string UserMetadataFolder
        {
            get
            {
                string userMetadataFolder = Properties.Settings.Default.MetadataPath;
                if (!Path.IsPathRooted(userMetadataFolder))
                {
                    userMetadataFolder = Path.Combine(UserDataFolder, "metadata");
                    Properties.Settings.Default.MetadataPath = userMetadataFolder;
                    Properties.Settings.Default.Save();
                }

                if (!Directory.Exists(userMetadataFolder))
                    Directory.CreateDirectory(userMetadataFolder);

                return userMetadataFolder;
            }
        }

        public static void SaveProfile()
        {
            XmlSerializer writer = new XmlSerializer(typeof(UserDefaults));
            string path = Path.Combine(UserProfileFolder, profile.ProfileName + ".xmlprofile");
            string tempPath = path + ".tmp";

            using (FileStream tempFile = File.Create(tempPath))
                writer.Serialize(tempFile, profile);
     
            if (File.Exists(path))
                File.Delete(path);

            File.Move(tempPath, path);
        }

        public static bool LoadProfile(string name)
        {
            UserDefaults profile;
            XmlSerializer reader = new XmlSerializer(typeof(UserDefaults));
            string path = Path.Combine(UserProfileFolder, name + ".xmlprofile");

            if (!File.Exists(path))
                return false;

            using (StreamReader file = new StreamReader(path))
                profile = (UserDefaults)reader.Deserialize(file);

            Defaults.profile = profile;
            Properties.Settings.Default.CurrentProfile = Defaults.profile.ProfileName;
            Properties.Settings.Default.Save();
            return true;
        }
    }
}
