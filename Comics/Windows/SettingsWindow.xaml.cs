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

namespace Comics {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged {
        // These following properties are observable by the ui, when the property changes, 
        // The UI will automatically update.
        // The list of profiles to switch between
        public const string ProfilesPropertyName = "Profiles";
        public const string NewProfileName = "New Profile...";
        private ObservableCollection<string> profiles = new ObservableCollection<string>();
        public ObservableCollection<string> Profiles {
            get => this.profiles;
            set {
                if (this.profiles == value) {
                    return;
                }

                this.profiles = value;
                NotifyPropertyChanged(ProfilesPropertyName);
            }
        }
        // The extensions for the currently selected profile
        public const string ExtensionsPropertyName = "Extensions";
        private ObservableCollection<StringObject> extensions;
        public ObservableCollection<StringObject> Extensions {
            get => this.extensions;
            set {
                if (this.extensions == value) {
                    return;
                }

                this.extensions = value;
                NotifyPropertyChanged(ExtensionsPropertyName);
            }
        }
        // Categories | Paths for the current profile
        public const string CategoriesPropertyName = "Categories";
        private ObservableCollection<Defaults.CategorizedPath> categories;
        public ObservableCollection<Defaults.CategorizedPath> Categories {
            get => this.categories;
            set {
                if (this.categories == value) {
                    return;
                }

                this.categories = value;
                NotifyPropertyChanged(CategoriesPropertyName);
            }
        }
        // List of ignored prefixes for the profile
        public const string IgnoredPrefixesPropertyName = "IgnoredPrefixes";
        private ObservableCollection<StringObject> ignoredPrefixes;
        public ObservableCollection<StringObject> IgnoredPrefixes {
            get => this.ignoredPrefixes;
            set {
                if (this.ignoredPrefixes == value) {
                    return;
                }

                this.ignoredPrefixes = value;
                NotifyPropertyChanged(IgnoredPrefixesPropertyName);
            }
        }
        // An indicator that the profile was modified
        public const string ProfileChangedPropertyName = "ProfileChanged";
        private bool profileChanged = false;
        public bool ProfileChanged {
            get => this.profileChanged;
            set {
                if (this.profileChanged == value) {
                    return;
                }

                this.profileChanged = value;
                NotifyPropertyChanged(ProfileChangedPropertyName);
            }
        }

        // Initializer
        public SettingsWindow() {
            InitializeComponent();
            App.SettingsWindow = this;
            PopulateProfileSettings();
        }

        // Populates the extensions, categories, and prefixes boxes after a profile is selected.
        public void PopulateProfileSettings() {
            this.Profiles = new ObservableCollection<string>(App.ViewModel.Profiles);
            this.ProfileSelector.SelectedIndex = App.ViewModel.SelectedProfile;
            this.Extensions = StringCollection(Defaults.Profile.Extensions);
            this.Categories = new ObservableCollection<Defaults.CategorizedPath>(Defaults.Profile.RootPaths);
            this.IgnoredPrefixes = StringCollection(Defaults.Profile.IgnoredPrefixes);
            this.OpenApplicationTextBox.Text = Defaults.Profile.DefaultApplication?.Name ?? "";
            this.OpenArgumentsTextBox.Text = Defaults.Profile.ExecutionArguments;
        }

        // Creates an observable collection of "String Objects" from a collectoin of strings
        private ObservableCollection<StringObject> StringCollection(ICollection<string> collection) {
            ObservableCollection<StringObject> result = new ObservableCollection<StringObject>();
            foreach (string str in collection) {
                result.Add(new StringObject { Value = str });
            }

            return result;
        }

        // Changes the currently selected profile. The UI is then updated by the viewmodel.
        private void ProfileSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            App.ViewModel.SelectedProfile = ((ComboBox)sender).SelectedIndex;
        }

        // Writes the current changes to the profile when the user selects "apply" or "confirm"
        private void WriteChangesToProfile() {
            if (!this.ProfileChanged) {
                return;
            }

            this.ProfileChanged = false;
            Defaults.Profile.Extensions = this.Extensions.Select(o => o.Value).Where(o => !String.IsNullOrEmpty(o)).ToList();

            Defaults.Profile.IgnoredPrefixes = this.IgnoredPrefixes.Select(o => o.Value).Where(o => !String.IsNullOrEmpty(o)).ToList();
            Defaults.Profile.RootPaths = this.Categories.Where(o => !String.IsNullOrEmpty(o.Category) && !String.IsNullOrEmpty(o.Path)).ToList();

            try {
                Defaults.Profile.DefaultApplication = Defaults.StartupApplication.Interpolate(this.OpenApplicationTextBox.Text);
                Comic.TestExecutionString(this.OpenArgumentsTextBox.Text);
                Defaults.Profile.ExecutionArguments = this.OpenArgumentsTextBox.Text;
            } catch (Exception) { }
            Defaults.SaveProfile();
            App.ViewModel.UpdateComicsAfterProfileUpdated();
        }

