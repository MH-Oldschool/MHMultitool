using System;
using System.Collections.Generic;
using static APX_Viewer.FileBuffer;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace APX_Viewer
{
    class ImageCollection
    {
        public List<Bitmap> images;

        public void Parse()
        {

            images = new List<Bitmap>();

            List<uint> imgsstart = new List<uint>();
            List<uint> imgssize = new List<uint>();

            filePos = 0;

            uint imgcnt = readInt();
            //verify not ecd or whatever here
            if(imgcnt == 0x1A646365)
            {
                filePos -= 4;
                ecdDecrypt();
                imgcnt = readInt();
            }
            bool tim2 = false;
            for (int i = 0; i < imgcnt; i++)
            {
                imgsstart.Add(readInt());
                imgssize.Add(readInt());
            } //load header pointers
            for (int i = 0; i < imgcnt; i++)
            {
                filePos = (int)imgsstart[i];
                if (!filename.EndsWith("_tex.bin"))
                {
                    string ogfile = Path.GetFileNameWithoutExtension(filename);
                    string path = Directory.GetCurrentDirectory() + "/export/" + ogfile + "/";
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(path + i + ".png")))
                    {
                        for (int b = 0; b < imgssize[i]; b++)
                            bw.Write(readByte());
                    }
                    Bitmap png = (Bitmap)Image.FromFile(path + i + ".png");
                    images.Add(png);
                }
                else
                {
                    string test = readString(4);
                    if (test == "TIM2")
                        tim2 = true;
                    filePos -= 4;
                    if (tim2)
                    {
                        ImageTM2 img = new ImageTM2();
                        img.Parse();
                        images.Add(img.img);
                        /*imgdata dat = new imgdata();
                        filepos += 4;
                        readByte();
                        byte spacer = readByte();
                        dat.mipmaps = readByte();
                        readByte();
                        filepos += 0x8 + (spacer * 0x70);
                        readInt(); //size related to image + this subheader?
                        dat.start = (uint)(imgsdata[i].start + 0x20 + (spacer * 0xC0)); //0x20 to align with apx
                        dat.palSize = readInt(); //pallete size
                        dat.size = readInt(); //image data size
                        readShort();
                        dat.palOffset = dat.size;//how big this subheader is?
                        palCount = readShort(); //number of entries
                        if (palCount == 0x100)
                            dat.pixelBits = 8;
                        else if (palCount == 0x10)
                            dat.pixelBits = 4;
                        //dat.pixelBits = (ushort)(dat.palSize / palCount * 8);//this is currently bits per palette entry
                        readShort();
                        readByte();
                        readByte();
                        dat.width = readShort();//width?
                        dat.height = readShort();//height?
                        imgsdata[i] = dat;*/
                    }
                    else
                    {
                        ImageAPX img = new ImageAPX();
                        img.Parse();
                        images.Add(img.img);
                    }
                    /*imgsdata[i] = new imgdata
                    {
                        start = imgsdata[i].start,
                        size = readInt(),
                        palOffset = readInt(),
                        palSize = readInt(),
                        pixelBits = readShort(),
                        width = readShort(),
                        height = readShort(),
                        mipmaps = readShort()
                    };*/
                }
            }
            //then always 0x20 and 0x1, shorts?
            Debug.WriteLine("Images: " + imgcnt);
            /*Debug.WriteLine("Palette size: " + imgsdata[image].palSize);
            Debug.WriteLine("Pixel bits: " + imgsdata[image].pixelBits);
            Debug.WriteLine("Image dimensions: " + imgsdata[image].width + " X " + imgsdata[image].height);
            Debug.WriteLine("Number of mipmaps: " + imgsdata[image].mipmaps);
            //palettes
            filepos = (int)(0x20 + imgsdata[image].start + imgsdata[image].palOffset);
            int palEntries;
            //if (palCount == 0)
            palEntries = (int)Math.Pow(2, imgsdata[image].pixelBits);
            //else
            //    palEntries = palCount;
            List<Color> pal = new List<Color>();
            List<SolidBrush> palBrushes = new List<SolidBrush>();
            for (int c = 0; c < palEntries; c++)
            {
                if (imgsdata[image].palSize / palEntries == 2) //two bytes per entry
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
                else if (imgsdata[image].palSize / palEntries == 4) //four bytes per entry
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
            if (tim2)
            {
                //unfilter palettes
                List<Color> pal2 = new List<Color>();
                int parts = pal.Count / 32;//part
                int blocks = 2;//block
                int stripes = 2;//stripe
                int colours = 8;//swatches
                for (int part = 0; part < parts; part++)
                    for (int block = 0; block < blocks; block++)
                        for (int stripe = 0; stripe < stripes; stripe++)
                            for (int colour = 0; colour < colours; colour++)
                                pal2.Add(pal[part * colours * stripes * blocks + block * colours + stripe * stripes * colours + colour]);
                if (parts > 0)
                    pal = pal2;
            }
            for (int i = 0; i < pal.Count; i++)
                palBrushes.Add(new SolidBrush(pal[i]));
            //data
            ClientSize = new Size(imgsdata[image].width, imgsdata[image].height);
            buffer2 = new Bitmap(imgsdata[image].width, imgsdata[image].height);
            g = Graphics.FromImage(buffer2);
            g.Clear(Color.Transparent);
            filepos = (int)(0x20 + imgsdata[image].start);
            for (int y = 0; y < imgsdata[image].height; y++)
                for (int x = 0; x < imgsdata[image].width; x++)
                {
                    if (imgsdata[image].pixelBits == 4)
                    {
                        byte b = readByte();
                        g.FillRectangle(palBrushes[b >> 4], x + 1, y, 1, 1);
                        g.FillRectangle(palBrushes[b & 0x0F], x, y, 1, 1);
                        x++;
                    }
                    if (imgsdata[image].pixelBits == 8)
                    {
                        g.FillRectangle(palBrushes[readByte()], x, y, 1, 1);
                    }
                    if (imgsdata[image].pixelBits == 0)
                    {
                        byte rb = readByte();
                        byte gb = readByte();
                        byte bb = readByte();
                        byte ab = readByte();
                        g.FillRectangle(new SolidBrush(Color.FromArgb(ab, rb, gb, bb)), x, y, 1, 1);
                    }
                }
            BackgroundImage = buffer2;
            //g.Flush();
            Invalidate();
            Update();
            */
        }

    }
}
