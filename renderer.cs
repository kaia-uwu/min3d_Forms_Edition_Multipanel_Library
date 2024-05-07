using System;
using System.Drawing;
using System.Numerics;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace min3d_Forms_Edition_Multipanel_Library
{
    unsafe public class renderer : IDisposable
    {
        public readonly List<render_instance> instances;
        private Thread? render_thread;

        private bool thread_should_run = false;
        public bool thread_running { get; private set; } = false;

        #region DEBUG
#if DEBUG
        public TimeSpan render_delta;
        public float fps;
#endif
        #endregion

        public renderer()
        {
            instances = new List<render_instance>();

            start();
        }

        public void Dispose()
        {
            stop();
        }

        #region start/stop
        public void start()
        {
            if (!thread_running)
            {
                thread_should_run = true;

                render_thread = new Thread(render_loop)
                {
                    Name = "render_thread",
                    Priority = ThreadPriority.Highest,
                    IsBackground = true
                };

                render_thread.Start();
            }
        }
        public void stop()
        {
            if (render_thread != null && thread_running)
            {
                thread_should_run = false;
                render_thread.Join();
            }
        }
        #endregion

        private void render_loop()
        {
            #region shared
            long last_tick = 0;
            float move_speed = 0.05f;
            float rotation_speed = 0.05f;

            int[] fps_buffer = new int[10];

            byte[] keyboard = new byte[256];

            Queue<tri> tris_to_rasterize = new Queue<tri>();
            Queue<tri> tri_queue = new Queue<tri>();

            tri[] clipped = new tri[2];

            thread_running = true;
            #endregion

            while (thread_should_run)
            {
                for (int i_instance = 0; i_instance < instances.Count && thread_should_run; i_instance++)
                {
                    #region get instance
                    render_instance instance = instances[i_instance];

                    if (instance.stopped)
                        continue;
                    #endregion

                    #region controls handling
                    {
                        float ms_delta = (DateTime.Now.Ticks - last_tick) / 10_000f;
                        last_tick = DateTime.Now.Ticks;
                        if (ms_delta > 250)
                            ms_delta = 250;
                        float delta_multiplier = ms_delta / 16.6f;

                        float current_move_speed = move_speed * delta_multiplier;
                        float current_rotation_speed = rotation_speed * delta_multiplier;

                        if (instance.controls_enabled)
                        {
                            winapi.GetKeyState(0);
                            winapi.GetKeyboardState(keyboard);
                            if ((keyboard[0x57] & 0b10000000) > 0) // W 
                            {
                                instance.cam_pos -= instance.cam_fwd * current_move_speed;
                            }
                            if ((keyboard[0x41] & 0b10000000) > 0) // A
                            {
                                instance.cam_pos += instance.cam_right * current_move_speed;
                            }
                            if ((keyboard[0x53] & 0b10000000) > 0) // S
                            {
                                instance.cam_pos += instance.cam_fwd * current_move_speed;
                            }
                            if ((keyboard[0x44] & 0b10000000) > 0) // D
                            {
                                instance.cam_pos -= instance.cam_right * current_move_speed;
                            }
                            if ((keyboard[0x52] & 0b10000000) > 0) // R
                            {
                                instance.cam_pos += instance.cam_up * current_move_speed;
                            }
                            if ((keyboard[0x46] & 0b10000000) > 0) // F
                            {
                                instance.cam_pos -= instance.cam_up * current_move_speed;
                            }
                            if ((keyboard[0x45] & 0b10000000) > 0) // E
                            {
                                instance.cam_up =
                                    Vector3.Transform(
                                        instance.cam_up,
                                        Quaternion.CreateFromAxisAngle(
                                            instance.cam_fwd,
                                            -current_rotation_speed
                                            )
                                        );
                                instance.cam_right = Vector3.Cross(instance.cam_fwd, instance.cam_up);

                            }
                            if ((keyboard[0x51] & 0b10000000) > 0) // Q
                            {
                                instance.cam_up =
                                    Vector3.Transform(
                                        instance.cam_up,
                                        Quaternion.CreateFromAxisAngle(
                                            instance.cam_fwd,
                                            current_rotation_speed
                                            )
                                        );
                                instance.cam_right = Vector3.Cross(instance.cam_fwd, instance.cam_up);
                            }
                            if ((keyboard[0x27] & 0b10000000) > 0) // RIGHT ARROW
                            {
                                instance.cam_fwd =
                                    Vector3.Transform(
                                        instance.cam_fwd,
                                        Quaternion.CreateFromAxisAngle(
                                            instance.cam_up,
                                            -current_rotation_speed
                                            )
                                        );
                                instance.cam_right = Vector3.Cross(instance.cam_fwd, instance.cam_up);
                            }
                            if ((keyboard[0x25] & 0b10000000) > 0) // LEFT ARROW
                            {
                                instance.cam_fwd =
                                    Vector3.Transform(
                                        instance.cam_fwd,
                                        Quaternion.CreateFromAxisAngle(
                                            instance.cam_up,
                                            current_rotation_speed
                                            )
                                        );
                                instance.cam_right = Vector3.Cross(instance.cam_fwd, instance.cam_up);
                            }
                            if ((keyboard[0x26] & 0b10000000) > 0) // UP ARROW
                            {
                                instance.cam_fwd =
                                    Vector3.Transform(
                                        instance.cam_fwd,
                                        Quaternion.CreateFromAxisAngle(
                                            instance.cam_right,
                                            -current_rotation_speed
                                            )
                                        );
                                instance.cam_up = Vector3.Cross(instance.cam_right, instance.cam_fwd);
                            }
                            if ((keyboard[0x28] & 0b10000000) > 0) // DOWN ARROW
                            {
                                instance.cam_fwd =
                                    Vector3.Transform(
                                        instance.cam_fwd,
                                        Quaternion.CreateFromAxisAngle(
                                            instance.cam_right,
                                            current_rotation_speed
                                            )
                                        );
                                instance.cam_up = Vector3.Cross(instance.cam_right, instance.cam_fwd);
                            }
                        }
                    }
                    #endregion

                    #region mouse stuff for ray tri intersect test !commented out!
                    /* 
                    bool cursor_ii_viewport = false;
                    Vector2 cursor_pos = Vector2.Zero;

                
                    #region mouse stuff for ray tri intersect test

                    Point cursor_pos_px = Cursor.Position; // screen x y
                    Point owner_pos_px = viewport.TopLevelControl.Location; // top left

                    cursor_pos_px.X -= owner_pos_px.X;
                    cursor_pos_px.Y -= owner_pos_px.Y + 32;

                    if (cursor_pos_px.X > 0 && cursor_pos_px.Y > 0)
                    {
                        if (cursor_pos_px.X < render_resolution.x && cursor_pos_px.Y < render_resolution.y)
                        {
                            cursor_ii_viewport = true;
                            cursor_pos = new Vector2((float)cursor_pos_px.X / render_resolution.x, (float)cursor_pos_px.Y / render_resolution.y);
                        }
                    }
                    #endregion
                    */
                    #endregion

                    Stopwatch stopwatch = Stopwatch.StartNew();

                    #region get buffers
                    render_buffers buffers = instance.take_render_buffers();

                    Bitmap buffer = buffers.buffer;
                    Size resolution = buffers.resolution;

                    float[] depth_buffer = buffers.depth_buffer;
                    #endregion

                    #region get instance data
                    List<textured_mesh> meshes = instance.meshes;

                    void* program_buffer = instance.program_buffer;
                    delegate*<void*, void> frame_program = instance.frame_program;
                    delegate*<tri, void*, tri> geometry_program = instance.geometry_program;

                    Vector3 light_direction = instance.light_direction;
                    Vector3 cam_pos = instance.cam_pos;
                    Vector3 cam_fwd = instance.cam_fwd;
                    Vector3 cam_up = instance.cam_up;

                    float fov = instance.fov;
                    #endregion

                    #region precalculated for later
                    Vector4 viewport_transform_final = new Vector4(
                        0.5f * resolution.Width,
                        0.5f * resolution.Height,
                        1, 1);
                    #endregion

                    #region matrices
                    Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                        fov,
                        (float)resolution.Width / resolution.Height,
                        0.1f, 1000);
                    projection.M34 = -projection.M34; // invert w

                    Matrix4x4 view = Matrix4x4.CreateLookAt(
                        cam_pos, cam_pos + cam_fwd, cam_up);

                    #region ortho projection test !commented out!
                    /*
                    projection = Matrix4x4.CreateOrthographic(1.8f * resolution.Width / resolution.Height, 1.8f, 0.1f, 1000);
                    projection.M34 = -projection.M33; // this is so we get a depth value
                    */
                    #endregion

                    #endregion

                    #region lock bitmap buffer
                    BitmapData locked_buffer = buffer.LockBits(
                        new Rectangle(0, 0, buffer.Width, buffer.Height),
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb);

                    Span<byte> bitmap_buffer = new Span<byte>((void*)locked_buffer.Scan0, buffer.Width * buffer.Height * 4);
                    #endregion

                    #region set up buffers
                    Array.Clear(depth_buffer, 0, depth_buffer.Length);
                    bitmap_buffer.Clear();
                    #endregion

                    frame_program(program_buffer);

                    for (int i_mesh = 0; i_mesh < meshes.Count; i_mesh++)
                    {
                        #region get mesh data
                        textured_mesh textured_mesh = meshes[i_mesh];

                        mesh mesh = textured_mesh.mesh;
                        texture? texture = textured_mesh.texture;

                        byte[] texture_buffer = texture.buffer;
                        Size texture_resolution = texture.size;
                        #endregion

                        for (int i_tri = 0; i_tri < mesh.tris.Length; i_tri++)
                        {
                            tri tri = mesh.tris[i_tri];

                            tri = geometry_program(tri, program_buffer);

                            // normal
                            Vector3 line_1 = new Vector3(tri.v1.X, tri.v1.Y, tri.v1.Z) - new Vector3(tri.v0.X, tri.v0.Y, tri.v0.Z);
                            Vector3 line_2 = new Vector3(tri.v2.X, tri.v2.Y, tri.v2.Z) - new Vector3(tri.v0.X, tri.v0.Y, tri.v0.Z);
                            Vector3 normal = Vector3.Normalize(Vector3.Cross(line_1, line_2));

                            Vector3 cam_ray = new Vector3(tri.v0.X, tri.v0.Y, tri.v0.Z) - cam_pos;

                            // normal culling
                            if (Vector3.Dot(normal, cam_ray) >= 0.0f)
                                continue;

                            // normal shading
                            float dot_prod = MathF.Max(0.1f, Vector3.Dot(light_direction, normal));
                            tri.c *= new Vector4(dot_prod, dot_prod, dot_prod, 1);

                            // view
                            tri.v0 = Vector4.Transform(tri.v0, view);
                            tri.v1 = Vector4.Transform(tri.v1, view);
                            tri.v2 = Vector4.Transform(tri.v2, view);

                            // clipping
                            int clipped_tri_cnt = tri.clip_against_plane
                                (
                                    new Vector3(0.0f, 0.0f, 0.1f),
                                    new Vector3(0.0f, 0.0f, 1.0f),
                                    ref clipped[0], ref clipped[1]
                                );

                            for (int k = 0; k < clipped_tri_cnt; k++)
                            {
                                tri tri_clipped = clipped[k];

                                // projection
                                tri_clipped.v0 = Vector4.Transform(tri_clipped.v0, projection);
                                tri_clipped.v1 = Vector4.Transform(tri_clipped.v1, projection);
                                tri_clipped.v2 = Vector4.Transform(tri_clipped.v2, projection);

                                #region post-projection correction
                                float w0 = tri_clipped.v0.W;
                                float w1 = tri_clipped.v1.W;
                                float w2 = tri_clipped.v2.W;

                                // perspective divide
                                tri_clipped.v0 /= w0;
                                tri_clipped.v1 /= w1;
                                tri_clipped.v2 /= w2;

                                tri_clipped.t0 /= w0;
                                tri_clipped.t1 /= w1;
                                tri_clipped.t2 /= w2;

                                // implement inverted z buffering, where its 1 - (1 / w) so as to maximize resolution
                                tri_clipped.v0.W = 1 / w0;
                                tri_clipped.v1.W = 1 / w1;
                                tri_clipped.v2.W = 1 / w2;

                                // viewport transform
                                tri_clipped.v0 *= new Vector4(-1, -1, 1, 1);
                                tri_clipped.v1 *= new Vector4(-1, -1, 1, 1);
                                tri_clipped.v2 *= new Vector4(-1, -1, 1, 1);

                                tri_clipped.v0 += new Vector4(1, 1, 0, 0);
                                tri_clipped.v1 += new Vector4(1, 1, 0, 0);
                                tri_clipped.v2 += new Vector4(1, 1, 0, 0);

                                tri_clipped.v0 *= viewport_transform_final;
                                tri_clipped.v1 *= viewport_transform_final;
                                tri_clipped.v2 *= viewport_transform_final;
                                #endregion

                                tris_to_rasterize.Enqueue(tri_clipped);
                            }

                            #region clip and tesselate
                            while (tris_to_rasterize.Count > 0)
                            {
                                tri_queue.Enqueue(tris_to_rasterize.Dequeue());
                                int tris_to_clip = 1;

                                for (int plane = 0; plane < 4; plane++)
                                {
                                    for (int tris_to_add = 0; tris_to_clip > 0; tris_to_clip--)
                                    {
                                        tri tri_to_clip = tri_queue.Dequeue();

                                        #region clipping
                                        switch (plane)
                                        {
                                            case 0:
                                                tris_to_add = tri_to_clip.clip_against_plane
                                                    (
                                                        new Vector3(0.0f, 0.0f, 0.0f),
                                                        new Vector3(0.0f, 1.0f, 0.0f),
                                                        ref clipped[0], ref clipped[1]
                                                    );
                                                break;
                                            case 1:
                                                tris_to_add = tri_to_clip.clip_against_plane
                                                    (
                                                        new Vector3(0.0f, resolution.Height - 1.0f, 0.0f),
                                                        new Vector3(0.0f, -1.0f                   , 0.0f),
                                                        ref clipped[0], ref clipped[1]
                                                    );
                                                break;
                                            case 2:
                                                tris_to_add = tri_to_clip.clip_against_plane
                                                    (
                                                        new Vector3(0.0f, 0.0f, 0.0f),
                                                        new Vector3(1.0f, 0.0f, 0.0f),
                                                        ref clipped[0], ref clipped[1]
                                                    );
                                                break;
                                            case 3:
                                                tris_to_add = tri_to_clip.clip_against_plane
                                                    (
                                                        new Vector3(resolution.Width - 1.0f, 0.0f, 0.0f),
                                                        new Vector3(-1.0f                  , 0.0f, 0.0f),
                                                        ref clipped[0], ref clipped[1]
                                                    );
                                                break;
                                        }
                                        for (int w = 0; w < tris_to_add; w++)
                                        {
                                            tri_queue.Enqueue(clipped[w]);
                                        }
                                        #endregion
                                    }

                                    tris_to_clip = tri_queue.Count;
                                }

                                while (tri_queue.Count != 0)
                                {
                                    tri cur_tri = tri_queue.Dequeue();

                                    fill_tri(
                                        (int)cur_tri.v0.X, (int)cur_tri.v0.Y, cur_tri.v0.W, cur_tri.t0.X, cur_tri.t0.Y,
                                        (int)cur_tri.v1.X, (int)cur_tri.v1.Y, cur_tri.v1.W, cur_tri.t1.X, cur_tri.t1.Y,
                                        (int)cur_tri.v2.X, (int)cur_tri.v2.Y, cur_tri.v2.W, cur_tri.t2.X, cur_tri.t2.Y,
                                        bitmap_buffer, depth_buffer, resolution,
                                        texture_buffer, texture_resolution, cur_tri.c);
                                }

                                tri_queue.Clear();
                            }
                            #endregion
                        }
                    }

                    #region dot at origin test !DEBUG! 
#if DEBUG
                    if (i_instance != 0)
                        goto skip;

                    Vector4 test = Vector4.Transform(new Vector4(0, 0, 0, 1), view * projection);
                    float tw = test.W;
                    test /= tw;
                    test.W = 1 / tw;
                    if (test.W < 0)
                        goto skip;
                    test *= new Vector4(-1, -1, 1, 1);
                    test += new Vector4(1, 1, 0, 0);
                    test *= new Vector4(
                        0.5f * resolution.Width,
                        0.5f * resolution.Height,
                        1, 1);

                    for (int y = -2; y < 2 && test.Y + y > 0 && test.Y + y < resolution.Height; y++)
                        for (int x = -2; x < 2 && test.X + x > 0 && test.X + x < resolution.Width; x++)
                        {
                            int bitmap_buffer_offset = (int)(test.Y + y) * buffers.resolution.Width * 4 + (int)(test.X + x) * 4;

                            bitmap_buffer[bitmap_buffer_offset + 0] = 255;
                            bitmap_buffer[bitmap_buffer_offset + 1] = 000;
                            bitmap_buffer[bitmap_buffer_offset + 2] = 255;
                            bitmap_buffer[bitmap_buffer_offset + 3] = 255;
                        }

                    skip:
#endif
                    #endregion

                    #region unlock bitmap buffer
                    buffer.UnlockBits(locked_buffer);
                    #endregion

                    instance.return_render_buffers();
                    instance.swap();

                    stopwatch.Stop();

                    #region DEBUG
#if DEBUG
                    render_delta = stopwatch.Elapsed;
                    Array.Copy(fps_buffer, 0, fps_buffer, 1, 9);
                    fps_buffer[0] = (int)(1000 / render_delta.TotalMilliseconds);
                    fps = 0;
                    for (int j = 0; j < 10; j++)
                    {
                        fps += fps_buffer[j];
                    }
                    fps /= 10;
#endif
                    #endregion
                }
            }

            thread_running = false;
        }

        #region tessalation
        #region util
        private static void swap(ref int a, ref int b)
        {
            (b, a) = (a, b);
        }
        private static void swap(ref float a, ref float b)
        {
            (b, a) = (a, b);
        }
        #endregion

        #region fill_tri
        private static void fill_tri(
            int x0, int y0, float w0, float u0, float v0,
            int x1, int y1, float w1, float u1, float v1,
            int x2, int y2, float w2, float u2, float v2,
            Span<byte> p_bitmap_buffer, float[] p_depth_buffer, Size p_resolution,
            byte[] p_texture_buffer, Size p_texture_resolution, Vector4 p_c)
        {
            if (y1 < y0)
            {
                swap(ref y0, ref y1);
                swap(ref x0, ref x1);
                swap(ref u0, ref u1);
                swap(ref v0, ref v1);
                swap(ref w0, ref w1);
            }

            if (y2 < y0)
            {
                swap(ref y0, ref y2);
                swap(ref x0, ref x2);
                swap(ref u0, ref u2);
                swap(ref v0, ref v2);
                swap(ref w0, ref w2);
            }

            if (y2 < y1)
            {
                swap(ref y1, ref y2);
                swap(ref x1, ref x2);
                swap(ref u1, ref u2);
                swap(ref v1, ref v2);
                swap(ref w1, ref w2);
            }

            int dy0 = y1 - y0;
            int dx0 = x1 - x0;
            float dv0 = v1 - v0;
            float du0 = u1 - u0;
            float dw0 = w1 - w0;

            int dy1 = y2 - y0;
            int dx1 = x2 - x0;
            float dv1 = v2 - v0;
            float du1 = u2 - u0;
            float dw1 = w2 - w0;

            float dax_step = 0, dbx_step = 0,
                du0_step = 0, dv0_step = 0,
                du1_step = 0, dv1_step = 0,
                dw0_step = 0, dw1_step = 0;

            if (dy0 > 0) dax_step = dx0 / (float)MathF.Abs(dy0);
            if (dy1 > 0) dbx_step = dx1 / (float)MathF.Abs(dy1);

            if (dy0 > 0)
            {
                du0_step = du0 / (float)MathF.Abs(dy0);
                dv0_step = dv0 / (float)MathF.Abs(dy0);
                dw0_step = dw0 / (float)MathF.Abs(dy0);
            }

            if (dy1 > 0)
            {
                du1_step = du1 / (float)MathF.Abs(dy1);
                dv1_step = dv1 / (float)MathF.Abs(dy1);
                dw1_step = dw1 / (float)MathF.Abs(dy1);
            }

            if (dy0 > 0)
            {
                for (int x = y0; x <= y1; x++)
                {
                    int ax = (int)(x0 + ((float)x - y0) * dax_step);
                    int bx = (int)(x0 + ((float)x - y0) * dbx_step);

                    float su = u0 + ((float)x - y0) * du0_step;
                    float sv = v0 + ((float)x - y0) * dv0_step;
                    float sw = w0 + ((float)x - y0) * dw0_step;

                    float eu = u0 + ((float)x - y0) * du1_step;
                    float ev = v0 + ((float)x - y0) * dv1_step;
                    float ew = w0 + ((float)x - y0) * dw1_step;

                    fill_tri_inner(
                        x,
                        ax, bx, su, eu, sv, ev, sw, ew,
                        p_bitmap_buffer, p_depth_buffer, p_resolution,
                        p_texture_buffer, p_texture_resolution, p_c);
                }
            }

            dy0 = y2 - y1;
            dx0 = x2 - x1;
            dv0 = v2 - v1;
            du0 = u2 - u1;
            dw0 = w2 - w1;

            if (dy0 > 0) dax_step = dx0 / (float)MathF.Abs(dy0);
            if (dy1 > 0) dbx_step = dx1 / (float)MathF.Abs(dy1);

            du0_step = 0; dv0_step = 0;

            if (dy0 > 0)
            {
                du0_step = du0 / (float)MathF.Abs(dy0);
                dv0_step = dv0 / (float)MathF.Abs(dy0);
                dw0_step = dw0 / (float)MathF.Abs(dy0);
            }

            if (dy0 > 0)
            {
                for (int x = y1; x <= y2; x++)
                {
                    int ax = (int)(x1 + ((float)x - y1) * dax_step);
                    int bx = (int)(x0 + ((float)x - y0) * dbx_step);

                    float su = u1 + ((float)x - y1) * du0_step;
                    float sv = v1 + ((float)x - y1) * dv0_step;
                    float sw = w1 + ((float)x - y1) * dw0_step;

                    float eu = u0 + ((float)x - y0) * du1_step;
                    float ev = v0 + ((float)x - y0) * dv1_step;
                    float ew = w0 + ((float)x - y0) * dw1_step;

                    fill_tri_inner(
                        x,
                        ax, bx, su, eu, sv, ev, sw, ew,
                        p_bitmap_buffer, p_depth_buffer, p_resolution,
                        p_texture_buffer, p_texture_resolution, p_c);
                }
            }
        }
        private static void fill_tri_inner(
            int x,
            int ax, int bx, float su, float eu, float sv, float ev, float sw, float ew,
            Span<byte> p_bitmap_buffer, float[] p_depth_buffer, Size p_resolution,
            byte[] p_texture_buffer, Size p_texture_resolution, Vector4 p_c)
        {
            if (ax > bx)
            {
                swap(ref ax, ref bx);
                swap(ref su, ref eu);
                swap(ref sv, ref ev);
                swap(ref sw, ref ew);
            }

            float tstep = 1.0f / (bx - ax);
            float t = 0.0f;

            for (int y = ax; y < bx; y++)
            {
                float w = (1.0f - t) * sw + t * ew;
                float u = ((1.0f - t) * su + t * eu) / w;
                float v = ((1.0f - t) * sv + t * ev) / w;

                int depth_buffer_offset = x * p_resolution.Width + y;
                if (w > p_depth_buffer[depth_buffer_offset])
                {
                    int tex_x = (int)(v * (p_texture_resolution.Width - 1));
                    int tex_y = (int)(u * (p_texture_resolution.Height - 1));
                    int tex_offset = tex_y * p_texture_resolution.Width * 4 + tex_x * 4;

                    int bitmap_buffer_offset = x * p_resolution.Width * 4 + y * 4;

                    p_bitmap_buffer[bitmap_buffer_offset + 0] = (byte)(p_texture_buffer[tex_offset + 0] * p_c.Z);
                    p_bitmap_buffer[bitmap_buffer_offset + 1] = (byte)(p_texture_buffer[tex_offset + 1] * p_c.Y);
                    p_bitmap_buffer[bitmap_buffer_offset + 2] = (byte)(p_texture_buffer[tex_offset + 2] * p_c.X);
                    p_bitmap_buffer[bitmap_buffer_offset + 3] = (byte)(p_texture_buffer[tex_offset + 3] * p_c.W);

                    p_depth_buffer[depth_buffer_offset] = w;
                }
                t += tstep;
            }
        }
        #endregion
        #endregion

        #region other
        private static bool ray_tri_intersect(Vector3 p_origin, Vector3 p_ray, tri p_tri, ref Vector3 p_intersection_point, ref float p_distance)
        {
            Vector3 v0 = new Vector3(p_tri.v0.X, p_tri.v0.Y, p_tri.v0.Z);
            Vector3 v1 = new Vector3(p_tri.v1.X, p_tri.v1.Y, p_tri.v1.Z);
            Vector3 v2 = new Vector3(p_tri.v2.X, p_tri.v2.Y, p_tri.v2.Z);

            Vector3 edge1, edge2, h, s, q;
            float a, f, u, v;
            edge1 = v1 - v0;
            edge2 = v2 - v0;
            h = Vector3.Cross(p_ray, edge2);
            a = Vector3.Dot(edge1, h);

            if (a > -float.Epsilon && a < float.Epsilon)
                return false;

            f = 1.0f / a;
            s = p_origin - v0;
            u = f * Vector3.Dot(s, h);

            if (u < 0.0 || u > 1.0)
                return false;

            q = Vector3.Cross(s, edge1);
            v = f * Vector3.Dot(p_ray, q);

            if (v < 0.0 || u + v > 1.0)
                return false;

            float t = f * Vector3.Dot(edge2, q);

            if (t > float.Epsilon)
            {
                p_distance = t;
                p_intersection_point = p_origin + p_ray * t;
                return true;
            }
            else
                return false;
        }
        #endregion
    }
}