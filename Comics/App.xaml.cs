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
namespace Comics
{
    // Interaction logic for App.xaml
    public partial class App : Application
    {
        public static MainWindow ComicsWindow = null;
        public static SettingsWindow SettingsWindow = null;
        private static MainViewModel viewModel = null;
        public static MainViewModel ViewModel
        {
            get
            {
                if (viewModel == null)
                    viewModel = new MainViewModel();
                return viewModel;
            }
        }
    }
}
