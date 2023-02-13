using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using VenomRAT_HVNC.StreamLibrary.src;

namespace VenomRAT_HVNC.StreamLibrary.UnsafeCodecs
{
    public class UnsafeStreamCodec : IUnsafeCodec
    {
        public override ulong CachedSize { get; internal set; }

        public override int BufferCount
        {
            get
            {
                return 1;
            }
        }

        public override CodecOption CodecOptions
        {
            get
            {
                return CodecOption.RequireSameSize;
            }
        }

        public Size CheckBlock { get; private set; }

        public override event IVideoCodec.VideoDebugScanningDelegate onCodeDebugScan;

        public override event IVideoCodec.VideoDebugScanningDelegate onDecodeDebugScan;

        public UnsafeStreamCodec(int ImageQuality = 100, bool UseJPEG = true)
            : base(ImageQuality)
        {
            this.CheckBlock = new Size(50, 1);
            this.UseJPEG = UseJPEG;
        }

        public override unsafe void CodeImage(IntPtr Scan0, Rectangle ScanArea, Size ImageSize, PixelFormat Format, Stream outStream)
        {
            object imageProcessLock = base.ImageProcessLock;
            lock (imageProcessLock)
            {
                int ptr = Scan0.ToInt32();
                if (!outStream.CanWrite)
                {
                    throw new Exception("Must have access to Write in the Stream");
                }
                int num = 0;
                int num2;
                if (Format <= PixelFormat.Format32bppRgb)
                {
                    if (Format == PixelFormat.Format24bppRgb || Format == PixelFormat.Format32bppRgb)
                    {
                        num2 = 3;
                        goto IL_84;
                    }
                }
                else if (Format == PixelFormat.Format32bppPArgb || Format == PixelFormat.Format32bppArgb)
                {
                    num2 = 4;
                    goto IL_84;
                }
                throw new NotSupportedException(Format.ToString());
            IL_84:
                int num3 = ImageSize.Width * num2;
                num = num3 * ImageSize.Height;
                if (this.EncodeBuffer == null)
                {
                    this.EncodedFormat = Format;
                    this.EncodedWidth = ImageSize.Width;
                    this.EncodedHeight = ImageSize.Height;
                    this.EncodeBuffer = new byte[num];
                    byte[] array;
                    byte* value;
                    if ((array = this.EncodeBuffer) == null || array.Length == 0)
                    {
                        value = null;
                    }
                    else
                    {
                        value = (byte*)array[0];
                    }
                    byte[] array2 = null;
                    using (Bitmap bitmap = new Bitmap(ImageSize.Width, ImageSize.Height, num3, Format, Scan0))
                    {
                        array2 = this.jpgCompression.Compress(bitmap);
                    }
                    outStream.Write(BitConverter.GetBytes(array2.Length), 0, 4);
                    outStream.Write(array2, 0, array2.Length);
                    NativeMethods.memcpy(new IntPtr((void*)value), Scan0, (uint)num);
                    array = null;
                }
                else
                {
                    long position = outStream.Position;
                    outStream.Write(new byte[4], 0, 4);
                    int num4 = 0;
                    if (this.EncodedFormat != Format)
                    {
                        throw new Exception("PixelFormat is not equal to previous Bitmap");
                    }
                    if (this.EncodedWidth != ImageSize.Width || this.EncodedHeight != ImageSize.Height)
                    {
                        throw new Exception("Bitmap width/height are not equal to previous bitmap");
                    }
                    List<Rectangle> list = new List<Rectangle>();
                    Size size = new Size(ScanArea.Width, this.CheckBlock.Height);
                    Size size2 = new Size(ScanArea.Width % this.CheckBlock.Width, ScanArea.Height % this.CheckBlock.Height);
                    int num5 = ScanArea.Height - size2.Height;
                    int num6 = ScanArea.Width - size2.Width;
                    Rectangle rectangle = default(Rectangle);
                    List<Rectangle> list2 = new List<Rectangle>();
                    size = new Size(ScanArea.Width, size.Height);
                    byte[] array;
                    byte* ptr2;
                    if ((array = this.EncodeBuffer) == null || array.Length == 0)
                    {
                        ptr2 = null;
                    }
                    else
                    {
                        ptr2 = (byte*)array[0];
                    }
                    for (int num7 = ScanArea.Y; num7 != ScanArea.Height; num7 += size.Height)
                    {
                        if (num7 == num5)
                        {
                            size = new Size(ScanArea.Width, size2.Height);
                        }
                        rectangle = new Rectangle(ScanArea.X, num7, ScanArea.Width, size.Height);
                        if (this.onCodeDebugScan != null)
                        {
                            this.onCodeDebugScan(rectangle);
                        }
                        int num8 = num7 * num3 + ScanArea.X * num2;
                        {
                            int index = list.Count - 1;
                            if (list.Count != 0 && list[index].Y + list[index].Height == rectangle.Y)
                            {
                                rectangle = new Rectangle(list[index].X, list[index].Y, list[index].Width, list[index].Height + rectangle.Height);
                                list[index] = rectangle;
                            }
                            else
                            {
                                list.Add(rectangle);
                            }
                        }
                    }
                    int i = 0;
                    int num9 = ScanArea.X;
                    while (i < list.Count)
                    {
                        size = new Size(this.CheckBlock.Width, list[i].Height);
                        for (num9 = ScanArea.X; num9 != ScanArea.Width; num9 += size.Width)
                        {
                            if (num9 == num6)
                            {
                                size = new Size(size2.Width, list[i].Height);
                            }
                            rectangle = new Rectangle(num9, list[i].Y, size.Width, list[i].Height);
                            bool flag2 = false;
                            int count = num2 * rectangle.Width;
                            for (int j = 0; j < rectangle.Height; j++)
                            {
                                int num10 = num3 * (rectangle.Y + j) + num2 * rectangle.X;
                                {
                                    flag2 = true;
                                }
                                NativeMethods.memcpy((void*)(ptr2 + num10), (void*)(ptr + num10), (uint)count);
                            }
                            if (this.onCodeDebugScan != null)
                            {
                                this.onCodeDebugScan(rectangle);
                            }
                            if (flag2)
                            {
                                int index = list2.Count - 1;
                                if (list2.Count > 0 && list2[index].X + list2[index].Width == rectangle.X)
                                {
                                    Rectangle rectangle2 = list2[index];
                                    int width = rectangle.Width + rectangle2.Width;
                                    rectangle = new Rectangle(rectangle2.X, rectangle2.Y, width, rectangle2.Height);
                                    list2[index] = rectangle;
                                }
                                else
                                {
                                    list2.Add(rectangle);
                                }
                            }
                        }
                        i++;
                    }
                    array = null;
                    for (int k = 0; k < list2.Count; k++)
                    {
                        Rectangle rectangle3 = list2[k];
                        int num11 = num2 * rectangle3.Width;
                        Bitmap bitmap2 = new Bitmap(rectangle3.Width, rectangle3.Height, Format);
                        BitmapData bitmapData = bitmap2.LockBits(new Rectangle(0, 0, bitmap2.Width, bitmap2.Height), ImageLockMode.ReadWrite, bitmap2.PixelFormat);
                        int l = 0;
                        int num12 = 0;
                        while (l < rectangle3.Height)
                        {
                            int num13 = num3 * (rectangle3.Y + l) + num2 * rectangle3.X;
                            NativeMethods.memcpy((void*)((byte*)bitmapData.Scan0.ToPointer() + num12), (void*)(ptr + num13), (uint)num11);
                            num12 += num11;
                            l++;
                        }
                        bitmap2.UnlockBits(bitmapData);
                        outStream.Write(BitConverter.GetBytes(rectangle3.X), 0, 4);
                        outStream.Write(BitConverter.GetBytes(rectangle3.Y), 0, 4);
                        outStream.Write(BitConverter.GetBytes(rectangle3.Width), 0, 4);
                        outStream.Write(BitConverter.GetBytes(rectangle3.Height), 0, 4);
                        outStream.Write(new byte[4], 0, 4);
                        long num14 = outStream.Length;
                        long position2 = outStream.Position;
                        if (this.UseJPEG)
                        {
                            this.jpgCompression.Compress(bitmap2, ref outStream);
                        }
                        else
                        {
                            this.lzwCompression.Compress(bitmap2, outStream, null);
                        }
                        num14 = outStream.Position - num14;
                        outStream.Position = position2 - 4L;
                        outStream.Write(BitConverter.GetBytes((int)num14), 0, 4);
                        outStream.Position += num14;
                        bitmap2.Dispose();
                        num4 += (int)num14 + 20;
                    }
                    outStream.Position = position;
                    outStream.Write(BitConverter.GetBytes(num4), 0, 4);
                    list.Clear();
                    list2.Clear();
                }
            }
        }

