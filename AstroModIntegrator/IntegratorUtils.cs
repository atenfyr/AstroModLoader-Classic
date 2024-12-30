using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace AstroModIntegrator
{
    public class UString
    {
        public string Value;
        public Encoding Encoding;

        public UString(string value, Encoding encoding)
        {
            Value = value;
            Encoding = encoding;
        }

        public UString()
        {

        }
    }

    public static class IntegratorUtils
    {
        public static readonly EngineVersion EngineVersion = EngineVersion.VER_UE4_23;
        public static readonly Version CurrentVersion = new Version(1, 7, 2, 0);
        public static readonly string[] IgnoredModIDs = new string[]
        {
            "AstroModIntegrator",
            "ModIntegrator"
        };

        internal static void CopySplitUp(Stream input, Stream output, int start, int leng)
        {
            input.Seek(start, SeekOrigin.Begin);
            output.Seek(0, SeekOrigin.Begin);

            byte[] buffer = new byte[32768];
            int read;
            while (leng > 0 && (read = input.Read(buffer, 0, Math.Min(buffer.Length, leng))) > 0)
            {
                output.Write(buffer, 0, read);
                leng -= read;
            }
        }

        public static byte[] Concatenate(byte[] one, byte[] two)
        {
            byte[] final = new byte[one.Length + two.Length];
            Buffer.BlockCopy(one, 0, final, 0, one.Length);
            Buffer.BlockCopy(two, 0, final, one.Length, two.Length);
            return final;
        }

        public static void SplitExportFiles(UAsset y, string desiredPath, Dictionary<string, byte[]> createdPakData)
        {
            MemoryStream newData = y.WriteData();

            long breakingOffPoint = y.Exports[0].SerialOffset;
            using (MemoryStream assetFile = new MemoryStream((int)breakingOffPoint))
            {
                CopySplitUp(newData, assetFile, 0, (int)breakingOffPoint);
                createdPakData[desiredPath] = assetFile.ToArray();
            }

            int lengthOfRest = (int)(newData.Length - breakingOffPoint);
            using (MemoryStream exportFile = new MemoryStream(lengthOfRest))
            {
                CopySplitUp(newData, exportFile, (int)breakingOffPoint, lengthOfRest);
                createdPakData[Path.ChangeExtension(desiredPath, ".uexp")] = exportFile.ToArray();
            }
        }

        public static UString ReadUStringWithEncoding(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            switch (length)
            {
                case 0:
                    return null;
                default:
                    if (length < 0)
                    {
                        byte[] data = reader.ReadBytes(-length * 2);
                        return new UString(Encoding.Unicode.GetString(data, 0, data.Length - 2), Encoding.Unicode);
                    }
                    else
                    {
                        byte[] data = reader.ReadBytes(length);
                        return new UString(Encoding.ASCII.GetString(data, 0, data.Length - 1), Encoding.ASCII);
                    }
            }
        }

        public static string ReadUString(this BinaryReader reader)
        {
            return ReadUStringWithEncoding(reader)?.Value;
        }

        public static string ReadUStringWithGUID(this BinaryReader reader, out uint guid)
        {
            string str = reader.ReadUString();
            if (!string.IsNullOrEmpty(str))
            {
                guid = reader.ReadUInt32();
            }
            else
            {
                guid = 0;
            }
            return str;
        }

        public static UString ReadUStringWithGUIDAndEncoding(this BinaryReader reader, out uint guid)
        {
            UString str = reader.ReadUStringWithEncoding();
            if (!string.IsNullOrEmpty(str.Value))
            {
                guid = reader.ReadUInt32();
            }
            else
            {
                guid = 0;
            }
            return str;
        }

        public static void WriteUString(this BinaryWriter writer, string str, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.ASCII;

            switch (str)
            {
                case null:
                    writer.Write((int)0);
                    break;
                default:
                    string nullTerminatedStr = str + "\0";
                    writer.Write(encoding is UnicodeEncoding ? -nullTerminatedStr.Length : nullTerminatedStr.Length);
                    writer.Write(encoding.GetBytes(nullTerminatedStr));
                    break;
            }
        }

        public static void WriteUString(this BinaryWriter writer, UString str)
        {
            WriteUString(writer, str?.Value, str?.Encoding);
        }

        public static readonly Regex GameRegex = new Regex(@"^\/Game\/", RegexOptions.Compiled);
        public static string ConvertGamePathToAbsolutePath(this string gamePath, string targetExtension = ".uasset")
        {
            if (!GameRegex.IsMatch(gamePath)) return string.Empty;
            string newPath = GameRegex.Replace(gamePath, "Astro/Content/", 1);

            if (Path.HasExtension(newPath)) return newPath;
            return Path.ChangeExtension(newPath, targetExtension);
        }

        public static string GetEnumMemberAttrValue(this object enumVal)
        {
            EnumMemberAttribute attr = enumVal.GetType().GetMember(enumVal.ToString())[0].GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();
            return attr?.Value;
        }
    }
}
