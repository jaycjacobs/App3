using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Cirros.Imaging
{
    public class Effects
    {
        private static Point Bilinear(Point[] points, double U, double V)
        {
            Point R = new Point();
            Point S = new Point();

            R.X = (1 - U) * points[0].X + U * points[1].X;
            R.Y = (1 - U) * points[0].Y + U * points[1].Y;
            S.X = (1 - U) * points[3].X + U * points[2].X;
            S.Y = (1 - U) * points[3].Y + U * points[2].Y;

            return new Point((1 - V) * R.X + V * S.X, (1 - V) * R.Y + V * S.Y);
        }

        public static WriteableBitmap FastWarp(WriteableBitmap sourceBitmap, Point[] points, Rect destBounds)
        {
            WriteableBitmap destinationBitmap = new WriteableBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight);

            using (BitmapContext dbc = destinationBitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                int[] destpm = dbc.Pixels;

                using (BitmapContext sbc = sourceBitmap.GetBitmapContext(ReadWriteMode.ReadOnly))
                {
                    int[] srcpm = sbc.Pixels;

                    for (int y = 0; y < destinationBitmap.PixelHeight; y++)
                    {
                        double V = (y - destBounds.Y) / destBounds.Height;

                        for (int x = 0; x < destinationBitmap.PixelWidth; x++)
                        {
                            uint cs = 0x00000000;
                            double U = (x - destBounds.X) / destBounds.Width;
#if false
                            Point p = Bilinear(points, U, V);
                            if (p.X >= 0 && p.X < destinationBitmap.PixelWidth && p.Y >= 0 && p.Y < destinationBitmap.PixelHeight)
                            {
                                cs = (uint)sbc.WriteableBitmap.GetPixeli((int)p.X, (int)p.Y);
                            }
#else
                            double RX = (1 - U) * points[0].X + U * points[1].X;
                            double RY = (1 - U) * points[0].Y + U * points[1].Y;
                            double SX = (1 - U) * points[3].X + U * points[2].X;
                            double SY = (1 - U) * points[3].Y + U * points[2].Y;

                            int PX = (int)((1 - V) * RX + V * SX);
                            int PY = (int)((1 - V) * RY + V * SY);

                            if (PX >= 0 && PX < destinationBitmap.PixelWidth && PY >= 0 && PY < destinationBitmap.PixelHeight)
                            {
                                int srs = PY * sourceBitmap.PixelWidth;
                                int sro = PX % sourceBitmap.PixelWidth;

                                int sindex = srs + sro;
                                if (sindex >= 0 && sindex < srcpm.Length)
                                {
                                    cs = (uint)srcpm[sindex];
                                }
                            }
#endif
                            int drs = y * destinationBitmap.PixelWidth;
                            int dro = x % destinationBitmap.PixelWidth;

                            int index = drs + dro;
                            if (index >= 0 && index < destpm.Length)
                            {
                                destpm[index] = (int)cs;
                            }
                        }
                    }
                }
            }

            return destinationBitmap;
        }

        public static WriteableBitmap Warp(WriteableBitmap sourceBitmap, Point[] points, Rect destBounds)
        {
            // warp using bilinear transformation AND bilinear interpolation!

            WriteableBitmap destinationBitmap = new WriteableBitmap(sourceBitmap.PixelWidth, sourceBitmap.PixelHeight);

            using (BitmapContext dbc = destinationBitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                int[] destpm = dbc.Pixels;

                using (BitmapContext sbc = sourceBitmap.GetBitmapContext(ReadWriteMode.ReadOnly))
                {
                    int[] srcpm = sbc.Pixels;
                    int widthSource = sbc.Width;
                    int heightSource = sbc.Height;

                    for (int y = 0; y < destinationBitmap.PixelHeight; y++)
                    {
                        double V = (y - destBounds.Y) / destBounds.Height;

                        for (int x = 0; x < destinationBitmap.PixelWidth; x++)
                        {
                            uint cs = 0x00000000;
                            double U = (x - destBounds.X) / destBounds.Width;

                            Point p = Bilinear(points, U, V);
                            if (p.X >= 0 && p.X < destinationBitmap.PixelWidth && p.Y >= 0 && p.Y < destinationBitmap.PixelHeight)
                            {
                                int x0 = (int)p.X;
                                int y0 = (int)p.Y;

                                // Calculate coordinates of the 4 interpolation points
                                float fracx = (float)p.X - x0;
                                float fracy = (float)p.Y - y0;
                                float ifracx = 1f - fracx;
                                float ifracy = 1f - fracy;

                                int x1 = x0 + 1;
                                if (x1 >= widthSource)
                                {
                                    x1 = x0;
                                }
                                int y1 = y0 + 1;
                                if (y1 >= heightSource)
                                {
                                    y1 = y0;
                                }

                                // Read source color
                                int c = srcpm[y0 * widthSource + x0];
                                byte c1a = (byte)(c >> 24);
                                byte c1r = (byte)(c >> 16);
                                byte c1g = (byte)(c >> 8);
                                byte c1b = (byte)(c);

                                c = srcpm[y0 * widthSource + x1];
                                byte c2a = (byte)(c >> 24);
                                byte c2r = (byte)(c >> 16);
                                byte c2g = (byte)(c >> 8);
                                byte c2b = (byte)(c);

                                c = srcpm[y1 * widthSource + x0];
                                byte c3a = (byte)(c >> 24);
                                byte c3r = (byte)(c >> 16);
                                byte c3g = (byte)(c >> 8);
                                byte c3b = (byte)(c);

                                c = srcpm[y1 * widthSource + x1];
                                byte c4a = (byte)(c >> 24);
                                byte c4r = (byte)(c >> 16);
                                byte c4g = (byte)(c >> 8);
                                byte c4b = (byte)(c);

                                // Calculate colors
                                // Alpha
                                float l0 = ifracx * c1a + fracx * c2a;
                                float l1 = ifracx * c3a + fracx * c4a;
                                byte a = (byte)(ifracy * l0 + fracy * l1);

                                // Red
                                l0 = ifracx * c1r * c1a + fracx * c2r * c2a;
                                l1 = ifracx * c3r * c3a + fracx * c4r * c4a;
                                float rf = ifracy * l0 + fracy * l1;

                                // Green
                                l0 = ifracx * c1g * c1a + fracx * c2g * c2a;
                                l1 = ifracx * c3g * c3a + fracx * c4g * c4a;
                                float gf = ifracy * l0 + fracy * l1;

                                // Blue
                                l0 = ifracx * c1b * c1a + fracx * c2b * c2a;
                                l1 = ifracx * c3b * c3a + fracx * c4b * c4a;
                                float bf = ifracy * l0 + fracy * l1;

                                // Divide by alpha
                                if (a > 0)
                                {
                                    rf = rf / a;
                                    gf = gf / a;
                                    bf = bf / a;
                                }

                                // Cast to byte
                                byte r = (byte)rf;
                                byte g = (byte)gf;
                                byte b = (byte)bf;

                                cs = (uint)((a << 24) | (r << 16) | (g << 8) | b);
                            }

                            // Write destination
                            int drs = y * destinationBitmap.PixelWidth;
                            int dro = x % destinationBitmap.PixelWidth;

                            int index = drs + dro;
                            if (index >= 0 && index < destpm.Length)
                            {
                                destpm[index] = (int)cs;
                            }
                        }
                    }
                }
            }

            return destinationBitmap;
        }

        public static void RotateFilter(WriteableBitmap bitmap, int angle)
        {
            if (angle % 360 != 0)
            {
                WriteableBitmap rbm = bitmap.Rotate(angle % 360);
                byte[] ba = rbm.ToByteArray();
                bitmap.FromByteArray(ba);
            }
        }

#if true
        public static void GrayScaleFilter(WriteableBitmap bitmap)
        {
            byte[] srcbytes = bitmap.ToByteArray();

            for (int i = 0; i < srcbytes.Length; i += 4)
            {
                byte blue = srcbytes[i];
                byte green = srcbytes[i + 1];
                byte red = srcbytes[i + 2];

                byte b = (byte)(.299 * red + .587 * green + .114 * blue);

                srcbytes[i] = b;
                srcbytes[i + 1] = b;
                srcbytes[i + 2] = b;
            }

            bitmap.FromByteArray(srcbytes);
        }
#else
        public static void GrayScaleFilter(WriteableBitmap bitmap)
        {
            using (BitmapContext bc = bitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                for (int i = 0; i < bc.Pixels.Length; i++)
                {
                    int pixel = bc.Pixels[i];

                    byte alpha = (byte)((pixel >> 24) & 0xff);
                    byte red = (byte)((pixel >> 16) & 0xff);
                    byte green = (byte)((pixel >> 8) & 0xff);
                    byte blue = (byte)(pixel & 0xff);

                    byte b = (byte)(.299 * red + .587 * green + .114 * blue);
                    bc.Pixels[i] = alpha << 24 | b << 16 | b << 8 | b;

                }
            }
        }
#endif

#if true
        public static void BrightnessFilter(WriteableBitmap bitmap, int brightness)
        {
            byte[] srcbytes = bitmap.ToByteArray();

            for (int i = 0; i < srcbytes.Length; i += 4)
            {
                byte blue = srcbytes[i];
                byte green = srcbytes[i + 1];
                byte red = srcbytes[i + 2];

                srcbytes[i] = (byte)Math.Max(0, Math.Min(255, (int)blue + brightness));
                srcbytes[i + 1] = (byte)Math.Max(0, Math.Min(255, (int)green + brightness));
                srcbytes[i + 2] = (byte)Math.Max(0, Math.Min(255, (int)red + brightness));
            }

            bitmap.FromByteArray(srcbytes);
        }
#else
        public static void BrightnessFilter(WriteableBitmap bitmap, int brightness)
        {
            using (BitmapContext bc = bitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                for (int i = 0; i < bc.Pixels.Length; i++)
                {
                    int pixel = bc.Pixels[i];

                    byte alpha = (byte)((pixel >> 24) & 0xff);
                    byte red = (byte)((pixel >> 16) & 0xff);
                    byte green = (byte)((pixel >> 8) & 0xff);
                    byte blue = (byte)(pixel & 0xff);

                    red = (byte)Math.Max(0, Math.Min(255, (int)red + brightness));
                    green = (byte)Math.Max(0, Math.Min(255, (int)green + brightness));
                    blue = (byte)Math.Max(0, Math.Min(255, (int)blue + brightness));

                    bc.Pixels[i] = alpha << 24 | red << 16 | green << 8 | blue;
                }
            }
        }
#endif

#if true
        public static void ContrastFilter(WriteableBitmap bitmap, double contrast)
        {
            contrast = (1.0 + contrast) / 1.0;

            byte[] srcbytes = bitmap.ToByteArray();

            for (int i = 0; i < srcbytes.Length; i += 4)
            {
                byte blue = srcbytes[i];
                byte green = srcbytes[i + 1];
                byte red = srcbytes[i + 2];

                double b = blue / 255.0;
                b -= 0.5;
                b *= contrast;
                b += 0.5;
                srcbytes[i] = (byte)Math.Max(0, Math.Min(255, (int)(b * 255)));

                double g = green / 255.0;
                g -= 0.5;
                g *= contrast;
                g += 0.5;
                srcbytes[i + 1] = (byte)Math.Max(0, Math.Min(255, (int)(g * 255)));

                double r = red / 255.0;
                r -= 0.5;
                r *= contrast;
                r += 0.5;
                srcbytes[i + 2] = (byte)Math.Max(0, Math.Min(255, (int)(r * 255)));
            }

            bitmap.FromByteArray(srcbytes);
        }
#else
        public static void ContrastFilter(WriteableBitmap bitmap, double contrast)
        {
            contrast = (1.0 + contrast) / 1.0;

            using (BitmapContext bc = bitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                for (int i = 0; i < bc.Pixels.Length; i++)
                {
                    int pixel = bc.Pixels[i];

                    byte alpha = (byte)((pixel >> 24) & 0xff);
                    byte red = (byte)((pixel >> 16) & 0xff);
                    byte green = (byte)((pixel >> 8) & 0xff);
                    byte blue = (byte)(pixel & 0xff);

                    double r = red / 255.0;
                    r -= 0.5;
                    r *= contrast;
                    r += 0.5;
                    red = (byte)Math.Max(0, Math.Min(255, (int)(r * 255)));

                    double g = green / 255.0;
                    g -= 0.5;
                    g *= contrast;
                    g += 0.5;
                    green = (byte)Math.Max(0, Math.Min(255, (int)(g * 255)));

                    double b = blue / 255.0;
                    b -= 0.5;
                    b *= contrast;
                    b += 0.5;
                    blue = (byte)Math.Max(0, Math.Min(255, (int)(b * 255)));

                    bc.Pixels[i] = alpha << 24 | red << 16 | green << 8 | blue;
                }
            }
        }
#endif

        public static void ConvolutionFilter(WriteableBitmap destinationBitmap, int[,] filterMatrix, int factor = 1, int bias = 0)
        {
            WriteableBitmap convolutedBitmap = destinationBitmap.Convolute(filterMatrix, factor, bias);
            byte[] bytes = convolutedBitmap.ToByteArray();

            for (int i = 3; i < bytes.Length; i += 4)
            {
                bytes[i] = 255;
            }
             
            destinationBitmap.FromByteArray(bytes);
        }

#if false
        public static void ConvolutionFilter(WriteableBitmap destinationBitmap, int[,] filterMatrix, double factor = 1, int bias = 0)
        {
            WriteableBitmap sourceBitmap = destinationBitmap.Clone();

            using (BitmapContext dbc = destinationBitmap.GetBitmapContext(ReadWriteMode.ReadWrite))
            {
                int[] destpm = dbc.Pixels;

                using (BitmapContext sbc = sourceBitmap.GetBitmapContext(ReadWriteMode.ReadOnly))
                {
                    int[] srcpm = sbc.Pixels;

                    int filterWidth = filterMatrix.GetLength(1);
                    int filterOffset = (filterWidth - 1) / 2;

                    for (int offsetY = filterOffset; offsetY < sourceBitmap.PixelHeight - filterOffset; offsetY++)
                    {
                        for (int offsetX = filterOffset; offsetX < sourceBitmap.PixelWidth - filterOffset; offsetX++)
                        {
                            double blue = 0;
                            double green = 0;
                            double red = 0;

                            int pixelOffset = offsetY * sourceBitmap.PixelWidth + offsetX;

                            for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                            {
                                for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                                {
                                    int offset = pixelOffset + filterX + (filterY * sourceBitmap.PixelWidth);
                                    int pixel = sbc.Pixels[offset];

                                    double rv = (double)((pixel >> 16) & 0xff);
                                    double gv = (double)((pixel >> 8) & 0xff);
                                    double bv = (double)(pixel & 0xff);

                                    red += rv * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                                    green += gv * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                                    blue += bv * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                                }
                            }

                            byte r = (byte)Math.Max(0, Math.Min(255, (int)(factor * red + bias)));
                            byte g = (byte)Math.Max(0, Math.Min(255, (int)(factor * green + bias)));
                            byte b = (byte)Math.Max(0, Math.Min(255, (int)(factor * blue + bias)));

                            dbc.Pixels[pixelOffset] = 0xff << 24 | r << 16 | g << 8 | b;
                        }
                    }
                }
            }

            destinationBitmap.Invalidate();
        }
#else
        public static void ConvolutionFilter(WriteableBitmap destinationBitmap, int[,] filterMatrix, double factor, int bias)
        {
            byte[] srcbytes = destinationBitmap.ToByteArray();
            byte[] dstbytes = new byte[srcbytes.Length];

            int filterWidth = filterMatrix.GetLength(1);
            int filterOffset = (filterWidth - 1) / 2;

            for (int offsetY = filterOffset; offsetY < destinationBitmap.PixelHeight - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < destinationBitmap.PixelWidth - filterOffset; offsetX++)
                {
                    double blue = 0;
                    double green = 0;
                    double red = 0;

                    int stride = destinationBitmap.PixelWidth * 4;
                    int byteOffset = offsetY * stride + offsetX * 4;

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {

                            int calcOffset = byteOffset + (filterX * 4) + (filterY * stride);

                            blue += (double)(srcbytes[calcOffset]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            green += (double)(srcbytes[calcOffset + 1]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                            red += (double)(srcbytes[calcOffset + 2]) * filterMatrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }

                    dstbytes[byteOffset] = (byte)Math.Max(0, Math.Min(255, (int)(factor * blue + bias)));
                    dstbytes[byteOffset + 1] = (byte)Math.Max(0, Math.Min(255, (int)(factor * green + bias)));
                    dstbytes[byteOffset + 2] = (byte)Math.Max(0, Math.Min(255, (int)(factor * red + bias)));
                    dstbytes[byteOffset + 3] = 255;
                }
            }

            destinationBitmap.FromByteArray(dstbytes);
            destinationBitmap.Invalidate();
        }
#endif

        public static void ConvolutionFilter(WriteableBitmap destinationBitmap, int[,] xFilterMatrix, int[,] yFilterMatrix, double factor, int bias)
        {
            byte[] srcbytes = destinationBitmap.ToByteArray();
            byte[] dstbytes = new byte[srcbytes.Length];

            int filterOffset = 1;

            for (int offsetY = filterOffset; offsetY < destinationBitmap.PixelHeight - filterOffset; offsetY++)
            {
                for (int offsetX = filterOffset; offsetX < destinationBitmap.PixelWidth - filterOffset; offsetX++)
                {
                    double blueX = 0.0;
                    double greenX = 0.0;
                    double redX = 0.0;

                    double blueY = 0.0;
                    double greenY = 0.0;
                    double redY = 0.0;

                    int stride = destinationBitmap.PixelWidth * 4;
                    int byteOffset = offsetY * stride + offsetX * 4;

                    for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                    {
                        for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                        {
                            int calcOffset = byteOffset + (filterX * 4) + (filterY * stride);

                            blueX += (double)(srcbytes[calcOffset]) * xFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                            greenX += (double)(srcbytes[calcOffset + 1]) * xFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                            redX += (double)(srcbytes[calcOffset + 2]) * xFilterMatrix[filterY + filterOffset, filterX + filterOffset];

                            blueY += (double)(srcbytes[calcOffset]) * yFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                            greenY += (double)(srcbytes[calcOffset + 1]) * yFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                            redY += (double)(srcbytes[calcOffset + 2]) * yFilterMatrix[filterY + filterOffset, filterX + filterOffset];
                        }
                    }

                    int blueTotal = (int)Math.Sqrt((blueX * blueX) + (blueY * blueY));
                    int greenTotal = (int)Math.Sqrt((greenX * greenX) + (greenY * greenY));
                    int redTotal = (int)Math.Sqrt((redX * redX) + (redY * redY));

                    dstbytes[byteOffset] = (byte)Math.Max(0, Math.Min(255, blueTotal));
                    dstbytes[byteOffset + 1] = (byte)Math.Max(0, Math.Min(255, greenTotal));
                    dstbytes[byteOffset + 2] = (byte)Math.Max(0, Math.Min(255, redTotal));
                    dstbytes[byteOffset + 3] = 255;
                }
            }

            destinationBitmap.FromByteArray(dstbytes);
            destinationBitmap.Invalidate();
        }
    }
}
