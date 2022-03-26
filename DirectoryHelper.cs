using System.Linq;

namespace DirectorySizes {

   /// <summary>
   /// Helper class for data gather and other operations
   /// </summary>
   internal static class DirectoryHelper {

      /// <summary>
      /// Files found in drive
      /// </summary>
      private static readonly System.Collections.Generic.HashSet<FileDetail> FileDetails = new System.Collections.Generic.HashSet<FileDetail>();
      /// <summary>
      /// Directories found in drive
      /// </summary>
      private static readonly System.Collections.Generic.HashSet<DirectoryDetail> DirectoryDetails = new System.Collections.Generic.HashSet<DirectoryDetail>();
      /// <summary>
      /// Directories skipped because of insufficient privileges. In many cases junction points
      /// </summary>
      private static readonly System.Collections.Generic.HashSet<string> SkippedDirectories = new System.Collections.Generic.HashSet<string>();
      /// <summary>
      /// Extension statistics
      /// </summary>
      private static readonly System.Collections.Generic.HashSet<ExtensionInfo> ExtensionDetails = new System.Collections.Generic.HashSet<ExtensionInfo>();

      /// <summary>
      /// Event used to send information about the gather process
      /// </summary>
      internal static event System.EventHandler<string> StatusUpdate;

      /// <summary>
      /// Event used to inform that the overall counters have been changed
      /// </summary>
      internal static event System.EventHandler CountersChanged;

      /// <summary>
      /// Event used to inform that a directory is skipped
      /// </summary>
      internal static event System.EventHandler<string> DirectorySkipped;

      /// <summary>
      /// Event used to send information when the state of the gather process changes
      /// </summary>
      internal static event System.EventHandler<bool> GatherInProgressChanges;

      /// <summary>
      /// Event used to inform that one root level directory has been completed
      /// </summary>
      internal static event System.EventHandler<DirectoryDetail> DirectoryComplete;

      /// <summary>
      /// Task used for data gathering
      /// </summary>
      private static System.Threading.Tasks.Task _gatherTask;

      /// <summary>
      /// Is a gathering requested to be stopped
      /// </summary>
      private static bool _stopRequested;

      /// <summary>
      /// Lock object for threads
      /// </summary>
      private static object _lockObject = new object();

      /// <summary>
      /// Overall file count
      /// </summary>
      public static int OverallFileCount { get; set; }

      /// <summary>
      /// Overall directory count
      /// </summary>
      public static int OverallDirectoryCount { get; set; }

      /// <summary>
      /// Current state of the data gathering
      /// </summary>
      internal static bool GatherInProgress {
         get {
            return DirectoryHelper._gatherTask == null ? false : DirectoryHelper._gatherTask.Status == System.Threading.Tasks.TaskStatus.Running;
         }
      }

      /// <summary>
      /// Starts the data gathering process
      /// </summary>
      /// <param name="drive"></param>
      /// <returns></returns>
      internal static bool StartDataGathering(System.IO.DriveInfo driveInfo) {
         DirectoryHelper.FileDetails.Clear();
         DirectoryHelper.DirectoryDetails.Clear();
         DirectoryHelper.ExtensionDetails.Clear();
         DirectoryHelper.SkippedDirectories.Clear();
         DirectoryHelper.OverallDirectoryCount = 0;
         DirectoryHelper.OverallFileCount = 0;
         DirectoryHelper._stopRequested = false;

         DirectoryHelper._gatherTask = new System.Threading.Tasks.Task(() => { GatherData(driveInfo); }, System.Threading.Tasks.TaskCreationOptions.LongRunning);
         DirectoryHelper._gatherTask.Start();

         return true;
      }

      /// <summary>
      /// Starts the data gathering process
      /// </summary>
      /// <param name="drive"></param>
      /// <returns></returns>
      internal static bool StopDataGathering() {
         if (DirectoryHelper._gatherTask.Status != System.Threading.Tasks.TaskStatus.Running) {
            return true;
         }
         lock (DirectoryHelper._lockObject) {
            DirectoryHelper._stopRequested = true;
         }
         DirectoryHelper._gatherTask.Wait();

         return true;
      }

      /// <summary>
      /// Returns a list of ready drives
      /// </summary>
      /// <returns>Drive list</returns>
      internal static System.Collections.Generic.IEnumerable<System.IO.DriveInfo> GetDrives() {
         return System.IO.DriveInfo.GetDrives().Where(drive => drive.IsReady);
      }

      /// <summary>
      /// Collects the data for a drive
      /// </summary>
      /// <param name="drive">Drive to investigate</param>
      /// <returns>True if succesful</returns>
      private static bool GatherData(System.IO.DriveInfo driveInfo) {
         DirectoryHelper.RaiseGatherInProgressChanges(true);
         DirectoryHelper.ListFiles(new DirectoryDetail(driveInfo.Name, 0, driveInfo.TotalSize - driveInfo.AvailableFreeSpace));

         DirectoryHelper.RaiseStatusUpdate("Calculating statistics...");
         DirectoryHelper.CalculateStatistics();

         DirectoryHelper.RaiseStatusUpdate("Idle");
         DirectoryHelper.RaiseGatherInProgressChanges(false);

         return true;
      }

