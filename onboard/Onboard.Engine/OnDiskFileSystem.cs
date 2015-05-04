using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ShareFile.Onboard.Engine
{
    public class OnDiskFileSystem : RemoteFileSystem
    {
        public RemoteFolder Root { get; private set; }

        public OnDiskFileSystem(string path)
        {
            Root = new OnDiskFolder(new DirectoryInfo(path));
        }
    }

    public class OnDiskFolder : OnDiskFileSystemObject, RemoteFolder
    {
        private DirectoryInfo directory;        

        public OnDiskFolder(DirectoryInfo directory) : base(directory) { this.directory = directory; }

        public Task<IEnumerable<RemoteFolder>> GetChildFolders()
        {
            return Task.Run(() => directory.EnumerateDirectories().Select(dir => new OnDiskFolder(dir) { Parent = this } as RemoteFolder));
        }

        public Task<IEnumerable<RemoteFile>> GetChildFiles()
        {
            return Task.Run(() => directory.EnumerateFiles().Select(file => new OnDiskFile(file) { Parent = this } as RemoteFile));
        }

        public Task<object> GetPermissions()
        {
            throw new NotImplementedException();
        }
    }

    public class OnDiskFile : OnDiskFileSystemObject, RemoteFile
    {
        private FileInfo file;

        public OnDiskFile(FileInfo file) : base(file) { this.file = file; }

        public long Size { get { return file.Length; } }

        public Task<Stream> GetContent()
        {
            throw new NotImplementedException();
        }
    }

    public class OnDiskFileSystemObject : RemoteFileSystemObject
    {
        private FileSystemInfo file;

        public OnDiskFileSystemObject(FileSystemInfo file)
        {
            this.file = file;
        }

        public string Name { get { return file.Name; } }
        public string Path { get { return file.FullName; } }
        public RemoteFolder Parent { get; set; }
    }
}
