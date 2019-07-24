using System;
using System.IO;

namespace NuGetTypeSearch
{
    public class TemporaryFileStream : FileStream
    {
        public static TemporaryFileStream Create()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString() + ".tmp");

            return new TemporaryFileStream(path);
        }

        private readonly string _path;

        private TemporaryFileStream(string path)
            : base(path, FileMode.OpenOrCreate, FileAccess.ReadWrite)
        {
            _path = path;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            try
            {
                File.Delete(_path);
            }
            catch
            {
                // Best-effort...
            }
        }
    }
}