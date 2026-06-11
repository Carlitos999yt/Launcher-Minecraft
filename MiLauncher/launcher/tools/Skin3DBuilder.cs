using System;
using System.Windows;
using System.Windows.Media.Media3D;

namespace MiLauncher.launcher.tools
{
    public static class Skin3DBuilder
    {
        public static MeshGeometry3D BuildSteveMesh(bool isSlim)
        {
            var mesh = new MeshGeometry3D();
            
            // Proporciones (Minecraft real pixel scale)
            // Head: 8x8x8
            // Torso: 8x12x4
            // Arms: 4x12x4 (o 3x12x4 slim)
            // Legs: 4x12x4
            
            // Uvs divided by 64
            Func<double, double, double, double, Rect> uv = (x, y, w, h) => new Rect(x / 64.0, y / 64.0, w / 64.0, h / 64.0);

            // HEAD (Center Y=28, 8x8x8)
            AddBox(mesh, new Point3D(0, 28, 0), 8, 8, 8,
                uv(8, 8, 8, 8),     // Front
                uv(24, 8, 8, 8),    // Back
                uv(0, 8, 8, 8),     // Right
                uv(16, 8, 8, 8),    // Left
                uv(8, 0, 8, 8),     // Top
                uv(16, 0, 8, 8)     // Bottom
            );

            // TORSO (Center Y=18, 8x12x4)
            AddBox(mesh, new Point3D(0, 18, 0), 8, 12, 4,
                uv(20, 20, 8, 12),  // Front
                uv(32, 20, 8, 12),  // Back
                uv(16, 20, 4, 12),  // Right
                uv(28, 20, 4, 12),  // Left
                uv(20, 16, 8, 4),   // Top
                uv(28, 16, 8, 4)    // Bottom
            );

            // RIGHT ARM (Center X=-6 (or -5.5), Y=18, 4x12x4)
            double armW = isSlim ? 3 : 4;
            double rArmX = isSlim ? -5.5 : -6;
            AddBox(mesh, new Point3D(rArmX, 18, 0), armW, 12, 4,
                uv(44, 20, armW, 12),  // Front
                uv(52, 20, armW, 12),  // Back
                uv(40, 20, 4, 12),     // Right
                uv(48, 20, 4, 12),     // Left
                uv(44, 16, armW, 4),   // Top
                uv(48, 16, armW, 4)    // Bottom
            );

            // LEFT ARM (Center X=6 (or 5.5), Y=18)
            double lArmX = isSlim ? 5.5 : 6;
            AddBox(mesh, new Point3D(lArmX, 18, 0), armW, 12, 4,
                uv(36, 52, armW, 12),  // Front
                uv(44, 52, armW, 12),  // Back
                uv(32, 52, 4, 12),     // Right
                uv(40, 52, 4, 12),     // Left
                uv(36, 48, armW, 4),   // Top
                uv(40, 48, armW, 4)    // Bottom
            );

            // RIGHT LEG (Center X=-2, Y=6)
            AddBox(mesh, new Point3D(-2, 6, 0), 4, 12, 4,
                uv(4, 20, 4, 12),   // Front
                uv(12, 20, 4, 12),  // Back
                uv(0, 20, 4, 12),   // Right
                uv(8, 20, 4, 12),   // Left
                uv(4, 16, 4, 4),    // Top
                uv(8, 16, 4, 4)     // Bottom
            );

            // LEFT LEG (Center X=2, Y=6)
            AddBox(mesh, new Point3D(2, 6, 0), 4, 12, 4,
                uv(20, 52, 4, 12),  // Front
                uv(28, 52, 4, 12),  // Back
                uv(16, 52, 4, 12),  // Right
                uv(24, 52, 4, 12),  // Left
                uv(20, 48, 4, 4),   // Top
                uv(24, 48, 4, 4)    // Bottom
            );

            return mesh;
        }

        private static void AddBox(MeshGeometry3D mesh, Point3D center, double dx, double dy, double dz, 
            Rect uvFront, Rect uvBack, Rect uvRight, Rect uvLeft, Rect uvTop, Rect uvBottom)
        {
            double hx = dx / 2; double hy = dy / 2; double hz = dz / 2;
            
            Point3D p0 = new Point3D(center.X - hx, center.Y + hy, center.Z + hz); // Front Top Left
            Point3D p1 = new Point3D(center.X + hx, center.Y + hy, center.Z + hz); // Front Top Right
            Point3D p2 = new Point3D(center.X - hx, center.Y - hy, center.Z + hz); // Front Bottom Left
            Point3D p3 = new Point3D(center.X + hx, center.Y - hy, center.Z + hz); // Front Bottom Right
            
            Point3D p4 = new Point3D(center.X - hx, center.Y + hy, center.Z - hz); // Back Top Left
            Point3D p5 = new Point3D(center.X + hx, center.Y + hy, center.Z - hz); // Back Top Right
            Point3D p6 = new Point3D(center.X - hx, center.Y - hy, center.Z - hz); // Back Bottom Left
            Point3D p7 = new Point3D(center.X + hx, center.Y - hy, center.Z - hz); // Back Bottom Right

            Action<Point3D, Point3D, Point3D, Point3D, Rect> addFace = (a, b, c, d, uv) => 
            {
                int i = mesh.Positions.Count;
                mesh.Positions.Add(a); mesh.Positions.Add(b); mesh.Positions.Add(c); mesh.Positions.Add(d);
                mesh.TriangleIndices.Add(i); mesh.TriangleIndices.Add(i+2); mesh.TriangleIndices.Add(i+1);
                mesh.TriangleIndices.Add(i+1); mesh.TriangleIndices.Add(i+2); mesh.TriangleIndices.Add(i+3);
                mesh.TextureCoordinates.Add(new Point(uv.Left, uv.Top));
                mesh.TextureCoordinates.Add(new Point(uv.Right, uv.Top));
                mesh.TextureCoordinates.Add(new Point(uv.Left, uv.Bottom));
                mesh.TextureCoordinates.Add(new Point(uv.Right, uv.Bottom));
            };

            addFace(p0, p1, p2, p3, uvFront);
            addFace(p5, p4, p7, p6, uvBack);
            addFace(p4, p0, p6, p2, uvRight); 
            addFace(p1, p5, p3, p7, uvLeft);  
            addFace(p4, p5, p0, p1, uvTop);
            addFace(p2, p3, p6, p7, uvBottom);
        }
    }
}
