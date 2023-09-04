namespace DirectorySizeBroswer
{
    /// <summary>
    /// Class to contain information about a directory
    /// </summary>
    public sealed class DirectoryDetail
    {

        #region Fields
        // Parsed directory name
        private string _directoryName = null;
        #endregion Fields

        #region Auto-implemented properties
        /// <summary>
        /// Directory path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Total size of the files in this directory.
        /// Child directories not included
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// Cumulative size of the files in this directory or child directories.
        /// </summary>
        public long CumulativeSize { get; set; }

        /// <summary>
        /// Number of files in directory
        /// </summary>
        public long NumberOfFiles { get; set; }

        /// <summary>
        /// Cumulative number of files in directory and it's subdirectories
        /// </summary>
        public long CumulativeNumberOfFiles { get; set; }

        //return this.Path.EndsWith(":\\") ? 0 :  this.Path.ToCharArray().Where(x => x == '\\').Count();
        /// <summary>
        /// Depth of the directory
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Parent directory detail if any
        /// </summary>
        public DirectoryDetail ParentDirectoryDetail { get; set; }

        /// <summary>
        /// Subdirectory details
        /// </summary>
        public System.Collections.Generic.HashSet<DirectoryDetail> SubDirectoryDetails { get; private set; }

        /// <summary>
        /// Total disk size
        /// </summary>
        public static long UsedDiskSize { get; set; }

        #endregion Auto-implemented properties

        #region Calculated properties
        /// <summary>
        /// Name of the directory
        /// </summary>
        public string DirectoryName
        {
            get
            {
                if (this._directoryName == null)
                {
                    this._directoryName = this.Path.Substring(this.Path.LastIndexOf('\\')).TrimStart('\\');
                }
                return this._directoryName;
            }
        }

        /// <summary>
        /// This directory size percentage from total size
        /// </summary>
        public double SizePercentage
        {
            get
            {
                return ((double)this.Size / (double)DirectoryDetail.UsedDiskSize) * 100d;
            }
        }

        /// <summary>
        /// Cumulative size percentage from total size
        /// </summary>
        public double CumulativeSizePercentage
        {
            get
            {
                return ((double)this.CumulativeSize / (double)DirectoryDetail.UsedDiskSize) * 100d;
            }
        }

        /// <summary>
        /// Formatted string representation of bytes
        /// </summary>
        public string FormattedBytes
        {
            get
            {
                return DirectoryHelper.FormatBytesText(this.Size);
            }
        }

        /// <summary>
        /// Formatted string representation of bytes
        /// </summary>
        public string FormattedCumulativeBytes
        {
            get
            {
                return DirectoryHelper.FormatBytesText(this.CumulativeSize);
            }
        }

        #endregion Calculated properties

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public DirectoryDetail()
        {
            this.SubDirectoryDetails = new System.Collections.Generic.HashSet<DirectoryDetail>();
        }

        /// <summary>
        /// Constructor with path and depth
        /// </summary>
        /// <param name="path">Path for the directory</param>
        /// <param name="depth">Depth of the directory</param>
        /// <param name="usedDiskSize">Amount of used space for the disk in bytes</param>
        public DirectoryDetail(string path, int depth, long usedDiskSize)
           : this()
        {
            this.Path = path;
            this.Depth = depth;
            DirectoryDetail.UsedDiskSize = usedDiskSize;
        }

        /// <summary>
        /// Constructor with path, depth and parent directory
        /// </summary>
        /// <param name="path">Path for the directory</param>
        /// <param name="depth">Depth of the directory</param>
        /// <param name="parentDirectoryDetail">Parent directory</param>
        public DirectoryDetail(string path, int depth, DirectoryDetail parentDirectoryDetail)
           : this(path, depth, DirectoryDetail.UsedDiskSize)
        {
            this.ParentDirectoryDetail = parentDirectoryDetail;
        }

        #endregion Constructors
    }
}
