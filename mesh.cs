using System;
using System.IO;
using System.Numerics;
using System.Globalization;
using System.Collections.Generic;

namespace min3d_Forms_Edition_Multipanel_Library
{
    public class mesh
    {
        public tri[] tris;

        public mesh(tri[] p_tris)
        {
            tris = p_tris;
        }
        public mesh(string p_file_path)
        {
            List<tri> loaded_tris = new List<tri>();
            StreamReader sr = File.OpenText(p_file_path);
            List<Vector4> verts = new List<Vector4>();

            string? line = sr.ReadLine();
            while (!sr.EndOfStream)
            {
                if (line != null)
                {
                    if (line != "")
                    {
                        switch (line.Split(' ')[0])
                        {
                            case "v":
                                {
                                    string[] coords = line.Split(' ');
                                    Vector4 v = new Vector4(
                                            float.Parse(coords[1], CultureInfo.InvariantCulture),
                                            float.Parse(coords[2], CultureInfo.InvariantCulture),
                                            float.Parse(coords[3], CultureInfo.InvariantCulture),
                                            1
                                        );
                                    verts.Add(v);
                                    break;
                                }
                            case "f":
                                {
                                    string[] tri_verts = line.Split(' ');
                                    loaded_tris.Add(
                                        new tri()
                                        {
                                            v0 = verts[int.Parse(tri_verts[1].Split('/')[0]) - 1],
                                            t0 = new Vector2(0, 1),
                                            v1 = verts[int.Parse(tri_verts[2].Split('/')[0]) - 1],
                                            t1 = new Vector2(0, 0),
                                            v2 = verts[int.Parse(tri_verts[3].Split('/')[0]) - 1],
                                            t2 = new Vector2(1, 0),
                                            c = new Vector4(1, 1, 1, 1),
                                        });
                                    break;
                                }
                        }
                    }
                }
                line = sr.ReadLine();
            }

            tris = loaded_tris.ToArray();

            verts.Clear();
            loaded_tris.Clear();
            sr.Dispose();
            GC.Collect();
        }
    };
}
