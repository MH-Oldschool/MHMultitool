using System;
using System.Collections.Generic;
using static APX_Viewer.FileBuffer;
using System.Text;
using System.IO;

namespace APX_Viewer
{
    class ArchiveMOMO
    {
        public void Unpack()
        {
            if (readString(0x4) != "MOMO")
                throw new Exception("Not a MOMO archive?");
            uint files = readInt();
            List<uint> positions = new List<uint>();
            List<uint> lengths = new List<uint>();
            for (int i = 0; i < files; i++)
            {
                positions.Add(readInt());
                lengths.Add(readInt());
            }
            string path = Path.GetDirectoryName(filename) + "/" + Path.GetFileNameWithoutExtension(filename) + "/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            for (int i = 0; i < files; i++)
            {
                filePos = (int)positions[i];
                using (BinaryWriter bw = new BinaryWriter(File.OpenWrite(path + Path.GetFileNameWithoutExtension(filename) + "_" + i + ".TM2")))
                    for (int b = 0; b < lengths[i]; b++)
                        bw.Write(readByte());
            }
        }

    }
}
