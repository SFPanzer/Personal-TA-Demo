using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Utils.Assets.RIFF_File
{
    public class MagicVoxelFile : RiffFile
    {
        public MagicVoxelFile()
        {
            HeaderSize = 8;
        }

        protected override bool CheckFileHeader(byte[] fileHeader)
        {
            var identifier = Encoding.ASCII.GetString(fileHeader, 0, 4);
            if (identifier != "VOX ")
            {
                Debug.LogError($"File Header is {identifier}");
                return false;
            }

            var version = BitConverter.ToInt32(fileHeader, 4);
            if (version != 150)
            {
                Debug.LogError($"File version is {version}");
                return false;
            }

            return true;
        }

        protected override byte[] ReadChunk(Stream fileStream, out string chunkName)
        {
            // Read chunk identifier.
            var identifierBytes = new byte[4];
            var bytesRead = fileStream.Read(identifierBytes, 0, 4);
            if (bytesRead != 4)
                throw new IOException(
                    $"Reading the chunk identifier requires reading 4 bytes, but only {bytesRead} are read.");
            chunkName = Encoding.UTF8.GetString(identifierBytes);
            // Read chunk content size.
            var sizeBytes = new byte[4];
            bytesRead = fileStream.Read(sizeBytes, 0, 4);
            if (bytesRead != 4)
                throw new IOException(
                    $"Reading the chunk \"{chunkName}\" content size requires reading 4 bytes, but only {bytesRead} are read.");
            var chunkContentSize = BitConverter.ToInt32(sizeBytes, 0);
            // Read chunk content size.
            bytesRead = fileStream.Read(sizeBytes, 0, 4);
            if (bytesRead != 4)
                throw new IOException(
                    $"Reading the chunk \"{chunkName}\" children size requires reading 4 bytes, but only {bytesRead} are read.");
            var childrenChunksSize = BitConverter.ToInt32(sizeBytes, 0);
            // Read chunk data.
            var chunkData = new byte[chunkContentSize];
            bytesRead = fileStream.Read(chunkData, 0, chunkContentSize);
            if (bytesRead != chunkContentSize)
                throw new IOException(
                    $"Reading the chunk \"{chunkName}\" data requires reading {chunkContentSize} bytes, but only {bytesRead} are read.");
            if (chunkName == "MAIN")
                EndOfFileIndex = fileStream.Position + childrenChunksSize;
            return chunkData;
        }
    }
}