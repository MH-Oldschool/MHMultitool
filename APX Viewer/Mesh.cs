using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using static APX_Viewer.FileBuffer;
using System.Text;

namespace APX_Viewer
{
    class Mesh
    {
        //format is similar to Outbreak's, with a couple tags tweaked
        public class MeshObject
        {
            /*public struct Vertex
            {
                public int position;
                public int normal;
                public int colour;
                public int uvcoord;
            }*/
            public struct Face
            {
                public uint a;
                public uint b;
                public uint c;
            }
            public List<Face> faces;
            //public List<Vertex> vertices;
            public List<Vector3> positions;
            public List<Vector3> normals;
            public List<Vector4> colours;
            public List<Vector2> uvcoords;
            public List<int> stripstarts;
            public List<int> stripmats;
            public List<VertWeight> weights;
        }

        public struct VertWeight
        {
            public struct WeightEntry
            {
                public uint boneID;
                public float weight;
            }
            public List<WeightEntry> entries;
        }

        public class Material
        {
            public Vector4 ambient;
            public Vector4 diffuse;
            public Vector4 specular;
            public float specfalloff;
            public List<int> textures;
        }

        bool amh;


        List<MeshObject> meshes = new List<MeshObject>();
        List<Material> materials = new List<Material>();

        List<int> matswap = new List<int>();
        List<int> texswap = new List<int>();
        List<int> boneswap = new List<int>();

        List<uint> rootnodes = new List<uint>();
        List<Node> nodes = new List<Node>();

