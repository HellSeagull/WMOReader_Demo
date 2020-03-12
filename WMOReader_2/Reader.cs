using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace WMOReader_2
{
    class Reader
    {

        Dictionary<string, long> DuplicatePositionOffset;

        private readonly string[] ChunkNames = new[] { "REVM", "OMOM", "DHOM", "XTOM", "TMOM", "VOUM", "NGOM",
                                                        "IGOM", "BSOM", "VPOM", "TPOM", "RPOM", "VVOM", "BVOM",
                                                        "TLOM", "SDOM", "NDOM", "DDOM", "GOFM", "PVCM", "DIFG"};

        private readonly string[] SubChunkNames = new[] { "REVM", "PGOM", "YPOM", "IVOM", "TVOM", "RNOM", "VTOM",
                                                        "ABOM", "RLOM", "RDOM", "NBOM", "RBOM", "VBPM", "PBPM",
                                                        "IBPM", "GBPM", "QILM", "IROM", "BROM", "2VTOM", "2VCOM"};

        private Dictionary<string, long> Offsets = new Dictionary<string, long>();

        public void lectureWMO(string path, byte[] buffer)
        {

            string modelName = Path.GetFileName(path);
            string[] array = modelName.Split('.');
            string subName = array[0];

            Console.WriteLine("WMO : " + subName);
            Console.WriteLine();
            Console.WriteLine();

            foreach (var chunk in ChunkNames)
            {
                if (Encoding.UTF8.GetString(buffer).Contains(chunk))
                {
                    long offset = SearchPattern(buffer, Encoding.UTF8.GetBytes(chunk));
                    Offsets.Add(chunk, offset);
                }
            }

            DuplicatePositionOffset = Offsets;

            foreach (KeyValuePair<string, long> entry in Offsets)
            {
                Console.WriteLine("CHUNK: " + entry.Key + " at Offset " + entry.Value);
            }

            ReadDHOM(path);
            Console.WriteLine();
            Console.WriteLine();
            ReadDIFG(path);
            Console.WriteLine();
            Console.WriteLine();
            if (DuplicatePositionOffset.ContainsKey("XTOM"))
                ReadXTOM(path);
            else
                if (DuplicatePositionOffset.ContainsKey("TMOM"))
                    ReadTMOM(path);
        }

        protected void ReadDHOM(string path)
        {

            Console.WriteLine();
            Console.WriteLine("Lecture DHOM");

            foreach (KeyValuePair<string, long> entry in Offsets)
            {
                if (entry.Key.Equals("DHOM"))
                {

                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    using (var br = new BinaryReader(fs))
                    {

                        br.BaseStream.Seek(entry.Value + 4, SeekOrigin.Current);
                        UInt32 CHUNKSize = br.ReadUInt32() / 4;
                        Console.Write("Nombre de groupe d'éléments: " + CHUNKSize);
                        Console.WriteLine();

                        long positionStream = br.BaseStream.Position;

                        Console.WriteLine("Nb Texture: " + br.ReadUInt32());
                        Console.WriteLine("Nb Groupe: " + br.ReadUInt32());
                        Console.WriteLine("Nb Portals: " + br.ReadUInt32());
                        Console.WriteLine("Nb Light: " + br.ReadUInt32());
                        Console.WriteLine("Nb Models: " + br.ReadUInt32());
                        Console.WriteLine("Nb Doodads: " + br.ReadUInt32());
                        Console.WriteLine("Nb Sets: " + br.ReadUInt32());
                        Console.WriteLine("Ambient Color: ");
                        Console.Write("Rouge: " + br.ReadByte() + ", ");
                        Console.Write("Vert: " + br.ReadByte() + ", ");
                        Console.Write("Bleu: " + br.ReadByte() + ", ");
                        Console.Write("Opacité: " + br.ReadByte());
                        Console.WriteLine();
                        Console.WriteLine("WMO ID (WMO Area Table): " + br.ReadUInt32());

                    }

                }

            }
        }

        protected void ReadDIFG(string path)
        {

            Console.WriteLine();
            Console.WriteLine("Lecture DIFG");

            foreach (KeyValuePair<string, long> entry in Offsets)
            {
                if (entry.Key.Equals("DIFG"))
                {
                    List<UInt32> LODFileDataID = new List<UInt32>();

                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    using (var br = new BinaryReader(fs))
                    {

                        br.BaseStream.Seek(entry.Value + 4, SeekOrigin.Current);
                        UInt32 CHUNKSize = br.ReadUInt32() / 4;
                        Console.WriteLine("DIFG Size : " + CHUNKSize);

                        long EndPose = entry.Value + 8 + CHUNKSize * 4;

                        do
                        {
                            if (br.BaseStream.Position <= EndPose - 4)
                            {
                                UInt32 LOD = br.ReadUInt32();
                                LODFileDataID.Add(LOD);
                            }

                        } while (br.BaseStream.Position <= EndPose - 4);
                    }

                    for (UInt16 i = 0; i < LODFileDataID.Count; i++)
                    {
                        Console.WriteLine("LODFileDataID " + i + ": " + LODFileDataID.ElementAt(i));
                    }

                    Console.WriteLine();
                    Console.WriteLine();

                    List<string> found = new List<string>();

                    string line = string.Empty;

                    List<string> LodFilesLinked = new List<string>();

                    foreach (var lod in LODFileDataID)
                    {
                        string html = string.Empty;
                        string url = @"https://wow.tools/files/scripts/filedata_api.php?filedataid=" + lod;

                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                        try
                        {
                            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                            using (Stream stream = response.GetResponseStream())
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                html = reader.ReadToEnd();
                                html = html.Substring(html.IndexOf("Filename"), (html.IndexOf("Lookup") - html.IndexOf("Filename")));
                                html = html.Replace("Filename", "");
                                html = html.Replace("<td>", "").Replace("</td>", "").Replace("<tr>", "").Replace("</tr>", "");
                                LodFilesLinked.Add(html);

                            }
                        }
                        catch
                        {
                            Console.WriteLine("Error connecting, relaunch the app or try again later to have the lod names");
                        }
                    }

                    for (UInt16 i = 0; i < LodFilesLinked.Count; i++)
                    {
                        Console.WriteLine("Fichier Lod lié " + i + ": " + LodFilesLinked.ElementAt(i));
                    }

                }

            }
        }

        protected void ReadXTOM(string path)
        {

            Console.WriteLine();
            Console.WriteLine("Lecture XTOM");

            foreach (KeyValuePair<string, long> entry in Offsets)
            {
                if (entry.Key.Equals("XTOM"))
                {

                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    using (var br = new BinaryReader(fs))
                    {

                        br.BaseStream.Seek(entry.Value + 4, SeekOrigin.Current);
                        long positionStream = br.BaseStream.Position;
                        br.ReadUInt32();

                        List<byte> data = new List<byte>();

                        long offsetDebut = 0;
                        long offsetFin = 0;

                        for (int i = 0; i < DuplicatePositionOffset.Keys.Count(); i++)
                        {
                            KeyValuePair<string, long> t = DuplicatePositionOffset.ElementAt(i);
                            if (t.Key == "XTOM")
                                offsetDebut = t.Value;
                            else
                            {
                                if (offsetDebut != 0 && t.Value > offsetDebut)
                                {
                                    offsetFin = t.Value - 1;
                                    break;
                                }
                            }
                        }

                        while (br.BaseStream.Position < offsetFin)
                        {
                            byte b = br.ReadByte();
                            if (b != 0)
                            {
                                data.Add(b);
                            }
                        }

                        string result = "";
                        byte[] tex = new byte[data.Count()];
                        for(int i = 0; i < tex.Length; i++)
                        {
                            tex[i] = data[i];
                        }

                        result = Encoding.UTF8.GetString(tex);
                        foreach(string s in result.Split(new[] { ".blp" }, StringSplitOptions.None))
                        {
                            Console.WriteLine("Texture: " + s + ".blp");
                        }
                        Console.WriteLine();
                    }

                }

            }
        }

        protected void ReadTMOM(string path)
        {

            Console.WriteLine();
            Console.WriteLine("Lecture TMOM");

            foreach (KeyValuePair<string, long> entry in Offsets)
            {
                if (entry.Key.Equals("TMOM"))
                {

                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                    using (var br = new BinaryReader(fs))
                    {

                        br.BaseStream.Seek(entry.Value + 4, SeekOrigin.Current);
                        long positionStream = br.BaseStream.Position;
                        br.ReadUInt32();

                        List<Int32> data = new List<Int32>();

                        long offsetDebut = 0;
                        long offsetFin = 0;

                        for(int i = 0; i < DuplicatePositionOffset.Keys.Count(); i++)
                        {
                            KeyValuePair<string, long> t = DuplicatePositionOffset.ElementAt(i);
                            if (t.Key == "TMOM")
                                offsetDebut = t.Value;
                            else
                            {
                                if(offsetDebut != 0 && t.Value > offsetDebut)
                                {
                                    offsetFin = t.Value - 1;
                                    break;
                                }
                            }
                        }

                        while (br.BaseStream.Position < offsetFin)
                        {
                            Int32 b = br.ReadInt32();
                            if (b > 0)
                            {
                                data.Add(b);
                            }
                        }

                        List<string> textureFiles = new List<string>();

                        foreach (var i in data)
                        {
                            string html = string.Empty;
                            string url = @"https://wow.tools/files/scripts/filedata_api.php?filedataid=" + i;

                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                            try
                            {
                                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                                using (Stream stream = response.GetResponseStream())
                                using (StreamReader reader = new StreamReader(stream))
                                {
                                    html = reader.ReadToEnd();
                                    if (!html.ToLowerInvariant().Contains("could not find"))
                                    {
                                        html = html.Substring(html.IndexOf("Filename"), (html.IndexOf("Lookup") - html.IndexOf("Filename")));
                                        html = html.Replace("Filename", "");
                                        html = html.Replace("<td>", "").Replace("</td>", "").Replace("<tr>", "").Replace("</tr>", "");
                                        textureFiles.Add(html + "|" + i);
                                    }

                                }
                            }
                            catch
                            {
                                Console.WriteLine("Error connecting, relaunch the app or try again later to have the lod names");
                            }
                        }

                        for (UInt16 i = 0; i < textureFiles.Count; i++)
                        {
                            Console.WriteLine("Texture FileDataID: " + textureFiles[i].Split('|')[1] + " FileName: " + textureFiles[i].Split('|')[0]);
                        }

                        Console.WriteLine();
                    }

                }

            }
        }

        private unsafe long SearchPattern(byte[] haystack, byte[] needle)
        {
            fixed (byte* h = haystack) fixed (byte* n = needle)
            {
                for (byte* hNext = h, hEnd = h + haystack.Length + 1 - needle.Length, nEnd = n + needle.Length; hNext < hEnd; hNext++)
                    for (byte* hInc = hNext, nInc = n; *nInc == *hInc; hInc++)
                        if (++nInc == nEnd)
                            return hNext - h;
                return -1;
            }
        }
    }
}
