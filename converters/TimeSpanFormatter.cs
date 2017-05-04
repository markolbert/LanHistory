using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Olbert.LanHistory
{
    public class TimeSpanFormatter : IValueConverter
    {
        public static string Format( TimeSpan toConv )
        {
            if( toConv.Equals( TimeSpan.MaxValue ) ) return "Other";

            StringBuilder retVal = new StringBuilder();

            if( toConv.Days > 0 )
                retVal.Append( $"{toConv.Days} day" + ( toConv.Days == 1 ? String.Empty : "s" ) );

            if( toConv.Hours > 0 )
            {
                if( retVal.Length > 0 ) retVal.Append( " " );
                retVal.Append( $"{toConv.Hours} hour" + ( toConv.Hours == 1 ? String.Empty : "s" ) );
            }

            if( toConv.Minutes > 0 )
            {
                if (retVal.Length > 0) retVal.Append(" ");
                retVal.Append( $"{toConv.Minutes} minute" + ( toConv.Minutes == 1 ? String.Empty : "s" ) );
            }

            return retVal.ToString();
        }

        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if( value == null ) return String.Empty;

            if( value is TimeSpan timeSpan )
                return Format( timeSpan );

            return "not a TimeSpan object";
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }
}
