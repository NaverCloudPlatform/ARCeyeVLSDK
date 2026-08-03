using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class MetadataReader
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    static extern void SHGetPropertyStoreFromParsingName(
        [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        IntPtr zeroWorks,
        GETPROPERTYSTOREFLAGS flags,
        ref Guid iid,
        [Out][MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppv
    );

    [DllImport("ole32.dll")]
    public static extern void PropVariantClear(ref PROPVARIANT pvar);

    [Flags]
    public enum GETPROPERTYSTOREFLAGS : uint
    {
        GPS_DEFAULT = 0x00000000
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPropertyStore
    {
        uint GetCount(out uint propertyCount);
        uint GetAt(uint propertyIndex, out PROPERTYKEY key);
        uint GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
        uint SetValue(ref PROPERTYKEY key, ref PROPVARIANT pv);
        uint Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr pwszVal;
    }

    public static bool IsQuickTimeFormat(string filePath)
    {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            // Read box size (4 bytes)
            uint size = ReadUInt32BE(reader);
            // Read box type (4 bytes)
            string boxType = Encoding.ASCII.GetString(reader.ReadBytes(4));

            if (boxType != "ftyp")
                return false;

            // Read major_brand (4 bytes)
            string majorBrand = Encoding.ASCII.GetString(reader.ReadBytes(4));
            // Skip minor_version (4 bytes)
            reader.ReadBytes(4);

            // Read remaining compatible brands
            int brandCount = ((int)size - 16) / 4;
            for (int i = 0; i < brandCount; i++)
            {
                string brand = Encoding.ASCII.GetString(reader.ReadBytes(4));
                if (brand.Trim() == "qt") return true;
            }

            return majorBrand.Trim() == "qt";
        }
    }

    public static string GetQuickTimeSoftwareTag(string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        using (BinaryReader reader = new BinaryReader(fs))
        {
            while (fs.Position < fs.Length)
            {
                long boxStart = fs.Position;
                if (!TryReadBoxHeader(reader, out uint boxSize, out string boxType)) break;

                if (boxType == "moov")
                {
                    long moovEnd = boxStart + boxSize;
                    while (fs.Position < moovEnd)
                    {
                        long metaStart = fs.Position;
                        if (!TryReadBoxHeader(reader, out uint metaSize, out string metaType)) break;

                        if (metaType == "meta")
                        {
                            long metaEnd = metaStart + metaSize;
                            fs.Seek(metaStart + 8, SeekOrigin.Begin); // skip 'meta' header

                            Dictionary<int, string> keyMap = new();

                            while (fs.Position < metaEnd)
                            {
                                long innerStart = fs.Position;
                                if (!TryReadBoxHeader(reader, out uint innerSize, out string innerType)) break;

                                if (innerType == "hdlr")
                                {
                                    reader.ReadBytes(8); // version, flags, pre_defined
                                    string handlerType = Encoding.ASCII.GetString(reader.ReadBytes(4));
                                    if (handlerType != "mdta") return "(Not mdta handler)";
                                    fs.Seek(innerStart + innerSize, SeekOrigin.Begin);
                                }
                                else if (innerType == "keys")
                                {
                                    reader.ReadBytes(4); // version + flags
                                    uint entryCount = ReadUInt32BE(reader);
                                    for (int i = 0; i < entryCount; i++)
                                    {
                                        uint entrySize = ReadUInt32BE(reader);
                                        reader.ReadBytes(4); // namespace
                                        byte[] keyBytes = reader.ReadBytes((int)(entrySize - 8));
                                        string keyStr = Encoding.UTF8.GetString(keyBytes).Trim('\0');
                                        keyMap[i + 1] = keyStr;
                                    }
                                }
                                else if (innerType == "ilst")
                                {
                                    long ilstEnd = innerStart + innerSize;
                                    while (fs.Position < ilstEnd)
                                    {
                                        long itemStart = fs.Position;
                                        if (!TryReadBoxHeader(reader, out uint itemSize, out _)) break;
                                        long itemEnd = itemStart + itemSize;

                                        long subBoxStart = fs.Position;
                                        if (!TryReadBoxHeader(reader, out uint dataBoxSize, out string dataBoxType)) break;
                                        if (dataBoxType == "data")
                                        {
                                            reader.ReadBytes(8); // reserved
                                            byte[] dataBytes = reader.ReadBytes((int)(dataBoxSize - 16));
                                            string value = Encoding.UTF8.GetString(dataBytes).Trim('\0');

                                            if (keyMap.TryGetValue(1, out string keyName) && keyName == "com.apple.quicktime.software")
                                                return value;
                                        }
                                        fs.Seek(itemEnd, SeekOrigin.Begin);
                                    }
                                }
                                else
                                {
                                    fs.Seek(innerStart + innerSize, SeekOrigin.Begin);
                                }
                            }
                        }
                        else
                        {
                            fs.Seek(metaStart + metaSize, SeekOrigin.Begin);
                        }
                    }
                }
                else
                {
                    fs.Seek(boxStart + boxSize, SeekOrigin.Begin);
                }
            }
        }
        return "";
    }
    private static bool TryReadBoxHeader(BinaryReader reader, out uint size, out string type)
    {
        try
        {
            size = ReadUInt32BE(reader);
            byte[] typeBytes = reader.ReadBytes(4);
            type = Encoding.ASCII.GetString(typeBytes);
            return true;
        }
        catch
        {
            size = 0;
            type = null;
            return false;
        }
    }
    private static uint ReadUInt32BE(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }
}
