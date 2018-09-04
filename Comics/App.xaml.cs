using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

// Comics: An iTunes-like organizer and viewer for comics and much more. 
// Copyright (C) Tianyi Cao, 2018
// Licensed under the BSD 2-Clause "Simplified" License. You should have received a copy of the license with this software.

/* TODOS
 * 1. Implement more customization options through the settings pane
 * 2. Blur the boundaries a bit between folder, file, comic, so you can have .pdfs or .zips as works, 
 *        Note: notably, there are comics in folders, comics as pdfs, and comics in zip files. These are all
 *        comics, and you want then to show up in the same library. But they have to be opened differently. The
 *        current program only allows you to have one way to open stuff, which will have to be changed.
 *    or folders as subitems in a work
 * 3. Create the ViewerWindow API to have different kinds of viewers with consistent behavior
 *       Note: for example, when there are multiple built-in viewers, they should all be abstracted at a level
 *       where the program doesn't care about what type of viewer it is. Currently, the program adds a context
 *       menu to viewer, and the viewer is able to report which page it currently is on (and which file that is).
 *       We will create an Interface so that this behavior can be generalized
 * 4. Implement library files so you don't have to index everything from disk everytime you startup the program
 * 5. Add keyboard shortcuts and controls since basically everything only works from the mouse right now
 * 
 * Last modified Tianyi Cao, 2018-03-11.
 */
namespace Comics {
    // Interaction logic for App.xaml
    public partial class App : Application {
        public static MainWindow ComicsWindow = null;
        public static SettingsWindow SettingsWindow = null;
        public static InfoWindow InfoWindow = null;
        private static MainViewModel viewModel = null;
        public static MainViewModel ViewModel {
            get {
                if (viewModel == null) {
                    viewModel = new MainViewModel();
                }

                return viewModel;
            }
        }
    }
}
