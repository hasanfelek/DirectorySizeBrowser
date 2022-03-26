namespace DirectorySizes {
   /// <summary>
   /// Calculates the progress bar width within a listview column
   /// </summary>
   public class ProgressBarWidthConverter : System.Windows.Data.IValueConverter {
      public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         return ((double)value) - 15;
      }

      public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         throw new System.NotImplementedException();
      }
   }
}
