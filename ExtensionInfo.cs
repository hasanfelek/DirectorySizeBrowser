namespace DirectorySizes {
   /// <summary>
   /// HOlds information about an extension
   /// </summary>
   public class ExtensionInfo {
      /// <summary>
      /// The extension
      /// </summary>
      public string Extension { get; set; }
      /// <summary>
      /// Total bytes for the extension
      /// </summary>
      public long TotalBytes { get; set; }
      /// <summary>
      /// Formatted total bytes for the extension
      /// </summary>
      public string FormattedTotalBytes {
         get {
            return DirectoryHelper.FormatBytesText(this.TotalBytes);
         }
      }
      /// <summary>
      /// Total percentage for this extension
      /// </summary>
      public double TotalPercentage {
         get {
            return ((double)this.TotalBytes / (double)DirectoryDetail.UsedDiskSize) * 100d;
         }
      }
   }
}
