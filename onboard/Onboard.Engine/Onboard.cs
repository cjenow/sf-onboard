using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        // progress handlers

        // options

        public Onboard(IShareFileClient api, RemoteFileSystem fileSystem)
        {
            this.api = api;
            this.fileSystem = fileSystem;
            uploader = new Uploader(api);
        }

        public Task Upload(Uri sfRoot)
        {
            return UploadFolder(fileSystem.Root, sfRoot);
        }

        private async Task UploadFolder(RemoteFolder folder, Uri sfParent)
        {
            var sfFolder = await CreateFolder(folder, sfParent);

            var fileTasks = (await folder.GetChildFiles()).Select(file => uploader.Upload(file, sfFolder.url)).ToArray();
            var folderTasks = (await folder.GetChildFolders()).Select(childFolder => UploadFolder(childFolder, sfFolder.url)).ToArray();

            await Task.WhenAll(fileTasks.Cast<Task>().Concat(folderTasks));
        }

        private async Task<models.Folder> CreateFolder(RemoteFolder remoteFolder, Uri parent)
        {
            var folder = new models.Folder
            {
                Name = remoteFolder.Name,
            };
            // set permissions here or after all child uploads are done?
            // probably here so platform doesn't have to propagate down?
            return await api.Items.CreateFolder(parent, folder).ExecuteAsync();
        }

    }
}
