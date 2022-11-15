using System;
using System.Collections.Generic;
using System.IO;
using static APX_Viewer.FileBuffer;
using System.Text;
using System.Diagnostics;

namespace APX_Viewer
{
    class ArchivePAC
    {
        public void Unpack() //wip
        {
            string ogfile = Path.GetFileNameWithoutExtension(filename);
            string path = Directory.GetCurrentDirectory() + "/export/frontier/" + ogfile + "/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            List<uint> mainpos = new List<uint>();
            List<uint> mainsize = new List<uint>();
            List<uint> subpos;
            List<uint> subsize;

            uint num = readInt();
            for (int i = 0; i < num; i++)
            {
                mainpos.Add(readInt());
                mainsize.Add(readInt());
            }
            for (int i = 0; i < num; i++)
            {
                setBuffer(0);//always reset to main, since jpk sets to 1 at the end
                filePos = (int)mainpos[i];
                if (mainsize[i] == 0) //skip blank effects
                    continue;
                subpos = new List<uint>();
                subsize = new List<uint>();
                if (ogfile.StartsWith("em"))
                {
                    //monster file
                    //file 0 is 2size JKR; monster AMO and AHI
                    //file 1 is png bundle; monster textures
                    //file 2 is single JKR type 4; monster tbl.bin
                    //file 3 is 2size jkr type 4; monster effect AMO and AHI
                    //file 4 is png bundle; monster effect textures
                    //file 5 is single jkr? type 3, contains 3 files... first is only 0x0C long?? not sure what the others relate to

                    if (i == 1 || i == 4) //file 1 is bundled, uncompressed png images?
                    {
                        uint count = readInt();
                        for (int f = 0; f < count; f++)
                        {
                            subpos.Add(readInt());
                            subsize.Add(readInt());
                        }
                        for (int f = 0; f < count; f++)
                        {
                            using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_" + f + ".png")))
                            {
                                filePos = (int)(subpos[f] + mainpos[i]);
                                for (int b = 0; b < subsize[f]; b++)
                                    bw.Write(readByte());
                            }
                        }
                    }
                    else if (i == 2 || i == 5)
                    {
                        jpkUncompress();
                        //write the file out
                        using (BinaryWriter br = new BinaryWriter(File.Create(path + ogfile + "_" + i + ".bin")))
                            for (int b = 0; b < bufSize; b++)
                                br.Write(readByte());
                    }
                    else if (i == 0 || i == 3)
                    {
                        uint count = readInt();
                        for (int f = 0; f < count; f++)
                        {
                            subpos.Add(readInt());
                            subsize.Add(readInt());
                        }
                        for (int f = 0; f < count; f++)
                        {
                            setBuffer(0);//always reset to main, since jpk sets to 1 at the end
                            filePos = (int)(subpos[f] + mainpos[i]);
                            jpkUncompress();
                            //write the file out
                            using (BinaryWriter br = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_" + f + (f == 0 ? ".amo" : ".ahi"))))
                                for (int b = 0; b < bufSize; b++)
                                    br.Write(readByte());
                        }
                    }
                    else
                    {

                        using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + ".bin")))
                        {
                            for (int b = 0; b < mainsize[i]; b++)
                                bw.Write(readByte());
                        }
                    }
                }
                else if (ogfile.StartsWith("som"))
                {
                    //not sure, doesn't seem to parse right currently
                    //amo/ahi, png bundle, collisionmap
                    if (i < 2)
                        using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + (i == 0 ? ".amo" : ".ahi"))))
                        {
                            filePos = (int)(mainpos[i]);
                            jpkUncompress();
                            for (int b = 0; b < mainsize[i]; b++)
                                bw.Write(readByte());
                            setBuffer(0);
                        }
                    else if (i == 2)
                    {
                        filePos = (int)(mainpos[i]);
                        List<uint> nestpos = new List<uint>();
                        List<uint> nestsize = new List<uint>();
                        uint nestcount = readInt();
                        for (int n = 0; n < nestcount; n++)
                        {
                            nestpos.Add(readInt());
                            nestsize.Add(readInt());
                        }
                        for (int n = 0; n < nestcount; n++)
                        {
                            using (BinaryWriter bw = new BinaryWriter(File.Create(path + i + "_" + n + ".png")))
                            {
                                filePos = (int)(nestpos[n] + mainpos[i]);
                                for (int b = 0; b < nestsize[n]; b++)
                                    bw.Write(readByte());
                            }
                        }
                    }
                    else if (i == 3)
                    {
                        using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + ".bin")))
                        {
                            filePos = (int)(mainpos[i]);
                            jpkUncompress();
                            for (int b = 0; b < mainsize[i]; b++)
                                bw.Write(readByte());
                            setBuffer(0);
                        }
                    }
                    else
                    {
                        using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + ".tmp")))
                        {
                            filePos = (int)(mainpos[i]);
                            for (int b = 0; b < mainsize[i]; b++)
                                bw.Write(readByte());
                        }
                    }
                }
                else if (ogfile.StartsWith("sef"))
                {
                    //stage effects? special effects?
                    //contains packs of two JKRs (amo/ahi?) and a PNG bundle
                    uint count = readInt();
                    for (int f = 0; f < count; f++)
                    {
                        subpos.Add(readInt());
                        subsize.Add(readInt());
                    }
                    for (int f = 0; f < count; f++)
                    {
                        if (f < 2)
                            using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_" + f + (f == 0 ? ".amo" : ".ahi"))))
                            {
                                filePos = (int)(subpos[f] + mainpos[i]);
                                jpkUncompress();
                                for (int b = 0; b < subsize[f]; b++)
                                    bw.Write(readByte());
                                setBuffer(0);
                            }
                        else if (f == 2)
                        {
                            filePos = (int)(subpos[f] + mainpos[i]);
                            List<uint> nestpos = new List<uint>();
                            List<uint> nestsize = new List<uint>();
                            uint nestcount = readInt();
                            for (int n = 0; n < nestcount; n++)
                            {
                                nestpos.Add(readInt());
                                nestsize.Add(readInt());
                            }
                            for (int n = 0; n < nestcount; n++)
                            {
                                using (BinaryWriter bw = new BinaryWriter(File.Create(path + i + "_" + n + ".png")))
                                {
                                    filePos = (int)(nestpos[n] + subpos[f] + mainpos[i]);
                                    for (int b = 0; b < nestsize[n]; b++)
                                        bw.Write(readByte());
                                }
                            }
                        }
                        else
                        {
                            using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_" + f + ".bin")))
                            {
                                filePos = (int)(subpos[f] + mainpos[i]);
                                for (int b = 0; b < subsize[f]; b++)
                                    bw.Write(readByte());
                            }
                        }
                    }
                }
                else if (ogfile.StartsWith("nso"))
                {
                    //5 file entries?
                    //file 0 is raw data
                    //file 1 is a jkr - amo
                    //file 2 is a jkr - ahi
                    //file 3 is a png bundle
                    //file 4 is raw? unknown
                    if (i == 1 || i == 2)
                    {
                        jpkUncompress();
                        //write the file out
                        using (BinaryWriter br = new BinaryWriter(File.Create(path + ogfile + "_" + i + (i == 1 ? ".amo" : ".ahi"))))
                            for (int b = 0; b < bufSize; b++)
                                br.Write(readByte());
                    }
                    else if (i == 3)
                    {
                        uint count = readInt();
                        for (int f = 0; f < count; f++)
                        {
                            subpos.Add(readInt());
                            subsize.Add(readInt());
                        }
                        for (int f = 0; f < count; f++)
                        {
                            using (BinaryWriter bw = new BinaryWriter(File.Create(path + f + ".png")))
                            {
                                filePos = (int)(subpos[f] + mainpos[i]);
                                for (int b = 0; b < subsize[f]; b++)
                                    bw.Write(readByte());
                            }
                        }
                    }
                    else
                        using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + ".bin")))
                            for (int b = 0; b < mainsize[i]; b++)
                                bw.Write(readByte());
                }
                else if (ogfile.StartsWith("st"))
                {
                    //stage file, 0x20 file entries
                    //file 0 is two JKRs and an uncompressed file - amo/ahi and ??? sshort num of entries, 0x54 sized entry?
                    //file 2 is two JKRs - LW and LG?
                    //file 3 is bundled PNGs - original main textures?
                    //file 5 is bundled PNGs - effects?
                    //file 8 is two JKRs - amo/ahi
                    //11-28 are three sets of six, composed of three PNG bundles and three JKR'd amo/ahi bundlepairs.
                    //these handle jumbo village's upgradeable sections... for some reason

                    //file 29 is a loose JKR - CMD file
                    //file 30 is a loose JKR - text table
                    //file 31 is a funny container:
                    // 0x00 is pointer to section, 0x04 is its size
                    // 0x10 is a pointer, 0x14 is its size?
                    // 0x18 is the number of entries in the header
                    // from here to the end of the header, entries consist of ID, pointer, size
                    // each entry is a bundle, with the usual order being:
                    //  raw values ???
                    //  jkr - amo
                    //  jkr - ahi
                    //  bundled pngs
                    //  raw file?
                    //  raw? starts with 80000001
                    //  raw? starts with 80000002
                    //  raw? starts with 80000002
                    //  JKR - bundle of files
                    // first bundle is the main stage file
                    // second bundle is the decorations/foliage file

                    if (mainsize[i] != 0) //filter out empty entries
                    {
                        if (i == 0)
                        {
                            uint count = readInt();
                            for (int f = 0; f < count; f++)
                            {
                                subpos.Add(readInt());
                                subsize.Add(readInt());
                            }
                            for (int f = 0; f < 2; f++)
                            {
                                setBuffer(0);//always reset to main, since jpk sets to 1 at the end
                                filePos = (int)(subpos[f] + mainpos[i]);
                                jpkUncompress();
                                //write the file out
                                using (BinaryWriter br = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_" + f + (f == 0?".amo":".ahi"))))
                                    for (int b = 0; b < bufSize; b++)
                                        br.Write(readByte());
                            }
                            setBuffer(0);
                            filePos = (int)(subpos[2] + mainpos[i]);
                            using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_2" + ".bin")))
                                for (int b = 0; b < subsize[2]; b++)
                                    bw.Write(readByte());
                        }
                        else if (i == 2 || i == 8)
                        {
                            uint count = readInt();
                            for (int f = 0; f < count; f++)
                            {
                                subpos.Add(readInt());
                                subsize.Add(readInt());
                            }
                            for (int f = 0; f < 2; f++)
                            {
                                setBuffer(0);//always reset to main, since jpk sets to 1 at the end
                                filePos = (int)(subpos[f] + mainpos[i]);
                                jpkUncompress();
                                //write the file out
                                using (BinaryWriter br = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_" + f + (i==2?".bin":(f == 0 ? ".amo" : ".ahi")))))
                                    for (int b = 0; b < bufSize; b++)
                                        br.Write(readByte());
                            }
                        }
                        else if (i == 3 || i == 5) //file 1 is bundled, uncompressed png images
                        {
                            uint count = readInt();
                            for (int f = 0; f < count; f++)
                            {
                                subpos.Add(readInt());
                                subsize.Add(readInt());
                            }
                            for (int f = 0; f < count; f++)
                            {
                                using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + "_" + f + ".png")))
                                {
                                    filePos = (int)(subpos[f] + mainpos[i]);
                                    for (int b = 0; b < subsize[f]; b++)
                                        bw.Write(readByte());
                                }
                            }
                        }
                        else if (i == 29 || i == 30)
                        {
                            filePos = (int)mainpos[i];
                            jpkUncompress();
                            //write the file out
                            using (BinaryWriter br = new BinaryWriter(File.Create(path + ogfile + "_" + i + ".bin")))
                                for (int b = 0; b < bufSize; b++)
                                    br.Write(readByte());
                        }
                        else if (i == 31)
                        {
                            if (!Directory.Exists(path + "31/"))
                                Directory.CreateDirectory(path + "31/");
                            subpos.Add(readInt()); //this holds all the files inside _31
                            subsize.Add(readInt());
                            subpos.Add(readInt());
                            subsize.Add(readInt());
                            subpos.Add(readInt());
                            subsize.Add(readInt());
                            uint count = readInt();
                            for (int f = 0; f < count; f++)
                            {
                                readInt(); //ID?
                                subpos.Add(readInt());
                                subsize.Add(readInt());
                            }
                            for (int f = 0; f < count + 3; f++) //for each of the files in here:
                            {
                                if (subsize[f] != 0)
                                {
                                    filePos = (int)(subpos[f] + mainpos[i]);
                                    if (f < 3)
                                    {
                                        using (BinaryWriter bw = new BinaryWriter(File.Create(path + "31/" + f + ".tmp")))
                                            for (int b = 0; b < subsize[f]; b++)
                                                bw.Write(readByte());
                                    }
                                    else
                                    {
                                        if (!Directory.Exists(path + "31/" + f + "/"))
                                            Directory.CreateDirectory(path + "31/" + f + "/");
                                        List<uint> nestpos = new List<uint>(); //all the files inside _31_f
                                        List<uint> nestsize = new List<uint>();
                                        uint nestcount = readInt();
                                        for (int n = 0; n < nestcount; n++)
                                        {
                                            nestpos.Add(readInt());
                                            nestsize.Add(readInt());
                                        }
                                        for (int n = 0; n < nestcount; n++)
                                        {
                                            if (n == 1 || n == 2)
                                            {
                                                //jkr
                                                setBuffer(0);//always reset to main, since jpk sets to 1 at the end
                                                filePos = (int)(nestpos[n] + subpos[f] + mainpos[i]);
                                                uint magic = readInt(); //magic
                                                filePos -= 4;
                                                if (magic != 0x1A524B4A)
                                                {
                                                    Debug.WriteLine("oops! not compressed!");

                                                }
                                                else
                                                    jpkUncompress();
                                                //write the file out
                                                using (BinaryWriter br = new BinaryWriter(File.Create(path + "31/" + f + "/" + ogfile + (n == 1?".amo":".ahi"))))
                                                    for (int b = 0; b < bufSize; b++)
                                                        br.Write(readByte());
                                                setBuffer(0);
                                            }
                                            else if(n == 8)
                                            {
                                                //jkr
                                                setBuffer(0);//always reset to main, since jpk sets to 1 at the end
                                                filePos = (int)(nestpos[n] + subpos[f] + mainpos[i]);
                                                jpkUncompress();
                                                //write the file out
                                                using (BinaryWriter br = new BinaryWriter(File.Create(path + "31/" + f + "/" + n + ".bin")))
                                                    for (int b = 0; b < bufSize; b++)
                                                        br.Write(readByte());
                                                setBuffer(0);
                                            }
                                            else if (n == 3)
                                            {
                                                filePos = (int)(nestpos[n] + subpos[f] + mainpos[i]);
                                                List<uint> nestsubpos = new List<uint>();
                                                List<uint> nestsubsize = new List<uint>();
                                                uint nestsubcount = readInt();
                                                if (nestsubcount > 0x1000)
                                                {
                                                    filePos -= 4;
                                                    using (BinaryWriter bw = new BinaryWriter(File.Create(path + "31/" + f + "/" + n + ".tmp")))
                                                    {
                                                        for (int b = 0; b < nestsize[n]; b++)
                                                            bw.Write(readByte());
                                                    }
                                                }
                                                else
                                                {
                                                    for (int s = 0; s < nestsubcount; s++)
                                                    {
                                                        nestsubpos.Add(readInt());
                                                        nestsubsize.Add(readInt());
                                                    }
                                                    for (int s = 0; s < nestsubcount; s++)
                                                    {
                                                        using (BinaryWriter bw = new BinaryWriter(File.Create(path + "31/" + f + "/" + s + ".png")))
                                                        {
                                                            filePos = (int)(nestsubpos[s] + nestpos[n] + subpos[f] + mainpos[i]);
                                                            for (int b = 0; b < nestsubsize[s]; b++)
                                                                bw.Write(readByte());
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                                using (BinaryWriter bw = new BinaryWriter(File.Create(path + "31/" + f + "/" + n + ".tmp")))
                                                {
                                                    filePos = (int)(nestpos[n] + subpos[f] + mainpos[i]);
                                                    for (int b = 0; b < nestsize[n]; b++)
                                                        bw.Write(readByte());
                                                }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            filePos = (int)mainpos[i];
                            using (BinaryWriter bw = new BinaryWriter(File.Create(path + ogfile + "_" + i + ".bin")))
                                for (int b = 0; b < mainsize[i]; b++)
                                    bw.Write(readByte());
                        }
                    }

                }
            }
        }

    }
}
