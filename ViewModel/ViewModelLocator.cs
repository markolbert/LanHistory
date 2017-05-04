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
using Olbert.LanHistory.Design;
using Microsoft.Practices.ServiceLocation;
using Olbert.LanHistory.Model;
using Serilog;
using Serilog.Core;

namespace Olbert.LanHistory.ViewModel
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

            builder.Register<Model.LanHistory>( ctx =>
                {
                    var lhm = new Model.LanHistory();

                    var dataService = ctx.Resolve<IDataService>();
                    var curLH = dataService.GetLanHistory();

                    if( curLH != null )
                    {
                        lhm.LastBackup = curLH.LastBackup;
                        lhm.MacAddress = curLH.MacAddress;
                        lhm.UNCPath = curLH.UNCPath;
                    }

                    return lhm;
                } )
                .SingleInstance();

            builder.RegisterType<ContextMenuViewModel>().SingleInstance();
            builder.RegisterType<BackupTimer>().SingleInstance();

            // define shared rolling log files
            string localAppPath = System.Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );

            builder.Register<Logger>( ctx =>
                    new LoggerConfiguration()
                        .WriteTo
                        .RollingFile( pathFormat: $@"{localAppPath}\LanHistory\log-{{Date}}.txt", shared: true )
                        .CreateLogger() )
                .As<ILogger>();

            _container = builder.Build();

            ServiceLocator.SetLocatorProvider( () => new AutofacServiceLocator( _container ) );
        }

        ///// <summary>
        ///// Gets the Configuration property.
        ///// </summary>
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
        //    "CA1822:MarkMembersAsStatic",
        //    Justification = "This non-static member is needed for data binding purposes.")]
        //public ConfigurationViewModel Configuration
        //{
        //    get
        //    {
        //        return ServiceLocator.Current.GetInstance<ConfigurationViewModel>();
        //    }
        //}

        public Model.LanHistory LanHistory => ServiceLocator.Current.GetInstance<Model.LanHistory>();
        public ILogger Logger => ServiceLocator.Current.GetInstance<ILogger>();
        public BackupTimer BackupTimer => ServiceLocator.Current.GetInstance<BackupTimer>();
        public IDataService DataService => ServiceLocator.Current.GetInstance<IDataService>();

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
        }
    }
}