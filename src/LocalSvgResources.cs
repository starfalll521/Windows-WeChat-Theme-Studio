using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

internal sealed class LocalSvgResources
{
    readonly byte[] source;
    readonly string directory;
    readonly int tree, names, data;
    readonly Dictionary<string, string> files = new Dictionary<string, string>(StringComparer.Ordinal);

    LocalSvgResources(byte[] source, string directory)
    {
        this.source = source;
        this.directory = directory;
        tree = Offset(0x8ef0ad0);
        names = Offset(0x8ed2ef0);
        data = Offset(0x86a4dc0);
    }

    internal static Dictionary<string, string> Read()
    {
        byte[] original = Engine.Normalize(File.ReadAllBytes(Engine.Target));
        string directory = Path.Combine(Engine.Store, "local-svg", Engine.OriginalHash);
        Directory.CreateDirectory(directory);
        var resources = new LocalSvgResources(original, directory);
        resources.Visit(0, "");
        return resources.files;
    }

    int Offset(int address)
    {
        int pe = BitConverter.ToInt32(source, 0x3c);
        int first = pe + 24 + BitConverter.ToUInt16(source, pe + 20);
        int count = BitConverter.ToUInt16(source, pe + 6);
        for (int i = 0; i < count; i++)
        {
            int section = first + 40 * i;
            int delta = address - BitConverter.ToInt32(source, section + 12);
            if (delta >= 0 && delta < BitConverter.ToInt32(source, section + 16))
                return BitConverter.ToInt32(source, section + 20) + delta;
        }
        throw new InvalidDataException("无法定位微信图标资源。");
    }

    int Read16(int offset) { return (source[offset] << 8) | source[offset + 1]; }
    int Read32(int offset) { return (Read16(offset) << 16) | Read16(offset + 2); }

    void Visit(int index, string parent)
    {
        int entry = tree + index * 22;
        int nameOffset = names + Read32(entry);
        string name = Encoding.BigEndianUnicode.GetString(source, nameOffset + 6, Read16(nameOffset) * 2);
        string uri = name.Length == 0 ? parent : parent + "/" + name;
        int flags = Read16(entry + 4);
        if ((flags & 2) != 0)
        {
            int count = Read32(entry + 6);
            int first = Read32(entry + 10);
            for (int i = 0; i < count; i++) Visit(first + i, uri);
            return;
        }
        if (!uri.EndsWith(".svg", StringComparison.Ordinal)) return;
        string file = Path.Combine(directory, index + ".svg");
        if (!File.Exists(file))
        {
            int offset = data + Read32(entry + 10);
            int length = Read32(offset);
            byte[] svg;
            if ((flags & 4) != 0) throw new InvalidDataException("不支持的图标压缩格式。");
            using (var decoded = new MemoryStream())
            {
                if ((flags & 1) != 0)
                {
                    using (var compressed = new MemoryStream(source, offset + 10, length - 10))
                    using (var inflater = new DeflateStream(compressed, CompressionMode.Decompress))
                        inflater.CopyTo(decoded);
                }
                else decoded.Write(source, offset + 4, length);
                svg = decoded.ToArray();
            }
            string temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temp, svg);
                try { File.Move(temp, file); }
                catch (IOException) { if (!File.Exists(file)) throw; }
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        files[uri.StartsWith("/gui/", StringComparison.Ordinal) ? ":" + uri.Substring(4) : ":" + uri] = file;
    }
}
