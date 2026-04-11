using System;
using System.IO;
using System.Text;
using Avalonia.Media.Imaging;

namespace Insait_Audio_Player.Services;

public sealed class Mp3CoverArtProvider
{
    public Bitmap? TryLoadCover(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            if (!TryReadCoverBytes(stream, out var coverBytes) || coverBytes.Length == 0)
            {
                return null;
            }

            using var memoryStream = new MemoryStream(coverBytes);
            return new Bitmap(memoryStream);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryReadCoverBytes(Stream stream, out byte[] coverBytes)
    {
        coverBytes = Array.Empty<byte>();

        Span<byte> header = stackalloc byte[10];
        if (stream.Read(header) != header.Length)
        {
            return false;
        }

        if (header[0] != (byte)'I' || header[1] != (byte)'D' || header[2] != (byte)'3')
        {
            return false;
        }

        var version = header[3];
        var flags = header[5];
        var tagSize = ReadSynchsafeInteger(header.Slice(6, 4));
        var tagData = new byte[tagSize];
        if (stream.Read(tagData, 0, tagData.Length) != tagData.Length)
        {
            return false;
        }

        var offset = 0;
        if ((flags & 0x40) != 0)
        {
            offset = GetExtendedHeaderSize(tagData, version);
            if (offset < 0 || offset >= tagData.Length)
            {
                return false;
            }
        }

        return version switch
        {
            2 => TryReadFramesV22(tagData, offset, out coverBytes),
            3 or 4 => TryReadFramesV23OrV24(tagData, offset, version, out coverBytes),
            _ => false
        };
    }

    private static int GetExtendedHeaderSize(byte[] tagData, int version)
    {
        if (tagData.Length < 4)
        {
            return -1;
        }

        var headerSize = version == 4
            ? ReadSynchsafeInteger(tagData.AsSpan(0, 4))
            : ReadBigEndianInteger(tagData.AsSpan(0, 4)) + 4;

        return Math.Clamp(headerSize, 0, tagData.Length);
    }

    private static bool TryReadFramesV23OrV24(byte[] tagData, int offset, int version, out byte[] coverBytes)
    {
        coverBytes = Array.Empty<byte>();

        while (offset + 10 <= tagData.Length)
        {
            var frameHeader = tagData.AsSpan(offset, 10);
            if (IsPadding(frameHeader))
            {
                return false;
            }

            var frameId = Encoding.ASCII.GetString(frameHeader.Slice(0, 4));
            var frameSize = version == 4
                ? ReadSynchsafeInteger(frameHeader.Slice(4, 4))
                : ReadBigEndianInteger(frameHeader.Slice(4, 4));

            if (frameSize <= 0 || offset + 10 + frameSize > tagData.Length)
            {
                return false;
            }

            var frameData = tagData.AsSpan(offset + 10, frameSize);
            if (frameId == "APIC" && TryExtractApic(frameData, out coverBytes))
            {
                return true;
            }

            offset += 10 + frameSize;
        }

        return false;
    }

    private static bool TryReadFramesV22(byte[] tagData, int offset, out byte[] coverBytes)
    {
        coverBytes = Array.Empty<byte>();

        while (offset + 6 <= tagData.Length)
        {
            var frameHeader = tagData.AsSpan(offset, 6);
            if (IsPadding(frameHeader))
            {
                return false;
            }

            var frameId = Encoding.ASCII.GetString(frameHeader.Slice(0, 3));
            var frameSize = (frameHeader[3] << 16) | (frameHeader[4] << 8) | frameHeader[5];
            if (frameSize <= 0 || offset + 6 + frameSize > tagData.Length)
            {
                return false;
            }

            var frameData = tagData.AsSpan(offset + 6, frameSize);
            if (frameId == "PIC" && TryExtractPic(frameData, out coverBytes))
            {
                return true;
            }

            offset += 6 + frameSize;
        }

        return false;
    }

    private static bool TryExtractApic(ReadOnlySpan<byte> frameData, out byte[] coverBytes)
    {
        coverBytes = Array.Empty<byte>();
        if (frameData.Length < 4)
        {
            return false;
        }

        var textEncoding = frameData[0];
        var position = 1;
        var mimeTerminatorIndex = frameData.Slice(position).IndexOf((byte)0);
        if (mimeTerminatorIndex < 0)
        {
            return false;
        }

        position += mimeTerminatorIndex + 1;
        if (position >= frameData.Length)
        {
            return false;
        }

        position += 1;
        var descriptionLength = GetEncodedStringLength(frameData.Slice(position), textEncoding);
        if (descriptionLength < 0)
        {
            return false;
        }

        position += descriptionLength;
        if (position >= frameData.Length)
        {
            return false;
        }

        coverBytes = frameData.Slice(position).ToArray();
        return coverBytes.Length > 0;
    }

    private static bool TryExtractPic(ReadOnlySpan<byte> frameData, out byte[] coverBytes)
    {
        coverBytes = Array.Empty<byte>();
        if (frameData.Length < 6)
        {
            return false;
        }

        var textEncoding = frameData[0];
        var position = 1 + 3 + 1;
        var descriptionLength = GetEncodedStringLength(frameData.Slice(position), textEncoding);
        if (descriptionLength < 0)
        {
            return false;
        }

        position += descriptionLength;
        if (position >= frameData.Length)
        {
            return false;
        }

        coverBytes = frameData.Slice(position).ToArray();
        return coverBytes.Length > 0;
    }

    private static int GetEncodedStringLength(ReadOnlySpan<byte> data, byte textEncoding)
    {
        var terminatorLength = textEncoding is 1 or 2 ? 2 : 1;
        if (terminatorLength == 1)
        {
            var terminatorIndex = data.IndexOf((byte)0);
            return terminatorIndex < 0 ? -1 : terminatorIndex + 1;
        }

        for (var i = 0; i + 1 < data.Length; i += 2)
        {
            if (data[i] == 0 && data[i + 1] == 0)
            {
                return i + 2;
            }
        }

        return -1;
    }

    private static bool IsPadding(ReadOnlySpan<byte> data)
    {
        foreach (var value in data)
        {
            if (value != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static int ReadSynchsafeInteger(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 21) | (bytes[1] << 14) | (bytes[2] << 7) | bytes[3];
    }

    private static int ReadBigEndianInteger(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }
}
