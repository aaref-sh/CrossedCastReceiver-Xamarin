using Microsoft.AspNetCore.SignalR.Client;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.IO;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace CrossedCastReceiver
{
    public partial class MainPage : ContentPage
    {
        HubConnection connection;
        float NewPartPosX, NewPartPosY;
        SKBitmap NewPart;
        SKImageInfo info;
        SKSurface surface;
        MemoryStream stream;
        static string pass = "mainmain";
        double d,d1, ih, iw, wdth, heit;


        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            DeviceDisplay.KeepScreenOn = !DeviceDisplay.KeepScreenOn;
            ConfigSignalRConnection();
        }
        async void ConfigSignalRConnection()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://192.168.1.111:5000/CastHub")
                .WithAutomaticReconnect()
                .Build();
            connection.On<string, int, int, bool, int, int>("UpdateScreen", UpdateScreen);
            await connection.StartAsync();
            await connection.InvokeAsync("AddToGroup", "main");
            initvars();
            await connection.InvokeAsync("getscreen");
        }
        void initvars()
        {
            wdth = DeviceDisplay.MainDisplayInfo.Width-100;
            heit = DeviceDisplay.MainDisplayInfo.Height;
            d = wdth / heit;
            info = new SKImageInfo((int)wdth, (int)heit);
            surface = SKSurface.Create(info);
            surface.Canvas.Clear(SKColors.CornflowerBlue);
            Device.StartTimer(TimeSpan.FromSeconds(1f / 20), () => { skiascreen.InvalidateSurface();return true; });
        }
        void UpdateScreen(string ms, int r, int c, bool encrypted, int height, int width)
        {
            if (ms != null)
            {
                if (encrypted) ms = Decoded(ms);
                stream = new MemoryStream(Convert.FromBase64String(ms));
                NewPart = SKBitmap.Decode(stream);
                stream.Close();
                stream?.Dispose();
                d1 = 1.0 * width / height;
                if (d > d1){ ih = heit / 10; iw = ih * d1; }
                else { iw = wdth / 10; ih = iw / d1; }
                NewPart = NewPart.Resize(new SKSizeI((int)iw, (int)ih), SKFilterQuality.None);
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
