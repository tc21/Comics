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
        // These following properties are observable by the ui, when the property changes, 
        // The UI will automatically update.
        // The list of profiles to switch between
        public const string ProfilesPropertyName = "Profiles";
        private ObservableCollection<string> profiles = new ObservableCollection<string>();
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
        // The extensions for the currently selected profile
        public const string ExtensionsPropertyName = "Extensions";
        private ObservableCollection<StringObject> extensions;
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
        // Categories | Paths for the current profile
        public const string CategoriesPropertyName = "Categories";
        private ObservableCollection<Defaults.CategorizedPath> categories;
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
        // List of ignored prefixes for the profile
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
        // An indicator that the profile was modified
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

        // Initializer
        public SettingsWindow()
        {
            InitializeComponent();
            App.SettingsWindow = this;
            PopulateProfileSettings();
        }

        // Populates the extensions, categories, and prefixes boxes after a profile is selected.
        public void PopulateProfileSettings()
        {
            Profiles = new ObservableCollection<string>(App.ViewModel.Profiles);
            ProfileSelector.SelectedIndex = App.ViewModel.SelectedProfile;
            Extensions = StringCollection(Defaults.Profile.Extensions);
            Categories = new ObservableCollection<Defaults.CategorizedPath>(Defaults.Profile.RootPaths);
            IgnoredPrefixes = StringCollection(Defaults.Profile.IgnoredPrefixes);
        }

        // Creates an observable collection of "String Objects" from a collectoin of strings
        private ObservableCollection<StringObject> StringCollection(ICollection<string> collection)
        {
            ObservableCollection<StringObject> result = new ObservableCollection<StringObject>();
            foreach (string str in collection)
                result.Add(new StringObject { Value = str });
            return result;
        }
        
        // Changes the currently selected profile. The UI is then updated by the viewmodel.
        private void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.ViewModel.SelectedProfile = ((ComboBox)sender).SelectedIndex;
        }

        // Writes the current changes to the profile when the user selects "apply" or "confirm"
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

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // A wrapper around strings to make DataGrid's automatic row adding work
        public class StringObject
        {
            public string Value { get; set; }
        }

        // After the user edits a cell in the DataGrid, either "normalizes" the user's input
        // or sets it to null.
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
