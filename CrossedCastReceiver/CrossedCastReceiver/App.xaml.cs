﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CrossedCastReceiver
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new logger());
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
