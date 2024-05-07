using System;
using System.Drawing;
using System.Windows.Forms;

namespace min3d_Forms_Edition_Multipanel_Library
{
    public partial class viewport : Control
    {
        private render_instance? _instance;
        public bool has_instance
        {
            get
            {
                if (_instance == null || _instance.disposed)
                    return false;
                return true;
            }
        }
        public render_instance instance
        {
            get
            {
                if (has_instance)
                {
#pragma warning disable CS8603 // has_instance() checks for null, so this cannot return null
                    return _instance;
#pragma warning restore CS8603
                }
                throw new NullReferenceException("instance is null");
            }
            set
            {
                _instance = value;
            }
        }
        public bool stopped { get; private set; } = true;

        #region DEBUG
#if DEBUG
        private DateTime last_write;
        private readonly int[] write_fps_buffer = new int[10];
        private float write_fps;
#endif
        #endregion

        public viewport()
        {
            InitializeComponent();
        }
        public viewport(render_instance p_instance, bool p_stopped = true)
        {
            InitializeComponent();

            instance = p_instance;
            stopped = p_stopped;

            if (!p_stopped)
                start();
        }

        public void stop()
        {
            if (!stopped)
            {
                invalidation_timer.Stop();
                stopped = true;
            }
        }
        public void start()
        {
            if (!has_instance)
                throw new Exception("cannot start with no instance created");

            if (stopped)
            {
                invalidation_timer.Start();
                stopped = false;
            }
        }

        private bool should_handle()
        {
            if (stopped)
                return false;

            if (!has_instance || instance.stopped)
            {
                stop();
                return false;
            }

            return true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (!should_handle())
                return;

            Bitmap buffer = instance.take_read_buffer();
            e.Graphics.DrawImage(
                buffer,
                new Rectangle(
                    new Point(0, 0),
                    Size),
                new Rectangle(
                    new Point(0, 0),
                    instance.resolution),
                GraphicsUnit.Pixel);
            instance.return_read_buffer();

            #region DEBUG
#if DEBUG
            DateTime current_write = DateTime.Now;
            TimeSpan write_delta = current_write - last_write;
            last_write = current_write;

            Array.Copy(write_fps_buffer, 0, write_fps_buffer, 1, 9);
            write_fps_buffer[0] = (int)(1000 / write_delta.TotalMilliseconds);
            write_fps = 0;
            for (int i = 0; i < 10; i++)
            {
                write_fps += write_fps_buffer[i];
            }
            write_fps /= 10;
#endif
            #endregion
        }

        private void tick_handler(object? sender, EventArgs e)
        {
            if (!should_handle())
                return;

            Invalidate();

            #region DEBUG
#if DEBUG
            debug_label.Text = $"{Name}" +
                $" | {instance.renderer.render_delta.TotalMilliseconds:0000.0} ms" +
                $" | {instance.renderer.fps:0000.0} rfps" +
                $" | {write_fps:0000.0} wfps";
#endif
            #endregion
        }
    }
}
