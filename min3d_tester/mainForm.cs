using System.Drawing;
using System;
using System.Numerics;
using System.Windows.Forms;
using min3d_Forms_Edition_Multipanel_Library;

namespace min3d_tester
{
    public partial class mainForm : Form
    {
        public mainForm()
        {
            InitializeComponent();

            renderer renderer = new renderer();

            viewport viewport_1 = new viewport();
            viewport_1.Location = new Point(10, 10);
            viewport_1.Size = new Size(960, 480);
            viewport_1.BackColor = Color.LightGray;
            Controls.Add(viewport_1);

            viewport_1.instance = new render_instance(renderer, viewport_1);

            mesh mesh = new mesh("resources/models/teapot.obj");
            texture wood = new texture("resources/textures/texture.png");

            textured_mesh teapot_mesh = new textured_mesh(mesh, wood);
            viewport_1.instance.meshes.Add(teapot_mesh);

            viewport_1.instance.cam_pos = new Vector3(0.6f, 1.5f, 5);
            viewport_1.instance.light_direction = Vector3.Normalize(new Vector3(1, 1, 1));
            unsafe
            {
                viewport_1.instance.alloc_program_buffer(sizeof(long) + sizeof(float) + (sizeof(Matrix4x4) * 5)); // apparently sizeof(Matrix4x4) is not predefined and thus needs to be in unsafe

                static void frame_program(void* p_plane_positionrogram_buffer)
                {
                    #region time delta multiplier
                    long last_tick = *(long*)p_plane_positionrogram_buffer;
                    *(long*)p_plane_positionrogram_buffer = DateTime.Now.Ticks;
                    float ms_delta = (DateTime.Now.Ticks - last_tick) / 10_000f;
                    if (ms_delta > 250)
                        ms_delta = 250;
                    float delta_multiplier = ms_delta / 16.6f;
                    #endregion

                    #region rotation
                    float theta = *(float*)((long*)p_plane_positionrogram_buffer + 1) + (0.005f * delta_multiplier);
                    *(float*)((long*)p_plane_positionrogram_buffer + 1) = theta;

                    Matrix4x4 world = Matrix4x4.CreateRotationY(theta);

                    *(Matrix4x4*)((float*)((long*)p_plane_positionrogram_buffer + 1) + 1) = world; // !note! (float*)pointer + 1 means the address gets 1 * sizeof(float) added to it
                    #endregion
                }
                viewport_1.instance.frame_program = &frame_program;

                static tri scene_geometry_program(tri p_tri, void* p_plane_positionrogram_buffer)
                {
                    Matrix4x4 world = *(Matrix4x4*)((float*)((long*)p_plane_positionrogram_buffer + 1) + 1);

                    p_tri.v0 = Vector4.Transform(p_tri.v0, world);
                    p_tri.v1 = Vector4.Transform(p_tri.v1, world);
                    p_tri.v2 = Vector4.Transform(p_tri.v2, world);

                    return p_tri;
                }
                viewport_1.instance.geometry_program = &scene_geometry_program;

                viewport_1.instance.controls_enabled = true;
            }

            renderer.instances.Add(viewport_1.instance);
            viewport_1.start();
            renderer.start();
        }
    }
}