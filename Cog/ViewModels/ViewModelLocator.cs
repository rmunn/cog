/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:Cog"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using SIL.Cog.Services;
using SIL.Machine;

namespace SIL.Cog.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}
            SimpleIoc.Default.Register<IWindowViewModelMappings, WindowViewModelMappings>();
	        SimpleIoc.Default.Register<IViewRegistrationService>(() => ViewRegistrationService.Instance);
			SimpleIoc.Default.Register<IDialogService, DialogService>();
			SimpleIoc.Default.Register<SpanFactory<ShapeNode>, ShapeSpanFactory>();

            SimpleIoc.Default.Register<MainWindowViewModel>();
			SimpleIoc.Default.Register<DataMasterViewModel>();
			SimpleIoc.Default.Register<ComparisonMasterViewModel>();
			SimpleIoc.Default.Register<VisualizationMasterViewModel>();
			SimpleIoc.Default.Register<WordListsViewModel>();
			SimpleIoc.Default.Register<VarietiesViewModel>();
			SimpleIoc.Default.Register<SensesViewModel>();
			SimpleIoc.Default.Register<DataSettingsViewModel>();
			SimpleIoc.Default.Register<StemmerSettingsViewModel>();
        }

        public MainWindowViewModel Main
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainWindowViewModel>();
            }
        }
        
        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}