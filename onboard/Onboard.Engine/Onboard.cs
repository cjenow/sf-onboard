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
            var start = DateTimeOffset.Now;
            return new OnboardResult(await UploadFolder(fileSystem.Root, sfRoot)) { Start = start };
        }

        private async Task<FolderResult> UploadFolder(RemoteFolder folder, Uri parent)
        {
            var createdAt = DateTimeOffset.Now;
            var fileTasks = parent.AbsoluteUri.Contains("allshared")
                ? new Task<FileResult>[] { }
                : (await folder.GetChildFiles()).Select(file => UploadFile(file, parent)).ToArray();
            var folderTasks = (await folder.GetChildFolders()).Select(childFolder => UploadChildFolder(childFolder, parent)).ToArray();

            await Task.WhenAll(folderTasks);

            var result = new FolderResult(folder, parent) // this parent url is wrong
            {
                CreateSucceeded = true,
                Uri = parent,
                ChildFileTasks = fileTasks,
                ChildFolders = folderTasks.Select(t => t.Result).ToArray(),
                ProcessedAt = createdAt,
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

        public async Task<OnboardResult> BeginRetryFailed(OnboardResult result)
        {
            var fileTasks = result.AllFileResults.Where(fileResult => !fileResult.UploadSucceeded).Select(fileResult => UploadFile(fileResult.File, fileResult.ParentUri)).ToArray();
            var folderTasks = result.AllFolderResults.Where(folderResult => !folderResult.CreateSucceeded).Select(folderResult => UploadChildFolder(folderResult.Folder, folderResult.ParentUri)).ToArray();

            await Task.WhenAll(fileTasks.Cast<Task>().Concat(folderTasks));

            var folderResults = folderTasks.Select(t => new OnboardResult(t.Result));

            await Task.WhenAll(folderResults.Select(r => r.FileUploadsFinished));

            return new OnboardResult
            {
                AllFileResults = fileTasks.Select(t => t.Result).Concat(folderResults.SelectMany(fr => fr.AllFileResults)).ToArray(),
                AllFolderResults = folderResults.SelectMany(fr => fr.AllFolderResults).ToArray(),
                FileUploadsFinished = Task.FromResult(new object())
            };
        }
    }
}
