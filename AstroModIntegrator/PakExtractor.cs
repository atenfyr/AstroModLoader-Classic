using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UAssetAPI;

namespace AstroModIntegrator
{
    public enum CompressionMethod
    {
        NONE,
        ZLIB,
        GZIP,
        CUSTOM // Also "Oodle"
    }

    [Flags]
    public enum RecordFlags : uint
    {
        Flag_None = 0x00,
        Flag_Encrypted = 0x01,
        Flag_Deleted = 0x02
    }

    public enum PakVersion
    {
        PakFile_Version_Initial = 1,
        PakFile_Version_NoTimestamps = 2,
        PakFile_Version_CompressionEncryption = 3,
        PakFile_Version_IndexEncryption = 4,
        PakFile_Version_RelativeChunkOffsets = 5,
        PakFile_Version_DeleteRecords = 6,
        PakFile_Version_EncryptionKeyGuid = 7,
        PakFile_Version_FNameBasedCompressionMethod = 8,
        PakFile_Version_FrozenIndex = 9,
        PakFile_Version_PathHashIndex = 10,
        PakFile_Version_Fnv64BugFix = 11,


        PakFile_Version_Last,
        PakFile_Version_Invalid,
        PakFile_Version_Latest = PakFile_Version_Last - 1
    }

    public struct Block
    {
        public long Start;
        public long Size;

        public Block(long start, long size)
        {
            Start = start;
            Size = size;
        }
    }

    public class Record
    {
        public string fileName;
        public long offset;
        public long fileSize;
        public long sizeDecompressed;
        public CompressionMethod compressionMethod;
        public RecordFlags Flags;
        public uint compressionBlockSize;
        public List<Block> compressionBlocks;
        public byte[] dataHash;

        public void Read(BinaryReader reader, PakVersion fileVersion, bool includesHeader)
        {
            if (includesHeader) fileName = reader.ReadUString();
            offset = reader.ReadInt64();
            fileSize = reader.ReadInt64();
            sizeDecompressed = reader.ReadInt64();
            compressionMethod = (CompressionMethod)reader.ReadUInt32();

            if (fileVersion <= PakVersion.PakFile_Version_Initial)
            {
                ulong timestamp = reader.ReadUInt64();
            }

            dataHash = reader.ReadBytes(20); // sha1 hash

            if (fileVersion >= PakVersion.PakFile_Version_CompressionEncryption)
            {
                if (compressionMethod != CompressionMethod.NONE)
                {
                    compressionBlocks = new List<Block>();
                    uint blockCount = reader.ReadUInt32();
                    for (int j = 0; j < blockCount; j++)
                    {
                        long startOffset = reader.ReadInt64();
                        long endOffset = reader.ReadInt64();
                        compressionBlocks.Add(new Block(startOffset, endOffset - startOffset));
                    }
                }

                Flags = (RecordFlags)reader.ReadByte();
                compressionBlockSize = reader.ReadUInt32(); // max size of each block
            }
        }

        public void Write(BinaryWriter writer, byte[] data, bool includesHeader, bool autoAdjustBlocks, List<Block> blockOffsets = null, byte[] compressedData = null) // fileVersion is 4
        {
            if (autoAdjustBlocks)
            {
                fileSize = compressedData.Length;
                sizeDecompressed = data.Length;
                compressionMethod = CompressionMethod.ZLIB;
                dataHash = new SHA1Managed().ComputeHash(compressedData);
            }

            if (includesHeader) writer.WriteUString(fileName);
            writer.Write(offset);
            writer.Write(fileSize); // normal size
            writer.Write(sizeDecompressed); // decompressed size
            writer.Write((int)compressionMethod);

            writer.Write(dataHash);
            long blockOffsetWritingStart = 0;
            if (autoAdjustBlocks)
            {
                writer.Write(blockOffsets.Count);
                blockOffsetWritingStart = writer.BaseStream.Position;
                foreach (Block b in blockOffsets)
                {
                    writer.Write(b.Start);
                    writer.Write(b.Start + b.Size);
                }
            }
            else
            {
                writer.Write(compressionBlocks.Count);
                foreach (Block b in compressionBlocks)
                {
                    writer.Write(b.Start);
                    writer.Write(b.Start + b.Size);
                }
            }
            writer.Write((byte)0); // not encrypted
            writer.Write((int)sizeDecompressed);

            if (autoAdjustBlocks)
            {
                long endOffset = writer.BaseStream.Position;
                writer.Seek((int)blockOffsetWritingStart, SeekOrigin.Begin);

                compressionBlocks = new List<Block>();
                for (int i = 0; i < blockOffsets.Count; i++)
                {
                    long newStart = endOffset + blockOffsets[i].Start;
                    long newEnd = endOffset + blockOffsets[i].Start + blockOffsets[i].Size;
                    writer.Write(newStart);
                    writer.Write(newEnd);
                    compressionBlocks.Add(new Block(newStart, newEnd - newStart));
                }

                writer.Seek((int)endOffset, SeekOrigin.Begin);
            }
        }