        public override unsafe Bitmap DecodeData(IntPtr CodecBuffer, uint Length)
        {
            if (Length < 4U)
            {
                return this.decodedBitmap;
            }
            int num = *(int*)(void*)CodecBuffer;
            if (this.decodedBitmap == null)
            {
                byte[] array = new byte[num];
                byte[] array2;
                byte* value;
                if ((array2 = array) == null || array2.Length == 0)
                {
                    value = null;
                }
                else
                {
                    value = (byte*)array2[0];
                }
                NativeMethods.memcpy(new IntPtr((void*)value), new IntPtr(CodecBuffer.ToInt32() + 4), (uint)num);
                array2 = null;
                this.decodedBitmap = (Bitmap)Image.FromStream(new MemoryStream(array));
                return this.decodedBitmap;
            }
            return this.decodedBitmap;
        }

        public override Bitmap DecodeData(Stream inStream)
        {
            Bitmap result;
            try
            {
                byte[] array = new byte[4];
                inStream.Read(array, 0, 4);
                int i = BitConverter.ToInt32(array, 0);
                if (this.decodedBitmap == null)
                {
                    array = new byte[i];
                    inStream.Read(array, 0, array.Length);
                    this.decodedBitmap = (Bitmap)Image.FromStream(new MemoryStream(array));
                    result = this.decodedBitmap;
                }
                else
                {
                    using (Graphics graphics = Graphics.FromImage(this.decodedBitmap))
                    {
                        while (i > 0)
                        {
                            byte[] array2 = new byte[20];
                            inStream.Read(array2, 0, array2.Length);
                            Rectangle scanArea = new Rectangle(BitConverter.ToInt32(array2, 0), BitConverter.ToInt32(array2, 4), BitConverter.ToInt32(array2, 8), BitConverter.ToInt32(array2, 12));
                            int num = BitConverter.ToInt32(array2, 16);
                            byte[] array3 = new byte[num];
                            inStream.Read(array3, 0, array3.Length);
                            if (this.onDecodeDebugScan != null)
                            {
                                this.onDecodeDebugScan(scanArea);
                            }
                            using (MemoryStream memoryStream = new MemoryStream(array3))
                            {
                                using (Bitmap bitmap = (Bitmap)Image.FromStream(memoryStream))
                                {
                                    graphics.DrawImage(bitmap, scanArea.Location);
                                }
                            }
                            i -= num + 20;
                        }
                    }
                    result = this.decodedBitmap;
                }
            }
            catch
            {
                result = null;
            }
            return result;
        }

        private byte[] EncodeBuffer;

        private Bitmap decodedBitmap;

        private PixelFormat EncodedFormat;

        private int EncodedWidth;

        private int EncodedHeight;

        private bool UseJPEG;
    }
}