        public void Parse(bool hasamh)
        {
            int ahioffset = -1;
            amh = hasamh;
            if (amh)
            {
                uint num = readInt();
                if (num != 2)
                    Debug.WriteLine("more than two fields in the .amh!!!");
                uint amooff = readInt();
                readInt(); //size
                ahioffset = (int)readInt();
                readInt(); //size
                filePos = (int)amooff;
            }
            try
            {
                bool alt = false;

                ushort header = readShort(); //1, sometimes 7?
                readShort();//0
                readInt(); //children
                uint filesize = readInt(); //add 3C to this to get the size of the whole file!
                if (!amh)
                    filesize += 0x3C;
                int curmesh = -1;

                if (header == 7)
                    alt = true;

                int section = 0;
                int tristrips = 0;
                while (filePos < filesize)
                {
                    //group mesh into objects?
                    uint tagID = readInt();
                    ushort chunktype = (ushort)(tagID & 0xFFFF); //short chunk type
                    ushort datatype = (ushort)((tagID >> 16) & 0xFFFF); //short data type
                    uint children = readInt();   //int, number of children
                    uint size = readInt();   //int, size of this chunk (including header)
                    if (alt ? (false) : (chunktype == 1 && section == 0))
                    {
                        //file start
                    }
                    else if (alt ? (chunktype == 1 && section == 0) : (chunktype == 2)) //meshes start
                    {
                        section = 1;
                    }
                    else if (alt ? (chunktype == 2) : (chunktype == 9)) //materials start
                    {
                        section = 2;
                    }
                    else if (alt ? (chunktype == 4) : (chunktype == 10)) //textures start
                        section = 3;
                    else if (alt ? (chunktype == 0xDF02) : (chunktype == 4 && section == 1)) //mesh
                    {
                        MeshObject mo = new MeshObject();
                        mo.positions = new List<Vector3>();
                        mo.normals = new List<Vector3>();
                        mo.uvcoords = new List<Vector2>();
                        mo.colours = new List<Vector4>();
                        mo.faces = new List<MeshObject.Face>();
                        mo.stripstarts = new List<int>();
                        mo.stripmats = new List<int>();
                        mo.weights = new List<VertWeight>();
                        curmesh++;
                        tristrips = 0;
                        meshes.Add(mo);

                    }
                    else if (alt ? (false) : (chunktype == 5 && section == 1))
                    {
                        //face definitions!
                    }
                    else if (alt ? (chunktype == 0x200) : (chunktype == 0 && datatype == 2))
                    {
                        //date!
                        uint val = readInt();
                        byte day = (byte)((val >> 8) & 0xFF);
                        byte month = (byte)((val >> 16) & 0xFF);
                        byte year = (byte)((val >> 24) & 0xFF);
                        Debug.WriteLine("date: " + month + "/" + day + "/" + (2000 + year));
                    }
                    else if (alt ? (chunktype > 0x7) : (chunktype == 0 && section == 1))
                    {
                        if (alt ? (chunktype == 0x100) : (datatype == 3 || datatype == 4)) //if datatype is 3 or 4, these are vertex strips
                        {
                            for (int s = 0; s < children; s++)
                            {
                                bool lefty = false;
                                uint indices = readInt(); //number of verts in the tristrip
                                if ((indices & 0x80000000) != 0)
                                {
                                    lefty = true;
                                    indices &= 0x7FFFFFFF;
                                }
                                //if (datatype == 3)
                                //    Debug.WriteLine("full weight strip");
                                //else if (datatype == 4)
                                //    Debug.WriteLine("partial weight strip");
                                uint p1 = 0;
                                uint p2 = 0;
                                uint p3 = 0;
                                meshes[curmesh].stripstarts.Add(meshes[curmesh].faces.Count);
                                tristrips++;
                                for (int c = 0; c < indices; c++)
                                {
                                    p3 = p2;
                                    p2 = p1;
                                    p1 = readInt();
                                    if (c > 1 && p1 != p2 && p1 != p3 && p2 != p3)
                                    {
                                        if ((c % 2) == 0)
                                            meshes[curmesh].faces.Add(new MeshObject.Face { a = p3, b = p2, c = p1 });
                                        else
                                            meshes[curmesh].faces.Add(new MeshObject.Face { a = p3, b = p1, c = p2 });
                                    }
                                }
                            }
                        }
                        else if (alt ? (false) : (datatype == 5))
                        {
                            //material ID list?
                            for (int i = 0; i < children; i++)
                                matswap.Add((int)readInt());
                        }
                        else if (alt ? (chunktype == 0x400) : (datatype == 6))
                        {
                            //material IDs for tri strips
                            Debug.WriteLine("tri material list found: " + children.ToString("X") + " tristrips: " + tristrips.ToString("X"));
                            for (int i = 0; i < children; i++)
                                meshes[curmesh].stripmats.Add((int)readInt()); //material entry ID for each tristrip
                        }
                        else if (alt ? (chunktype == 0x800) : (datatype == 7)) //datatype of 7 is vertex positions
                        {
                            for (int c = 0; c < children; c++)
                                meshes[curmesh].positions.Add(new Vector3(readFloat(), readFloat(), readFloat()));
                        }
                        else if (alt ? (chunktype == 0x1000) : (datatype == 8)) //datatype of 8 is vertex normals
                        {
                            for (int c = 0; c < children; c++)
                                meshes[curmesh].normals.Add(new Vector3(readFloat(), readFloat(), readFloat()));
                        }
                        else if (alt ? (chunktype == 0x4000) : (datatype == 10)) //datatype of A is texture UV coordinates
                        {
                            for (int c = 0; c < children; c++)
                                meshes[curmesh].uvcoords.Add(new Vector2(readFloat(), 1 - readFloat())); //invert Y
                        }
                        else if (alt ? (chunktype == 0x8000) : (datatype == 11)) //datatype of B is vertex colours
                        {
                            for (int c = 0; c < children; c++)
                                meshes[curmesh].colours.Add(new Vector4(readFloat(), readFloat(), readFloat(), readFloat()));
                        }
                        else if (alt ? (false) : (datatype == 12)) //datatype of C, weight list
                        {
                            //each vertex gets a weight entry
                            for (int v = 0; v < children; v++)
                            {
                                uint weightcount = readInt();
                                VertWeight weight = new VertWeight();
                                weight.entries = new List<VertWeight.WeightEntry>();
                                for (int i = 0; i < weightcount; i++)
                                    weight.entries.Add(new VertWeight.WeightEntry { boneID = readInt(), weight = readFloat() });
                                meshes[curmesh].weights.Add(weight);
                            }
                        }
                        else if (alt ? (false) : (datatype == 14)) //datatype of E
                        {
                            //unknown
                            Debug.WriteLine("Datatype E found at 0x" + (filePos - 0xC).ToString("X"));
                            for (int i = 0; i < children; i++)
                            {
                                uint count = readInt();
                                for (int n = 0; n < count; n++)
                                    readInt(); //usually/always -1? only in monster meshes?
                            }
                        }
                        else if (alt ? (false) : (datatype == 15)) //datatype of F, "attribute"
                        {
                            readInt(); //version?
                            readInt(); //1 means attributes exist
                            readInt(); //material
                            readInt(); //specular
                            readInt(); //cull
                            readInt(); //scissor, sometimes 2? 0x10
                            readInt(); //light
                            readInt(); //unused
                            readInt(); //uvscroll
                            readInt(); //fog, sometimes 1? 0x20
                            readInt(); //fadecolour, 1 bit in size?
                            readInt(); //2? sets renderstate if 5 or 6
                            readInt(); //3? 4 bits in size
                            readInt(); //4 bits in size. 0x30
                            readInt(); //2 bits in size
                            readInt(); //1 bit in size
                            readInt(); //2 bits in size
                            filePos += (int)(size - 0x44 - 0xC);
                        }
                        else if (alt ? (false) : (datatype == 16)) //datatype of 10, bone list
                        {
                            for (int i = 0; i < children; i++)
                                boneswap.Add((int)readInt());
                        }
                        else
                        {
                            Debug.WriteLine("unimplemented chunk: chunk " + chunktype + " data " + datatype + " section " + section + " pos 0x" + (filePos - 0xC).ToString("X"));
                            for (int i = 0xC; i < size; i++) //data header always size 0xC
                                readByte();//advance through the data chunk
                        }
                    }
                    else if (alt ? (chunktype == 1 && section == 2) : ((chunktype == 0 || chunktype == 1 || chunktype == 5) && section == 2))
                    {
                        //material entry - 0 is constant, 1 is lambert? 5 used in map chunks?
                        Material mat = new Material();
                        //ambient colour
                        float r = readFloat();
                        float g = readFloat();
                        float b = readFloat();
                        float a = readFloat();
                        mat.ambient = new Vector4(r, g, b, a);
                        //diffuse colour
                        mat.diffuse = new Vector4(readFloat(), readFloat(), readFloat(), readFloat());
                        //specular colour
                        mat.specular = new Vector4(readFloat(), readFloat(), readFloat(), readFloat());
                        //specular decay
                        mat.specfalloff = readFloat();
                        //number of textures?
                        uint texnum = readInt();
                        //empty?
                        filePos += 0xC8;
                        //read texture IDs
                        mat.textures = new List<int>();
                        for (int i = 0; i < texnum; i++)
                            mat.textures.Add((int)readInt()); //this is an ID referring to a texture entry?
                        materials.Add(mat);
                    }
                    else if (chunktype == 0 && section == 3)
                    {
                        //texture data
                        texswap.Add((int)readInt()); //texture page
                        readInt(); //width
                        readInt(); //height
                        filePos += (int)(size - 0x18);//bunch of spare space?
                    }
                    else
                    {
                        Debug.WriteLine("unimplemented chunk: chunk " + chunktype + " data " + datatype + " section " + section + " pos 0x" + (filePos - 0xC).ToString("X"));
                        filePos += (int)(size - 0xC);
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                //done reading!
                Debug.WriteLine("overread the model file - get this checked out?");
            }

            //now let's try and find the .ahi file!
            if (amh)
                filePos = ahioffset;
            else
            {
                filename = Path.GetDirectoryName(filename) + "/" + Path.GetFileNameWithoutExtension(filename) + ".ahi";
                if (File.Exists(filename))
                {
                    loadFile(filename);
                    amh = true;
                }
            }

            if (amh)
            {
                int startpos = filePos;
                //load up the heirarchy file
                readShort(); //1
                readShort();//0
                uint count = readInt(); //children
                uint filesize = readInt();
                for (int c = 0; c < count; c++) //can i just use the children count instead?
                {
                    uint chunktype = readInt(); //short data type
                    uint children = readInt();   //int, number of children
                    uint size = readInt();   //int, size of this chunk (including header)
                    if (chunktype == 0xC0000000)
                    {
                        //heirarchy tag, children is the total amount of tags in the file (excluding this), size is the filesize
                    }
                    else if (chunktype == 0x00000000)
                    {
                        //root list tag, children is the amount of root nodes, its data is the list
                        for (int i = 0; i < children; i++)
                            rootnodes.Add(readInt());
                    }
                    else if (chunktype == 0x40000001 || chunktype == 0x40000002 || chunktype == 0x40000003 || chunktype == 1 || chunktype == 2)
                    {
                        //node! let's party!
                        Node node = new Node();
                        //current node id, int
                        node.ID = (int)readInt();
                        //parent node id, int
                        node.ParentID = (int)readInt();
                        //child node id, int
                        node.ChildID = (int)readInt();
                        //unknown node id, int
                        node.unkID = (int)readInt();

                        //four floats, XYZ scale?
                        node.scale = new Vector3 { X = readFloat(), Y = readFloat(), Z = readFloat() };
                        float test = readFloat();
                        //four floats, XYZ rotation?
                        node.rotation = new Vector3 { X = readFloat(), Y = readFloat(), Z = readFloat() };
                        readFloat();
                        //four floats, XYZ position?
                        node.position = new Vector3 { X = readFloat(), Y = readFloat(), Z = readFloat() };
                        readFloat();
                        if (node.position.X == 0 && node.position.Y * node.scale.Y > 680 && node.position.Z * node.scale.Z > 30)
                            Debug.WriteLine("node found with position y " + (node.position.Y * node.scale.Y) + ", z " + (node.position.Z * node.scale.Z));

                        node.mesh = (int)readInt();
                        node.bone = (int)readInt();

                        nodes.Add(node);

                        filePos += 0xB8;

                        if (chunktype == 0x40000001)
                            Debug.WriteLine("Node");
                        else if (chunktype == 0x40000002)
                            Debug.WriteLine("Node mesh");
                        else if (chunktype == 0x40000003)
                            Debug.WriteLine("Node joint");
                    }
                    else
                    {
                        throw new Exception("other node datatype");
                    }
                }
            }
        }

        public void Export()
        { 
            string path = Directory.GetCurrentDirectory() + "/export/m_" + Path.GetFileNameWithoutExtension(filename) + "/";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            //model parsed, now let's save it out?
            using (StreamWriter sw = new StreamWriter(File.Create(path + Path.GetFileNameWithoutExtension(filename) + ".OBJ")))
            {
                sw.WriteLine("# MH model exporter");
                sw.WriteLine("mtllib " + Path.GetFileNameWithoutExtension(filename) + ".MTL");
                int verts = 1; //1 indexed
                int mats = 0;
                List<int> usedmats = new List<int>();
                if (amh && rootnodes.Count > 0 && false) //remove false when rigging is supported
                {
                    //go through the nodes, draw any meshes
                    for (int t = 0; t < rootnodes.Count; t++)
                    {
                        int n = (int)rootnodes[t];
                        Vector3 nodepos = new Vector3(0);
                        Vector3 noderot = new Vector3(0);
                        Vector3 nodescale = new Vector3(1);
                        while (true)
                        {
                            //loop through the node chain
                            /*if(nodes[n].rotation != new Vector3(0))
                            {
                                nodepos = rotatecoord(nodepos, nodes[n].rotation);
                            }*/
                            int m = nodes[n].mesh;
                            if (m != -1)
                            {
                                if (m >= meshes.Count)
                                {
                                    Debug.WriteLine("BIG OOPS");

                                }
                                else
                                {
                                    //copy the mesh into data
                                    sw.WriteLine("o mesh_" + n);

                                    //list the vertices
                                    sw.WriteLine("# verts");
                                    for (int v = 0; v < meshes[m].positions.Count; v++)
                                    {
                                        Vector3 vert = rotatecoord(meshes[m].positions[v], nodes[n].rotation);
                                        vert.X *= nodes[n].scale.X;
                                        vert.Y *= nodes[n].scale.Y;
                                        vert.Z *= nodes[n].scale.Z;
                                        vert.X += nodes[n].position.X;
                                        vert.Y += nodes[n].position.Y;
                                        vert.Z += nodes[n].position.Z;
                                        sw.WriteLine("v " + vert.X + " " + vert.Y + " " + vert.Z + " 1 " +
                                            meshes[m].colours[v].X + " " + meshes[m].colours[v].Y + " " + meshes[m].colours[v].Z);
                                    }
                                    sw.WriteLine("# uv coords");
                                    for (int v = 0; v < meshes[m].uvcoords.Count; v++)
                                        sw.WriteLine("vt " + meshes[m].uvcoords[v].X + " " + meshes[m].uvcoords[v].Y);
                                    sw.WriteLine("# normals");
                                    for (int v = 0; v < meshes[m].normals.Count; v++)
                                        sw.WriteLine("vn " + meshes[m].normals[v].X + " " + meshes[m].normals[v].Y + " " + meshes[m].normals[v].Z);
                                    sw.WriteLine("# faces");
                                    int lastmat = -1;
                                    for (int f = 0; f < meshes[m].faces.Count; f++)
                                    {
                                        if (meshes[m].stripstarts.Contains(f))
                                        {
                                            int mat = meshes[m].stripmats[meshes[m].stripstarts.IndexOf(f)] + mats;
                                            if (matswap.Count > 0)
                                                mat = matswap[mat];
                                            if (mat != lastmat)
                                            {
                                                sw.WriteLine("usemtl " + mat);
                                                lastmat = mat; //this should cut down on redundant declarations...
                                                if (!usedmats.Contains(mat))
                                                    usedmats.Add(mat);
                                            }
                                        }
                                        sw.WriteLine("f " + (meshes[m].faces[f].a + verts) + "/" + (meshes[m].faces[f].a + verts) + "/" + (meshes[m].faces[f].a + verts) +
                                            " " + (meshes[m].faces[f].b + verts) + "/" + (meshes[m].faces[f].b + verts) + "/" + (meshes[m].faces[f].b + verts) +
                                            " " + (meshes[m].faces[f].c + verts) + "/" + (meshes[m].faces[f].c + verts) + "/" + (meshes[m].faces[f].c + verts));
                                    }
                                    verts += meshes[m].positions.Count;
                                    mats += usedmats.Count;
                                    usedmats = new List<int>();
                                }
                            }
                            else //should be a nodejoint?
                            {
                                Debug.WriteLine("node group " + nodes[n].bone);
                            }

                            int next = -1;
                            if (nodes[n].ChildID != -1)
                                next = nodes[n].ChildID;
                            else
                            {
                                if (nodes[n].unkID != -1)
                                    next = nodes[n].unkID;
                                else
                                    while (nodes[n].ParentID != -1)
                                    {
                                        n = nodes[n].ParentID;
                                        if (nodes[n].unkID != -1)
                                            next = nodes[n].unkID;
                                    }
                            }
                            if (next == -1)
                                break;
                            n = next;
                            //children of the node have additive offsets to parent nodes? or no?
                        }
                    }
                }
                else
                {
                    for (int m = 0; m < meshes.Count; m++)
                    {
                        //group the mesh data together
                        sw.WriteLine("o mesh_" + m);
                        //list the vertices
                        sw.WriteLine("# verts");
                        for (int v = 0; v < meshes[m].positions.Count; v++)
                            sw.WriteLine("v " + meshes[m].positions[v].X + " " + meshes[m].positions[v].Y + " " + meshes[m].positions[v].Z + " 1 " +
                                meshes[m].colours[v].X + " " + meshes[m].colours[v].Y + " " + meshes[m].colours[v].Z);
                        sw.WriteLine("# uv coords");
                        for (int v = 0; v < meshes[m].uvcoords.Count; v++)
                            sw.WriteLine("vt " + meshes[m].uvcoords[v].X + " " + meshes[m].uvcoords[v].Y);
                        sw.WriteLine("# normals");
                        for (int v = 0; v < meshes[m].normals.Count; v++)
                            sw.WriteLine("vn " + meshes[m].normals[v].X + " " + meshes[m].normals[v].Y + " " + meshes[m].normals[v].Z);
                        sw.WriteLine("# faces");
                        int lastmat = -1;
                        for (int f = 0; f < meshes[m].faces.Count; f++)
                        {
                            if (meshes[m].stripstarts.Contains(f))
                            {
                                int mat = meshes[m].stripmats[meshes[m].stripstarts.IndexOf(f)] + mats;
                                if (matswap.Count > 0)
                                    mat = matswap[mat];
                                if (mat != lastmat)
                                {
                                    sw.WriteLine("usemtl " + mat);
                                    lastmat = mat; //this should cut down on redundant declarations...
                                    if (!usedmats.Contains(mat))
                                        usedmats.Add(mat);
                                }
                            }
                            sw.WriteLine("f " + (meshes[m].faces[f].a + verts) + "/" + (meshes[m].faces[f].a + verts) + "/" + (meshes[m].faces[f].a + verts) +
                                " " + (meshes[m].faces[f].b + verts) + "/" + (meshes[m].faces[f].b + verts) + "/" + (meshes[m].faces[f].b + verts) +
                                " " + (meshes[m].faces[f].c + verts) + "/" + (meshes[m].faces[f].c + verts) + "/" + (meshes[m].faces[f].c + verts));
                        }
                        verts += meshes[m].positions.Count;
                        mats += usedmats.Count;
                        usedmats = new List<int>();
                    }
                }
            }
            //now let's write the materials file!
            using (StreamWriter sw = new StreamWriter(File.Create(path + Path.GetFileNameWithoutExtension(filename) + ".MTL")))
            {
                for (int m = 0; m < materials.Count; m++)
                {
                    sw.WriteLine("newmtl " + m);
                    sw.WriteLine("Ka " + materials[m].ambient.X + " " + materials[m].ambient.Y + " " + materials[m].ambient.Z);
                    sw.WriteLine("Kd " + materials[m].diffuse.X + " " + materials[m].diffuse.Y + " " + materials[m].diffuse.Z);
                    //sw.WriteLine("Ks " + materials[m].specular.X + " " + materials[m].specular.Y + " " + materials[m].specular.Z); //too shiny!!! is this not specular?
                    sw.WriteLine("Ks 0 0 0");
                    sw.WriteLine("Ns " + materials[m].specfalloff);
                    int tex = materials[m].textures[0];
                    if (texswap.Count > 0)
                        tex = texswap[tex];
                    sw.WriteLine("map_Kd " + tex + ".png");
                }
            }
        }

        public struct Node
        {
            public int ID;
            public int ParentID;
            public int ChildID;
            public int unkID;
            public Vector3 scale;
            public Vector3 rotation;
            public Vector3 position;
            public int mesh;
            public int bone;
        }

        public Vector3 rotatecoord(Vector3 coord, Vector3 axes)
        {
            Vector3 result = new Vector3(0);
            //x axis rotation
            double sin = Math.Sin(axes.X);
            double cos = Math.Cos(axes.X);
            result.X = coord.X;
            result.Y = (float)(coord.Y * cos - coord.Z * sin);
            result.Z = (float)(coord.Y * sin + coord.Z * cos);
            //y axis rotation
            sin = Math.Sin(axes.Y);
            cos = Math.Cos(axes.Y);
            result.X = (float)(coord.X * cos + coord.Z * sin);
            result.Z = (float)(coord.Z * cos - coord.X * sin);
            //z axis rotation
            sin = Math.Sin(axes.Z);
            cos = Math.Cos(axes.Z);
            result.X = (float)(coord.X * cos - coord.Y * sin);
            result.Y = (float)(coord.X * sin + coord.Y * cos);
            return result;
        }

    }
}
