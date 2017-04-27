using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace LanHistory
{
    public class PhysicalAddressFormatter : IValueConverter
    {
        public static string Format( PhysicalAddress macAddress )
        {
            if( macAddress == null ) return PhysicalAddress.None.ToString();

            return string.Join( ":", macAddress.GetAddressBytes()
                .Where( ( x, i ) => i < 6 )
                .Select( z => z.ToString( "X2" ) ) );
        }

        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if( value == null ) return PhysicalAddress.None.ToString();

            if( value is PhysicalAddress macAddress )
                return Format( macAddress );

            return "not a PhysicalAddress object";
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}