        private void Button_Cancel(object sender, RoutedEventArgs e) {
            Close();
        }

        private void Button_Apply(object sender, RoutedEventArgs e) {
            WriteChangesToProfile();
        }

        private void Button_Confirm(object sender, RoutedEventArgs e) {
            WriteChangesToProfile();
            Close();
        }

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // A wrapper around strings to make DataGrid's automatic row adding work
        public class StringObject {
            public string Value { get; set; }
        }

        // After the user edits a cell in the DataGrid, either "normalizes" the user's input
        // or sets it to null.
        private void CellEditEnding(object sender, DataGridCellEditEndingEventArgs e) {
            if (e.EditAction == DataGridEditAction.Commit) {
                string header = e.Column.Header as string;
                TextBox textBox = e.EditingElement as TextBox;
                if (header == null || textBox == null) {
                    return;
                }

                if (String.IsNullOrWhiteSpace(textBox.Text)) {
                    return;
                }

                switch (header) {
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
                if (String.IsNullOrEmpty(textBox.Text)) {
                    e.Cancel = true;
                }

                this.ProfileChanged = true;
            }
        }

        private void SettingsWindow_Closing(object sender, CancelEventArgs e) {
            App.SettingsWindow = null;
        }

        private void ProfileOptionsButton_Click(object sender, RoutedEventArgs e) {
            if (!this.ProfileOptionsButton.ContextMenu.IsOpen) {
                e.Handled = true;

                var mouseRightClickEvent = new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Right) {
                    RoutedEvent = Mouse.MouseUpEvent,
                    Source = sender,
                };
                InputManager.Current.ProcessInput(mouseRightClickEvent);
            }
        }

        private void ProfileMenu_Rename(object sender, RoutedEventArgs e) {
            this.ProfileNameEditor.Text = this.ProfileSelector.SelectedItem.ToString();
            this.ProfileNameEditor.Visibility = Visibility.Visible;
            Keyboard.Focus(this.ProfileNameEditor);
            this.ProfileNameEditor.ScrollToEnd();
            this.ProfileNameEditor.SelectAll();
        }

        private void ProfileMenu_Delete(object sender, RoutedEventArgs e) {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this profile? This action cannot be undone.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) {
                return;
            }

            Defaults.DeleteCurrentProfile();

            int index = this.ProfileSelector.SelectedIndex;
            index += (index == 0) ? 1 : -1;

            App.ViewModel.ReloadProfiles();
            PopulateProfileSettings();

            if (index < this.Profiles.Count) {
                App.ViewModel.SelectedProfile = index;
                this.ProfileSelector.SelectedIndex = index;
            }
        }

        private void ProfileMenu_New(object sender, RoutedEventArgs e) {
            string newname = Defaults.GenerateValidNameForNewProfile(Defaults.Profile.ProfileName);

            Defaults.CreateNewProfile(newname);
            App.ViewModel.ReloadProfiles();
            PopulateProfileSettings();

            int newindex = this.Profiles.IndexOf(newname);
            App.ViewModel.SelectedProfile = newindex;
            this.ProfileSelector.SelectedIndex = newindex;

        }

        private void ProfileNameEditor_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                this.ProfileNameEditor.Visibility = Visibility.Hidden;
                string newname = this.ProfileNameEditor.Text.Trim();

                if (String.IsNullOrEmpty(newname) || !Defaults.IsValidFileName(this.ProfileNameEditor.Text)) {
                    return;
                }

                Defaults.RenameCurrentProfile(newname);

                App.ViewModel.ReloadProfiles();
                PopulateProfileSettings();

                int newindex = this.Profiles.IndexOf(newname);
                App.ViewModel.SelectedProfile = newindex;
                this.ProfileSelector.SelectedIndex = newindex;
            }
        }

        private void OpenArgumentsTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            try {
                string arguments = Comic.TestExecutionString(this.OpenArgumentsTextBox.Text);
                this.CommandExampleLabel.Text = "\"" + this.OpenApplicationTextBox.Text + "\" " + arguments;
            } catch (Exception ex) {
                this.CommandExampleLabel.Text = "Error: " + ex.Message;
                return;
            }
            this.ProfileChanged = true;
        }

        private void OpenApplicationTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            string application = this.OpenApplicationTextBox.Text;
            if (application == Defaults.StartupApplication.ViewerIndicator || File.Exists(application)) {
                OpenArgumentsTextBox_TextChanged(sender, e);
            } else {
                this.CommandExampleLabel.Text = "Error: File not found";
            }
        }
    }
}
