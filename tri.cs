using System.Numerics;

namespace min3d_Forms_Edition_Multipanel_Library
{
    public struct tri
    {
        public Vector4 v0;
        public Vector4 v1;
        public Vector4 v2;

        public Vector2 t0;
        public Vector2 t1;
        public Vector2 t2;

        public Vector4 c;

        private static float dist(Vector4 p_v, Vector3 p_plane_normal, Vector3 p_plane_position)
        {
            return p_plane_normal.X * p_v.X + p_plane_normal.Y * p_v.Y + p_plane_normal.Z * p_v.Z - Vector3.Dot(p_plane_normal, p_plane_position);
        }
        internal readonly unsafe int clip_against_plane(Vector3 p_plane_position, Vector3 p_plane_normal, ref tri p_out_1, ref tri p_out_2)
        {
            p_plane_normal = Vector3.Normalize(p_plane_normal);

            Vector4* inside_v = stackalloc Vector4[3];
            Vector4* outside_v = stackalloc Vector4[3];
            Vector2* inside_t = stackalloc Vector2[3];
            Vector2* outside_t = stackalloc Vector2[3];

            int inside_count = 0; int outside_count = 0;
            int inside_t_count = 0; int outside_t_count = 0;

            float d0 = dist(v0, p_plane_normal, p_plane_position);
            float d1 = dist(v1, p_plane_normal, p_plane_position);
            float d2 = dist(v2, p_plane_normal, p_plane_position);

            if (d0 >= 0)
            {
                inside_v[inside_count++] = v0;
                inside_t[inside_t_count++] = t0;
            }
            else
            {
                outside_v[outside_count++] = v0;
                outside_t[outside_t_count++] = t0;
            }

            if (d1 >= 0)
            {
                inside_v[inside_count++] = v1;
                inside_t[inside_t_count++] = t1;
            }
            else
            {
                outside_v[outside_count++] = v1;
                outside_t[outside_t_count++] = t1;
            }
            if (d2 >= 0)
            {
                inside_v[inside_count++] = v2;
                inside_t[inside_t_count++] = t2;
            }
            else
            {
                outside_v[outside_count++] = v2;
                outside_t[outside_t_count++] = t2;
            }

            if (inside_count == 0)
            {
                return 0;
            }

            if (inside_count == 3)
            {
                p_out_1 = this;

                return 1;
            }

            if (inside_count == 1 && outside_count == 2)
            {
                float t = 0;

                #region p_out_1
                p_out_1.c = c;
                p_out_1.v0 = inside_v[0];
                p_out_1.t0 = inside_t[0];

                p_out_1.v1 = intersect_plane(p_plane_position, p_plane_normal, inside_v[0], outside_v[0], ref t);
                p_out_1.t1 = t * (outside_t[0] - inside_t[0]) + inside_t[0];
                p_out_1.v1.W = t * (outside_v[0].W - inside_v[0].W) + inside_v[0].W;

                p_out_1.v2 = intersect_plane(p_plane_position, p_plane_normal, inside_v[0], outside_v[1], ref t);
                p_out_1.t2 = t * (outside_t[1] - inside_t[0]) + inside_t[0];
                p_out_1.v2.W = t * (outside_v[1].W - inside_v[0].W) + inside_v[0].W;
                #endregion

                return 1;
            }

            if (inside_count == 2 && outside_count == 1)
            {
                float t = 0;

                #region p_out_1
                p_out_1.c = c;
                p_out_1.v0 = inside_v[0];
                p_out_1.t0 = inside_t[0];

                p_out_1.v1 = inside_v[1];
                p_out_1.t1 = inside_t[1];

                p_out_1.v2 = intersect_plane(p_plane_position, p_plane_normal, inside_v[0], outside_v[0], ref t);
                p_out_1.t2 = t * (outside_t[0] - inside_t[0]) + inside_t[0];
                p_out_1.v2.W = t * (outside_v[0].W - inside_v[0].W) + inside_v[0].W;
                #endregion

                #region p_out_2
                p_out_2.c = c;
                p_out_2.v0 = inside_v[1];
                p_out_2.t0 = inside_t[1];

                p_out_2.v1 = p_out_1.v2;
                p_out_2.t1 = p_out_1.t2;

                p_out_2.v2 = intersect_plane(p_plane_position, p_plane_normal, inside_v[1], outside_v[0], ref t);
                p_out_2.t2 = t * (outside_t[0] - inside_t[1]) + inside_t[1];
                p_out_2.v2.W = t * (outside_v[0].W - inside_v[1].W) + inside_v[1].W;
                #endregion

                return 2;
            }
            return 0;
        }
        private static Vector4 intersect_plane(Vector3 p_plane_position, Vector3 p_plane_normal, Vector4 p_start, Vector4 p_end, ref float p_t)
        {
            p_plane_normal = Vector3.Normalize(p_plane_normal);

            Vector3 start = new Vector3(p_start.X, p_start.Y, p_start.Z);
            Vector3 end = new Vector3(p_end.X, p_end.Y, p_end.Z);

            float plane_d = -Vector3.Dot(p_plane_normal, p_plane_position);
            float ad = Vector3.Dot(start, p_plane_normal);
            float bd = Vector3.Dot(end, p_plane_normal);
            p_t = (-plane_d - ad) / (bd - ad);
            Vector3 result = start + ((end - start) * p_t);

            return new Vector4(result, 1);
        }
    }
}
