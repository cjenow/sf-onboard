using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ShareFile.Onboard.Engine
{
    public interface RemoteFileSystem
    {
        public RemoteFolder Root { get; set; }
    }

    public interface RemoteFolder : RemoteFileSystemObject
    {
        public Task<IEnumerable<RemoteFolder>> GetChildFolders();
        public Task<IEnumerable<RemoteFile>> GetChildFiles();
        public Task<object> GetPermissions(); //tbd..
    }

    public interface RemoteFile : RemoteFileSystemObject
    {
        public long Size { get; set; }
        public Task<Stream> GetContent(); 
    }

    public interface RemoteFileSystemObject
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public RemoteFolder Parent { get; set; }
    }
}
