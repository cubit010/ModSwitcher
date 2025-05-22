using System.Configuration;
using System.Data;
using System.Windows;

using System.IO;

namespace ModSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                File.WriteAllText("crash.log", args.ExceptionObject.ToString());

            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                File.WriteAllText("crash.log", ex.ToString());
                Shutdown();
            }
        }
    }

}
