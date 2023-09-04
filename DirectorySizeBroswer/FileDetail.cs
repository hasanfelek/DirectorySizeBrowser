namespace DirectorySizeBroswer
{

    /// <summary>
    /// Class to contain information about a single files
    /// </summary>
    public sealed class FileDetail
    {
        #region Auto-implemented properties
        /// <summary>
        /// Name of the file
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Path to the file
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long Size { get; set; }
        /// <summary>
        /// When the file was last accessed
        /// </summary>
        public System.DateTime LastAccessed { get; set; }
        /// <summary>
        /// Extension of the file
        /// </summary>
        public string Extension { get; set; }
        /// <summary>
        /// Directory details
        /// </summary>
        public DirectoryDetail DirectoryDetail { get; set; }
        #endregion Auto-implemented properties

        #region Calculated properties
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
        #endregion Calculated properties
    }
}