        public Record()
        {

        }
    }

    public class MalformattedFileException : FormatException
    {
        public MalformattedFileException(string exText) : base(exText) { }
    }

    public class InvalidFileTypeException : IOException
    {
        public InvalidFileTypeException(string txt) : base(txt)
        {

        }
    }

    public class PakExtractor : IDisposable
    {
        internal static uint UE4_PAK_MAGIC = 0x5A6F12E1;
        private PakVersion fileVersion;
        private BinaryReader reader;
        private Stream internal_stream;

        public Dictionary<string, long> PathToOffset;
        public bool RepakBacked = false;
        private PakReader pakReader;

        public string MountPoint;

        public PakExtractor(Stream stream)
        {
            internal_stream = stream;
            this.reader = new BinaryReader(internal_stream);
            BuildDict();
        }

        public void Dispose()
        {
            pakReader?.Dispose();
            reader?.Dispose();
        }

        private void BuildDict()
        {
            if (RepakBacked) return;

            PathToOffset = new Dictionary<string, long>();

            reader.BaseStream.Seek(-44, SeekOrigin.End); // First we head straight to the footer

            uint magic = reader.ReadUInt32();
            if (magic != UE4_PAK_MAGIC) // Magic number
            {
                reader.BaseStream.Seek(-204, SeekOrigin.End);
                magic = reader.ReadUInt32();
                if (magic != UE4_PAK_MAGIC) throw new InvalidFileTypeException("Invalid file format, magic = " + magic);
            }

            fileVersion = (PakVersion)reader.ReadUInt32();
            if (fileVersion > (PakVersion)5)
            {
                // use repak
                RepakBacked = true;
                // discard binary reader, but don't dispose because we need to keep the stream open
                reader = null;

                internal_stream.Seek(0, SeekOrigin.Begin);
                pakReader = new PakBuilder().Reader(internal_stream);

                MountPoint = pakReader.GetMountPoint();
                return;
            }

            ulong indexOffset = reader.ReadUInt64();
            ulong indexSize = reader.ReadUInt64();

            // First we read the first file record to see if everything is OK
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            var firstRec = new Record();
            firstRec.Read(reader, fileVersion, false);
            if (firstRec.Flags.HasFlag(RecordFlags.Flag_Encrypted)) throw new NotImplementedException("Encryption is not supported");

            // Start reading the proper index
            reader.BaseStream.Seek((long)indexOffset, SeekOrigin.Begin);
            MountPoint = reader.ReadUString();
            int recordCount = reader.ReadInt32();

            for (int i = 0; i < recordCount; i++)
            {
                var rec = new Record();
                rec.Read(reader, fileVersion, true);
                PathToOffset.Add(rec.fileName, (long)rec.offset);
            }
        }

        public IReadOnlyList<string> GetAllPaths()
        {
            if (RepakBacked) return pakReader.Files().ToList().AsReadOnly();
            return new List<string>(PathToOffset.Keys).AsReadOnly();
        }

        public bool HasPath(string searchPath)
        {
            if (RepakBacked) return this.GetAllPaths().Contains(searchPath);
            return PathToOffset.ContainsKey(searchPath);
        }

        public byte[] ReadRaw(string searchPath, bool verifyChecksums = false)
        {
            if (!HasPath(searchPath)) return Array.Empty<byte>();
            if (RepakBacked) return pakReader.Get(internal_stream, searchPath);
            long fullOffset = PathToOffset[searchPath];
            return ReadRaw(fullOffset, verifyChecksums);
        }

