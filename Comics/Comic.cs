using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Comics
{
    [Serializable()]
    class Comic
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImagePath { get; set; }
        public string ThumbnailPath { get; set; }

        /// <summary>
        /// Calls Process.Start on ThumbnailPath.
        /// Maybe I will eventually code a viewer into this program, but I already have an image viewer.
        /// </summary>
        public void OpenWithDefaultApplication()
        {
            Process.Start(ImagePath);
        }
    }
}
