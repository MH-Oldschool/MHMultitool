using System;
using System.Collections.Generic;
using static APX_Viewer.FileBuffer;
using System.Text;
using System.Diagnostics;

namespace APX_Viewer
{
    class MotionTable
    {
        public enum trans { none, scale_x, scale_y, scale_z = 4, rot_x = 0x8, rot_y = 0x10, rot_z = 0x20, mov_x = 0x40, mov_y = 0x80, mov_z = 0x100 }

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

            List<uint> sizes = new List<uint>();
            Debug.WriteLine("Animation Entry:");
            //now we parse the entries!
            readInt(); //top container, should be nonzero
            uint cnt = readInt(); //number of bonetables
            readInt(); //filesize

            readInt(); //should be 1? is this animation type?
            float lp = readFloat(); //this is the loop point
            Debug.WriteLine("Loop Point: " + lp);

            //and now, the bones!
            for(int b = 0; b < cnt; b++)
            {
                uint bcon = readInt();
                if ((bcon & 0xFF000000) == 0x80000000)
                {
                    //new container
                    uint transforms = readInt(); //children
                    readInt(); //size
                    Debug.WriteLine("Bone " + b);
                    for (int t = 0; t < transforms; t++)
                    {
                        uint ttype = readInt();//tag
                        uint frames = readInt();//children
                        readInt();//size
                        byte atype = (byte)(ttype >> 16);
                        if (atype == 0x12)
                            for (int f = 0; f < frames; f++)
                            {
                                ushort val = readShort(); //val
                                ushort fr = readShort(); //frame
                                ushort rint = readShort(); //rint
                                ushort lint = readShort(); //lint
                                Debug.WriteLine(" " + (trans)(ttype & 0xffff) + ": " + val + " @" + fr + " l:" + lint + " r:" + rint);
                            }
                        else if (atype == 0x22)
                            for(int f = 0; f < frames; f++)
                            {
                                float val = readFloat(); //val
                                float fr = readFloat(); //frame
                                float rint = readFloat(); //rint
                                float lint = readFloat(); //lint
                                Debug.WriteLine(" " + (trans)(ttype & 0xffff) + ": " + val + " @" + fr + " l:" + lint + " r:" + rint);
                            }
                        else
                        {
                            
                        }
                    }
                    //Debug.WriteLine(new string(' ', sizes.Count) + "Container " + (val & 0x00FFFFFF).ToString("X6") + ", Children " + unk.ToString("X") + ", size " + size.ToString("X"));
                    
                }
                else
                {
                    //offset, and two shorts?
                    Debug.WriteLine(new string(' ', sizes.Count) + "Data " + bcon.ToString("X8") + " " + readShort().ToString("X4") + " " + readShort().ToString("X4"));
                    for (int s = sizes.Count - 1; s > -1; s--)
                    {
                        sizes[s] -= 0x8;
                        if (sizes[s] == 0)
                        {
                            sizes.RemoveAt(s);
                        }
                    }
                }
            }
        }

    }
}
