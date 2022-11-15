using System;
using System.Collections.Generic;
using static APX_Viewer.FileBuffer;
using System.Text;
using System.Diagnostics;
using System.Drawing;

namespace APX_Viewer
{
    class ImageAPX
    {
        public Bitmap img;

        public void Parse()
        {
            ushort pixelBits;
            ushort width;
            ushort height;
            ushort mipmaps;
            ushort palBits;
            ushort palCount;
            int filestart = filePos;
            filePos += 0xC;
            pixelBits = readShort();
            width = readShort();
            height = readShort();
            mipmaps = readShort();
            palBits = readShort();
            palCount = readShort();
            Debug.WriteLine("Palette bits: " + palBits);
            Debug.WriteLine("Pixel bits: " + pixelBits);
            Debug.WriteLine("Image dimensions: " + width + " X " + height);
            Debug.WriteLine("Number of mipmaps: " + mipmaps);

            int palEntries = (int)Math.Pow(2, pixelBits);

            List<Color> pal = new List<Color>();
            List<SolidBrush> palBrushes = new List<SolidBrush>();
            filePos += (int)(0x8 + width * height * pixelBits / 8);
            for (int c = 0; c < palEntries; c++)
            {
                if (palBits == 4)
                {
                    byte b1 = readByte();
                    byte b2 = readByte();
                    pal.Add(Color.FromArgb(b1 & 0xF0, (b1 << 4) & 0xF0, b2 & 0xF0, (b2 << 4) & 0xF0));
                }
                if (palBits == 16) //two bytes per entry
                {
                    //1555 format
                    ushort s = readShort();
                    byte a = (byte)(((s & 0x8000) == 0x8000) ? 0xFF : 0x00);
                    //byte b = (byte)((s >> 7) | (s >> 12)); //too intense!
                    //byte g = (byte)((s >> 2) | (s >> 7));
                    //byte r = (byte)((s << 3) | (s >> 2));
                    byte b = (byte)((s >> 7) & 0xF1);
                    byte g = (byte)((s >> 2) & 0xF1);
                    byte r = (byte)((s << 3) & 0xF1);
                    //pal.Add(Color.FromArgb(b2 & 0xF0, (b1 << 4) & 0xF0, b1 & 0xF0, (b2 << 4) & 0xF0));
                    pal.Add(Color.FromArgb(a, r, g, b));
                    //Debug.WriteLine("entry " + c + ": " + b1.ToString("X") + ", " + b2.ToString("X"));
                }
                if (palBits == 32)
                {
                    if (wii) //wii order
                    {

                        byte a = readByte();
                        byte b = readByte();
                        byte g = readByte();
                        byte r = readByte();
                        pal.Add(Color.FromArgb(a, r, g, b));
                    }

                    else //ps2 order
                    {
                        byte r = readByte();
                        byte g = readByte();
                        byte b = readByte();
                        byte a = readByte();
                        pal.Add(Color.FromArgb(a, r, g, b));
                    }
                }
                //g.FillRectangle(new SolidBrush(pal[c]), 0+c*20, 0, 20, 50);
            }
            for(int i = 0; i < pal.Count; i++)
                palBrushes.Add(new SolidBrush(pal[i]));
            //palette loaded, now draw the map!
            img = new Bitmap(width, height);
            Graphics gfx = Graphics.FromImage(img);
            gfx.Clear(Color.Transparent);
            filePos = filestart + 0x20;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (pixelBits == 4)
                    {
                        byte b = readByte();
                        gfx.FillRectangle(palBrushes[b >> 4], x + 1, y, 1, 1);
                        gfx.FillRectangle(palBrushes[b & 0x0F], x, y, 1, 1);
                        x++;
                    }
                    if (pixelBits == 8)
                    {
                        gfx.FillRectangle(palBrushes[readByte()], x, y, 1, 1);
                    }
                }
        }

    }
}
