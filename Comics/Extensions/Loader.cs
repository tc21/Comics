using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Comics.Extensions {
    public class Loader {
        private static string UserProfileExtensionsFolder => Path.Combine(Defaults.UserExtensionsFolder, Defaults.Profile.ProfileName);

        private static IEnumerable<string> Files() {
            if (Directory.Exists(Defaults.UserExtensionsFolder)) {
                foreach (var file in Directory.GetFiles(Defaults.UserExtensionsFolder)) {
                    yield return file;
                }
            }
            if (Directory.Exists(UserProfileExtensionsFolder)) {
                foreach (var file in Directory.GetFiles(UserProfileExtensionsFolder)) {
                    yield return file;
                }
            }
        }

        public static IEnumerable<Tuple<string, ICommand>> Actions() {
            App.ComicsWindow.Collection.CommandBindings.Clear();

            foreach (var file in Files()) {
                if (file.EndsWith(".extension.py")) {
                    var extensionCommand = new RoutedCommand();
                    void extensionAction(object sender, ExecutedRoutedEventArgs e) => PythonnetExtensions.ExecuteCommand(sender, e, file);
                    var extensionCommandBinding = new CommandBinding(extensionCommand, extensionAction, ApplicationCommands.Collection_OneOrMore_CanExecute);
                    App.ComicsWindow.Collection.CommandBindings.Add(extensionCommandBinding);

                    var fileInfo = new FileInfo(file);
                    var name = fileInfo.Name.Substring(0, fileInfo.Name.Length - ".extension.py".Length);

                    yield return new Tuple<string, ICommand>(name, extensionCommand);
                }
            }
        }
    }
}
