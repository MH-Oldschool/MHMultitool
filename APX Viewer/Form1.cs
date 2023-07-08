using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace APX_Viewer
{
    public partial class Form1 : Form
    {

        int curimage;
        ImageCollection imgs;

        public Form1()
        {
            InitializeComponent();
            Text = "APX viewer";
            DoubleBuffered = true;

            FileBuffer.init(false);

            int filter = 0;
            string filename = "";
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "APX files|*.apx|Texture bin files|*_tex.bin;*.txb;|Manual Decompress|*.*|TM2|*.TM2;yn*tex.bin|Movement Table|*_tbl.bin|Quest file|*.mib|Wii fpack file|*.fpk|" +
                    "Compressed AFS|*.AFS|Uncompressed AFS|DATA.BIN;*.AFS|multitex package|*.tex|AMO model|*.amo;*_amh.bin|PAC archive|*.pac|PZZ recompress|*.bin|Collision Maps|lw0*.bin;lg0*.bin;lwg*.bin|" +
                    "DTX archive|*.dtx|bin unpack|*.bin|jkr decompress|*.tmp|Quest fun|*.mib|Animation Destruction|*_tbl.bin|gather tables|*.69;*.95|meat zones|*.95|psp databin|DATA.BIN|mhp quest dump|*.bin|" +
                    "animation parse|*.aan|mhfo exe decrypt|mhf.exe";
                openFileDialog.FilterIndex = 2; //to save me some clicks
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    filename = openFileDialog.FileName;
                    filter = openFileDialog.FilterIndex;
                }
            }
            if (filename == "")
            {
                return;
            }
            FileBuffer.loadFile(filename);
            curimage = 0;
            if (filter == 1)
            {
                ImageAPX img = new ImageAPX();
                img.Parse();

                displayImage(img.img);
            }
            else if (filter == 2)
            {
                imgs = new ImageCollection();
                imgs.Parse();

                displayImage(imgs.images[0]);
            }
            else if (filter == 3)
            {
                List<byte> decomp = new List<byte>(FileBuffer.bufSize);
                FileBuffer.PzzDecompress(decomp);
                string dir = Directory.GetCurrentDirectory();
                if (!Directory.Exists(dir + "/decompress/"))
                    Directory.CreateDirectory(dir + "/decompress/");
                using (BinaryWriter bw = new BinaryWriter(File.Create(dir + "/decompress/" + Path.GetFileName(filename))))
                {
                    for (int i = 0; i < decomp.Count; i++)
                        bw.Write(decomp[i]);
                }
                Debug.WriteLine("Successfully decompressed!");
            }
            else if (filter == 4)
            {
                ImageTM2 img = new ImageTM2();
                img.Parse();

                displayImage(img.img);
            }
            else if (filter == 5)
            {
                MotionTable tbl = new MotionTable();
                tbl.Parse();
            }
            else if (filter == 6)
            {
                //MHG quest numbering scheme is 00XXX for low rank, 01XXX for high rank, 02XXX for G rank, 03XXX for online events, 10XXX for village, 20XXX for training
                //MH2 adds time of day (d or n) as well as season number, and uses 000XX for basic training, 001XX for advanced training, 002XX for arena training,
                //01XXX for offline, 03XXX for Arena, 05XXX for urgent requests, 06XXX for unlockables? yian garuga's in here
                //10XXX for online hunting exercises, 11XXX for low rank free quests, 12XXX for high rank free quests 
                //15XXX for Official Hunting Tests (online), 20XXX for repel missions, 25XXX for versus matches, lots of others i have yet to map out
                //coverage = new List<byte>(new byte[filebuf.Count]);
                //checkcoverage = true;
                Quest quest = new Quest();
                quest.Parse();

                //load up the map file
                //checkcoverage = false;
                filename = Path.GetDirectoryName(filename);
                if (quest.game != Quest.Game.MH2)
                {
                    filename += "\\map" + (quest.locale == 1 ? "" : quest.locale.ToString()) + ".apx";
                    if (File.Exists(filename))
                    {
                        FileBuffer.loadFile(filename);
                        ImageAPX map = new ImageAPX();
                        map.Parse();
                        displayImage(map.img);
                    }
                }
                else
                {
                    filename = Directory.GetParent(filename).FullName + "\\map" + (quest.locale == 1 ? "" : quest.locale.ToString()) + ".tm2";

                    if (File.Exists(filename))
                    {
                        FileBuffer.loadFile(filename);
                        ImageTM2 map = new ImageTM2();
                        map.Parse();
                        displayImage(map.img);
                    }
                }

                //draw gathering locations?
                List<Pen> pinPens = new List<Pen> { new Pen(new SolidBrush(Color.Red)), new Pen(Color.Transparent), new Pen(Color.Transparent), new Pen(new SolidBrush(Color.Yellow)), new Pen(new SolidBrush(Color.Blue)) };
                Pen monstpen = new Pen(new SolidBrush(Color.Red));
                if (BackgroundImage != null)
                {

                    Graphics g = Graphics.FromImage(BackgroundImage);
                    for (int i = 0; i < quest.pins.Count; i++)
                    {
                        g.DrawEllipse(monstpen, quest.pins[i].x / ConstantsLocales.MapScales[(int)quest.locale][0], quest.pins[i].y / ConstantsLocales.MapScales[(int)quest.locale][1], 3, 3);
                    }
                }
            }
            else if (filter == 7) //wii archive
            {
                FileBuffer.wii = true;
                ArchiveFPK archive = new ArchiveFPK();
                archive.Unpack();
            }
            else if (filter == 8) //compressed afs archive
            {
                ArchiveAFS archive = new ArchiveAFS();
                archive.Unpack(true);
            }
            else if (filter == 9) //uncompressed afs archive
            {
                ArchiveAFS archive = new ArchiveAFS();
                archive.Unpack(false);
            }
            else if (filter == 10) //tex packs
            {
                string magic = FileBuffer.readString(4);
                FileBuffer.filePos -= 4;
                if (magic == "AFS")
                {
                    ArchiveAFS archive = new ArchiveAFS();
                    archive.Unpack(true);
                }
                else if (magic == "MOMO")
                {
                    ArchiveMOMO archive = new ArchiveMOMO();
                    archive.Unpack();
                }
            }
            else if (filter == 11) //.amo 3d models
            {
                bool amh = false;
                if (Path.GetExtension(filename) == ".bin")
                    amh = true;
                Mesh mesh = new Mesh();
                mesh.Parse(amh);
                mesh.Export();
            }
            else if (filter == 12) //.pac archives
            {
                string magic = FileBuffer.readString(4);
                FileBuffer.filePos -= 4;
                if (magic == "ecd" + "\x1A")
                    FileBuffer.ecdDecrypt();
                ArchivePAC archive = new ArchivePAC();
                archive.Unpack();
            }
            else if (filter == 13)
            {
                //here we go
                List<byte> bytes = new List<byte>();
                FileBuffer.PzzCompress(ref bytes);
                string path = Directory.GetCurrentDirectory() + "\\export\\" + Path.GetFileName(filename);
                //filename = path + "/u" + file.ToString("000") + ".mib";
                using (BinaryWriter bw = new BinaryWriter(File.Create(path)))
                    for (int b = 0; b < bytes.Count; b++)
                        bw.Write(bytes[b]);
            }
            else if (filter == 14)
            {
                //wall and ground collision maps
                CollisionMap map = new CollisionMap();
                if (filename.Contains("lwg"))
                {
                    //conjoined table
                    FileBuffer.readInt();
                    uint o1 = FileBuffer.readInt();
                    FileBuffer.readInt();
                    uint o2 = FileBuffer.readInt();
                    FileBuffer.readInt();
                    FileBuffer.filePos = (int)o1;
                    map.ParseWalls();
                    FileBuffer.filePos = (int)o2;
                    map.ParseGrounds();
                }
                else
                {
                    //split table, load walls first
                    string mapnum = Path.GetFileNameWithoutExtension(filename).Substring(2);
                    FileBuffer.loadFile(Path.GetDirectoryName(filename) + "/lw" + mapnum + ".bin");
                    map.ParseWalls();
                    //then load grounds
                    FileBuffer.loadFile(Path.GetDirectoryName(filename) + "/lg" + mapnum + ".bin");
                    map.ParseGrounds();
                }
                map.Export();
            }
            else if (filter == 15)
            {
                ArchiveDTX archive = new ArchiveDTX();
                archive.Unpack();
            }
            else if (filter == 16)
            {

                //used to inspect frontier data bins
                //in mhf-360, 0, 1, 5, and 6 are dds bundles, 2 3 and 4 are jkr'd HLSL bundles, 7's an ogg, and 8's a jpg bundle
                //0 - 5, 11 - 13 in mhf.bin are pngs, 6 and 7 is png bundle, 9 and 10 are jkr'd D3DX8 shaders and ???.. 8 is a quest?!?
                //mytra.bin: 0-3 are jkr (two amo and two ahi), 4 and 5 are png bundles, 6-11 are motion tables!

                //in mhfinf, 0x2C is pointer to table of quest header pointers
                List<List<byte>> dat = new List<List<byte>>();
                FileBuffer.unBundle(ref dat);
                if (dat.Count == 0)
                    return;
                string path = Directory.GetCurrentDirectory() + "\\export\\" + Path.GetFileNameWithoutExtension(filename) + "\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                for (int i = 0; i < dat.Count; i++)
                {
                    using (BinaryWriter bw = new BinaryWriter(File.Create(path + i + ".tmp")))
                    {
                        for (int b = 0; b < dat[i].Count; b++)
                            bw.Write(dat[i][b]);
                    }
                }
            }
            else if (filter == 17)
            {
                FileBuffer.jpkUncompress();
                using (BinaryWriter bw = new BinaryWriter(File.Create(Directory.GetParent(filename).FullName + "\\" + Path.GetFileNameWithoutExtension(filename) + ".bin")))
                    for (int i = 0; i < FileBuffer.bufSize; i++)
                        bw.Write(FileBuffer.readByte());
            }
            else if (filter == 18)
            {
                string path = Directory.GetParent(filename).FullName;
                string[] files = Directory.GetFiles(path, "*.mib");
                foreach(string f in files)
                {
                    //if (File.Exists(path + "\\m" + i.ToString("000") + ".mib"))
                    //{
                        FileBuffer.loadFile(f);
                        //that's about it?
                        Quest q = new Quest();
                        q.Parse();
                    //}
                }
            }
            else if (filter == 19)
            {
                //load the animation file
                //parse our lists
                //copy the data out
                //profit
                //uint animCount = FileBuffer.readInt();
                List<uint> ptrs = new List<uint>();
                List<uint> animCts = new List<uint>();
                List<uint> ptrs2 = new List<uint>();
                List<uint> animCts2 = new List<uint>();
                List<string> names = new List<string> { "Main", "Head", "Tail" }; 
                while(true)
                {
                    uint val = FileBuffer.readInt();
                    uint val2 = FileBuffer.readInt();
                    if (val == 0)
                        break;
                    ptrs.Add(val2);
                    animCts.Add(val);
                    animCts2.Add(FileBuffer.readInt());
                    ptrs2.Add(FileBuffer.readInt());
                }
                if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\export\\" + Path.GetFileNameWithoutExtension(filename) + "\\"))
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\export\\" + Path.GetFileNameWithoutExtension(filename) + "\\");
                for (int sub = 0; sub < ptrs.Count; sub++)
                {
                    FileBuffer.filePos = (int)ptrs[sub];
                    for (int p = 0; p < animCts[sub]; p++)
                    {
                        //grab the value
                        uint val = FileBuffer.readInt();
                        if (val == 0xFFFFFFFF)
                            continue;
                        int backup = FileBuffer.filePos;
                        FileBuffer.filePos = (int)val;
                        FileBuffer.readInt();
                        FileBuffer.readInt();
                        uint len = FileBuffer.readInt();
                        FileBuffer.filePos -= 12;
                        using (BinaryWriter bw = new BinaryWriter(File.Create(Directory.GetCurrentDirectory() + "\\export\\" + Path.GetFileNameWithoutExtension(filename) + "\\" + p + "_" + names[sub] + ".aan")))
                        {
                            for (int b = 0; b < len; b++)
                                bw.Write(FileBuffer.readByte());
                        }
                        FileBuffer.filePos = backup;
                    }
                }
                for (int sub = 0; sub < ptrs2.Count; sub++)
                {
                    FileBuffer.filePos = (int)ptrs2[sub];
                    for (int p = 0; p < animCts2[sub]; p++)
                    {
                        //grab the value
                        uint val = FileBuffer.readInt();
                        if (val == 0xFFFFFFFF)
                            continue;
                        int backup = FileBuffer.filePos;
                        FileBuffer.filePos = (int)val;
                        FileBuffer.readInt();
                        FileBuffer.readInt();
                        uint len = FileBuffer.readInt();
                        FileBuffer.filePos -= 12;
                        using (BinaryWriter bw = new BinaryWriter(File.Create(Directory.GetCurrentDirectory() + "\\export\\" + Path.GetFileNameWithoutExtension(filename) + "\\sub_"+ p + "_" + names[sub] + ".aan")))
                        {
                            for (int b = 0; b < len; b++)
                                bw.Write(FileBuffer.readByte());
                        }
                        FileBuffer.filePos = backup;
                    }
                }
            }
            else if(filter == 20)
            {
                //export the gathering table!
                //0x1FCB70 for mh1j exe
                //0x191D50 for mhgp... 6D are in the file, but 2E is in the exe
                //exe needs offsets of +100000, then -180 for 1j and -200 for gp
                //game.bin needs -542600
                string f = Path.GetFileName(filename);
                if (f == "SLPM_654.95")
                {
                    //mh1j
                    FileBuffer.filePos = 0x1FCB70;
                    List<uint> ptrs = new List<uint>();
                    while(true)
                    {
                        uint val = FileBuffer.readInt();
                        if (val > 0x40000000)
                            break;
                        ptrs.Add(val);
                    }
                    string path = Directory.GetCurrentDirectory() + "\\export\\g_mh1j";
                    BinaryWriter bw = new BinaryWriter(File.Create(path));
                    for(int p = 0; p < ptrs.Count; p++)
                    {
                        FileBuffer.filePos = (int)(ptrs[p] - 0x100000 + 0x180);
                        //read the line, write it out somewhere
                        while(true)
                        {
                            ushort val1 = FileBuffer.readShort();
                            ushort val2 = FileBuffer.readShort();
                            bw.Write(val1);
                            bw.Write(val2);
                            if (val1 == 0xFFFF)
                                break;
                        }
                        //advance to the next line in the output, for clean parsing
                        while ((bw.BaseStream.Position % 0x10) != 0)
                            bw.Write((byte)0);
                    }
                }
                else if(f == "SLPM_658.69")
                {
                    //mhgp
                    //open up game.bin
                    BinaryReader br = new BinaryReader(File.OpenRead(Path.GetDirectoryName(filename) + "\\extract\\game.bin"));
                    br.BaseStream.Position = 0x191D50;
                    List<uint> ptrs = new List<uint>();
                    while(true)
                    {
                        uint val = br.ReadUInt32();
                        if (val < 0x30000)
                            break;
                        ptrs.Add(val);
                    }
                    string path = Directory.GetCurrentDirectory() + "\\export\\g_mhgp";
                    BinaryWriter bw = new BinaryWriter(File.Create(path));
                    for (int p = 0; p < ptrs.Count; p++)
                    {
                        if(ptrs[p] > 0x542600)
                        {
                            //in the bin
                            br.BaseStream.Position = ptrs[p] - 0x542600;
                            //read the line, write it out somewhere
                            while (true)
                            {
                                ushort val1 = br.ReadUInt16();
                                ushort val2 = br.ReadUInt16();
                                bw.Write(val1);
                                bw.Write(val2);
                                if (val1 == 0xFFFF)
                                    break;
                            }
                        }
                        else
                        {
                            //exe
                            FileBuffer.filePos = (int)ptrs[p] - 0x100000 + 0x200;
                            //read the line, write it out somewhere
                            while (true)
                            {
                                ushort val1 = FileBuffer.readShort();
                                ushort val2 = FileBuffer.readShort();
                                bw.Write(val1);
                                bw.Write(val2);
                                if (val1 == 0xFFFF)
                                    break;
                            }
                        }
                        //advance to the next line in the output, for clean parsing
                        while ((bw.BaseStream.Position % 0x10) != 0)
                            bw.Write((byte)0);
                    }
                }
            }
            else if(filter == 21)
            {
                //go to the master table then parse the entries
                List<int> ptrs = new List<int>();
                FileBuffer.filePos = (0x357580 - 0x100000 + 0x180);
                for(int i = 0; i < 36; i++)
                {
                    ptrs.Add((int)FileBuffer.readInt());
                }
                List<string> mons = new List<string> {"null", "rathian", "fatalis", "kelbi", "mosswine", "bullfango", "yian kut ku", "laoshan lung", "cephadrome", "felyne", "veggie elder", "rathalos", "aptonoth", "genprey", "diablos",  
                "khezu", "velociprey", "gravios", "cart", "vespoid", "gypceros", "plesioth", "basarios", "melynx", "hornetaur", "apceros", "monoblos", "velocidrome", "gendrome", "rock", "ioprey", "iodrome", "pugi", "kirin", "cephalos"};
                for(int i = 0; i < 35; i++)
                {
                    FileBuffer.filePos = (ptrs[i] - 0x100000 + 0x180);
                    Debug.WriteLine(mons[i]);
                    for (int p = 0; p < 8; p++)
                    {
                        byte unk = FileBuffer.readByte();
                        byte cut = FileBuffer.readByte();
                        byte blt = FileBuffer.readByte();
                        byte gun = FileBuffer.readByte();
                        byte fire = FileBuffer.readByte();
                        byte water = FileBuffer.readByte();
                        byte thunder = FileBuffer.readByte();
                        byte dragon = FileBuffer.readByte();
                        Debug.WriteLine("unk: " + unk + "% cutting: " + cut + "% blunt: " + blt + "% bullet: " + gun + "% fire: " + fire + "% water: " + water + "% thunder: " + thunder + "% dragon: " + dragon + "%");
                    }
                }
            }
            else if(filter == 22)
            {
                //psp data.bin
                //each int here is a file start??
                //multiply by 2048, scale of 2KB
                List<int> ptrs = new List<int>();
                while(true)
                {
                    uint p = FileBuffer.readInt()*0x800;
                    ptrs.Add((int)p);
                    if (p == FileBuffer.bufSize)
                        break;
                }
                string path = Path.GetDirectoryName(filename) + "\\extract\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                for (int i = 0; i < ptrs.Count-1; i++)
                {
                    FileBuffer.filePos = ptrs[i];
                    using (BinaryWriter br = new BinaryWriter(File.Create(path + i + ".bin")))
                        for (int b = 0; b < ptrs[i + 1] - ptrs[i]; b++)
                            br.Write(FileBuffer.readByte());

                }
            }
            else if(filter == 23)
            {
                string path = Directory.GetParent(filename).FullName;
                string[] files = Directory.GetFiles(path, "*.bin");
                foreach (string f in files)
                {
                    //if (File.Exists(path + "\\m" + i.ToString("000") + ".mib"))
                    //{
                    FileBuffer.loadFile(f);
                    //that's about it?
                    FileBuffer.filePos = 0x5C;
                    Debug.WriteLine(FileBuffer.readJISString());
                    //}
                }
            }
            else if(filter == 24)
            {
                MotionTable mt = new MotionTable();
                mt.Parse();
            }
            else if(filter == 25)
            {
                FileBuffer.filePos = 0xBC15E;
                byte[] dc = new byte[0x7A8];
                for(int i = 0; i <= 0x7A4/4; i++)
                {
                    uint v = FileBuffer.readInt();
                    v += 0x9a61adf1;
                    v ^= 0x15651e22;
                    dc[i * 4] = (byte)(v & 0xFF);
                    dc[i * 4 + 1] = (byte)(v >> 8);
                    dc[i * 4 + 2] = (byte)(v >> 16);
                    dc[i * 4 + 3] = (byte)(v >> 24);
                }
                //now the second pass!
                for(int i = 0; i <= 0x1B3; i++)
                {
                    uint v = (uint)(dc[0x7A3 - i * 4] + (dc[0x7A3 - i * 4 + 1] << 8) + (dc[0x7A3 - i * 4 + 2] << 16) + (dc[0x7A3 - i * 4 + 3] << 24));
                    v ^= 0x29786713;
                    v -= 0x635ddd50;
                    v ^= 0x6445cb49;
                    dc[0x7A3 - i * 4] = (byte)(v & 0xFF);
                    dc[0x7A3 - i * 4 + 1] = (byte)(v >> 8);
                    dc[0x7A3 - i * 4 + 2] = (byte)(v >> 16);
                    dc[0x7A3 - i * 4 + 3] = (byte)(v >> 24);
                }
                //and the third pass!
                for (int i = 0; i <= 0x5cc/4; i++)
                {
                    uint v = (uint)(dc[0x7A3 - i * 4] + (dc[0x7A3 - i * 4 + 1] << 8) + (dc[0x7A3 - i * 4 + 2] << 16) + (dc[0x7A3 - i * 4 + 3] << 24));
                    v ^= 0x04423016;
                    v -= 0x41d95d97;
                    v -= 0x7ddb0b84;
                    dc[0x7A3 - i * 4] = (byte)(v & 0xFF);
                    dc[0x7A3 - i * 4 + 1] = (byte)(v >> 8);
                    dc[0x7A3 - i * 4 + 2] = (byte)(v >> 16);
                    dc[0x7A3 - i * 4 + 3] = (byte)(v >> 24);
                }
                //fourth pass!
                for (int i = 0; i <= 0x144; i++)
                {
                    uint v = (uint)(dc[0x7A4 - i * 4] + (dc[0x7A4 - i * 4 + 1] << 8) + (dc[0x7A4 - i * 4 + 2] << 16) + (dc[0x7A4 - i * 4 + 3] << 24));
                    v += 0x36af1f7d;
                    v += 0x3bd6ae72;
                    v ^= 0x41bb32c3;
                    dc[0x7A4 - i * 4] = (byte)(v & 0xFF);
                    dc[0x7A4 - i * 4 + 1] = (byte)(v >> 8);
                    dc[0x7A4 - i * 4 + 2] = (byte)(v >> 16);
                    dc[0x7A4 - i * 4 + 3] = (byte)(v >> 24);
                }

                //string thing
                /*{
                    uint key = 0x9c3b248e;
                    FileBuffer.filePos = 0x16db0;//todo, fill this
                    while (true)
                    {
                        byte v = FileBuffer.readByte();
                        if (v == 0)
                            break;
                        key ^= v;
                        for(int i = 0; i < 8; i++)
                        {
                            bool f = (key & 1) == 1;
                            key >>= 1;
                            if (f) key ^= 0xc1a7f39a;
                            Debug.WriteLine(key.ToString("X8"));
                        }
                    }
                    if (key == 0xB72551A7)
                        Debug.WriteLine("MATCH");

                }*/

                //decompression of data at 0x4e3bd4 (bcbd4)
                {
                    FileBuffer.fexedecompress();

                }


                //write results
                /*string path = Directory.GetCurrentDirectory() + "\\export\\";
                using (BinaryWriter bw = new BinaryWriter(File.Create(path + "exe4.bin")))
                {
                    for (int i = 0; i < dc.Length; i++)
                        bw.Write(dc[i]);
                }

                FileBuffer.filePos = 0x198;*/
            }

            Text = Path.GetFileName(filename);

            Visible = true; //needed for some reason
            Activate();
        }

        void displayImage(Bitmap img)
        {
            ClientSize = new Size(img.Width, img.Height);
            BackgroundImage = img;
            Invalidate();
            Update();
        }
        
        
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (imgs != null && imgs.images.Count > 0)
            {
                //capture left arrow key
                if (keyData == Keys.Left)
                {
                    curimage = ((curimage + ((int)imgs.images.Count - 1)) % (int)imgs.images.Count);
                    displayImage(imgs.images[curimage]);
                    return true;
                }
                //capture right arrow key
                if (keyData == Keys.Right)
                {
                    curimage = ((curimage+1) % (int)imgs.images.Count);
                    displayImage(imgs.images[curimage]);
                    return true;
                }
            }
            if(keyData == Keys.Enter)
            {
                string dir = Directory.GetCurrentDirectory();
                if (!Directory.Exists(dir + "/export/"))
                    Directory.CreateDirectory(dir + "/export/");
                BackgroundImage.Save(dir + "/export/" + Path.GetFileName(FileBuffer.filename) + "_" + curimage + ".png");
                Debug.WriteLine("Saved image!");
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        
    }
}
