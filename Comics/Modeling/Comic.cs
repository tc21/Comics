﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Comics.Support;

namespace Comics {
    public class Comic : INotifyPropertyChanged {
        private readonly string title;
        private readonly string author;
        private readonly string category;
        private readonly string path;
        private string imagePath = null;
        private List<string> filePaths = new List<string>();

        public string UniqueIdentifier => $"[{this.author}]{this.title}";

        public string ImagePath => this.Metadata.ThumbnailSource ?? this.imagePath;
        // The thumbnail and metadata files are not to be generated by the Comic class itself (for performance reasons)
        public string ThumbnailPath => Path.Combine(Defaults.UserThumbnailsFolder, this.UniqueIdentifier + ".jpgthumbnail");
        public string MetadataPath => Path.Combine(Defaults.UserMetadataFolder, this.UniqueIdentifier + ".xmlmetadata");
        public string ContainingPath => this.path;
        public List<string> FilePaths => this.filePaths;

        public int Random { get; set; }

        public Metadata Metadata { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Comic(string title, string author, string category, string path) {
            this.title = title;
            this.author = author;
            this.category = category;
            this.path = path;

            if (Directory.Exists(path)) {
                AddDirectory(new DirectoryInfo(path));
            } else if (File.Exists(path)) {
                this.filePaths.Add(path);
            } else {
                throw new Exception("Invalid path given to comic");
            }

            if (!LoadMetadata()) {
                this.Metadata = new Metadata();
            }
        }

        public void AddDirectory(DirectoryInfo directory) {
            FileInfo[] files = directory.GetFilesInNaturalOrder("*.*");

            foreach (FileInfo file in files) {
                string suffix = Path.GetExtension(file.Name).ToLowerInvariant();
                if (Defaults.Profile.Extensions.Contains(suffix)) {
                    this.filePaths.Add(file.FullName);
                }

                if (this.imagePath == null && Defaults.ImageExtensions.Contains(suffix)) {
                    this.imagePath = file.FullName;
                }
            }
        }

        // Creates a thumbnail for this comic and saves it to disk
        public void CreateThumbnail() {
            int width = Defaults.ThumbnailWidthForVisual();
            if (File.Exists(this.ImagePath) &&
                Thumbnails.CreateThumbnailFromImage(this.ImagePath, width, this.ThumbnailPath)) {
            } else if (Thumbnails.CanCreateThumbnailFromAudio(this.ImagePath) &&
                       Thumbnails.CreateThumbnailFromAudio(this.ImagePath, width, this.ThumbnailPath)) {
            } else if (Thumbnails.CanCreateThumbnailFromVideo(this.ImagePath) &&
                       Thumbnails.CreateThumbnailFromVideo(this.ImagePath, width, this.ThumbnailPath)) {
            } else if (Thumbnails.CanCreateThumbnailFromAudio(this.FilePaths.First()) &&
                       Thumbnails.CreateThumbnailFromAudio(this.FilePaths.First(), width, this.ThumbnailPath)) {
            } else if (Thumbnails.CanCreateThumbnailFromVideo(this.FilePaths.First()) &&
                       Thumbnails.CreateThumbnailFromVideo(this.FilePaths.First(), width, this.ThumbnailPath)) {
            } else {
                return;
            }

            NotifyPropertyChanged("ThumbnailPath");
        }

        // Public properties that update the UI when changed
        public SortedString Title {
            get => this.Metadata.Title ?? new SortedString(this.title);
            set { this.Metadata.Title = value; SaveMetadata(); NotifyPropertyChanged("Title"); }
        }

        public SortedString Author {
            get => this.Metadata.Author ?? new SortedString(this.author);
            set { this.Metadata.Author = value; SaveMetadata(); NotifyPropertyChanged("Author"); }
        }

        public SortedString Category {
            get => this.Metadata.Category ?? new SortedString(this.category);
            set { this.Metadata.Category = value; SaveMetadata(); NotifyPropertyChanged("Category"); }
        }

        public bool Loved {
            get => this.Metadata.Loved;
            set { this.Metadata.Loved = value; SaveMetadata(); NotifyPropertyChanged("Loved"); }
        }

        public bool Disliked {
            get => this.Metadata.Disliked;
            set { this.Metadata.Disliked = value; SaveMetadata(); NotifyPropertyChanged("Disliked"); }
        }

        // Maybe I will eventually code a viewer into this program, but I already have an image viewer.
        public void Open() {
            // Temporary handle for viewer
            if (Defaults.Profile.DefaultApplication != null) {
                Defaults.Profile.DefaultApplication.StartComic(this);
            } else {
                Process.Start(this.filePaths.First());
            }
        }

        public void OpenContainingFolder() {
            Process.Start(this.path);
        }

        public static string TestExecutionString(string format) {
            return String.Join(" ", ExecutionString.CreateTestExecutionString(format));
        }

        public static class ExecutionString {
            // A special syntax for "opening" an item. The syntax defines 3 "special" characters:
            // { : used to start an expression; } : used to end an expression; \ : used to escape a special character
            // An expression uses the syntax {key:args}, where key and args are alphanumeric. 
            // "key" specifies what kind of argument, while "args" modify the way it is presented.
            private const string FirstFileKey = "first";
            private const string AllFilesKey = "all";
            private const string ContainingFolderKey = "folder";
            private const string FirstFilenameKey = "firstname";
            private const string AllFilenamesKey = "allname";
            private const string TitleKey = "title";
            private const string AuthorKey = "author";
            private const string CategoryKey = "category";

            public static IEnumerable<string> CreateExecutionArguments(string format, Comic comic) {

                if (String.IsNullOrEmpty(format)) {
                    return new List<string> { comic?.FilePaths.First() ?? "C:\\comic\\first.png" };
                }

                return Tokenize(format, comic);
            }
            
            private static IEnumerable<string> Tokenize(string format, Comic comic) {
                // Custom parsing
                var token = new List<char>();
                foreach (var c in format) {
                    if (token.Count == 0) {
                        if (!Char.IsWhiteSpace(c)) {
                            token.Add(c);
                        }
                    } else {
                        if (token[token.Count - 1] == '\\') {
                            token[token.Count - 1] = Unescape(c);
                        } else if (token[0] == '{') {
                            if (c == '{') {
                                throw new TokenFormatException("Nested token (nested '{')");
                            } else if (c == '}') {
                                token.Add(c);
                                var match = Regex.Match(new String(token.ToArray()), "{([^:}]*)(:)?([^}]*)?}");
                                foreach (var str in ProcessToken(comic, match.Groups[1].Value, match.Groups[2].Success, match.Groups[3].Value)) {
                                    yield return str;
                                }
                                token.Clear();
                            } else {
                                token.Add(c);
                            }
                        } else {
                            if (Char.IsWhiteSpace(c)) {
                                yield return new String(token.ToArray());
                                token.Clear();
                            } else {
                                token.Add(c);
                            }
                        }
                    }
                }
                if (token.Count > 0) {
                    if (token[0] == '{') {
                        throw new TokenFormatException("Unfinished token (unmatched '{')");
                    }
                    yield return new string(token.ToArray());
                }
            }

            public static IEnumerable<string> CreateTestExecutionString(string format) {
                return CreateExecutionArguments(format, null);
            }

            private static char Unescape(char token) {
                switch (token) {
                    case '\\':
                    case '{':
                    case '}':
                    case ' ':
                        return token;
                    default:
                        throw new TokenFormatException("Invalid escape sequence: \\" + token);
                }
            }

            private static List<string> testfiles = new List<string>
            {
                "C:\\comic\\first.png",
                "C:\\comic\\second.png",
                "C:\\comic\\last.png"
            };

            public class TokenFormatException : Exception {
                public TokenFormatException(string message) : base(message) { }
            }

            // pass in a null comic for a test result
            private static IEnumerable<string> ProcessToken(Comic comic, string key, bool argsGiven, string args) {
                List<string> files = comic?.FilePaths ?? testfiles;

                switch (key) {
                    case FirstFileKey:
                        yield return files.First();
                        yield break;
                    case AllFilesKey: {
                            if (!argsGiven || String.IsNullOrEmpty(args)) {
                                foreach (var f in files) {
                                    yield return f;
                                }
                                yield break;
                            } else {
                                yield return String.Join(args, files);
                                yield break;
                            }
                        }
                    case ContainingFolderKey:
                        yield return comic?.ContainingPath ?? "C:\\comic";
                        yield break;
                    case FirstFilenameKey:
                        yield return Path.GetFileName(files.First());
                        yield break;
                    case AllFilenamesKey: {
                            var filenames = files.Select(p => Path.GetFileName(p));
                            if (!argsGiven || String.IsNullOrEmpty(args)) {
                                foreach (var fn in filenames) {
                                    yield return fn;
                                }
                                yield break;
                            } else {
                                yield return String.Join(args, filenames);
                                yield break;
                            }
                        }
                    case TitleKey:
                        yield return comic?.Title.Display ?? "TITLE";
                        yield break;
                    case AuthorKey:
                        yield return comic?.Author.Display ?? "AUTHOR";
                        yield break;
                    case CategoryKey:
                        yield return comic?.Category.Display ?? "CATEGORY";
                        yield break;
                    default:
                        throw new TokenFormatException("Invalid key: " + key);
                }
            }

            //private static string Quote(string s) {
            //    return "\"" + s + "\"";
            //}
        }



        public bool MatchesSearchText(string searchText) {
            if (String.IsNullOrWhiteSpace(searchText)) {
                return true;
            }

            searchText = searchText.ToLowerInvariant();
            return this.Title.Display.ToLowerInvariant().Contains(searchText) ||
                this.Title.Sort.ToLowerInvariant().Contains(searchText) ||
                this.Author.Display.ToLowerInvariant().Contains(searchText) ||
                this.Author.Sort.ToLowerInvariant().Contains(searchText);
        }

        public bool MatchesCategories(ISet<string> categories) {
            return categories.Count == 0 || categories.Contains(this.Category.Display);
        }

        public bool MatchesAuthors(ISet<string> authors) {
            return authors.Count == 0 || authors.Contains(this.Author.Display);
        }

        public static readonly List<string> SortPropertyNames = new List<string> { "Author", "Title", "Category", "Random" };
        public static readonly int RandomSortIndex = 3;
        public static List<string> SortDescriptionPropertyNamesForIndex(int index) {
            List<string> sortPropertyNames = new List<string>(SortPropertyNames);
            if (index > 0 && index < sortPropertyNames.Count) {
                string preferredProperty = sortPropertyNames[index];
                sortPropertyNames.RemoveAt(index);
                sortPropertyNames.Insert(0, preferredProperty);
            }
            return sortPropertyNames;
        }

        public void SaveMetadata() {
            XmlSerializer writer = new XmlSerializer(typeof(Metadata));
            string path = Path.Combine(Defaults.UserMetadataFolder, this.MetadataPath);
            string tempPath = path + ".tmp";

            using (FileStream tempFile = File.Create(tempPath)) {
                writer.Serialize(tempFile, this.Metadata);
            }

            if (File.Exists(path)) {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }

        public bool LoadMetadata() {
            Metadata profile;
            XmlSerializer reader = new XmlSerializer(typeof(Metadata));
            string path = Path.Combine(Defaults.UserMetadataFolder, this.MetadataPath);

            if (!File.Exists(path)) {
                return false;
            }

            using (StreamReader file = new StreamReader(path)) {
                profile = (Metadata)reader.Deserialize(file);
            }

            this.Metadata = profile;
            return true;
        }
    }

    public class Metadata {
        public SortedString Title { get; set; }
        public SortedString Author { get; set; }
        public SortedString Category { get; set; }
        public bool Loved { get; set; }
        public bool Disliked { get; set; }
        public string ThumbnailSource { get; set; }
    }

    public class SortedString : IComparable {
        public string Display { get; set; }
        public string Sort { get; set; }

        public SortedString() { }

        public SortedString(string display, string sort) {
            this.Display = display;
            this.Sort = sort;
        }

        public SortedString(string display) : this(display, display) { }

        public override string ToString() {
            return this.Display.ToString();
        }

        public override bool Equals(object obj) {
            if (obj is SortedString) {
                return this.Sort.Equals(((SortedString)obj).Sort);
            }

            return this.Sort.Equals(obj);
        }

        public override int GetHashCode() {
            return this.Sort.GetHashCode();
        }

        public int CompareTo(object other) {
            if (other is SortedString) {
                return this.Sort.CompareTo(((SortedString)other).Sort);
            }

            return this.Sort.CompareTo(other);
        }
    }
}
