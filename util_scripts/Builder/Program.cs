using System;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using System.Threading;

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
        SUCCESS = 0, REGISTER_FAILED = 1, BUILD_FAILED = 2
    }

    class Main
    {
        private System.Threading.Thread thread;
        private String slnPath;
        public ErrorCode errorCode = 0;

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

        private Boolean buildSolution(Solution solution, EnvDTE80.DTE2 dte) {
            Console.WriteLine("Started Build");
            solution.SolutionBuild.Build();

            vsBuildState state = solution.SolutionBuild.BuildState;
            while (solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
            {
                System.Threading.Thread.Sleep(500);
                state = solution.SolutionBuild.BuildState;
            }

            Boolean buildSuccess = (solution.SolutionBuild.LastBuildInfo == 0 && state == vsBuildState.vsBuildStateDone);

            dte.ToolWindows.ErrorList.ShowMessages = true;
            dte.ToolWindows.ErrorList.ShowErrors = true;
            dte.ToolWindows.ErrorList.ShowWarnings = true;

            Dictionary<vsBuildErrorLevel, String> errorLevel = new Dictionary<vsBuildErrorLevel, String>() { 
                    {vsBuildErrorLevel.vsBuildErrorLevelHigh, "Error"},
                    {vsBuildErrorLevel.vsBuildErrorLevelMedium, "Warning"},
                    {vsBuildErrorLevel.vsBuildErrorLevelLow, "Info"}};
            ErrorItems errors = dte.ToolWindows.ErrorList.ErrorItems;

            for (int i = 1; i <= errors.Count; i++)
            {
                ErrorItem error = errors.Item(i);
                Console.WriteLine("Build " + errorLevel[error.ErrorLevel] + ": " + error.Description);
            }

            return buildSuccess;
        }

        private void run() {
            EnvDTE80.DTE2 dte = getDTE(VSVersion.VS_2017);

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

            if (buildSolution(solution, dte))
            {
                Console.WriteLine("Build Succeeded");
            }
            else
            {
                this.errorCode = ErrorCode.BUILD_FAILED;
            }
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
