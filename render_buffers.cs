using System.Drawing;

namespace min3d_Forms_Edition_Multipanel_Library
{
    internal readonly struct render_buffers
    {
        internal readonly Bitmap buffer;
        internal readonly Size resolution;

        internal readonly float[] depth_buffer;

        internal render_buffers(
            Bitmap p_buffer,
            Size p_resolution,
            float[] p_depth_buffer)
        {
            buffer = p_buffer;
            resolution = p_resolution;
            depth_buffer = p_depth_buffer;
        }
    }
}
