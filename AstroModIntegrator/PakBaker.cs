using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UAssetAPI;

namespace AstroModIntegrator
{
    public static class PakBaker
    {
        internal static uint Adler32(byte[] data)
        {
            const int mod = 65521;
            uint a = 1, b = 0;
            foreach (byte c in data)
            {
                a = (a + c) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }

        internal static uint Adler32(BinaryReader data)
        {
            data.BaseStream.Seek(0, SeekOrigin.Begin);

            const int mod = 65521;
            uint a = 1, b = 0;
            while (data.BaseStream.Position != data.BaseStream.Length)
            {
                byte c = data.ReadByte();
                a = (a + c) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }

        private static byte[] CompressBuffer(byte[] data)
        {
            using (var compressStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(compressStream, CompressionMode.Compress))
                {
                    deflateStream.Write(data, 0, data.Length);
                }
                byte[] deflateData = compressStream.ToArray();
                byte[] zlibData = new byte[deflateData.Length + 6];
                zlibData[0] = 0x78; zlibData[1] = 0xDA; // zlib header
                Array.Copy(deflateData, 0, zlibData, 2, deflateData.Length);

                var stream2 = new MemoryStream(zlibData);
                var x = new BinaryWriter(stream2);
                x.Seek(-4, SeekOrigin.End);

                byte[] adler = BitConverter.GetBytes(Adler32(data));
                for (int i = adler.Length - 1; i >= 0; i--) x.Write(adler[i]); // Write the hash in reverse, zlib checksums are stored as big endian
                x.Flush();
                return stream2.ToArray();
            }
        }

        public static byte[] Bake(Dictionary<string, byte[]> data)
        {
            MemoryStream ourStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(ourStream);

            // Initial Record blocks
            Dictionary<Record, byte[]> records = new Dictionary<Record, byte[]>();
            foreach (KeyValuePair<string, byte[]> entry in data)
            {
                List<Block> blockOffsets = new List<Block>();
                byte[] fullCompressedData = CompressBuffer(entry.Value);
                blockOffsets.Add(new Block(0, fullCompressedData.Length));

                var rec = new Record { offset = (long)writer.BaseStream.Position, fileName = entry.Key };
                rec.Write(writer, entry.Value, false, true, blockOffsets, fullCompressedData);
                writer.Write(fullCompressedData);
                records.Add(rec, entry.Value);
            }

            // Mount point and record count
            ulong indexOffset = (ulong)writer.BaseStream.Position;
            writer.WriteUString("../../../");
            writer.Write((int)data.Count);

            // Second Record blocks
            foreach (KeyValuePair<Record, byte[]> entry in records)
            {
                entry.Key.Write(writer, entry.Value, true, false);
            }

            // Footer
            ulong indexEndOffset = (ulong)writer.BaseStream.Position;
            ulong indexLength = indexEndOffset - indexOffset;
            writer.Write((byte)0);
            writer.Write(PakExtractor.UE4_PAK_MAGIC); // magic number
            writer.Write((int)4); // type 4
            writer.Write(indexOffset);
            writer.Write(indexLength);

            ulong shaOffset = (ulong)writer.BaseStream.Position;
            writer.Seek((int)indexOffset, SeekOrigin.Begin);
            byte[] indexData = new byte[indexLength];
            writer.BaseStream.Read(indexData, 0, (int)indexLength);

            writer.Seek((int)shaOffset, SeekOrigin.Begin);
            writer.Write(new SHA1Managed().ComputeHash(indexData));

            return ourStream.ToArray();
        }
    }
}