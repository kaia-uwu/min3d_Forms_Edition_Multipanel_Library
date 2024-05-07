using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace min3d_Forms_Edition_Multipanel_Library
{
    public class texture
    {
        public byte[] buffer { get; private set; }
        public Size size { get; private set; }

        public texture(string p_file_path)
        {
            Image texture_image = Image.FromFile(p_file_path);
            Bitmap texture = new Bitmap(texture_image);
            texture_image.Dispose();

            size = new Size(texture.Width, texture.Height);
            int buffer_len = size.Width * size.Height * 4;
            buffer = new byte[buffer_len];

            BitmapData locked_buffer = texture.LockBits(
            new Rectangle(0, 0, size.Width, size.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

            IntPtr locked_buffer_ptr = locked_buffer.Scan0;

            Marshal.Copy(locked_buffer_ptr, buffer, 0, buffer_len);

            texture.UnlockBits(locked_buffer);

            texture.Dispose();
            GC.Collect();
        }
    }
}
