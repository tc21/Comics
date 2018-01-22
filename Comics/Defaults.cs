using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Comics
{
    class Defaults
    {
        // The default settings currently being used
        public static UserDefaults shared;

        // Default settings for each instance
        public class UserDefaults
        {
            public int ImageHeight;
            public int ImageWidth;
            public int TitleFontSize;
            public int SubtitleFontSize;
            public int TileMargin;
            // The all-important time between application is activated and application starts accepting left clicks
            public int ReactionTime;
            // This user-specified font family will fall back to default (probably Segoe UI) if it doesn't exist
            public string ApplicationFontFamily;

            public string ThumbnailFolder;
            public LinkedList<Tuple<string, string>> RootPaths;

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

        // Initialized a "default" defaults which is probably quickly replaced
        static Defaults()
        {
            shared = new UserDefaults()
            {
                ImageHeight = defaultImageHeight,
                ImageWidth = defaultWidth,
                TitleFontSize = defaultTitleFontSize,
                SubtitleFontSize = defaultSubtitleFontSize,
                ApplicationFontFamily = defaultFontFamily,
                TileMargin = defaultMargin,
                ReactionTime = defaultReactionTime,
                ThumbnailFolder = defaultThumbnailFolder,
                RootPaths = new LinkedList<Tuple<string, string>>()
            };
        }

        // These should be customizable in the future
        private const int defaultImageHeight = 254;
        private const int defaultTitleFontSize = 12;
        private const int defaultSubtitleFontSize = 10;
        private const string defaultFontFamily = "Segoe UI";
        private const int defaultWidth = 180;  // Using the recommended value of height / sqrt(2)
        private const int defaultMargin = 3;
        private const int defaultReactionTime = 140;
        private static string defaultThumbnailFolder
        {
            get
            {
                return Path.Combine(UserDataFolder, "Comics", "comics_thumbnails");
            }
        }


        // Calculates the height of a label containing a font with this size. 
        public static int LineHeightForFontWithSize(int fontsize)
        {
            return (int)Math.Ceiling(fontsize * 4.0 / 3.0);
        }

        // The one thing that needs to wait to be user-defined
        public static readonly string[] RootPaths = {
                "D:\\ACG\\S\\Images\\Comic\\Artists\\long",
                //"D:\\ACG\\S\\Images\\Comic\\Artists\\pictures",
                //"D:\\ACG\\S\\Images\\Comic\\Artists\\short",
            };

        public static readonly List<string> ImageSuffixes = new List<string> { ".jpg", ".jpeg", ".png", ".tiff", ".bmp", ".gif" };

        // How the application was first coded
        public static int SafetyMargin { get { return 16 + 2 * shared.TileMargin; } }
        public static int DefaultHeight { get { return shared.ImageHeight + shared.LabelHeight + 2 * shared.TileMargin; } }
        public static int DefaultWidth { get { return shared.ImageWidth + 2 * shared.TileMargin; } }
        public static int ActivationDelay { get { return shared.ReactionTime; } }
        public static string ThumbnailFolder { get { return shared.ThumbnailFolder; } }

        // Provides dynamically-sized widths and heights to the wrap panel
        private static int MaximumDynamicWidth { get { return 2 * DefaultWidth - 1; } }
        public static Size DynamicSize (double viewPortWidth)
        {
            if (viewPortWidth < DefaultWidth)
                return new Size(DefaultWidth, DefaultHeight);

            viewPortWidth -= 16;
            int numberOfColumns = EstimateNumberOfColumns(viewPortWidth / DefaultWidth);
            int dynamicWidth = (int)(viewPortWidth / numberOfColumns);
            int dynamicImageWidth = dynamicWidth - 2 * shared.TileMargin;
            int dynamicImageHeight = (int) Math.Round((double)shared.ImageHeight * dynamicImageWidth / DefaultWidth);
            return new Size(dynamicImageWidth + 2 * shared.TileMargin,
                dynamicImageHeight + shared.LabelHeight + 2 * shared.TileMargin);
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
                string parentFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string folder = Path.Combine(parentFolder, "TC-C7");
                return folder;
            }
        }

    }
}