      /// <summary>
      /// Raises the StausUpdate event
      /// </summary>
      /// <param name="status">Status to send</param>
      private static void RaiseStatusUpdate(string status) {
         if (DirectoryHelper.StatusUpdate != null) {
            DirectoryHelper.StatusUpdate(null, status);
         }
      }

      /// <summary>
      /// Raises the DirectorySkipped event
      /// </summary>
      /// <param name="path">Path of the directory skipped</param>
      private static void RaiseDirectorySkipped(string path) {
         if (DirectoryHelper.DirectorySkipped != null) {
            DirectoryHelper.DirectorySkipped(null, path);
         }
      }

      /// <summary>
      /// Raises the StateChange event
      /// </summary>
      /// <param name="status">New state for the process</param>
      private static void RaiseGatherInProgressChanges(bool newState) {
         if (DirectoryHelper.GatherInProgressChanges != null) {
            DirectoryHelper.GatherInProgressChanges(null, newState);
         }
      }

      /// <summary>
      /// Raises the CountersChange event
      /// </summary>
      private static void RaiseCountersChanged() {
         if (DirectoryHelper.CountersChanged != null) {
            DirectoryHelper.CountersChanged(null, new System.EventArgs());
         }
      }

      /// <summary>
      /// Adds recursively files and directories to hashsets
      /// </summary>
      /// <param name="directory">Directory to gather data from</param>
      /// <returns>Directory details</returns>
      private static DirectoryDetail ListFiles(DirectoryDetail thisDirectoryDetail) {
         DirectoryDetail subDirectoryDetail;
         System.IO.FileInfo fileInfo;

         // Exit if stop is requested
         lock (DirectoryHelper._lockObject) {
            if (DirectoryHelper._stopRequested) {
               return thisDirectoryDetail;
            }
         }

         RaiseStatusUpdate(string.Format("Analyzing {0}", DirectoryHelper.ShortenPath(thisDirectoryDetail.Path)));

         //List files in this directory
         try {
            // Loop through child directories
            foreach (string subDirectory in System.IO.Directory.EnumerateDirectories(thisDirectoryDetail.Path).OrderBy(x => x)) {
               subDirectoryDetail = ListFiles(new DirectoryDetail(subDirectory, thisDirectoryDetail.Depth + 1, thisDirectoryDetail));
               thisDirectoryDetail.CumulativeSize += subDirectoryDetail.CumulativeSize;
               thisDirectoryDetail.CumulativeNumberOfFiles += subDirectoryDetail.CumulativeNumberOfFiles;
               thisDirectoryDetail.SubDirectoryDetails.Add(subDirectoryDetail);
               // Break if stop is requested
               lock (DirectoryHelper._lockObject) {
                  if (DirectoryHelper._stopRequested) {
                     break;
                  }
               }
            }

            if (!DirectoryHelper._stopRequested) {
               // List files in this directory
               foreach (string file in System.IO.Directory.EnumerateFiles(thisDirectoryDetail.Path, "*.*", System.IO.SearchOption.TopDirectoryOnly)) {
                  fileInfo = new System.IO.FileInfo(file);
                  lock (DirectoryHelper._lockObject) {
                     FileDetails.Add(new FileDetail() {
                        Name = fileInfo.Name,
                        Path = fileInfo.DirectoryName,
                        Size = fileInfo.Length,
                        LastAccessed = fileInfo.LastAccessTime,
                        Extension = fileInfo.Extension,
                        DirectoryDetail = thisDirectoryDetail
                     });
                  }
                  thisDirectoryDetail.CumulativeSize += fileInfo.Length;
                  thisDirectoryDetail.Size += fileInfo.Length;
                  thisDirectoryDetail.NumberOfFiles++;
                  thisDirectoryDetail.CumulativeNumberOfFiles++;
                  DirectoryHelper.OverallFileCount++;
                  DirectoryHelper.RaiseCountersChanged();
               }
            }
            
            // add this directory to the collection
            lock (DirectoryHelper._lockObject) {
               DirectoryDetails.Add(thisDirectoryDetail);
            }
            DirectoryHelper.OverallDirectoryCount++;
            DirectoryHelper.RaiseCountersChanged();
         } catch (System.UnauthorizedAccessException exception) {
            // Listing files in the directory not allowed so ignore this directory
            lock (DirectoryHelper._lockObject) {
               DirectoryHelper.SkippedDirectories.Add(thisDirectoryDetail.Path);
            }
            DirectoryHelper.RaiseDirectorySkipped(string.Format("Skipped {0}, reason: {1}", thisDirectoryDetail.Path, exception.Message));
         } catch (System.IO.PathTooLongException exception) {
            // Path is too long
            lock (DirectoryHelper._lockObject) {
               DirectoryHelper.SkippedDirectories.Add(thisDirectoryDetail.Path);
            }
            DirectoryHelper.RaiseDirectorySkipped(string.Format("Skipped {0}, reason: {1}", thisDirectoryDetail.Path, exception.Message));
         }

         if (thisDirectoryDetail.Depth == 1) {
            if (DirectoryComplete != null) {
               DirectoryComplete(null, thisDirectoryDetail);
            }
         }

         return thisDirectoryDetail;
      }

