using System;
using System.Drawing;
using System.IO;
using System.Text;

namespace ImageEditor
{
    public class ImageEdit
    {
        int x = 10, y = 10;
        static string pass = "mainmain";
        public byte[] Update(string main, string ms, int r, int c, bool encrypted)
        {
            if (encrypted) ms = Decoded(ms.Substring(0, 200)) + ms.Substring(200);
            Bitmap pic = GetImage(main);
            Image img = GetImage(ms);

            if (pic.Width / x != img.Width || pic.Height / y != img.Height)
                pic = new Bitmap(img.Width * x, img.Height * y);
            Rectangle rec = new Rectangle(new System.Drawing.Point(0, 0), new System.Drawing.Size(img.Width, img.Height));
            using (Graphics g = Graphics.FromImage(pic))
                g.DrawImage(img, c * img.Width, r * img.Height, rec, GraphicsUnit.Pixel);
            using (MemoryStream m = new MemoryStream())
            {
                pic.Save(m, System.Drawing.Imaging.ImageFormat.Jpeg);
                return m.ToArray();
            }
        }
        public static Bitmap GetImage(string base64String)
        {
            //Convert Base64 string to byte[]
            byte[] byteBuffer = Convert.FromBase64String(base64String);
            MemoryStream memoryStream = new MemoryStream(byteBuffer);

            memoryStream.Position = 0;

            Bitmap bmpReturn = (Bitmap)Bitmap.FromStream(memoryStream);

            memoryStream.Close();
            memoryStream = null;
            byteBuffer = null;

            return bmpReturn;
        }
        public static string Decoded(string input)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
                sb.Append((char)(input[i] ^ pass[(i % pass.Length)]));
            return sb.ToString();
        }

    }
}
