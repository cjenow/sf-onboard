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
        public DateTimeOffset ProcessedAt { get; set; }
        public bool UploadSucceeded { get; set; }
        // set on success
        public Uri Uri { get; set; }
        // set on failure
        public Exception Exception { get; set; }

        public FileResult(RemoteFile file, Uri parent)
        {
            File = file;
            ParentUri = parent;
            ProcessedAt = DateTimeOffset.Now;
        }

        public string ToLogString()
        {
            if (UploadSucceeded)
            {
                return String.Format("{0} SUCCESS {1} {2}", ProcessedAt, File.Path, Uri);
            }
            else
            {
                return String.Format("{0} FAILED {1} {2}", ProcessedAt, File.Path, Exception);
            }
        }
    }

    public class FolderResult
    {
        public RemoteFolder Folder { get; set; }
        public Uri ParentUri { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
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
            ProcessedAt = DateTimeOffset.Now;
        }

        public string ToLogString()
        {
            if(CreateSucceeded)
            {
                return String.Format("{0} SUCCESS {1} {2}", ProcessedAt, Folder.Path, Uri);
            }
            else
            {
                return String.Format("{0} FAILED {1} {2}", ProcessedAt, Folder.Parent, Exception);
            }
        }
    }

    // this is dumb - shouldn't decide here whether flattened or tree is appropriate
    public class OnboardResult
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset Finished { get; set; }
        public TimeSpan Elapsed { get; set; }

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
            Finished = DateTimeOffset.Now;
            Elapsed = Finished - Start;
        }

        public async Task ToLogFile(System.IO.Stream output)
        {
            await FileUploadsFinished;
            var fileLogEntries = AllFileResults.Select(file => new { Time = file.ProcessedAt, LogLine = file.ToLogString() });
            var folderLogEntries = AllFolderResults.Select(folder => new { Time = folder.ProcessedAt, LogLine = folder.ToLogString() });
            var sorted = fileLogEntries.Concat(folderLogEntries).OrderBy(logEntry => logEntry.Time);
            using (var wr = new System.IO.StreamWriter(output))
            {
                foreach (string line in sorted.Select(logEntry => logEntry.LogLine))
                {
                    await wr.WriteLineAsync(line);
                }
            }
        }
    }

    static class ComparableExtensions
    {
        public static List<TElement> Sort<TElement, TKey>(this List<TElement> list, Func<TElement, TKey> key) where TKey : IComparable<TKey>
        {
            var comparer = new ComparerFunc<TElement>((x, y) => key(x).CompareTo(key(y)));
            list.Sort(comparer);
            return list;
        }

        private class ComparerFunc<T> : IComparer<T>
        {
            private Func<T, T, int> compare;

            public ComparerFunc(Func<T, T, int> compare) { this.compare = compare; }

            public int Compare(T x, T y)
            {
                return compare(x, y);
            }
        }
    }
}
