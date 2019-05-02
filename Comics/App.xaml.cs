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
 * 4. Add keyboard shortcuts and controls since basically everything only works from the mouse right now
 * 6. Add the ability to have custom actions: for example, you may be able to assign an action
 *        type: right click menu item
 *        name: upsample comic using waifu2x
 *        action: {
 *            subprocess.call(['waifu2x-cui', '-w', '2160', '-o', '{folder}/temp', '-i', '{folder}'])
 *            for f in os.listdir('{folder}/temp'):
 *                os.remove('{folder}/' + f)
 *                os.rename('{folder}/temp/' + f, '{folder}/' + f)
 *            os.rmdir('{folder}/temp')
 *        }
 *    which is saved per profile.
 *    
 *    
 * 7. Tag matching: switch between AND mode / OR mode
 * 8. Right click: show only this author
 * 9. (Long term) live author filtering
 * 10. Scroll bar for tags (min-height for scroll views)?
 * 
 * Last modified Tianyi Cao, 2019-05-02.
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
                if (viewModel is null) {
                    viewModel = new MainViewModel();
                }

                return viewModel;
            }
        }
    }
}
