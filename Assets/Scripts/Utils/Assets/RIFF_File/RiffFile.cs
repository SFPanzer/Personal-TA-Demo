using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Utils.Assets.RIFF_File
{
    public abstract class RiffFile
    {
        protected int HeaderSize { get; set; } = 12;
        protected long EndOfFileIndex = 0;
        public readonly Dictionary<string, List<byte[]>> Chunks = new();

        public void Load(string path)
        {
            try
            {
                ReadFile(path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Error when read RIFF file: {path}\n" + exception.Message);
            }
        }

        private void ReadFile(string path)
        {
            using var fileStream = new FileStream(path, FileMode.Open);
            EndOfFileIndex = fileStream.Length;
            // Read file header
            var header = new byte[HeaderSize];
            var bytesRead = fileStream.Read(header, 0, HeaderSize);
            if (bytesRead != HeaderSize)
                throw new IOException($"Reading the header requires reading {HeaderSize} bytes, but only {bytesRead} are read.");
            if (!CheckFileHeader(header))
                throw new IOException("Illegal file header.");
            // Read Chunks
            while (fileStream.Position < EndOfFileIndex)
            {
                var chunkData = ReadChunk(fileStream, out var chunkName);
                if(!Chunks.ContainsKey(chunkName))
                    Chunks.Add(chunkName, new List<byte[]>());
                Chunks[chunkName].Add(chunkData);
            }
        }

        protected abstract bool CheckFileHeader(byte[] fileHeader);
        
        protected virtual byte[] ReadChunk(Stream fileStream, out string chunkName)
        {
            // Read chunk identifier.
            var identifierBytes = new byte[4];
            var bytesRead = fileStream.Read(identifierBytes, 0, 4);
            if (bytesRead != 4)
                throw new IOException($"Reading the chunk identifier requires reading 4 bytes, but only {bytesRead} are read.");
            chunkName = Encoding.UTF8.GetString(identifierBytes);
            // Read chunk size.
            var sizeBytes = new byte[4];
            bytesRead = fileStream.Read(sizeBytes, 0, 4);
            if (bytesRead != 4)
                throw new IOException($"Reading the chunk \"{chunkName}\" size requires reading 4 bytes, but only {bytesRead} are read.");
            var chunkSize = BitConverter.ToInt32(sizeBytes, 0);
            // Read chunk data.
            var chunkData = new byte[chunkSize];
            bytesRead = fileStream.Read(chunkData, 0, chunkSize);
            if(bytesRead != chunkSize)
                throw new IOException($"Reading the chunk \"{chunkName}\" data requires reading {chunkSize} bytes, but only {bytesRead} are read.");

            return chunkData;
        }

        public List<byte[]> GetChunk(string chunkName)
        {
            return Chunks[chunkName];
        }

        public void Save(string path)
        {
            
        }
    }
}