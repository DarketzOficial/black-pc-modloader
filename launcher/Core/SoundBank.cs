using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BlackLauncher
{
    /// <summary>
    /// The launcher's sounds in one file: <c>launcher.sounds</c>, holding the music and the three interface
    /// effects encrypted, so the port folder does not carry loose audio files around.
    ///
    /// It is obfuscation, not secrecy: the key comes from the passphrase below, which sits in this
    /// executable, so anyone determined can still get the audio out. What it does do is keep the four clips
    /// in one tidy file that a casual copy ("look, a folder of mp3s") picks up as nothing.
    ///
    /// The file (all numbers little endian):
    /// <code>
    ///   "BLKSNB01" | iterations u32 | salt[16] | count u32
    ///   per entry: keyLen u16 | key | iv[16] | len u32 | tag[32] | data[len]
    /// </code>
    /// The AES-256-CBC key and the HMAC-SHA256 key are derived from PBKDF2(passphrase, salt, iterations);
    /// every entry has its own random IV, and <c>tag = HMAC(iv + data)</c> is checked on load, so a damaged
    /// or edited file is refused instead of being played as noise. The entry's <c>key</c> is a hash of the
    /// clip's own name, so the file does not spell out what it contains either.
    /// </summary>
    public static class SoundBank
    {
        /// <summary>The file's name, in the port folder next to blacklauncher.exe.</summary>
        public const string FileName = "launcher.sounds";

        /// <summary>The clip the launcher plays in a loop.</summary>
        public const string Music = "launchermusic.mp3";

        /// <summary>Every clip the launcher looks for, in the bank and next to the exe.</summary>
        public static readonly string[] Clips = { Music, "hover.mp3", "select.mp3", "saved.mp3" };

        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("BLKSNB01");
        private const string Passphrase = "BLACK-PC/ModLoader-1.0/launcher-sounds/Darketz";
        private const int Iterations = 20000;      // PBKDF2 rounds (a few tens of ms, once per launch)
        private const int IvSize = 16;
        private const int TagSize = 32;

        // ------------------------------------------------------------------ read ---
        /// <summary>Read a bank file. Null when the file is not there (no sounds are better than no
        /// launcher). Throws when the file is there but is damaged or is not a bank at all.</summary>
        public static Bank Load(string path)
        {
            if (!File.Exists(path)) return null;
            byte[] all = File.ReadAllBytes(path);
            using (var ms = new MemoryStream(all, false))
            using (var br = new BinaryReader(ms))
            {
                if (all.Length < Magic.Length || !Same(br.ReadBytes(Magic.Length), Magic))
                    throw new InvalidDataException("not a " + FileName + " file");

                int iterations = br.ReadInt32();
                byte[] salt = br.ReadBytes(IvSize);
                int count = br.ReadInt32();
                // Refuse absurd headers instead of trying to honour them (a corrupt file, not a bank).
                if (iterations <= 0 || iterations > 5000000 || count < 0 || count > 1024)
                    throw new InvalidDataException("damaged header");

                byte[] encKey, macKey;
                Keys(salt, iterations, out encKey, out macKey);

                var clips = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                for (int i = 0; i < count; i++)
                {
                    int keyLen = br.ReadUInt16();
                    string key = Encoding.ASCII.GetString(br.ReadBytes(keyLen));
                    byte[] iv = br.ReadBytes(IvSize);
                    int len = br.ReadInt32();
                    byte[] tag = br.ReadBytes(TagSize);
                    byte[] data = br.ReadBytes(len);

                    if (len < 0 || len > all.Length || iv.Length != IvSize || tag.Length != TagSize || data.Length != len)
                        throw new InvalidDataException("damaged entry " + key);
                    if (!Same(tag, Mac(macKey, iv, data)))
                        throw new InvalidDataException("entry " + key + " does not match its signature");

                    clips[key] = Decrypt(encKey, iv, data);
                }
                return new Bank(clips);
            }
        }

        /// <summary>One opened bank file: the clips it holds, looked up by the clip's own name.</summary>
        public sealed class Bank
        {
            private readonly Dictionary<string, byte[]> _clips;

            internal Bank(Dictionary<string, byte[]> clips) { _clips = clips; }

            /// <summary>How many clips the file holds.</summary>
            public int Count { get { return _clips.Count; } }

            /// <summary>One clip's bytes, or null when the bank does not have it.</summary>
            public byte[] Get(string clip)
            {
                byte[] data;
                return _clips.TryGetValue(KeyOf(clip), out data) ? data : null;
            }
        }

        /// <summary>The key one clip is stored under (a hash of its name, so the file names nothing).</summary>
        public static string KeyOf(string clip)
        {
            byte[] h;
            using (var sha = SHA256.Create()) h = sha.ComputeHash(Encoding.UTF8.GetBytes(clip.ToLowerInvariant()));
            var sb = new StringBuilder(16);
            for (int i = 0; i < 8; i++) sb.Append(h[i].ToString("x2"));
            return sb.ToString();
        }

        // ----------------------------------------------------------------- write ---
        /// <summary>Write a bank file, keyed by clip name. The packer tool uses this; the launcher only
        /// reads. Missing clips are simply not in the file, and are silent for the launcher.</summary>
        public static void Write(string path, IDictionary<string, byte[]> clips)
        {
            byte[] salt = new byte[IvSize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] encKey, macKey;
            Keys(salt, Iterations, out encKey, out macKey);

            using (var fs = File.Create(path))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write(Magic);
                bw.Write(Iterations);
                bw.Write(salt);
                bw.Write(clips.Count);
                foreach (var kv in clips)
                {
                    string key = KeyOf(kv.Key);
                    byte[] iv = new byte[IvSize];
                    using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(iv);
                    byte[] data = Encrypt(encKey, iv, kv.Value);
                    bw.Write((ushort)key.Length);
                    bw.Write(Encoding.ASCII.GetBytes(key));
                    bw.Write(iv);
                    bw.Write(data.Length);
                    bw.Write(Mac(macKey, iv, data));
                    bw.Write(data);
                }
            }
        }

        // ------------------------------------------------------------------ keys ---
        private static void Keys(byte[] salt, int iterations, out byte[] encKey, out byte[] macKey)
        {
            using (var kdf = new Rfc2898DeriveBytes(Passphrase, salt, iterations))
            {
                encKey = kdf.GetBytes(32);
                macKey = kdf.GetBytes(32);
            }
        }

        private static byte[] Encrypt(byte[] key, byte[] iv, byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;
                using (var enc = aes.CreateEncryptor())
                    return enc.TransformFinalBlock(data, 0, data.Length);
            }
        }

        private static byte[] Decrypt(byte[] key, byte[] iv, byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;
                using (var dec = aes.CreateDecryptor())
                    return dec.TransformFinalBlock(data, 0, data.Length);
            }
        }

        private static byte[] Mac(byte[] key, byte[] iv, byte[] data)
        {
            using (var hmac = new HMACSHA256(key))
            {
                hmac.TransformBlock(iv, 0, iv.Length, null, 0);
                hmac.TransformFinalBlock(data, 0, data.Length);
                return hmac.Hash;
            }
        }

        private static bool Same(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
