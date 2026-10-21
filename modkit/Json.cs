// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace BlackModKit
{
    /// <summary>Small wrapper around DataContractJsonSerializer so the manifest field names stay stable.</summary>
    public static class Json
    {
        private static readonly DataContractJsonSerializer Serializer =
            new DataContractJsonSerializer(typeof(ModManifest));

        public static ModManifest Read(Stream stream)
        {
            try
            {
                object value = Serializer.ReadObject(stream);
                return value as ModManifest;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("mod.json inválido: " + ex.Message, ex);
            }
        }

        public static ModManifest ReadFile(string path)
        {
            using (var stream = File.OpenRead(path))
                return Read(stream);
        }

        public static void Write(Stream stream, ModManifest manifest)
        {
            Serializer.WriteObject(stream, manifest);
        }

        public static void WriteFile(string path, ModManifest manifest)
        {
            string temporary = path + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                Write(stream, manifest);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }
    }
}
