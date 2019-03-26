using System;
using EnvDTE;
using System.Threading;
using Microsoft.Win32;

namespace BeckhoffBuilder
{   
    class VSVersion {
        public static readonly VSVersion VS_2010 = new VSVersion("VisualStudio.DTE.10.0");
        public static readonly VSVersion VS_2012 = new VSVersion("VisualStudio.DTE.11.0");
        public static readonly VSVersion VS_2013 = new VSVersion("VisualStudio.DTE.12.0");
        public static readonly VSVersion VS_2015 = new VSVersion("VisualStudio.DTE.14.0");
        public static readonly VSVersion VS_2017 = new VSVersion("VisualStudio.DTE.15.0");

        public String DTEDesc;

        public VSVersion(String DTEDesc) {
            this.DTEDesc = DTEDesc;
        }
    }

    enum ErrorCode {
        SUCCESS = 0, VS_NOT_FOUND, TWINCAT_NOT_FOUND, REGISTER_FAILED, PLC_NOT_FOUND, BUILD_FAILED
    }

    class Main
    {
        private System.Threading.Thread thread;
        private String slnPath;
        public ErrorCode errorCode = 0;
        private EnvDTE80.DTE2 dte;

        /// <summary>
        /// The main class of the program.
        /// </summary>
        /// <param name="path">A path to the solution to build.</param>
        public Main(String path)
        {
            this.slnPath = path;
            //Registering the message filter must be done on a STA thread.
            thread = new System.Threading.Thread(run);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        /// <summary>
        /// Gets the installed twincat version.
        /// </summary>
        /// <returns>The TwinCat version</returns>
        private Version getInstalledTwinCATVersion()
        {
            string ret = null;
            string path = string.Empty;

            if (Environment.Is64BitOperatingSystem) {
                path = "Software\\Wow6432Node\\Beckhoff\\TwinCAT3";
            } else {
                path = "Software\\Beckhoff\\TwinCAT3";
            }

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
            {
                ret = (string)key.GetValue("CurrentVersion");
            }

            if (ret == null) {
                throw new ApplicationException("Could not determine actual TwinCAT Version!");
            }
            return new Version(ret);
        }

        /// <summary>
        /// Gets the DTE, which causes the Visual Studio process to start up in the background.
        /// </summary>
        /// <param name="version">The version of Visual Studio to use, specified as a <see cref="VSVersion"/>.</param>
        /// <returns>The DTE object that can be used to interact with Visual Studio.</returns>
        private EnvDTE80.DTE2 getDTE(VSVersion version)
        {
            Console.WriteLine("Starting " + version.DTEDesc);

            Type VSType = System.Type.GetTypeFromProgID(version.DTEDesc);
            EnvDTE80.DTE2 dte = (EnvDTE80.DTE2)System.Activator.CreateInstance(VSType);

            dte.SuppressUI = true;
            dte.MainWindow.Visible = false;
            return dte;
        }

        /// <summary>
        /// Opens a solution and builds it.
        /// </summary>
        private void run() {
            try
            {
                dte = getDTE(VSVersion.VS_2017);
            } catch (Exception e) {
                Console.WriteLine("Failed to create DTE, check correct Visual Studio version is installed: ");
                Console.WriteLine(e);
                this.errorCode = ErrorCode.VS_NOT_FOUND;
                return;
            }
            try
            {
                Version twinCatVerison = getInstalledTwinCATVersion();
                Console.WriteLine("Twincat version: " + twinCatVerison.ToString());
                if (twinCatVerison < new Version(3,1)) {
                    Console.WriteLine("Requires version 3.1");
                    this.errorCode = ErrorCode.TWINCAT_NOT_FOUND;
                    return;
                }
            } catch (Exception e) {
                Console.WriteLine("Twincat version not found: " + e.Message);
                this.errorCode = ErrorCode.TWINCAT_NOT_FOUND;
                return;
            }

            Console.WriteLine("Registering message filter");
            int err = MessageFilter.Register();
            if (err != 0)
            {
                Console.WriteLine("Filter register failed: " + err);
                this.errorCode = ErrorCode.REGISTER_FAILED;
            }

            Solution solution = dte.Solution;
            Console.WriteLine("Opening " + slnPath);
            solution.Open(slnPath);

            Builder builder = new Builder(solution, dte);
            Project plcProject = builder.findPLCProject();
            if (plcProject == null)
            {
                Console.WriteLine("No PLC Projects found");
                this.errorCode = ErrorCode.PLC_NOT_FOUND;
            }
            else if (!builder.buildSolution())
            {
                this.errorCode = ErrorCode.BUILD_FAILED;
            }
            else
            {
                Console.WriteLine("Build Succeeded");
            }

            Runner runner = new Runner(plcProject, dte);
            runner.startPLC();
        }
    }


    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify a solution file to build");
                return 1;
            }

            Main m = new Main(args[0]);
            return (int)m.errorCode;
        }
    }
}
