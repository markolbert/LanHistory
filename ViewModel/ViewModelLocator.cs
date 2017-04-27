/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocatorTemplate xmlns:vm="clr-namespace:LanHistory.ViewModel"
                                   x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"
*/

using System;
using System.Diagnostics;
using Autofac;
using Autofac.Extras.CommonServiceLocator;
using GalaSoft.MvvmLight;
using LanHistory.Design;
using Microsoft.Practices.ServiceLocation;
using LanHistory.Model;
using Serilog;

namespace LanHistory.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class ViewModelLocator
    {
        private static readonly IContainer _container;

        static ViewModelLocator()
        {
            var builder = new ContainerBuilder();

            if( ViewModelBase.IsInDesignModeStatic ) builder.RegisterType<DesignDataService>().As<IDataService>();
            else builder.RegisterType<DataService>().As<IDataService>();

            builder.RegisterType<MainViewModel>();

            // define rolling log files
            string localAppPath = System.Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );

            ILogger logger = new LoggerConfiguration().WriteTo
                .RollingFile($@"{localAppPath}\LanHistory\log-{{Date}}.txt")
                .CreateLogger();

            builder.RegisterInstance( logger ).As<ILogger>().SingleInstance();

            _container = builder.Build();

            ServiceLocator.SetLocatorProvider( () => new AutofacServiceLocator( _container ) );
        }

        ///// <summary>
        ///// Gets the Main property.
        ///// </summary>
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
        //    "CA1822:MarkMembersAsStatic",
        //    Justification = "This non-static member is needed for data binding purposes.")]
        //public MainViewModel Main
        //{
        //    get
        //    {
        //        return ServiceLocator.Current.GetInstance<MainViewModel>();
        //    }
        //}

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
        }
    }
}