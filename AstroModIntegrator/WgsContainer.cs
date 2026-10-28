using System.Text;

/*
    https://github.com/13xforever/wgs-exporter
    
    MIT License

    Copyright (c) 2023 Ilya

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

namespace AstroModIntegrator
{
    public static class BinaryReaderExtensions
    {
        public static readonly Encoding Utf16 = new UnicodeEncoding(false, false);

        public static string ReadUtf16String(this BinaryReader reader)
        {
            var len = reader.ReadInt32();
            Span<byte> buf = stackalloc byte[len * 2];
            reader.BaseStream.ReadExactly(buf);
            return Utf16.GetString(buf);
        }

        public static Guid ReadGuid(this BinaryReader reader)
        {
            Span<byte> buf = stackalloc byte[16];
            reader.BaseStream.ReadExactly(buf);
            return new(buf);
        }
    }

    public sealed record WgsBlob(string Name, Guid WgsFilename);

    public sealed record WgsContainer(WgsBlob[] Blobs)
    {
        public static WgsContainer Read(Stream stream)
        {
            using var reader = new BinaryReader(stream, BinaryReaderExtensions.Utf16, true);
            var tmp = reader.ReadInt32(); // unk1
            if (tmp != 4)
                throw new InvalidDataException("Expected unk1 to be 4");

            var count = reader.ReadInt32();
            var entries = new WgsBlob[count];
            Span<byte> buf = stackalloc byte[64 * 2];
            for (var i = 0; i < count; i++)
            {
                stream.ReadExactly(buf);
                var name = BinaryReaderExtensions.Utf16.GetString(buf).TrimEnd('\0');
                var guid1 = reader.ReadGuid();
                var guid2 = reader.ReadGuid();
                if (guid1 != guid2)
                    throw new InvalidDataException($"Expected both guids to be the same in blob #{i}");

                entries[i] = new(name, guid2);
            }
            return new(entries);
        }
    }
}
