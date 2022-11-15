using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static APX_Viewer.FileBuffer;
using System.Text;

namespace APX_Viewer
{
    class ArchiveAFS
    {

        public struct afsEntry
        {
            public string name;
            public uint pointer;
            public uint size;
            public DateTime modified;
        }

        public void Unpack(bool compressed)
        {
            bool dos53 = false;
            uint numfiles = uint.MaxValue;
            //first is the magic
            string magic = readString(0x4);
            if (magic == "AFS")
            {
                //then number of files
                numfiles = readInt();
            }
            else if (magic == "S")
            {
                dos53 = true;
                readInt(); //first offset and first size, compacted?
            }
            //now a list of offsets and sizes for the list
            List<afsEntry> entries = new List<afsEntry>();
            for (int i = 0; i < numfiles; i++)
            {
                afsEntry entry = new afsEntry();
                entry.pointer = readInt(); //offset
                entry.size = readInt(); //size
                if (dos53 && entry.pointer == 0 && entry.size == 0)
                    break;
                entries.Add(entry);
            }
            //first offset - 8 is the attributes start
            filePos = (int)(entries[0].pointer - 8);
            uint attpos = readInt();
            //and after that is the attributes size
            uint attsize = readInt();

            if (attpos == bufSize)
            {
                attpos = 0;
                Debug.WriteLine("File has no attributes section!");
            }
            else
            {
                filePos = (int)attpos;
                for (int i = 0; i < entries.Count; i++)
                {
                    afsEntry entry = new afsEntry { pointer = entries[i].pointer, size = entries[i].size };
                    //attributes have an 0x20 name
                    entry.name = readString(0x20);
                    //six shorts to make up the modify time
                    entry.modified = new DateTime(readShort(), readShort(), readShort(), readShort(), readShort(), readShort());
                    //then another int, a copy of the size? (verify for mhg and mh2)
                    if (entry.size != readInt())
                        Debug.WriteLine("size mismatch in entry " + i);
                    entries[i] = entry; //update entry
                }
            }
            //now, for each entry, copy it into a buffer?
            List<byte> entrybuf;
            for (int i = 0; i < entries.Count; i++)
            {
                entrybuf = new List<byte>((int)entries[i].size);
                filePos = (int)entries[i].pointer;
                string test = readString(4);
                filePos -= 4;
                //if compressed and >= first, decompress it
                if ((test != "MWo3") && compressed)
                    PzzDecompress(entrybuf);
                else
                    for (int b = 0; b < entries[i].size; b++)
                        entrybuf.Add(readByte());
                //save it out - if no name supplied, generate one from filename? like DATA.BIN -> DATA_1.AFS? does any of MH2 have extensions?
                if (entries[i].name == null)
                {
                    afsEntry entry = new afsEntry { pointer = entries[i].pointer, size = entries[i].size };
                    string ext = Path.GetExtension(filename);
                    if (ext == ".tex") //these always contain .tm2 files
                        ext = ".TM2";
                    else if (ext == ".BIN") //data.bin contains two .afs files
                        ext = ".AFS";
                    else //otherwise, save as generic
                        ext = ".BIN";
                    entry.name = Path.GetFileNameWithoutExtension(filename) + "_" + i + ext;
                    entries[i] = entry;
                }
                Debug.WriteLine("Unpacking " + entries[i].name);
                string path = Path.GetDirectoryName(filename) + "/" + Path.GetFileNameWithoutExtension(filename) + "/" + entries[i].name;
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (BinaryWriter br = new BinaryWriter(File.OpenWrite(path)))
                {
                    for (int b = 0; b < entrybuf.Count; b++)
                        br.Write(entrybuf[b]);
                }
            }
        }
    }
}
