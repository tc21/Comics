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
                if (value.Count == 0) {
                    return;
                }

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

        protected override void OnInitialized(EventArgs e) {
            base.OnInitialized(e);
        }

        public void PopulateInitialInfo() {
            if (this.EditingComics == null) {
                return;
            }

            this.ComicTitle = null;
            this.ComicAuthor = null;
            this.ComicTags = null;

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
                
                if (this.ComicTags == null) {
                    this.ComicTags = comic.TagString;
                } else if (this.ComicTags != comic.TagString) {
                    this.ComicTags = "Various";
                }
            }


            this.TagEditor.Value = this.ComicTags;
            this.TitleEditor.Value = this.ComicTitle;
            this.AuthorEditor.Value = this.ComicAuthor;

            this.TitleEditor.SaveState();
            this.AuthorEditor.SaveState();
            this.TagEditor.SaveState();

            /* 1. this.Tags is null, because PopulateInitialInfo is called multiple times at random: it's the way it's supposed to work
             * 2. you should pass in the default list of tags, it should be showed as a list of checkboxes
             * 3. there should be an add tag button
             * 4. you still have to implement multiple editing.
             */
            //this.AvailableTags = new ObservableCollection<string>(this.Tags);
        }

        public const string ComicTitlePropertyName = "ComicTitle";
        private string comicTitle = null;
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

        public const string ComicTagsPropertyName = "ComicTags";
        public string comicTags = null;
        public string ComicTags {
            get => this.comicTags;
            set {
                if (this.comicTags == value) {
                    return;
                }

                this.comicTags = value;
                NotifyPropertyChanged(ComicTagsPropertyName);
            }
        }

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveChanges() {
            foreach (var comic in this.EditingComics) {
                // THIS IS A BUG and you need to fix it: this.ComicTitle, etc. is not implicitly changing, so you have to access values directly for now

                if (this.TitleEditor.Changed) {
                    comic.Title = this.TitleEditor.Value;
                }

                if (this.AuthorEditor.Changed) {
                    comic.Author = this.AuthorEditor.Value;
                }

                if (this.TagEditor.Changed) {
                    if ((bool)this.TagReplacementActionCheckBox.IsChecked) {
                        comic.TagString += ',' + this.TagEditor.Value;
                    } else {
                        comic.TagString = this.TagEditor.Value;
                    }

                }
            }

            App.ComicsWindow.ComicInfoUpdated();
            PopulateInitialInfo();
        }

        //private void Tag_Checked(object sender, RoutedEventArgs e) {
        //    // todo
        //}

        //private void Tag_Unchecked(object sender, RoutedEventArgs e) {

        //}

        //private void Button_EditTags(object sender, RoutedEventArgs e) {

        //}

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
        
        private void InfoWindow_Closing(object sender, CancelEventArgs e) {
            App.InfoWindow = null;
        }
    }
}
