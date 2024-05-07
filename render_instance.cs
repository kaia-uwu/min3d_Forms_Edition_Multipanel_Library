using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace min3d_Forms_Edition_Multipanel_Library
{
    public unsafe class render_instance : IDisposable
    {
        public readonly renderer renderer;

        #region buffers
        private readonly Bitmap[] buffers = new Bitmap[2];
        public readonly Size resolution;

        private readonly float[] depth_buffer;

        private int write_buffer = 0, read_buffer = 1;

        private readonly Mutex read_mutex = new Mutex();
        private readonly Mutex render_mutex = new Mutex();
        #endregion

        #region settings

        public List<textured_mesh> meshes = new List<textured_mesh>();

        public Vector3 cam_pos = new Vector3(0, 0, 0);
        public Vector3 cam_up = new Vector3(0, 1, 0);
        public Vector3 cam_fwd = new Vector3(0, 0, 1);
        public Vector3 cam_right = new Vector3(-1, 0, 0);

        public Vector3 light_direction = new Vector3(0, 1, 0);

        public float fov = 60 * (MathF.PI / 180);

        public bool controls_enabled = false;

        #region programs
        public void* program_buffer { get; private set; }
        public int program_buffer_length { get; private set; }

        public void alloc_program_buffer(int p_length)
        {
            render_mutex.WaitOne();

            if (program_buffer != (void*)0)
                Marshal.FreeHGlobal((IntPtr)program_buffer);

            program_buffer = (void*)Marshal.AllocHGlobal(p_length);
            Unsafe.InitBlockUnaligned(program_buffer, 0, (uint)p_length);
            program_buffer_length = p_length;

            render_mutex.ReleaseMutex();
        }

        public delegate*<void*, void> frame_program = &default_frame_program;
        public delegate*<tri, void*, tri> geometry_program = &default_geometry_program;

        #region default
        public static void default_frame_program(void* p_plane_positionrogram_buffer)
        {

        }
        public static tri default_geometry_program(tri p_tri, void* p_plane_positionrogram_buffer)
        {
            return p_tri;
        }
        #endregion

        #endregion

        #endregion

        public bool stopped { get; private set; } = false;

        public bool disposed { get; private set; } = false;

        public render_instance(renderer p_renderer, viewport p_viewport, bool p_stopped = false)
        {
            renderer = p_renderer;

            resolution = p_viewport.Size; // basically max resolution

            buffers[0] = new Bitmap(resolution.Width, resolution.Height, PixelFormat.Format32bppArgb);
            buffers[1] = new Bitmap(resolution.Width, resolution.Height, PixelFormat.Format32bppArgb);

            depth_buffer = new float[resolution.Width * resolution.Height];

            stopped = p_stopped;
        }

        public void Dispose()
        {
            stopped = true;
            disposed = true;
            
            read_mutex.WaitOne();
            render_mutex.WaitOne();

            buffers[0].Dispose();
            buffers[1].Dispose();

            render_mutex.ReleaseMutex();
            read_mutex.ReleaseMutex();

            if (program_buffer != (void*)0)
                Marshal.FreeHGlobal((IntPtr)program_buffer);
        }

        public void start()
        {
            if (!stopped) return;

            read_mutex.ReleaseMutex();
            render_mutex.ReleaseMutex();

            stopped = false;
        }
        public void stop()
        {
            if (stopped) return;

            stopped = true;
            read_mutex.WaitOne();
            render_mutex.WaitOne();
        }

        internal render_buffers take_render_buffers()
        {
            render_mutex.WaitOne();
            return new render_buffers(buffers[write_buffer], resolution, depth_buffer);
        }
        internal void return_render_buffers()
        {
            render_mutex.ReleaseMutex();
        }

        internal Bitmap take_read_buffer()
        {
            read_mutex.WaitOne();
            return buffers[read_buffer];
        }
        internal void return_read_buffer()
        {
            read_mutex.ReleaseMutex();
        }

        internal void swap()
        {
            if (disposed)
                return;

            read_mutex.WaitOne();
            render_mutex.WaitOne();

            (read_buffer, write_buffer) = (write_buffer, read_buffer);

            render_mutex.ReleaseMutex();
            read_mutex.ReleaseMutex();
        }
    }
}
