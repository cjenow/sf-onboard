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
        private IShareFileClient api;
        private RemoteFileSystem fileSystem;
        private Uploader uploader;

        public event EventHandler<OnboardProgressEventArgs> Progress;

        // options

        private const int RETRY_COUNT = 3;

        public Onboard(IShareFileClient api, RemoteFileSystem fileSystem)
        {
            this.api = api;
            this.fileSystem = fileSystem;
            uploader = new Uploader(api);
        }

        public async Task Upload(Uri sfRoot)
        {
            var fileTasks = sfRoot.AbsoluteUri.Contains("allshared") ? new Task<models.File>[] { } : (await fileSystem.Root.GetChildFiles()).Select(file => UploadFile(file, sfRoot)).ToArray();
            var folderTasks = (await fileSystem.Root.GetChildFolders()).Select(childFolder => UploadFolder(childFolder, sfRoot)).ToArray();

            await Task.WhenAll(fileTasks.Cast<Task>().Concat(folderTasks));
        }

        private async Task UploadFolder(RemoteFolder folder, Uri sfParent)
        {
            var sfFolder = await CreateFolder(folder, sfParent);

            var fileTasks = (await folder.GetChildFiles()).Select(file => UploadFile(file, sfFolder.url)).ToArray();
            var folderTasks = (await folder.GetChildFolders()).Select(childFolder => UploadFolder(childFolder, sfFolder.url)).ToArray();

            await Task.WhenAll(fileTasks.Cast<Task>().Concat(folderTasks));
        }

        private Task<models.Folder> CreateFolder(RemoteFolder folder, Uri parent)
        {
            var sfFolder = new models.Folder
            {
                Name = folder.Name,
            };
            // set permissions here or after all child uploads are done?
            // probably here so platform doesn't have to propagate down?
            return RetryAsync(() => api.Items.CreateFolder(parent, sfFolder).ExecuteAsync());
        }

        private Task<models.File> UploadFile(RemoteFile file, Uri parent)
        {
            return RetryAsync(() => uploader.Upload(file, parent));
        }

        private Task<T> RetryAsync<T>(Func<Task<T>> f)
        {
            return RetryAsync(f, RETRY_COUNT);
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

    public class OnboardProgressEventArgs : EventArgs
    {
        public long BytesUploaded { get; set; }
        public long TotalBytes { get; set; }
        public int FilesUploaded { get; set; }
        public int TotalFiles { get; set; }
    }
}
