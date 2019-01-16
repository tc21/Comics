using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Comics {
    public partial class InfoWindow : Window, INotifyPropertyChanged {

        private List<Comic> editingComics = null;
        public List<Comic> EditingComics {
            get => this.editingComics;
            set {
                this.editingComics = value;
                this.Title = String.Format("Editing {0}", value[0].Title.Display);
                if (value.Count > 1) {
                    this.Title += String.Format(" and {0} other", value.Count - 1);
                    throw new Exception("This should not happen. You are trying to edit mulpitle items, when it should not be possible.");
                }
                this.Title += "...";
                PopulateInitialInfo();
            }
        }

        public InfoWindow() {
            InitializeComponent();
            App.InfoWindow = this;
            PopulateInitialInfo();
        }

        public void PopulateInitialInfo() {
            if (this.EditingComics == null) {
                return;
            }

            foreach (var comic in this.EditingComics) {
                if (this.ComicTitle == null) {
                    this.ComicTitle = comic.Title;
                } else if (this.ComicTitle != comic.Title) {
                    this.ComicTitle = new SortedString("Various"); // todo not actually implemented: currently we just disallow multiple edits
                }

                if (this.ComicAuthor == null) {
                    this.ComicAuthor = comic.Author;
                } else if (this.ComicAuthor != comic.Author) {
                    this.ComicAuthor = new SortedString("Various");
                }
            }
        }

        public const string DisplayTitlePropertyName = "DisplayTitle";
        public const string SortTitlePropertyName = "SortTitle";
        private SortedString comicTitle = null;
        public SortedString ComicTitle {
            get => this.comicTitle;
            set {
                if (this.comicTitle == value) {
                    return;
                }

                this.comicTitle = value;
                NotifyPropertyChanged(DisplayTitlePropertyName);
            }

        }

        public string DisplayTitle {
            get => this.comicTitle?.Display ?? "";
            set {
                if (this.comicTitle == null || this.comicTitle.Display == value) {
                    return;
                }

                this.comicTitle.Display = value;
                NotifyPropertyChanged(DisplayTitlePropertyName);
            }
        }

        public string SortTitle {
            get => this.comicTitle?.Sort ?? "";
            set {
                if (this.comicTitle == null || this.comicTitle.Sort == value) {
                    return;
                }

                this.comicTitle.Sort = value;
                NotifyPropertyChanged(SortTitlePropertyName);
            }
        }

        public const string DisplayAuthorPropertyName = "DisplayAuthor";
        public const string SortAuthorPropertyName = "SortAuthor";
        private SortedString comicAuthor = null;
        public SortedString ComicAuthor {
            get => this.comicAuthor;
            set {
                if (this.comicAuthor == value) {
                    return;
                }

                this.comicAuthor = value;
                NotifyPropertyChanged(DisplayAuthorPropertyName);
                NotifyPropertyChanged(DisplayAuthorPropertyName);
            }

        }

        public string DisplayAuthor {
            get => this.comicAuthor?.Display ?? "";
            set {
                if (this.comicAuthor == null || this.comicAuthor.Display == value) {
                    return;
                }

                this.comicAuthor.Display = value;
                NotifyPropertyChanged(DisplayAuthorPropertyName);
            }
        }

        public string SortAuthor {
            get => this.comicAuthor?.Sort ?? "";
            set {
                if (this.comicAuthor == null || this.comicAuthor.Sort == value) {
                    return;
                }

                this.comicAuthor.Sort = value;
                NotifyPropertyChanged(SortAuthorPropertyName);
            }
        }

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveChanges() {
            foreach (var comic in this.EditingComics) {
                comic.Title = this.ComicTitle;
                comic.Author = this.ComicAuthor;
                break; // just in case - remove eventually
            }
        }

        private void Tag_Checked(object sender, RoutedEventArgs e) {

        }

        private void Tag_Unchecked(object sender, RoutedEventArgs e) {

        }

        private void Button_EditTags(object sender, RoutedEventArgs e) {

        }

        private void Button_Confirm(object sender, RoutedEventArgs e) {
            SaveChanges();
            Close();
        }

        private void Button_Apply(object sender, RoutedEventArgs e) {
            SaveChanges();
        }

        private void Button_Cancel(object sender, RoutedEventArgs e) {
            Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            // todo: this doesn't do anything at the moment, but eventually we should be able to detect and only save
            // fields to which the user changed stuff, and then we will be able to edit multiple items.
        }
        
        private void InfoWindow_Closing(object sender, CancelEventArgs e) {
            App.InfoWindow = null;
        }
    }
}
