using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;

namespace LanHistory
{
    public class ValidatedViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public bool HasErrors => ValidationErrors.Count > 0;

        protected Dictionary<string, ICollection<string>> ValidationErrors { get; } =
            new Dictionary<string, ICollection<string>>();

        public IEnumerable GetErrors( string propertyName )
        {
            if( string.IsNullOrEmpty( propertyName )
                || !ValidationErrors.ContainsKey( propertyName ) )
                return null;

            return ValidationErrors[ propertyName ];
        }

        protected bool Validate( object value, string propertyName )
        {
            if( ValidationErrors.ContainsKey( propertyName ) )
                ValidationErrors.Remove( propertyName );

            PropertyInfo propertyInfo = this.GetType().GetProperty( propertyName );

            var errors = propertyInfo.GetCustomAttributes<ValidationAttribute>( true )
                .Where( x => !x.IsValid( value ) )
                .Select( x => x.FormatErrorMessage( String.Empty ) )
                .ToList();

            ValidationErrors.Add( propertyName, errors );

            RaiseErrorsChanged( propertyName );

            return errors.Any();
        }

        private void RaiseErrorsChanged( string propertyName )
        {
            ErrorsChanged?.Invoke( this, new DataErrorsChangedEventArgs( propertyName ) );
        }
    }
}
