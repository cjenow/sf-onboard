using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareFile.Onboard.Engine
{
    public class FileResult
    {
        public RemoteFile File { get; set; }
        public Uri ParentUri { get; set; }
        public bool UploadSucceeded { get; set; }
        // set on success
        public Uri Uri { get; set; }
        // set on failure
        public Exception Exception { get; set; }

        public FileResult(RemoteFile file, Uri parent)
        {
            File = file;
            ParentUri = parent;
        }
    }

    public class FolderResult
    {
        public RemoteFolder Folder { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public Uri ParentUri { get; set; }
        public bool CreateSucceeded { get; set; }
        // set on success
        public Uri Uri { get; set; }
        public Task<FileResult>[] ChildFileTasks { get; set; }
        public FolderResult[] ChildFolders { get; set; }
        // set on failure
        public Exception Exception { get; set; }

        public FolderResult(RemoteFolder folder, Uri parent)
        {
            Folder = folder;
            ParentUri = parent;
            ChildFileTasks = new Task<FileResult>[] { };
            ChildFolders = new FolderResult[] { };
        }
    }

    public class OnboardResult
    {
        public FolderResult[] AllFolderResults { get; set; }
        public Task FileUploadsFinished { get; set; }
        public FileResult[] AllFileResults { get; set; }

        public OnboardResult() { }

        public OnboardResult(FolderResult rootFolderResult)
        {
            FileUploadsFinished = WaitForFileUploads(rootFolderResult);

            Func<FolderResult, IEnumerable<FolderResult>> f = null;
            f = folderResult => new[] { folderResult }.Concat(folderResult.ChildFolders.SelectMany(f));
            AllFolderResults = f(rootFolderResult).ToArray();
        }

        public async Task WaitForFileUploads(FolderResult rootFolderResult)
        {
            Func<FolderResult, IEnumerable<Task<FileResult>>> f = null;
            f = folderResult => folderResult.ChildFileTasks.Concat(folderResult.ChildFolders.SelectMany(f));
            AllFileResults = await Task.WhenAll(f(rootFolderResult));
        }
    }
}
