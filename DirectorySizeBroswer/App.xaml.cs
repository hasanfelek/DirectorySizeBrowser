using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DirectorySizeBroswer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            System.Security.Principal.WindowsIdentity identity;
            System.Security.Principal.WindowsPrincipal principal;
            System.Windows.MessageBoxResult result;
            System.Diagnostics.ProcessStartInfo adminProcess;

            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.AppStarting;

            { // --- Alternative for using manifest for elevated privileges

                // Check if admin rights are in place
                identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                principal = new System.Security.Principal.WindowsPrincipal(identity);

                // Ask for permission if not an admin
                if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    result = System.Windows.MessageBox.Show("Can the application run in elevated mode in order to access all files?",
                        "Directory Size Browser", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        // Re-run the application with administrator privileges
                        adminProcess = new System.Diagnostics.ProcessStartInfo();
                        adminProcess.UseShellExecute = true;
                        adminProcess.WorkingDirectory = System.Environment.CurrentDirectory;
                        adminProcess.FileName = Environment.ProcessPath;
                        adminProcess.Verb = "runas";
                        try
                        {
                            System.Diagnostics.Process.Start(adminProcess);
                            // quit after starting the new process
                            this.Shutdown(0);
                        }
                        catch (System.Exception exception)
                        {
                            System.Windows.MessageBox.Show(exception.Message, "Directory Size Browser",
                               System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                            this.Shutdown(-1);
                        }
                    }
                }

            } // --- Alternative for using manifest for elevated privileges

            this.MainWindow = new DirectorySizes.DirectoryBrowser();
            this.MainWindow.ShowDialog();
        }
    }
}
