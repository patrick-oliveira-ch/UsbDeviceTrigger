using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace IconGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            // Créer une icône USB simple
            using (var bitmap = new Bitmap(256, 256))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.Clear(Color.Transparent);

                // Couleur bleu pour le symbole USB
                var usbColor = Color.FromArgb(0, 120, 215);
                var brush = new SolidBrush(usbColor);
                var pen = new Pen(usbColor, 12);
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                // Dessiner le symbole USB
                // Ligne centrale verticale
                graphics.DrawLine(pen, 128, 200, 128, 80);

                // Branche gauche (cercle)
                graphics.DrawLine(pen, 128, 80, 80, 50);
                graphics.FillEllipse(brush, 70, 40, 20, 20);

                // Branche droite (carré)
                graphics.DrawLine(pen, 128, 80, 176, 50);
                graphics.FillRectangle(brush, 166, 40, 20, 20);

                // Branche milieu (triangle)
                graphics.DrawLine(pen, 128, 120, 90, 90);
                var trianglePoints = new PointF[] {
                    new PointF(90, 90),
                    new PointF(80, 100),
                    new PointF(100, 100)
                };
                graphics.FillPolygon(brush, trianglePoints);

                // Base (flèche)
                var arrowPoints = new PointF[] {
                    new PointF(128, 200),
                    new PointF(108, 180),
                    new PointF(118, 180),
                    new PointF(118, 160),
                    new PointF(138, 160),
                    new PointF(138, 180),
                    new PointF(148, 180)
                };
                graphics.FillPolygon(brush, arrowPoints);

                // Sauvegarder en ICO
                SaveAsIcon(bitmap, "usb-icon.ico");
                Console.WriteLine("Icône créée : usb-icon.ico");
            }
        }

        static void SaveAsIcon(Bitmap bitmap, string filename)
        {
            using (var ms = new MemoryStream())
            {
                using (var fs = new FileStream(filename, FileMode.Create))
                using (var bw = new BinaryWriter(fs))
                {
                    // En-tête ICO
                    bw.Write((short)0); // Reserved
                    bw.Write((short)1); // Type (1 = ICO)
                    bw.Write((short)3); // Nombre d'images (16, 32, 256)

                    var sizes = new int[] { 16, 32, 256 };
                    long offset = 6 + (16 * 3); // Header + directory entries

                    // Directory entries
                    foreach (var size in sizes)
                    {
                        using (var resized = new Bitmap(bitmap, size, size))
                        using (var pngStream = new MemoryStream())
                        {
                            resized.Save(pngStream, ImageFormat.Png);
                            var pngData = pngStream.ToArray();

                            bw.Write((byte)size); // Width
                            bw.Write((byte)size); // Height
                            bw.Write((byte)0);    // Color palette
                            bw.Write((byte)0);    // Reserved
                            bw.Write((short)1);   // Color planes
                            bw.Write((short)32);  // Bits per pixel
                            bw.Write((int)pngData.Length); // Size
                            bw.Write((int)offset); // Offset

                            offset += pngData.Length;
                        }
                    }

                    // Image data
                    foreach (var size in sizes)
                    {
                        using (var resized = new Bitmap(bitmap, size, size))
                        using (var pngStream = new MemoryStream())
                        {
                            resized.Save(pngStream, ImageFormat.Png);
                            bw.Write(pngStream.ToArray());
                        }
                    }
                }
            }
        }
    }
}
