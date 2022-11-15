using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using static APX_Viewer.FileBuffer;

namespace APX_Viewer
{
    class ArchiveDTX
    {
        public void Unpack()
        {
            wii = true;
            string path = Directory.GetCurrentDirectory() + "\\export\\dtx\\";
            uint magic = readInt(); //cocd
            readInt(); //unknown
            uint directorySize = readInt(); //directory data size
            int pos = filePos;
            filePos = (int)(directorySize + 0xC);
            uint stringsSize = readInt();
            filePos = pos;
            uint i1, i2, i3;
            int entry = 0;
            List<uint> sizes = new List<uint>();
            while(true)
            {
                i1 = readInt(); //pointer to name
                i2 = readInt(); 
                i3 = readInt();
                if(i2 == 0xFFFFFFFF)
                {
                    //folder
                    pos = filePos;
                    //get name
                    filePos = (int)(i1 + directorySize + 0xC + 0x4);
                    string nom = readString();
                    //handle folder
                    path += "\\" + nom;
                    filePos = pos;
                    sizes.Add(i3);
                    //create folder
                    Directory.CreateDirectory(path);
                }
                else
                {
                    //file
                    pos = filePos;
                    //fetch name
                    filePos = (int)(i1 + directorySize + 0xC + 0x4);
                    string nom = readString();
                    //seek to file
                    filePos = (int)(i3 + directorySize + stringsSize + 0xC + 0x4);
                    //and read it!
                    using(BinaryWriter bw = new BinaryWriter(File.OpenWrite(path + "\\" + nom)))
                    {
                        for (int b = 0; b < i2; b++)
                            bw.Write(readByte());
                    }
                    while(true)
                    {
                        sizes[^1]--;
                        if (sizes[^1] == 0)
                        {
                            sizes.RemoveAt(sizes.Count - 1);
                            path = Directory.GetParent(path).FullName;
                        }
                        else
                            break;
                        if (sizes.Count == 0)
                            break;
                    }
                    filePos = pos;
                }
                entry++;
                if (entry * 4 * 3 == directorySize)
                    break;
            }
            Debug.WriteLine("unpack successful");
        }
    }
}
