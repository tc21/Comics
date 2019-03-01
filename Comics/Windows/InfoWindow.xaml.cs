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
                this.Title = string.Format("Editing {0}", value[0].Title);
                if (value.Count > 1) {
                    this.Title += string.Format(" and {0} more", value.Count - 1);
                }
                this.Title += "...";
                PopulateInitialInfo();
            }
        }

        public List<string> Tags { get; set; }

        public InfoWindow() {
            InitializeComponent();
            App.InfoWindow = this;
            PopulateInitialInfo();
        }

        private static Brush ActiveTextForeground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        private static Brush InactiveTextForeground = new SolidColorBrush(Color.FromRgb(0x7f, 0x7f, 0x7f));

        public void PopulateInitialInfo() {
            if (this.EditingComics == null) {
                return;
            }

            foreach (var comic in this.EditingComics) {
                if (this.ComicTitle == null) {
                    this.ComicTitle = comic.Title;
                } else if (this.ComicTitle != comic.Title) {
                    this.ComicTitle = "Various"; // todo not actually implemented: currently we just disallow multiple edits
                }

                if (this.ComicAuthor == null) {
                    this.ComicAuthor = comic.Author;
                } else if (this.ComicAuthor != comic.Author) {
                    this.ComicAuthor = "Various";
                }
            }

            this.ResetChanges();

            /* 1. this.Tags is null, because PopulateInitialInfo is called multiple times at random: it's the way it's supposed to work
             * 2. you should pass in the default list of tags, it should be showed as a list of checkboxes
             * 3. there should be an add tag button
             * 4. you still have to implement multiple editing.
             */
            //this.AvailableTags = new ObservableCollection<string>(this.Tags);
        }

        public const string ComicTitlePropertyName = "ComicTitle";
        private string comicTitle = null;
        private bool titleChanged = false;
        public string ComicTitle {
            get => this.comicTitle;
            set {
                if (this.comicTitle == value) {
                    return;
                }

                this.comicTitle = value;
                NotifyPropertyChanged(ComicTitlePropertyName);
            }

        }

        public const string ComicAuthorPropertyName = "ComicAuthor";
        private string comicAuthor = null;
        private bool authorChanged = false;
        public string ComicAuthor {
            get => this.comicAuthor;
            set {
                if (this.comicAuthor == value) {
                    return;
                }

                this.comicAuthor = value;
                NotifyPropertyChanged(ComicAuthorPropertyName);
            }
        }

        public const string AvailableTagsPropertyName = "AvailableTags";
        public ObservableCollection<string> availableTags = new ObservableCollection<string>();
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

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveChanges() {
            foreach (var comic in this.EditingComics) {
                if (titleChanged) {
                    comic.Title = this.ComicTitle;
                }

                if (authorChanged) {
                    comic.Author = this.ComicAuthor;
                }
            }

            PopulateInitialInfo();
        }

        private void ResetChanges() {
            this.titleChanged = false;
            this.TitleTextBox.Foreground = InactiveTextForeground;
            this.TitleChangedIndicator.Visibility = Visibility.Hidden;
            
            this.authorChanged = false;
            this.AuthorTextBox.Foreground = InactiveTextForeground;
            this.AuthorChangedIndicator.Visibility = Visibility.Hidden;
        }

        private void Tag_Checked(object sender, RoutedEventArgs e) {
            // todo
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
            var textbox = sender as TextBox;

            // what we should to is write a custom usercontrol with properties such as "Edited", but this is just a proof of concept

            if (textbox == this.AuthorTextBox) {
                authorChanged = true;
                textbox.Foreground = ActiveTextForeground;
                AuthorChangedIndicator.Visibility = Visibility.Visible;
            }

            if (textbox == this.TitleTextBox) {
                titleChanged = true;
                textbox.Foreground = ActiveTextForeground;
                TitleChangedIndicator.Visibility = Visibility.Visible;
            }
        }
        
        private void InfoWindow_Closing(object sender, CancelEventArgs e) {
            App.InfoWindow = null;
        }
    }
}
