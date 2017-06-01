
// Copyright (c) 2017 Mark A. Olbert some rights reserved
//
// This software is licensed under the terms of the MIT License
// (https://opensource.org/licenses/MIT)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace Olbert.LanHistory
{
    /// <summary>
    /// Extends the MvvmLight ViewModelBase to support property validation via
    /// System.ComponentModel.DataAnnotations
    /// </summary>
    public class ValidatedViewModelBase : ViewModelBase, INotifyDataErrorInfo
    {
        /// <summary>
        /// Raised when the error collection has changed
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// Flag indicating whether or not validation errors were encountered
        /// </summary>
        public bool HasErrors => ValidationErrors.Count > 0;

        /// <summary>
        /// The collection of validation errors encountered
        /// </summary>
        protected Dictionary<string, ICollection<string>> ValidationErrors { get; } =
            new Dictionary<string, ICollection<string>>();

        /// <summary>
        /// Gets the validation errors associated with the specified property name
        /// </summary>
        /// <param name="propertyName">the name of a property that was validated</param>
        /// <returns>the validation errors associated with the specified property name, or null
        /// if none were found or an invalid property name was specified</returns>
        public IEnumerable GetErrors( string propertyName )
        {
            if( string.IsNullOrEmpty( propertyName )
                || !ValidationErrors.ContainsKey( propertyName ) )
                return null;

            return ValidationErrors[ propertyName ];
        }

        /// <summary>
        /// Validates the supplied property
        /// </summary>
        /// <param name="value">the property being validated</param>
        /// <param name="propertyName">the name of the property being validated</param>
        /// <returns>true if no errors were encountered during validation, false otherwise</returns>
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

            return !errors.Any();
        }

        private void RaiseErrorsChanged( string propertyName )
        {
            ErrorsChanged?.Invoke( this, new DataErrorsChangedEventArgs( propertyName ) );
        }
    }
}
