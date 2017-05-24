using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Olbert.LanHistory
{
    public class NullableBooleanConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if( targetType != typeof(bool))
                throw new ArgumentException($"{nameof(NullableBooleanConverter)}::Convert() -- {nameof(targetType)} is not a bool");

            bool? nb = value as bool?;

            if( !nb.HasValue ) return false;

            return nb.Value;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}
