using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Comics
{
    class Defaults
    {
        // These should be customizable in the future
        private const int imageHeight = 254;
        private const int labelHeight = 30;
        private const int width = 180;  // Using the recommended value of height / sqrt(2)
        private const int margin = 3;
        private const int reactionTime = 140;
        public static readonly string ThumbnailFolder = "C:\\Users\\Lanxia\\Downloads\\comics\\comics_thumbnails";
        public static readonly string[] RootPaths = {
                "D:\\ACG\\S\\Images\\Comic\\Artists\\long",
                "D:\\ACG\\S\\Images\\Comic\\Artists\\pictures",
                "D:\\ACG\\S\\Images\\Comic\\Artists\\short",
            };

        public static int SafetyMargin { get { return 16 + 2 * margin; } }

        public static int DefaultHeight { get { return imageHeight + labelHeight + 2 * margin; } }
        public static int DefaultWidth { get { return width + 2 * margin; } }

        public static int ActivationDelay { get { return reactionTime; } }

        public static Size DynamicSize (double viewPortWidth)
        {
            if (viewPortWidth < DefaultWidth)
                return new Size(DefaultWidth, DefaultHeight);

            viewPortWidth -= 16 + 2 * margin;

            int numberOfColumns = (int)(viewPortWidth / DefaultWidth);
            int dynamicWidth = (int)(viewPortWidth / numberOfColumns);
            int dynamicImageWidth = dynamicWidth - 2 * margin;
            int dynamicImageHeight = (int) Math.Round((double)imageHeight * dynamicImageWidth / width);
            return new Size(dynamicImageWidth + 2 * margin,
                dynamicImageHeight + labelHeight + 2 * margin);
        }

        public static int DynamicHeight(double viewPortWidth)
        {
            return (int)DynamicSize(viewPortWidth).Height;
        }

        public static int DynamicWidth(double viewPortWidth)
        {
            return (int)DynamicSize(viewPortWidth).Width;
        }

        public static int ThumbnailWidthForVisual(Visual visual)
        {
            double scale = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice.M11;
            return (int)Math.Ceiling(scale * 2 * DefaultWidth - SafetyMargin - 1);
        }
    

    }
}
