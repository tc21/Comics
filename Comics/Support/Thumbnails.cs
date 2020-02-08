using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Comics {
    class Thumbnails {
        // Thumbnail from image just uses Windows.Media.Imaging
        // From audio tags relies on TagLib
        // From video relies on ffmpeg (NReco.VideoConverter)
        private static readonly string[] AudioTagExtensions = { ".mp3", ".m4a", ".ogg", ".flac", ".aiff", ".mp4" };
        private static readonly string[] VideoExtensions = { ".mp4", ".mov", ".mkv", ".flv", ".avi", ".wmv", ".rmvb", ".m4v" };

        public static bool CreateThumbnailFromImage(string path, int width, string pathToSave) {
            try {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.DecodePixelWidth = width;
                image.EndInit();
                SaveBitmapImageToFile(image, pathToSave);
            } catch (Exception e) when (e is DirectoryNotFoundException || e is NotSupportedException || e is FileNotFoundException) {
                return false;
            }

            return true;
        }

        public static bool CanCreateThumbnailFromAudio(string path) {
            return AudioTagExtensions.Contains(Path.GetExtension(path));
        }

        public static bool CreateThumbnailFromAudio(string path, int width, string pathToSave) {
            try {
                using (var tagFile = TagLib.File.Create(path)) {
                    if (tagFile.Tag.Pictures.Length == 0) {
                        return false;
                    }

                    var dataStream = new MemoryStream(tagFile.Tag.Pictures[0].Data.Data);

                    var image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = dataStream;
                    image.DecodePixelWidth = width;
                    image.EndInit();

                    SaveBitmapImageToFile(image, pathToSave);
                }
            } catch (TagLib.CorruptFileException) {
                return false;
            } catch (DirectoryNotFoundException) {
                return false;
            } catch (FileNotFoundException) {
                return false;
            }
            return true;
        }

        public static bool CanCreateThumbnailFromVideo(string path) {
            return VideoExtensions.Contains(Path.GetExtension(path));
        }

        public static bool CreateThumbnailFromVideo(string path, int width, string pathToSave) {
            var ffmpeg = new NReco.VideoConverter.FFMpegConverter();
            var tempPath = Path.GetTempFileName();
            ffmpeg.GetVideoThumbnail(path, tempPath, 21);

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(tempPath);
            image.DecodePixelWidth = width;
            image.EndInit();

            SaveBitmapImageToFile(image, pathToSave);
            return true;
        }

        private static void SaveBitmapImageToFile(BitmapImage image, string pathToSave) {
            var bitmapEncoder = new JpegBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(image));
            using (var fileStream = new FileStream(pathToSave, FileMode.Create)) {
                bitmapEncoder.Save(fileStream);
            }
        }

    }
}
