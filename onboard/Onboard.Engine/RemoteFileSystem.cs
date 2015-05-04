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
        RemoteFolder Root { get; }
    }

    public interface RemoteFolder : RemoteFileSystemObject
    {
        Task<IEnumerable<RemoteFolder>> GetChildFolders();
        Task<IEnumerable<RemoteFile>> GetChildFiles();
        Task<object> GetPermissions(); //tbd..
    }

    public interface RemoteFile : RemoteFileSystemObject
    {
        long Size { get; }
        Task<Stream> GetContent(); 
    }

    public interface RemoteFileSystemObject
    {
        string Name { get; }
        string Path { get; }
        RemoteFolder Parent { get; }
    }
}
