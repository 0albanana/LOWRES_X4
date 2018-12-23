using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.IO.Compression;

namespace LOWRES_X4
{
    internal class CatIndex
    {
        public class Res
        {
            public int Count;
            public long RemovedBytes;
        }

        public void Load(string pathCat, string pathDat)
        {
            _pathCat = pathCat;
            _pathDat = pathDat;

            if (!new FileInfo(pathCat).Exists)
                throw new FileNotFoundException(".cat file");

            if (!new FileInfo(pathDat).Exists)
                throw new FileNotFoundException(".dat file");

            List<string> catLines;

            using (FileStream fs = new FileStream(pathCat, FileMode.Open, FileAccess.Read, FileShare.None))
            using (var reader = new StreamReader(fs, Encoding.UTF8))
            {
                catLines = reader.ReadToEnd().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (catLines.Count == 0)
                    throw new EndOfStreamException(".cat file empty");
            }

            _entries = ParseCat(catLines);
            if (_entries.Count == 0)
                throw new EndOfStreamException("could not parse .cat file");

            if (!pathDat.EndsWith("_sig.dat"))
                _datStream = new FileStream(pathDat, FileMode.Open, FileAccess.Read, FileShare.None);
        }

        public void Close(bool updateFiles = true)
        {
            try
            {
                if (updateFiles && _updateCat)
                {
                    using (FileStream fs = new FileStream(_pathCat, FileMode.Truncate, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(fs, Encoding.UTF8))
                    {
                        foreach (var e in _entries)
                        {
                            if (e.Data != null)
                                writer.Write(string.Format("{0} {1} {2} {3}\n", e.Path, e.Data.Length, e.TimeStamp, GetChecksum(e)));
                            else
                                writer.Write(string.Format("{0} {1} {2} {3}\n", e.Path, e.OrigSize, e.TimeStamp, e.ChkSum));
                        }
                    }
                }

                if (updateFiles && _updateDat && _datStream != null)
                {
                    using (FileStream fs = new FileStream(_pathDat + "_new", FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new BinaryWriter(fs))
                    {
                        long pos = 0;
                        foreach (var e in _entries)
                        {
                            if (e.Data != null)
                                writer.Write(e.Data);
                            else
                            {
                                _datStream.Seek(pos, SeekOrigin.Begin);
                                var oldDat = new byte[e.OrigSize];
                                _datStream.Read(oldDat, 0, (int)e.OrigSize);
                                writer.Write(oldDat);
                            }

                            pos += e.OrigSize;
                        }
                    }

                    _datStream.Close();
                    _datStream = null;

                    File.Delete(_pathDat);
                    File.Move(_pathDat + "_new", _pathDat);
                }
            }
            finally
            {
                if (_datStream != null)
                    _datStream.Close();
            }
        }

        public Res ProcessMeshes(LODLevels levels)
        {
            var res = new Res();
            if (DoRenameLOD(levels, res))
                _updateCat = true;

            return res;
        }

        public Res ProcessTextures(TextureLevels levels)
        {
            long currentPos = 0;
            var res = new Res();

            foreach (var e in _entries)
            {
                var cat = TextureEntry.GetCategory(e.Path);
                int qlevel = 0;

                switch (cat)
                {
                    case TextureEntry.TECategory.Unset:
                        currentPos += e.OrigSize;
                        continue;
                    case TextureEntry.TECategory.Fonts:
                        qlevel = levels.LvlFonts;
                        break;
                    case TextureEntry.TECategory.GUI:
                        qlevel = levels.LvlGUI;
                        break;
                    case TextureEntry.TECategory.NPCs:
                        qlevel = levels.LvlNPCs;
                        break;
                    case TextureEntry.TECategory.FX:
                        qlevel = levels.LvlFX;
                        break;
                    case TextureEntry.TECategory.Environments:
                        qlevel = levels.LvlEnvironments;
                        break;
                    case TextureEntry.TECategory.StationExteriors:
                        qlevel = levels.LvlStationExteriors;
                        break;
                    case TextureEntry.TECategory.StationInteriors:
                        qlevel = levels.LvlStationInteriors;
                        break;
                    case TextureEntry.TECategory.Ships:
                        qlevel = levels.LvlShips;
                        break;
                    case TextureEntry.TECategory.Misc:
                        qlevel = levels.LvlMisc;
                        break;
                }

                if (e.OrigSize >= levels.MinTextureSize)
                {
                    if (DoReduceDDS(e, cat, currentPos, qlevel, res))
                    {
                        _updateCat = true;
                        _updateDat = true;
                    }
                }

                currentPos += e.OrigSize;
            }

            return res;
        }
        
        private bool DoReduceDDS(CatEntry e, TextureEntry.TECategory cat, long pos, int levels, Res res)
        {
            var oldData = new byte[e.OrigSize];

            _datStream.Seek(pos, SeekOrigin.Begin);
            _datStream.Read(oldData, 0, oldData.Length);

            bool replaced = false;
            var h = new GCHandle();

            try
            {
                if (e.Compressed)
                    oldData = Decompress(oldData);
                
                h = GCHandle.Alloc(oldData, GCHandleType.Pinned);
                var img = DirectXTexNet.TexHelper.Instance.LoadFromDDSMemory(
                    h.AddrOfPinnedObject(),
                    oldData.Length,
                    DirectXTexNet.DDS_FLAGS.NONE);

                var imgCount = img.GetImageCount();
                if (imgCount > 1)
                {
                    for (int i = 0; i < Math.Min(imgCount - 1, levels); ++i)
                        img.FreeFirstImage();

                    var ufs = img.SaveToDDSMemory(DirectXTexNet.DDS_FLAGS.NONE);
                    var newData = new byte[ufs.Length];
                    ufs.Read(newData, 0, newData.Length);

                    res.RemovedBytes += oldData.LongLength - newData.LongLength;
                    ++res.Count;
                    replaced = true;

                    e.Data = e.Compressed ? Compress(newData) : newData;
                }
            }
            catch (Exception ex)
            {
                throw new IOException(string.Format("Category: {0}, Path: {1}", cat, e.Path), ex);
            }
            finally
            {
                if (h.IsAllocated) 
                    h.Free();
            }

            return replaced;
        }

        private List<CatEntry> ParseCat(List<string> lines)
        {
            var sep = new char[]{' '};

            var list = new List<CatEntry>(lines.Count);
            foreach (var l in lines)
            {
                var entry = new CatEntry();

                var sl = l.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                if (sl.Length != 4)
                {
                    entry.ChkSum = sl[sl.Length - 1];
                    entry.TimeStamp = sl[sl.Length - 2];
                    entry.OrigSize = long.Parse(sl[sl.Length - 3]); // let exception propagate

                    entry.Path = sl[0];
                    for (int i = 1; i < sl.Length - 3; ++i)
                        entry.Path += string.Format(" {0}", sl[i]);
                }
                else
                {
                    entry.Path = sl[0];
                    entry.OrigSize = long.Parse(sl[1]); // let exception propagate
                    entry.TimeStamp = sl[2];
                    entry.ChkSum = sl[3];
                }

                entry.Compressed = entry.Path.EndsWith(".gz");
                list.Add(entry);
            }

            return list;
        }

        private bool DoRenameLOD(LODLevels levels, Res res)
        {
            var lodEntries = new List<LodEntry>();

            var sdata = new LodEntry();
            string sbase = string.Empty;
            int strIndex = 0;

            for (int i = 0; i < _entries.Count; ++i)
            {
                var line = _entries[i].Path;

                if (sdata.Lod0Entry != null &&
                    sdata.Lod1Entry != null && // at least LOD0 and LOD1 files have to be available
                    !line.StartsWith(sbase))
                {
                    lodEntries.Add(sdata);
                    sdata = new LodEntry();
                    sbase = string.Empty;
                }

                if ((strIndex = line.IndexOf("lod0.xmf")) != -1)
                {
                    sdata.Category = LodEntry.GetCategory(line);
                    sbase = line.Substring(0, strIndex);
                    sdata.Lod0Entry = _entries[i];
                    sdata.Lod0StrIdx = strIndex;
                }
                else if ((strIndex = line.IndexOf("lod1.xmf")) != -1)
                {
                    sdata.Lod1Entry = _entries[i];
                    sdata.Lod1StrIdx = strIndex;
                }
                else if ((strIndex = line.IndexOf("lod2.xmf")) != -1)
                {
                    sdata.Lod2Entry = _entries[i];
                    sdata.Lod2StrIdx = strIndex;
                }
                else if ((strIndex = line.IndexOf("lod3.xmf")) != -1)
                {
                    sdata.Lod3Entry = _entries[i];
                    sdata.Lod3StrIdx = strIndex;
                }
            }

            if (lodEntries.Count == 0)
                return false;

            int count = 0;
            foreach (var le in lodEntries)
            {
                int qlevel = 0;
                switch (le.Category)
                {
                    case LodEntry.LECategory.Unset:
                        continue;
                    case LodEntry.LECategory.Collectables:
                        qlevel = levels.LvlCollectables;
                        break;
                    case LodEntry.LECategory.Environment:
                        qlevel = levels.LvlEnvironment;
                        break;
                    case LodEntry.LECategory.ShipExteriors:
                        qlevel = levels.LvlShipExteriors;
                        break;
                    case LodEntry.LECategory.ShipInteriors:
                        qlevel = levels.LvlShipInteriors;
                        break;
                    case LodEntry.LECategory.StationExteriors:
                        qlevel = levels.LvlStationExteriors;
                        break;
                    case LodEntry.LECategory.StationInteriors:
                        qlevel = levels.LvlStationInteriors;
                        break;
                }

                // step up quality if LOD entry not available
                if (qlevel == 3 && le.Lod3Entry == null) qlevel = 2;
                if (qlevel == 2 && le.Lod2Entry == null) qlevel = 1;
                if (qlevel == 1 && le.Lod1Entry == null) continue;

                le.Lod0Entry.Path = ReplaceString(le.Lod0Entry.Path, le.Lod0StrIdx, "_REP_L0_");

                res.RemovedBytes += le.Lod0Entry.OrigSize;
                ++count;
                
                if (qlevel == 3)
                {
                    le.Lod3Entry.Path = ReplaceString(le.Lod3Entry.Path, le.Lod3StrIdx, "lod0.xmf");
                    le.Lod1Entry.Path = ReplaceString(le.Lod1Entry.Path, le.Lod1StrIdx, "_REP_L1_");

                    res.RemovedBytes += le.Lod1Entry.OrigSize;
                    count += 2;

                    if (le.Lod2Entry != null)
                    {
                        le.Lod2Entry.Path = ReplaceString(le.Lod2Entry.Path, le.Lod2StrIdx, "_REP_L2_");
                        res.RemovedBytes += le.Lod2Entry.OrigSize;
                        ++count;
                    }
                }
                else if (qlevel == 2)
                {
                    le.Lod2Entry.Path = ReplaceString(le.Lod2Entry.Path, le.Lod2StrIdx, "lod0.xmf");
                    le.Lod1Entry.Path = ReplaceString(le.Lod1Entry.Path, le.Lod1StrIdx, "_REP_L1_");

                    res.RemovedBytes += le.Lod1Entry.OrigSize;
                    count += 2;

                    if (le.Lod3Entry != null)
                    {
                        le.Lod3Entry.Path = ReplaceString(le.Lod3Entry.Path, le.Lod3StrIdx, "lod1.xmf");
                        ++count;
                    }
                }
                else if (qlevel == 1)
                {
                    le.Lod1Entry.Path = ReplaceString(le.Lod1Entry.Path, le.Lod1StrIdx, "lod0.xmf");
                    ++count;

                    if (le.Lod2Entry != null)
                    {
                        le.Lod2Entry.Path = ReplaceString(le.Lod2Entry.Path, le.Lod2StrIdx, "lod1.xmf");
                        ++count;
                    }

                    if (le.Lod3Entry != null)
                    {
                        le.Lod3Entry.Path = ReplaceString(le.Lod3Entry.Path, le.Lod3StrIdx, "lod2.xmf");
                        ++count;
                    }
                }
            }

            res.Count += count;

            return count > 0;
        }

        static private string ReplaceString(string line, int idx, string rep)
        {
            var lineC = line.ToCharArray();
            for (int i = 0; i < Math.Min(rep.Length, line.Length - idx); ++i)
                lineC[idx + i] = rep[i];

            return new string(lineC);
        }

        static private byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (var stream = new GZipStream(output, CompressionLevel.Optimal))
                stream.Write(data, 0, data.Length);

            return output.ToArray();
        }

        static private byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (var stream = new GZipStream(input, CompressionMode.Decompress))
                stream.CopyTo(output);

            return output.ToArray();
        }

        static private string GetChecksum(CatEntry e)
        {
            // TODO: Actually compute checksum (if that is what that field contains)
            // Currently X4 seems to ignore this value, so we just return the current one
            return e.ChkSum;
        }

        private List<CatEntry> _entries;
        private FileStream _datStream;
        private bool _updateCat = false;
        private bool _updateDat = false;
        private string _pathCat, _pathDat;
    }
}

