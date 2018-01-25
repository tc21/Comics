using System;
using System.Collections.Generic;
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
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            ExtensionList.ItemsSource = Defaults.ImageSuffixes;
            CategoryList.ItemsSource = Defaults.RootPaths;
            
            FileInfo[] files = new DirectoryInfo(Defaults.UserProfileFolder).GetFiles("*.xmlprofile");
            string[] profiles = new string[files.Length];
            ProfileSelector.ItemsSource = profiles;

            int index = 0;
            for(int i = 0; i < profiles.Length; i++)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(files[i].Name);
                profiles[i] = name;
                if (Defaults.profile.ProfileName == name)
                    ProfileSelector.SelectedIndex = index;
                index++;
            }
        }

        private void ProfileChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedProfile = (string)((ComboBox)sender).SelectedItem;
            if (Defaults.profile.ProfileName == selectedProfile)
                return;

            if (Defaults.LoadProfile(selectedProfile))
                App.ViewModel.ReloadComics();

        }

        class ProfileItem
        {
            public string Name { get; set; }
            public bool Selected { get; set; }
        }

        private void Button_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
