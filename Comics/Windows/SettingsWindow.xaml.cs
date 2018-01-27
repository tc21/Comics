using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

namespace Comics
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public const string ProfilesPropertyName = "Profiles";
        public ObservableCollection<string> profiles = new ObservableCollection<string>();
        public ObservableCollection<string> Profiles
        {
            get { return profiles; }
            set
            {
                if (profiles == value)
                    return;
                profiles = value;
                NotifyPropertyChanged(ProfilesPropertyName);
            }
        }

        public const string ExtensionsPropertyName = "Extensions";
        public ObservableCollection<StringObject> extensions;
        public ObservableCollection<StringObject> Extensions
        {
            get { return extensions; }
            set
            {
                if (extensions == value)
                    return;
                extensions = value;
                NotifyPropertyChanged(ExtensionsPropertyName);
            }
        }

        public const string CategoriesPropertyName = "Categories";
        public ObservableCollection<Defaults.CategorizedPath> categories;
        public ObservableCollection<Defaults.CategorizedPath> Categories
        {
            get { return categories; }
            set
            {
                if (categories == value)
                    return;
                categories = value;
                NotifyPropertyChanged(CategoriesPropertyName);
            }
        }

        public const string IgnoredPrefixesPropertyName = "IgnoredPrefixes";
        private ObservableCollection<StringObject> ignoredPrefixes;
        public ObservableCollection<StringObject> IgnoredPrefixes
        {
            get { return ignoredPrefixes; }
            set
            {
                if (ignoredPrefixes == value)
                    return;
                ignoredPrefixes = value;
                NotifyPropertyChanged(IgnoredPrefixesPropertyName);
            }
        }

        public const string ProfileChangedPropertyName = "ProfileChanged";
        private bool profileChanged = false;
        public bool ProfileChanged
        {
            get { return profileChanged; }
            set
            {
                if (profileChanged == value)
                    return;
                profileChanged = value;
                NotifyPropertyChanged(ProfileChangedPropertyName);
            }
        }
        public SettingsWindow()
        {
            InitializeComponent();
            App.SettingsWindow = this;
            PopulateProfileSettings();
        }

        public void PopulateProfileSettings()
        {
            Profiles = new ObservableCollection<string>(App.ViewModel.Profiles);
            ProfileSelector.SelectedIndex = App.ViewModel.SelectedProfile;
            Extensions = StringCollection(Defaults.Profile.Extensions);
            Categories = new ObservableCollection<Defaults.CategorizedPath>(Defaults.Profile.RootPaths);
            IgnoredPrefixes = StringCollection(Defaults.Profile.IgnoredPrefixes);
        }

        private ObservableCollection<StringObject> StringCollection(ICollection<string> collection)
        {
            ObservableCollection<StringObject> result = new ObservableCollection<StringObject>();
            foreach (string str in collection)
                result.Add(new StringObject { Value = str });
            return result;
        }

        private void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.ViewModel.SelectedProfile = ((ComboBox)sender).SelectedIndex;
        }

        private void WriteChangesToProfile()
        {
            if (!ProfileChanged)
                return;
            ProfileChanged = false;
            Defaults.Profile.Extensions = Extensions.Select(o => o.Value).Where(o => !String.IsNullOrEmpty(o)).ToList();
            Defaults.Profile.IgnoredPrefixes = IgnoredPrefixes.Select(o => o.Value).Where(o => !String.IsNullOrEmpty(o)).ToList();
            Defaults.Profile.RootPaths = Categories.Where(o => !String.IsNullOrEmpty(o.Category) && !String.IsNullOrEmpty(o.Path)).ToList();
            Defaults.SaveProfile();
            App.ViewModel.UpdateUIAfterProfileChanged();
        }

        private class ProfileItem
        {
            public string Name { get; set; }
            public bool Selected { get; set; }
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Apply(object sender, RoutedEventArgs e)
        {
            WriteChangesToProfile();
        }

        private void Button_Confirm(object sender, RoutedEventArgs e)
        {
            WriteChangesToProfile();
            Close();
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class StringObject
        {
            public string Value { get; set; }
        }

        private void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                string header = e.Column.Header as string;
                TextBox textBox = e.EditingElement as TextBox;
                if (header == null || textBox == null)
                    return;
                if (String.IsNullOrWhiteSpace(textBox.Text))
                    return;

                switch (header)
                {
                    case "Extension":
                        textBox.Text = Defaults.FormatExtension(textBox.Text);
                        break;
                    case "Path":
                        textBox.Text = Defaults.FormatDirectory(textBox.Text);
                        break;
                    case "Prefix":
                    case "Category":
                        textBox.Text = Defaults.FormatText(textBox.Text);
                        break;
                    default:
                        textBox.Text = null;
                        break;
                }
                if (String.IsNullOrEmpty(textBox.Text))
                    e.Cancel = true;
                ProfileChanged = true;
            }
        }

        private void SettingsWindow_Closing(object sender, CancelEventArgs e)
        {
            App.SettingsWindow = null;
        }
    }
}
