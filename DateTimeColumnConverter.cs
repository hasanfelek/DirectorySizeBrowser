namespace DirectorySizes {
   /// <summary>
   /// Converts datetime to general date/time pattern with long time
   /// </summary>
   public class DateTimeColumnConverter : System.Windows.Data.IValueConverter {
      public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         string formattedValue = null;

         if (value != null) {
            formattedValue = ((System.DateTime)value).ToString("G");
         }

         return formattedValue;
      }

      public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture) {
         throw new System.NotImplementedException();
      }
   }
}
