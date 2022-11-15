using System;
using System.Collections.Generic;
using static APX_Viewer.FileBuffer;
using System.Text;
using System.Diagnostics;

namespace APX_Viewer
{
    class MotionTable
    {
        public void Parse()
        {
            //these are motion tables - actual animations??

            //pairs of ints (size and location), until a size of zero and pointer to the end of the primary tables. these pairs are also in pairs? second of each set seems to be blank...
            //these primary table entries point to the animations! FFFF is no data for that part for the animation

            //in the table entries: $80XXXXXX signifies a container, second int is number of children, third int is size including this node. read until end of node, if another $80XXXXXX, another child
            //top level in these tables are motionsets, motions, and fcurves
            //the "data" in a motionset is a loop definition
            //only parse the first table for research purposes
            //monhun seems to only use hermite curves? 22 and 12 https://www.cubic.org/docs/hermite.htm

            //this data gets stored to memory... 
            //destpointer + 2 is (short) child data entry count? (number of keyframes)
            //destpointer + 4 is allocated memory pointer (8 bytes per entry, each has pos 0 value 0x20 and pos 2 zeroed out.
            //so, at arg2 is a table of entries like ([byte]type, [byte]0, [short]count, [int]pointer)
            //and at the pointer, the four values (floats?) per entry are copied over. pointer is relative from the base fms0 pointer!
            //then the second byte in the table is set to what bit was set in the ID (low 9 bits of the container int containing the curve type)
            //after these are copied into the fms buffer, it's copied into PS2 system buffer and the buffer handle is returned and written to pivar3... motion_set_handle_tbl + some offset

            //readInt(); //uncomment these to grab later tables
            //readInt();
            //readInt();
            //readInt();
            //readInt();
            //readInt();
            //readInt();
            //readInt();
            uint firstsize = readInt();
            uint firsttable = readInt();
            filePos = (int)firsttable;
            List<uint> entries = new List<uint>();
            for (int i = 0; i < firstsize; i++)
            {
                uint val = readInt();
                if (val != 0xFFFFFFFF)
                    entries.Add(val);
            }
            for (int i = 0; i < entries.Count; i++)
            {
                List<uint> sizes = new List<uint>();
                filePos = (int)entries[i];
                Debug.WriteLine("Subtable Entry " + i);
                //now we parse the entries!
                do
                {
                    uint val = readInt();
                    if ((val & 0xFF000000) == 0x80000000)
                    {
                        //new entry!
                        uint unk = readInt();
                        uint size = readInt();
                        Debug.WriteLine(new string(' ', sizes.Count) + "Container " + (val & 0x00FFFFFF).ToString("X6") + ", Children " + unk.ToString("X") + ", size " + size.ToString("X"));
                        sizes.Add(size);
                        for (int s = sizes.Count - 1; s > -1; s--)
                        {
                            sizes[s] -= 0xC;
                            if (sizes[s] == 0)
                            {
                                sizes.RemoveAt(s);
                            }
                        }
                    }
                    else
                    {
                        //offset, and two shorts?
                        Debug.WriteLine(new string(' ', sizes.Count) + "Data " + val.ToString("X8") + " " + readShort().ToString("X4") + " " + readShort().ToString("X4"));
                        for (int s = sizes.Count - 1; s > -1; s--)
                        {
                            sizes[s] -= 0x8;
                            if (sizes[s] == 0)
                            {
                                sizes.RemoveAt(s);
                            }
                        }
                    }
                } while (sizes.Count > 0);
            }
        }

    }
}
