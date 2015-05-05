using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShareFile.Api.Client;
using models = ShareFile.Api.Models;

namespace ShareFile.Onboard.Engine
{
    public class Onboard
    {
        IShareFileClient api;
        RemoteFileSystem fileSystem;
        Uploader uploader;

        // progress handlers

        // options

        private const int FILE_RETRY_COUNT = 3;
        private const int FOLDER_RETRY_COUNT = 10;

        public Onboard(IShareFileClient api, RemoteFileSystem fileSystem)
        {
            this.api = api;
            this.fileSystem = fileSystem;
            uploader = new Uploader(api);
        }

        public async Task<FolderResult> Upload(Uri sfRoot)
        {
            try
            {
                var fileTasks = sfRoot.AbsoluteUri.Contains("allshared") ? new Task<FileResult>[] { } : (await fileSystem.Root.GetChildFiles()).Select(file => UploadFile(file, sfRoot)).ToArray();
                var folderTasks = (await fileSystem.Root.GetChildFolders()).Select(childFolder => UploadFolder(childFolder, sfRoot)).ToArray();

                await Task.WhenAll(fileTasks.Cast<Task>().Concat(folderTasks));

                var result = new FolderResult(fileSystem.Root, sfRoot) { FolderCreateSucceeded = true, Uri = sfRoot };
                result = fileTasks.Select(task => task.Result).Aggregate(result, (folderResult, fileResult) => folderResult.AddChild(fileResult));
                result = folderTasks.Select(task => task.Result).Aggregate(result, (folderResult, childFolderResult) => folderResult.AddChild(childFolderResult));
                return result;
            }
            catch(Exception ex)
            {
                return new FolderResult(fileSystem.Root, sfRoot) { FolderCreateSucceeded = false, Exception = ex };
            }
        }

        private async Task<FolderResult> UploadFolder(RemoteFolder folder, Uri parent)
        {
            try
            {
                var sfFolder = await CreateFolder(folder, parent);

                var fileTasks = (await folder.GetChildFiles()).Select(file => UploadFile(file, sfFolder.url)).ToArray();
                var folderTasks = (await folder.GetChildFolders()).Select(childFolder => UploadFolder(childFolder, sfFolder.url)).ToArray();

                await Task.WhenAll(fileTasks.Cast<Task>().Concat(folderTasks));

                var result = new FolderResult(folder, parent) { FolderCreateSucceeded = true, Uri = sfFolder.url };
                result = fileTasks.Select(task => task.Result).Aggregate(result, (folderResult, fileResult) => folderResult.AddChild(fileResult));
                result = folderTasks.Select(task => task.Result).Aggregate(result, (folderResult, childFolderResult) => folderResult.AddChild(childFolderResult));
                return result;
            }
            catch(Exception ex)
            {
                return new FolderResult(folder, parent) { FolderCreateSucceeded = false, Exception = ex };
            }
        }

        private Task<models.Folder> CreateFolder(RemoteFolder folder, Uri parent)
        {
            var sfFolder = new models.Folder
            {
                Name = folder.Name,
            };
            // set permissions here or after all child uploads are done?
            // probably here so platform doesn't have to propagate down?
            return RetryAsync(() => api.Items.CreateFolder(parent, sfFolder).ExecuteAsync(), FOLDER_RETRY_COUNT);
        }

        private async Task<FileResult> UploadFile(RemoteFile file, Uri parent)
        {
            try
            {
                var sfFile = await RetryAsync(() => uploader.Upload(file, parent), FILE_RETRY_COUNT);
                return new FileResult(file, parent) { UploadSucceeded = true, Uri = sfFile.url };
            }
            catch(Exception ex)
            {
                // log? .. maybe not
                return new FileResult(file, parent) { UploadSucceeded = false, Exception = ex };
            }
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> f, int retryCount)
        {
            try
            {
                return await f();
            }
            catch
            {
                if (retryCount <= 1)
                    throw;
            }
            return await RetryAsync(f, retryCount - 1);
        }
    }

    public class FileResult
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public Uri ParentUri { get; set; }
        public bool UploadSucceeded { get; set; }
        // set on success
        public Uri Uri { get; set; } 
        // set on failure
        public Exception Exception { get; set; }

        public FileResult(RemoteFile file, Uri parent)
        {
            Name = file.Name;
            Path = file.Path;
            Size = file.Size;
            ParentUri = parent;
        }
    }

    public class FolderResult
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public Uri ParentUri { get; set; }
        public bool FolderCreateSucceeded { get; set; }
        // set on success
        public Uri Uri { get; set; }
        // set on failure
        public Exception Exception { get; set; }

        public long TotalSizeSuccessfullyUploaded { get; set; }
        public int SuccessfulChildFiles { get; set; }
        public int SuccessfulChildFolders { get; set; }

        public IList<FileResult> FailedChildFiles { get; set; }
        public IList<FolderResult> FailedChildFolders { get; set; }

        public FolderResult(RemoteFolder folder, Uri parent)
        {
            Name = folder.Name;
            Path = folder.Path;
            FailedChildFiles = new List<FileResult>();
            FailedChildFolders = new List<FolderResult>();
            ParentUri = parent;
        }

        public FolderResult AddChild(FileResult fileResult)
        {

            return this;
        }

        public FolderResult AddChild(FolderResult folderResult)
        {

            return this;
        }
    }
}
