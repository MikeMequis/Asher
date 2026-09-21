using Asher.Core;
using Asher.Core.Platform;

namespace Asher.Services.Platform
{
    /// <summary>
    /// Minimal reader for the ELF header's e_machine field, used to warn when the deployed
    /// bootstrap does not match the game executable architecture. Returns null for non-ELF files.
    /// </summary>
    internal static class ElfArchitecture
    {
        public static string? TryRead(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return null;

                Span<byte> header = stackalloc byte[20];
                using var stream = File.OpenRead(path);
                if (stream.Read(header) < header.Length)
                    return null;

                if (header[0] != 0x7F || header[1] != (byte)'E'
                    || header[2] != (byte)'L' || header[3] != (byte)'F')
                {
                    return null;
                }

                var littleEndian = header[5] != 2;
                var machine = littleEndian
                    ? (ushort)(header[18] | (header[19] << 8))
                    : (ushort)((header[18] << 8) | header[19]);

                return machine switch
                {
                    0x03 => "x86",
                    0x3E => "x86_64",
                    0x28 => "arm",
                    0xB7 => "aarch64",
                    _ => $"machine:0x{machine:X}"
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
