using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Clipboard;
using Plugin.SimpleAudioPlayer;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Xamarin.Essentials;
using Xamarin.Forms;


namespace CrossedCastReceiver
{
    struct Part
    {
        public int r { get; set; }
        public int c { get; set; }
        public string ms { get; set; }
        public bool encrypted { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public Part(int r, int c, string ms, bool encrypted, int width, int height)
        {
            this.r = r;
            this.c = c;
            this.ms = ms;
            this.encrypted = encrypted;
            this.width = width;
            this.height = height;
        }
    }
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
        globalVars gv = new globalVars();
        SKImageInfo info;
        SKSurface surface;
        string group;
        static string pass;
        double d, d1, ih, iw, wdth, heit, restwidth;
        string myname = "df";
        int pastw, pasth;
        #endregion
        public MainPage()
        {
            NavigationPage.SetHasNavigationBar(this, false);
            InitializeComponent();
            DeviceDisplay.KeepScreenOn = !DeviceDisplay.KeepScreenOn;
            MessagingCenter.Send<object, bool>(this, "Hide", false);
            ConfigSignalRConnection();
            myname = logger.name;
            Thread updateCanvase = new Thread(updatecanvase);
            updateCanvase.Start();

        }


        public static byte[] key;
        static string DESDecrypt(string crypted)
        {
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(crypted));
            CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateDecryptor(key, key), CryptoStreamMode.Read);
            StreamReader reader = new StreamReader(cryptoStream);
            return reader.ReadToEnd();
        }
        static string DESEncrypt(string original)
        {
            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoProvider.CreateEncryptor(key, key), CryptoStreamMode.Write);
                using (StreamWriter writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(original);
                    writer.Flush();
                    cryptoStream.FlushFinalBlock();
                    writer.Flush();
                    cryptoProvider.Dispose();
                    return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                }
            }
        }
        int port;
        async void ConfigSignalRConnection()
        {
            connection = new HubConnectionBuilder().WithUrl("http://" + logger.URL + ":5000/CastHub").WithAutomaticReconnect().Build();
            connection.On<string, int, int, bool, int, int>("UpdateScreen", UpdateScreen);
            connection.On<string, string>("newMessage", NewMessage);
            await connection.StartAsync();
            group = pass = await connection.InvokeAsync<string>("GetGroupId", logger.RoomName);
            key = Encoding.ASCII.GetBytes(group.Substring(0, 8));
            await connection.InvokeAsync("AddToGroup", group);
            await connection.InvokeAsync("SetName", myname);
            port = await connection.InvokeAsync<int>("getport", group);
            Initvars();
            await connection.InvokeAsync("getscreen");
            await connection.InvokeAsync("getMessages");

        }
        void play(byte[] buffer)
        {
            ISimpleAudioPlayer player = CrossSimpleAudioPlayer.Current;
            MemoryStream stream = new MemoryStream(buffer);
            player.Load(stream);
            player.Play();
        }
        Stream GetStreamFromFile(string filename)
        {
            var assembly = typeof(App).GetTypeInfo().Assembly;
            var stream = assembly.GetManifestResourceStream("YourApp." + filename);
            return stream;
        }
        private void SendButton_Clicked(object sender, EventArgs e)
        {
            if (MessageBox.Text != "")
            {
                connection.SendAsync("newMessage", DESEncrypt(MessageBox.Text));
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
            wdth = DeviceDisplay.MainDisplayInfo.Width*0.8;
            heit = DeviceDisplay.MainDisplayInfo.Height;
            d = wdth / heit;
            clearsurface();
            skiascreen.WidthRequest = wdth / 2;
            restwidth = DeviceDisplay.MainDisplayInfo.Width * 0.07;
            SendButton.WidthRequest = restwidth/4;
            MessageBox.WidthRequest = restwidth * 3 / 4;
            rest.WidthRequest = restwidth;
            
            var tapImage = new TapGestureRecognizer();
            tapImage.Tapped += SendButton_Clicked;
            SendButton.GestureRecognizers.Add(tapImage);

            MessageBox.Unfocused += (a, b) => hide();
            MessageBox.Completed += (a, b) => hide();
            Device.StartTimer(TimeSpan.FromSeconds(1f / 17), () => { skiascreen.InvalidateSurface(); return true; });
        }
        void hide() => MessagingCenter.Send<object, bool>(this, "Hide", false);

        void NewMessage(string sender, string message)
        {
            Frame container = new Frame();
            container.BorderColor = sender == myname ? Color.DarkGreen : Color.Silver;
            if (sender == "Teacher") container.BorderColor = Color.DarkRed;
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
            mesg.Text = DESDecrypt(message);
            mesg.TextColor = Color.Black;
            mesg.FontFamily = "Times New Roman";
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
        DateTime last = DateTime.Now;
        bool Updating = false;
        public static bool ia(char glyph)
        {
            if (glyph >= 0x600 && glyph <= 0x6ff) return true;
            if (glyph >= 0x750 && glyph <= 0x77f) return true;
            if (glyph >= 0xfb50 && glyph <= 0xfc3f) return true;
            if (glyph >= 0xfe70 && glyph <= 0xfefc) return true;
            return false;
        }
        Dictionary<Tuple<int, int>, Tuple<string, bool, int, int>> NewParts = new Dictionary<Tuple<int, int>, Tuple<string, bool, int, int>>();
        void updatecanvase()
        {
            while (true)
            {
                try
                {
                    while (NewParts.Any())
                    {
                        var x = NewParts.First();
                        NewParts.Remove(x.Key);
                        int height = x.Value.Item3;
                        int width = x.Value.Item4;
                        bool encrypted = x.Value.Item2;
                        string ms = x.Value.Item1;
                        int c = x.Key.Item2;
                        int r = x.Key.Item1;
                        if (height != pasth || width != pastw)
                        {
                            pasth = height;
                            pastw = width;
                            clearsurface();
                        }
                        if (encrypted) ms = Decoded(ms.Substring(0, 200)) + ms.Substring(200);
                        using (MemoryStream stream = new MemoryStream(Convert.FromBase64String(ms)))
                        using (SKBitmap NewPart = SKBitmap.Decode(stream))
                        {
                            d1 = 1.0 * width / height;
                            if (d > d1) { ih = heit / 10; iw = ih * d1; }
                            else { iw = wdth / 10; ih = iw / d1; }
                            NewPartPosX = c * (int)iw;
                            NewPartPosY = r * (int)ih;
                            surface.Canvas.DrawBitmap(NewPart.Resize(new SKSizeI((int)iw, (int)ih), SKFilterQuality.Medium), NewPartPosX, NewPartPosY);
                        }
                        updated = true;
                    }
                }
                catch (Exception) { }
                Thread.Sleep(2);
            }
        }
        List<Part> NewPart = new List<Part>();
        void UpdateScreen(string ms, int r, int c, bool encrypted, int height, int width)
        {
            if (ms != null) NewParts.Add(new Tuple<int, int>(r, c), new Tuple<string, bool, int, int>(ms, encrypted, height, width));
        }
        public static string Decoded(string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ pass[(i % pass.Length)]));
            return sb.ToString();
        }
        bool updated = false;
        private void skiascreen_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (surface != null && updated)
            {
                updated = false;
                e.Surface.Canvas.DrawImage(surface.Snapshot(), 0, 0);
            }
        }
    }
}
