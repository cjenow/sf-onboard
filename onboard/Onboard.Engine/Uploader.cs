using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Threading;
using System.Threading;
using System.Threading.Tasks;
using models = ShareFile.Api.Models;
using ShareFile.Api.Client;
using System.IO;
using ShareFile.Api.Client.Transfers;
using ShareFile.Api.Client.Extensions;

namespace ShareFile.Onboard.Engine
{
    class Uploader
    {
        IShareFileClient api;
        AsyncSemaphore standardUploadQueue;
        AsyncSemaphore threadedUploadQueue;

        Api.Client.Transfers.Uploaders.FileUploaderConfig threadedUploaderConfig;

        const long KB = 1024;
        const long MB = 1024 * 1024;
        const long GB = 1024 * 1024 * 1024;

        const long STANDARD_UPLOADER_MAX_FILE_SIZE = 16 * MB;
        const int MAX_CONCURRENT_THREADED_UPLOAD = 4;
        const int MAX_CONCURRENT_STANDARD_UPLOAD = 20;

        public Uploader(IShareFileClient api)
        {
            this.api = api;
            standardUploadQueue = new AsyncSemaphore(MAX_CONCURRENT_STANDARD_UPLOAD);
            threadedUploadQueue = new AsyncSemaphore(MAX_CONCURRENT_THREADED_UPLOAD);

            threadedUploaderConfig = new Api.Client.Transfers.Uploaders.FileUploaderConfig
            {
                NumberOfThreads = 3,
                PartConfig = new Api.Client.Transfers.Uploaders.FilePartConfig
                {
                    InitialPartSize = (int)STANDARD_UPLOADER_MAX_FILE_SIZE,
                    MaxPartSize = (int)(STANDARD_UPLOADER_MAX_FILE_SIZE * 8),
                }
            };
        }

        public Task<models.File> Upload(RemoteFile file, Uri parent)
        {
            return file.Size < STANDARD_UPLOADER_MAX_FILE_SIZE ? UploadStandard(file, parent) : UploadThreaded(file, parent);
        }

        // batch upload method

        private async Task<models.File> UploadStandard(RemoteFile file, Uri parent)
        {
            using (await standardUploadQueue.EnterAsync())
            using (var platformFile = new LazyPlatformFileStream(file))
            {
                var uploader = api.GetAsyncFileUploader(
                    BuildUploadSpecificationRequest(file, parent, models.UploadMethod.Standard),
                    platformFile);
                return UploadResultToModel((await uploader.UploadAsync()).First());
            }
        }

        private async Task<models.File> UploadThreaded(RemoteFile file, Uri parent)
        {
            using (await threadedUploadQueue.EnterAsync())
            using (var platformFile = new LazyPlatformFileStream(file))
            {
                var uploader = api.GetAsyncFileUploader(
                    BuildUploadSpecificationRequest(file, parent, models.UploadMethod.Threaded),
                    platformFile, threadedUploaderConfig);
                return UploadResultToModel((await uploader.UploadAsync()).First());                
            }
        }

        private UploadSpecificationRequest BuildUploadSpecificationRequest(RemoteFile file, Uri sfParent, models.UploadMethod method)
        {
            return new UploadSpecificationRequest
            {
                FileName = file.Name,
                FileSize = file.Size,
                Method = method,
                Parent = sfParent,
            };
        }

        private models.File UploadResultToModel(UploadedFile uploadedFile)
        {
            return new models.File
            {
                FileName = uploadedFile.Filename,
                Name = uploadedFile.DisplayName,
                FileSizeBytes = uploadedFile.Size,
                Id = uploadedFile.Id,
                url = api.Items.GetAlias(uploadedFile.Id),
            };
        }
    }

    class LazyPlatformFileStream : ShareFile.Api.Client.FileSystem.IPlatformFile
    {
        private RemoteFile file;
        private Task<Stream> getStream;

        public LazyPlatformFileStream(RemoteFile file)
        {
            this.file = file;
            getStream = file.GetContent();
        }

        public string FullName { get { return file.Path; } }
        public long Length { get { return file.Size; } }
        public string Name { get { return file.Name; } }

        public Stream OpenRead()
        {
            return OpenReadAsync().Result;
        }

        public async Task<Stream> OpenReadAsync()
        {
            return await getStream;
        }

        public Stream OpenWrite()
        {
            throw new NotImplementedException();
        }

        public Task<Stream> OpenWriteAsync()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if (getStream != null && getStream.Result != null)
                getStream.Result.Dispose();
        }
    }
}
