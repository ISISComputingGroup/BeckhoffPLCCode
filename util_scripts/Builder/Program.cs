using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using EnvDTE80;
using System.IO;
using System.Reflection;
using System.Threading;
using TCatSysManagerLib;

namespace TestAutomationInterface
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

    enum PLCAction
    {
        LOGIN = 0, LOGOUT = 1, START = 2, STOP = 3
    }

    class Main
    {
        private System.Threading.Thread thread;
        public ErrorCode errorCode = 0;

        // The boilerplate for the xml that interacts with the PLC code
        private static String xmlTemplate = @"<TreeItem>
                                    <IECProjectDef>
                                        <OnlineSettings>
                                                <Commands>
                                                        <LoginCmd>{0}</LoginCmd>
                                                        <LogoutCmd>{1}</LogoutCmd>
                                                        <StartCmd>{2}</StartCmd>
                                                        <StopCmd>{3}</StopCmd>
                                                        <ResetColdCmd>false</ResetColdCmd>
                                                        <ResetOriginCmd>false</ResetOriginCmd>
                                                </Commands>
                                        </OnlineSettings>
                                    </IECProjectDef>
                                </TreeItem>";

        private String createXMLString(PLCAction action)
        {
            List<bool> options = Enumerable.Repeat(false, 4).ToList();
            options[(int)action] = true;
            List<String> strOptions = options.ConvertAll(o => o.ToString().ToLowerInvariant());
            return String.Format(xmlTemplate, strOptions.ToArray());
        }

        public Main()
        {
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

            dte.SuppressUI = false;
            dte.MainWindow.Visible = true;
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

        private void startPLC(Solution solution)
        {
            if (solution.Projects.Count > 1)
            {
                Console.WriteLine("Warning: multiple projects found, will use " + solution.Projects.Item(1).Name);
            }

            ITcSysManager systemManager = (ITcSysManager4)(solution.Projects.Item(1).Object);

            Console.WriteLine("Activating Config");
            systemManager.ActivateConfiguration();
            Console.WriteLine("Starting twinCAT");
            systemManager.StartRestartTwinCAT();

            ITcSmTreeItem plcProjectItem = systemManager.LookupTreeItem("TIPC^TestCode^TestCode Project");

            Console.WriteLine("Logging in");
            plcProjectItem.ConsumeXml(createXMLString(PLCAction.LOGIN));
            Console.WriteLine("Starting PLC");
            plcProjectItem.ConsumeXml(createXMLString(PLCAction.START));
        }

        private void run() {
            EnvDTE80.DTE2 dte = getDTE(VSVersion.VS_2017);

            Console.WriteLine("Registering message filter");
            int err = MessageFilter.Register();
            if (err != 0)
            {
                Console.WriteLine("Filter register failed: " + err);
                this.errorCode = ErrorCode.REGISTER_FAILED;
                Environment.Exit((int)this.errorCode);
            }

            Solution solution = dte.Solution;
            string sln = Path.Combine("C:\\Instrument\\Dev\\BeckhoffPLCCode\\dummy_PLC\\TestPLC.sln");
            Console.WriteLine("Opening " + sln);
            solution.Open(sln);

            if (buildSolution(solution, dte))
            {
                Console.WriteLine("Build Succeeded");
            }
            else
            {
                this.errorCode = ErrorCode.BUILD_FAILED;
                Environment.Exit((int)this.errorCode);
            }

            startPLC(solution);

            System.Threading.Thread.Sleep(1000000);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Main m = new Main();
        }
    }
}