        private byte[] ReadRaw(long fullOffset, bool verifyChecksums = false)
        {
            if (RepakBacked) throw new InvalidOperationException("Cannot pass offset into ReadRaw when RepakBacked");
            reader.BaseStream.Seek(fullOffset, SeekOrigin.Begin);
            var rec2 = new Record();
            rec2.Read(reader, fileVersion, false);

            switch (rec2.compressionMethod)
            {
                case CompressionMethod.NONE:
                    return reader.ReadBytes((int)rec2.fileSize);
                case CompressionMethod.ZLIB:
                    MemoryStream fullStream = new MemoryStream();
                    foreach (Block block in rec2.compressionBlocks)
                    {
                        long blockOffset = block.Start;
                        long blockSize = block.Size;
                        if (fileVersion >= PakVersion.PakFile_Version_RelativeChunkOffsets) // Relative offset
                        {
                            reader.BaseStream.Seek((long)blockOffset + fullOffset, SeekOrigin.Begin);
                        }
                        else // Absolute offset
                        {
                            reader.BaseStream.Seek((long)blockOffset, SeekOrigin.Begin);
                        }

                        byte[] thisRawBlockData = reader.ReadBytes((int)blockSize);
                        byte[] thisBlockData = new byte[thisRawBlockData.Length - 4];
                        byte[] blockRawChecksum = new byte[4];
                        Array.Copy(thisRawBlockData, 0, thisBlockData, 0, thisBlockData.Length);
                        Array.Copy(thisRawBlockData, thisRawBlockData.Length - blockRawChecksum.Length, blockRawChecksum, 0, blockRawChecksum.Length);
                        Array.Reverse(blockRawChecksum); // Read the hash in reverse, zlib checksums are stored as big endian
                        uint blockChecksum = BitConverter.ToUInt32(blockRawChecksum, 0);

                        var memStream = new MemoryStream(thisBlockData);

                        int CMF = memStream.ReadByte();
                        int CM = CMF & 15;
                        int CINFO = (CMF & 240) >> 4;
                        int FLG = memStream.ReadByte();
                        int FCHECK = FLG & 31;
                        bool FDICT = (FLG & 32) >> 5 == 1;
                        int FLEVEL = (FLG & 192) >> 6;

                        if (CM != 8 || CINFO > 7 || (CMF * 256 + FLG) % 31 != 0) throw new MalformattedFileException("Invalid zlib header: " + BitConverter.ToString(new byte[2] { (byte)CMF, (byte)FLG }));
                        if (FDICT) throw new NotImplementedException("Preset dictionary is not supported");

                        if (verifyChecksums)
                        {
                            var decompressedBlockStream = new MemoryStream((int)blockSize * 2);
                            decompressedBlockStream.Seek(0, SeekOrigin.Begin);

                            using (DeflateStream decompressionStream = new DeflateStream(memStream, CompressionMode.Decompress))
                            {
                                decompressionStream.CopyTo(decompressedBlockStream);
                            }

                            decompressedBlockStream.Seek(0, SeekOrigin.Begin);
                            fullStream.Seek(0, SeekOrigin.End);
                            decompressedBlockStream.CopyTo(fullStream);
                            decompressedBlockStream.Seek(0, SeekOrigin.Begin);
                            uint calculatedChecksum = PakBaker.Adler32(new BinaryReader(decompressedBlockStream));
                            if (calculatedChecksum != blockChecksum) throw new MalformattedFileException("Checksum check failed; compression block likely corrupted");
                        }
                        else
                        {
                            using (DeflateStream decompressionStream = new DeflateStream(memStream, CompressionMode.Decompress))
                            {
                                fullStream.Seek(0, SeekOrigin.End);
                                decompressionStream.CopyTo(fullStream);
                            }
                        }
                    }

                    return fullStream.ToArray();
                default:
                    throw new NotImplementedException("Unimplemented compression method " + rec2.compressionMethod);
            }
        }

        private static Metadata ParseMetadata(string data)
        {
            JObject jobj = JObject.Parse(data);
            int schemaVersion = jobj.ContainsKey("schema_version") ? (int)jobj["schema_version"] : 1;
            switch (schemaVersion)
            {
                case 1: // legacy
                    return JsonConvert.DeserializeObject<MetadataSchema1>(data).Convert();
                case 2:
                    return JsonConvert.DeserializeObject<Metadata>(data);
                default:
                    throw new NotImplementedException("Unimplemented schema version " + schemaVersion);
            }
        }

        public Metadata ReadMetadata()
        {
            string data = Encoding.UTF8.GetString(ReadRaw("metadata.json"));
            if (string.IsNullOrEmpty(data)) return null;
            try
            {
                return ParseMetadata(data);
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }
    }
}