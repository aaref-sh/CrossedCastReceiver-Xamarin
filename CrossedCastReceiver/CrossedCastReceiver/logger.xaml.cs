using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CrossedCastReceiver
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class logger : ContentPage
    {
        public static string name, RoomName,URL = "192.168.1.111";
        public logger()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();
            get_sessions();
            var tapImage = new TapGestureRecognizer();
            tapImage.Tapped += (sender,e)=>get_sessions();
            RefreshButton.GestureRecognizers.Add(tapImage);
        }
        void get_sessions()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] response = client.UploadValues("http://"+URL+"/api/Rooms/GetRooms", new NameValueCollection() { });
                    string result = Encoding.UTF8.GetString(response);
                    SessionList = JsonConvert.DeserializeObject<List<string>>(result);
                }
                session_list.Children?.Clear();
                foreach (var session in SessionList)
                {
                    RadioButton radiobutton = new RadioButton();
                    radiobutton.Content = session;
                    radiobutton.CheckedChanged += (sender, e) => RoomName = session;
                    session_list.Children.Add(radiobutton);
                }
            }
            catch{ }
        }
        List<string> SessionList;

        private void loginbutton_Clicked(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(nametb.Text))
            {
                DependencyService.Get<IMessage>().ShortAlert("يرجى ادخال اسمك");
                return;
            }
            if (string.IsNullOrEmpty(RoomName))
            {
                DependencyService.Get<IMessage>().ShortAlert("الرجاء اختيار محاضرة للدخول");
                return;
            }
            name = nametb.Text;
            Navigation.InsertPageBefore(new MainPage(),this);
            Navigation.PopAsync();
        }
    }
}