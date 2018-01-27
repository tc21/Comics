using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

// This project is currently closed source, non-production, for personal use by me only
// Copyright (C) Tianyi Cao, 2018, All Rights Reserved
// To anyone obtaining a copy of this, this project will *most likely* eventually
// Be licensed under BSD 2-clause. 
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
