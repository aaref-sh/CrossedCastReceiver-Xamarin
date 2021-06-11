using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Clipboard;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.IO;
using System.Linq;
using System.Text;
using VoiceClient;
using Xamarin.Essentials;
using Xamarin.Forms;


namespace CrossedCastReceiver
{
    public interface IMessage
    {
        void LongAlert(string message);
        void ShortAlert(string message);
    }
    public partial class MainPage : ContentPage
    {
        #region variables
        HubConnection connection;
        float NewPartPosX, NewPartPosY;
        SKBitmap NewPart;
        globalVars gv = new globalVars();
        SKImageInfo info;
        SKSurface surface;
        MemoryStream stream;
        string group;
        static string pass;
        double d, d1, ih, iw, wdth, heit, restwidth;
        string myname = "df";
        int pastw, pasth;
        bool sound_on = true;
        #endregion
        //StreamClient sc = new StreamClient(22);
        public MainPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();
            DeviceDisplay.KeepScreenOn = !DeviceDisplay.KeepScreenOn;
            MessagingCenter.Send<Object, bool>(this, "Hide", false);
            ConfigSignalRConnection();
            //sc.Init();
            //sc.ConnectToServer();
            group = pass = logger.group;
            myname = logger.name;
        }
        async void ConfigSignalRConnection()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://192.168.1.111:5000/CastHub")
                .WithAutomaticReconnect()
                .Build();
            connection.On<string, int, int, bool, int, int>("UpdateScreen", UpdateScreen);
            connection.On<string, string>("newMessage", NewMessage);
            await connection.StartAsync();
            await connection.InvokeAsync("AddToGroup", group);
            await connection.InvokeAsync("SetName", myname);
            Initvars();
            await connection.InvokeAsync("getscreen");
            await connection.InvokeAsync("getMessages");

        }
        private void SendButton_Clicked(object sender, EventArgs e)
        {
            if (MessageBox.Text != "")
            {
                connection.InvokeAsync("newMessage", MessageBox.Text);
                MessageBox.Text = "";
            }
        }
        private MemoryStream getbgarray() => new MemoryStream(Convert.FromBase64String(gv.backgroundbase64));
        void clearsurface()
        {
            info = new SKImageInfo((int)wdth, (int)heit);
            surface = SKSurface.Create(info);
            SKBitmap bgg = SKBitmap.Decode(getbgarray()).Resize(new SKSizeI((int)wdth, (int)heit), SKFilterQuality.None);
            surface.Canvas.Clear();
            surface.Canvas.DrawBitmap(bgg, 0, 0);
        }
        void Initvars()
        {
            wdth = DeviceDisplay.MainDisplayInfo.Width * 0.78;
            heit = DeviceDisplay.MainDisplayInfo.Height;
            d = wdth / heit;
            clearsurface();
            skiascreen.WidthRequest = wdth / 2 - 5;
            restwidth = DeviceDisplay.MainDisplayInfo.Width * 0.22 / 2 - 5;
            SendButton.WidthRequest = restwidth / 4;
            MessageBox.WidthRequest = restwidth * 3 / 4;
            rest.WidthRequest = restwidth;

            var tapImage = new TapGestureRecognizer();
            tapImage.Tapped += SendButton_Clicked;
            SendButton.GestureRecognizers.Add(tapImage);

            MessageBox.Unfocused += (a, b) => hide();
            MessageBox.Completed += (a, b) => hide();
            Device.StartTimer(TimeSpan.FromSeconds(1f / 20), () => { skiascreen.InvalidateSurface(); return true; });
        }
        void hide() => MessagingCenter.Send<Object, bool>(this, "Hide", false);

        void NewMessage(string sender, string message)
        {
            Frame container = new Frame();
            container.BorderColor = sender == myname ? Color.LightGreen : Color.Silver;
            container.CornerRadius = 7;
            container.Padding = 8;
            var tapmessage = new TapGestureRecognizer();
            tapmessage.Tapped += TapImage_Tapped;
            container.GestureRecognizers.Add(tapmessage);
            StackLayout msg = new StackLayout();
            Label sndr = new Label();
            sndr.FontSize = 11;
            sndr.TextColor = Color.DarkGray;
            Label mesg = new Label();
            mesg.Text = message;
            mesg.TextColor = Color.Black;

            sndr.Text = sender;
            if (ia(message[0])) mesg.FlowDirection = FlowDirection.RightToLeft;
            msg.Children.Add(sndr);
            msg.Children.Add(mesg);
            container.Content = msg;
            MessageList.Children.Add(container);
            var lastChild = MessageList.Children.LastOrDefault();
            if (lastChild != null) scroller.ScrollToAsync(lastChild, ScrollToPosition.MakeVisible, true);
        }

        private void TapImage_Tapped(object sender, EventArgs e)
        {
            string text = (((sender as Frame).Content as StackLayout).Children.ElementAt(1) as Label).Text;
            CrossClipboard.Current.SetText(text);
            DependencyService.Get<IMessage>().ShortAlert("تم النسخ");
        }
        public static bool ia(char glyph)
        {
            if (glyph >= 0x600 && glyph <= 0x6ff) return true;
            if (glyph >= 0x750 && glyph <= 0x77f) return true;
            if (glyph >= 0xfb50 && glyph <= 0xfc3f) return true;
            if (glyph >= 0xfe70 && glyph <= 0xfefc) return true;
            return false;
        }
        void UpdateScreen(string ms, int r, int c, bool encrypted, int height, int width)
        {
            if (ms != null)
            {
                if (height != pasth || width != pastw) { pasth = height; pastw = width; clearsurface(); }
                if (encrypted) ms = Decoded(ms.Substring(0, 200)) + ms.Substring(200);
                stream = new MemoryStream(Convert.FromBase64String(ms));
                NewPart = SKBitmap.Decode(stream);
                stream.Close();
                stream?.Dispose();
                d1 = 1.0 * width / height;
                if (d > d1) { ih = heit / 10; iw = ih * d1; }
                else { iw = wdth / 10; ih = iw / d1; }
                NewPart = NewPart.Resize(new SKSizeI((int)iw, (int)ih), SKFilterQuality.High);
                NewPartPosX = c * (int)iw;
                NewPartPosY = r * (int)ih;
                surface.Canvas.DrawBitmap(NewPart, NewPartPosX, NewPartPosY);
                NewPart?.Dispose();
            }
        }
        public static string Decoded(string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ pass[(i % pass.Length)]));
            return sb.ToString();
        }
        private void skiascreen_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (surface != null) e.Surface.Canvas.DrawImage(surface.Snapshot(), 0, 0);
        }
    }
}
