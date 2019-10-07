using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace auxmic
{
    /// <summary>
    /// Custom file cache implementation.
    /// Writes and reads object from/to CacheRootPath.
    /// </summary>
    internal sealed class FileCache
    {
        private static string _cacheRootPath;

        public static string TempExtension
        {
            get
            {
                return "tmp";
            }
        }

        /// <summary>
        /// Temp directory name. Formed as %temp%\auxmix_temp
        /// </summary>
        internal static string CacheRootPath
        {
            get
            {
                if (String.IsNullOrEmpty(_cacheRootPath))
                {
                    throw new ApplicationException("CacheRootPath is null or empty.");
                }

                return _cacheRootPath;
            }

            set
            {
                _cacheRootPath = value;

                DirectoryInfo cacheRoot = new DirectoryInfo(_cacheRootPath);

                // create if it doesn't exist
                if (!cacheRoot.Exists)
                    cacheRoot.Create();
            }
        }

        static FileCache()
        {
            CacheRootPath = Path.Combine(Path.GetTempPath(), "auxmic_temp");
        }

        internal static bool Contains(string key)
        {
            return File.Exists(Path.Combine(CacheRootPath, key));
        }

        /// <summary>
        /// Gets object by key. Key is a filename.
        /// Actually it reads file <paramref name="key"/> from CacheRootPath to object.
        /// If not such file - returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static object Get(string key)
        {
            object cached = null;

            if (Contains(key))
            {
                BinaryFormatter formatter = new BinaryFormatter();

                using (FileStream fs = new FileStream(Path.Combine(CacheRootPath, key), FileMode.Open))
                {
                    cached = formatter.Deserialize(fs);
                }
            }

            return cached;
        }

        internal static T Get<T>(string key)
        {
            return (T)Get(key);
        }

        /// <summary>
        /// Removes item from cache - deletes tmp-file if exists.
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(string key)
        {
            string fileToDelete = Path.Combine(CacheRootPath, key);

            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete); 
            }
        }

        /// <summary>
        /// Writes <paramref name="value"/> object to file with name <paramref name="key"/>
        /// in CacheRootPath directory.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal static void Set(string key, object value)
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream fs = File.Open(Path.Combine(CacheRootPath, key), FileMode.Create))
            {
                formatter.Serialize(fs, value);
            }
        }

        internal object this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Clears only temp files.
        /// </summary>
        /// <returns></returns>
        internal static int Clear()
        {
            return Clear(TempExtension);
        }

        /// <summary>
        /// Clears files with certain extension.
        /// </summary>
        /// <param name="extensionToClear"></param>
        /// <returns></returns>
        internal static int Clear(string extensionToClear)
        {
            int filesDeleted = 0;

            DirectoryInfo di = new DirectoryInfo(CacheRootPath);
            FileInfo[] files = di.GetFiles("*." + extensionToClear)
                                 .Where(p => p.Extension == "." + extensionToClear).ToArray();
            foreach (FileInfo tempFile in files)
            {
                tempFile.Attributes = FileAttributes.Normal;
                File.Delete(tempFile.FullName);
                filesDeleted++;
            }

            return filesDeleted;
        }

        internal static string GetTempFilename(string filename, int sampleRate)
        {
            return String.Format("{0}.{1}.{2}", 
                                Path.GetFileName(filename),
                                sampleRate,
                                FileCache.TempExtension);
        }
    }
}
