using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Comics {
    public partial class MainWindow: Window {
        private void CollectionEvent_OneOrMore_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = this.Collection.SelectedItems.Count > 0;
        }

        private void EditInfo_Executed(object sender, ExecutedRoutedEventArgs e) {
            if (App.InfoWindow != null) {
                App.InfoWindow.EditingComics = this.Collection.SelectedItems.Cast<Comic>().ToList();
                App.InfoWindow.Activate();
                return;
            }

            Window info = new InfoWindow {
                Owner = this,
                EditingComics = this.Collection.SelectedItems.Cast<Comic>().ToList(),
            };

            info.Show();
        }
    }

    public static class ApplicationCommands {
        public static readonly RoutedUICommand EditInfo = new RoutedUICommand("Edit Info", "EditInfo", typeof(ApplicationCommands));

        public static void Collection_OneOrMore_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = (sender as ListBox).SelectedItems.Count > 0;
        }
    }
}
