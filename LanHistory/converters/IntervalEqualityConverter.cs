using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Olbert.LanHistory
{
    public class IntervalEqualityConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if( targetType != typeof(bool))
                throw new ArgumentException($"{nameof(IntervalEqualityConverter)}::Convert() -- {nameof(targetType)} is not bool");

            bool retVal = false;

            int minutes = 0;

            if ( parameter is int )
                minutes = (int) parameter;

            if( value is TimeSpan )
                retVal = value.Equals( TimeSpan.FromMinutes( minutes ) );

            return retVal;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}
