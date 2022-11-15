using System;
using System.Collections.Generic;
using static APX_Viewer.FileBuffer;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace APX_Viewer
{
    class ArchiveFPK
    {
        public struct fpkFile
        {
            public string name;
            public uint pointer;
            public uint size;
            public uint decompressed;
        }

        public void Unpack()
        {

            //first int can determine endianness - if first short is blank, little endian! but we know this is... little endian?
            readInt();
            uint filequantity = readInt(); //number of files
            readInt(); //small number
            readInt(); //this fpk file's size
            Debug.WriteLine("Files count: " + filequantity);

            List<fpkFile> files = new List<fpkFile>();
            //then for each file, parse
            for (int f = 0; f < filequantity; f++)
            {
                fpkFile file = new fpkFile();
                for (int l = 0; l < 0x20; l++)
                {
                    byte letter = readByte();
                    if (letter != 0)
                        file.name += (char)letter;//name, 0x20
                }
                readInt();//unknown int
                file.pointer = readInt();//offset int
                file.size = readInt();//length int
                file.decompressed = readInt();//decompressed size int
                files.Add(file);
            }
            Debug.WriteLine("datas parsed");
            for (int f = 0; f < files.Count; f++)
            {
                //decompress!
                filePos = (int)files[f].pointer;
                List<byte> dest = new List<byte>();
                bitpos = 0;
                bool next = false;
                //if size and decompressed equal, skip decompression? i'm guessing this is what made quickBMS's fpk script choke
                if (files[f].size == files[f].decompressed)
                {
                    for (int i = 0; i < files[f].decompressed; i++)
                        dest.Add(readByte());
                }
                else
                    while (filePos < files[f].pointer + files[f].size)
                    {
                        next = false;
                        //get a bit, if one, copy a byte
                        if (readPackedBits(1) == 1)
                        {
                            dest.Add(readByte());
                        }
                        //else, decompress:
                        else
                        {
                            uint length;
                            uint offset;
                            //get a bit
                            if (readPackedBits(1) == 0)
                            {
                                //if zero, length is 2 bits + 2, position is byte-sized read
                                length = readPackedBits(2) + 2;
                                offset = 0xFFFFFF00 | readByte();
                            }
                            else
                            {
                                //if one, read word: length is bottom three bits, position is the rest shifted down, negative!
                                offset = 0xFFFF0000 | readShort();
                                length = offset & 0x07; //bottom three bits are length
                                offset = (uint)((int)offset >> 3); //sign the offset so we get arithmetic shift
                                                                   //if length zero, length next byte plus one; otherwise length += 2.
                                if (length == 0)
                                    length = (uint)(readByte() + 1);
                                else
                                    length += 2;
                            }
                            //then copy bytes from decompressed buffer using position as a negative offset
                            for (int i = 0; i < length; i++)
                            {
                                if (dest.Count == files[f].decompressed)
                                    break;
                                try
                                {
                                    dest.Add(dest[dest.Count + (int)offset]);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    next = true;
                                    break;
                                }
                            }
                            if (next)
                            {
                                Debug.WriteLine("File " + files[f].name + " at 0x" + files[f].pointer.ToString("X") + " failed to unpack at 0x" + filePos.ToString("X"));
                                break;
                            }
                        }
                    }
                //save file 
                string dir = Path.GetDirectoryName(filename);
                if (!Directory.Exists(dir + "/extract/" + Path.GetDirectoryName(files[f].name)))
                    Directory.CreateDirectory(dir + "/extract/" + Path.GetDirectoryName(files[f].name));
                using (BinaryWriter bw = new BinaryWriter(File.Create(dir + "/extract/" + files[f].name)))
                {
                    for (int i = 0; i < dest.Count; i++)
                        bw.Write(dest[i]);
                }

            }
            Debug.WriteLine("done unpacking fpack");
        }



    }
}
