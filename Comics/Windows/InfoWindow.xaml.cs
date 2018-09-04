using System;
using System.Collections.Generic;
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

namespace Comics.Windows {
    /// <summary>
    /// Interaction logic for InfoWindow.xaml
    /// </summary>
    public partial class InfoWindow : Window {
        public InfoWindow() {
            InitializeComponent();

            this.DataContext = App.ViewModel;
        }

        private void Tag_Checked(object sender, RoutedEventArgs e) {

        }

        private void Tag_Unchecked(object sender, RoutedEventArgs e) {

        }

        private void Button_EditCategories(object sender, RoutedEventArgs e) {

        }

        private void Button_Confirm(object sender, RoutedEventArgs e) {

        }

        private void Button_Apply(object sender, RoutedEventArgs e) {

        }

        private void Button_Cancel(object sender, RoutedEventArgs e) {

        }
    }
}
