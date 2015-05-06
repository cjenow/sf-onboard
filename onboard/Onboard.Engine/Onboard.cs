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

        public async Task<OnboardResult> BeginUpload(Uri sfRoot)
        {
            return new OnboardResult(await UploadFolder(fileSystem.Root, sfRoot));
        }

        private async Task<FolderResult> UploadFolder(RemoteFolder folder, Uri parent)
        {
            var fileTasks = parent.AbsoluteUri.Contains("allshared")
                ? new Task<FileResult>[] { }
                : (await fileSystem.Root.GetChildFiles()).Select(file => UploadFile(file, parent)).ToArray();
            var folderTasks = (await folder.GetChildFolders()).Select(childFolder => UploadChildFolder(childFolder, parent)).ToArray();

            await Task.WhenAll(folderTasks);

            var result = new FolderResult(folder, parent) // this parent url is wrong
            {
                CreateSucceeded = true,
                Uri = parent,
                ChildFileTasks = fileTasks,
                ChildFolders = folderTasks.Select(t => t.Result).ToArray(),
            };
            // result = fileTasks.Select(task => task.Result).Aggregate(result, (folderResult, fileResult) => folderResult.AddChild(fileResult));
            // result = folderTasks.Select(task => task.Result).Aggregate(result, (folderResult, childFolderResult) => folderResult.AddChild(childFolderResult));
            return result;
        }

        private async Task<FolderResult> UploadChildFolder(RemoteFolder folder, Uri parent)
        {
            try
            {
                var sfFolder = await CreateFolder(folder, parent);
                return await UploadFolder(folder, sfFolder.url);
            }
            catch(Exception ex)
            {
                return new FolderResult(folder, parent) { CreateSucceeded = false, Exception = ex };
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
        public bool CreateSucceeded { get; set; }
        // set on success
        public Uri Uri { get; set; }
        public Task<FileResult>[] ChildFileTasks { get; set; }
        public FolderResult[] ChildFolders { get; set; }
        // set on failure
        public Exception Exception { get; set; }
        
        public FolderResult(RemoteFolder folder, Uri parent)
        {
            Name = folder.Name;
            Path = folder.Path;
            ParentUri = parent;
            ChildFileTasks = new Task<FileResult>[] { };
            ChildFolders = new FolderResult[] { };
        }
    }

    public class OnboardResult
    {
        public FolderResult RootFolderResult { get; set; }
        public bool FileUploadsFinished { get; private set; }

        // set after FileUploadsFinished == true
        private FileResult[] fileResults;

        public OnboardResult(FolderResult rootFolderResult)
        {
            RootFolderResult = rootFolderResult;
            FileUploadsFinished = false;
        }

        public async Task WaitForFileUploads()
        {
            Func<FolderResult, IEnumerable<Task<FileResult>>> f;
            f = folderResult => folderResult.ChildFileTasks.Concat(folderResult.ChildFolders.SelectMany(f));
            fileResults = await Task.WhenAll(f(RootFolderResult));
            FileUploadsFinished = true;
            return;
        }
    }
}
