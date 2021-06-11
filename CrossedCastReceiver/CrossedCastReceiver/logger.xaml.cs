using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CrossedCastReceiver
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class logger : ContentPage
    {
        public static string name, group;
        public logger()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();
        }

        private void loginbutton_Clicked(object sender, EventArgs e)
        {
            name = nametb.Text;
            group = grouptb.Text;
            Navigation.InsertPageBefore(new MainPage(),this);
            Navigation.PopAsync();
        }
    }
}