      /// <summary>
      /// Formats bytes to bytes, kilobytes megabytes text etc depending on the size
      /// </summary>
      /// <param name="bytes">Actual size</param>
      /// <returns>Formatted text</returns>
      internal static string FormatBytesText(long bytes) {
         string bytesText = null;

         if (bytes < System.Math.Pow(1024, 1)) {
            bytesText = (bytes / System.Math.Pow(1024, 0)).ToString("N0") + " B";
         } else if (bytes < System.Math.Pow(1024, 2)) {
            bytesText = (bytes / System.Math.Pow(1024, 1)).ToString("N0") + " KB";
         } else if (bytes < System.Math.Pow(1024, 3)) {
            bytesText = (bytes / System.Math.Pow(1024, 2)).ToString("N2") + " MB";
         } else if (bytes < System.Math.Pow(1024, 4)) {
            bytesText = (bytes / System.Math.Pow(1024, 3)).ToString("N2") + " GB";
         }
         return bytesText;
      }

      /// <summary>
      /// Shortens the path to 100 characters so that the last directory is visible.
      /// Removed characters are replaced with ellipsis (...)
      /// </summary>
      /// <param name="fullPath">Full path</param>
      /// <returns>Shortened path</returns>
      internal static string ShortenPath(string fullPath) {
         string shortPath = fullPath;
         System.Text.StringBuilder sb;
         string[] pathParts;
         int partialLength;
         int targetLength = 100;

         if (shortPath.Length > targetLength) {
            pathParts = shortPath.Split('\\');
            partialLength = targetLength - 3 - (shortPath.Length - shortPath.LastIndexOf('\\'));
            sb = new System.Text.StringBuilder();
            sb.Append(shortPath.Take(partialLength).ToArray());
            sb.AppendFormat("...\\{0}", pathParts[pathParts.Length - 1]);
            shortPath = sb.ToString();
         }
         return shortPath;
      }

      /// <summary>
      /// Lists all the files in a directory sorted by size in descending order
      /// </summary>
      /// <param name="directoryDetail"></param>
      /// <returns></returns>
      internal static System.Collections.Generic.List<FileDetail> FilesInDirectory(DirectoryDetail directoryDetail) {
         System.Collections.Generic.List<FileDetail> fileList;

         lock (DirectoryHelper._lockObject) {
            fileList = DirectoryHelper.FileDetails.Where(x => x.DirectoryDetail == directoryDetail).OrderByDescending(x => x.Size).ToList();
         }

         return fileList;
      }
      /// <summary>
      /// Lists all the files in a directory sorted by size in descending order
      /// </summary>
      /// <param name="directoryDetail"></param>
      /// <returns></returns>
      internal static System.Collections.Generic.List<FileDetail> BiggestFilesInPath(DirectoryDetail directoryDetail, int amountToFetch) {
         System.Collections.Generic.List<FileDetail> fileList;

         lock (DirectoryHelper._lockObject) {
            fileList = DirectoryHelper.FileDetails.Where(x => x.Path.StartsWith(directoryDetail.Path)).OrderByDescending(x => x.Size).Take(amountToFetch).ToList();
         }

         return fileList;
      }

      /// <summary>
      /// Calculates statistics for the files
      /// </summary>
      /// <returns></returns>
      private static bool CalculateStatistics() {
         var extensionQuery = from file in DirectoryHelper.FileDetails
                              group file by file.Extension into extensionGroup
                              select new ExtensionInfo {
                                 Extension = extensionGroup.Key,
                                 TotalBytes = extensionGroup.Sum(file => file.Size)
                              };
         foreach (ExtensionInfo extensionInfo in extensionQuery) {
            DirectoryHelper.ExtensionDetails.Add(extensionInfo);
         }

         return true;
      }
      /// <summary>
      /// Lists the extensions in descending order based on total size
      /// </summary>
      /// <param name="directoryDetail"></param>
      /// <returns></returns>
      internal static System.Collections.Generic.IEnumerable<ExtensionInfo> ExtensionsBySize() {
         return DirectoryHelper.ExtensionDetails.OrderByDescending(x => x.TotalBytes);
      }
      /// <summary>
      /// Lists all the files in a directory sorted by size in descending order
      /// </summary>
      /// <param name="directoryDetail"></param>
      /// <returns></returns>
      internal static System.Collections.Generic.List<FileDetail> BiggestFilesByExtension(string extension, int amountToFetch) {
         System.Collections.Generic.List<FileDetail> fileList;

         lock (DirectoryHelper._lockObject) {
            fileList = DirectoryHelper.FileDetails.Where(x => x.Extension == extension).OrderByDescending(x => x.Size).Take(amountToFetch).ToList();
         }

         return fileList;
      }
   }
}
