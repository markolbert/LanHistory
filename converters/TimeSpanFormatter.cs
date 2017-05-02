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
            if( toConv.Days == 0 )
            {
                if( toConv.Hours == 0 ) return $"{toConv.Minutes} minutes";

                if( toConv.Minutes == 0 )
                    return $"{toConv.Hours} hour" + (toConv.Hours == 1 ? String.Empty : "s");
            }
            else
            {
                if( toConv.Hours == 0 && toConv.Minutes == 0 )
                    return $"{toConv.Days} day" + ( toConv.Days == 1 ? String.Empty : "s" );
            }

            return toConv.ToString( "d h:mm" );
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
