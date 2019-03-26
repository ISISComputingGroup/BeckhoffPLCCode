using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using TCatSysManagerLib;

namespace BeckhoffBuilder
{
    class Runner
    {
        private Project project;
        private DTE2 dte;

        enum PLCAction
        {
            LOGIN = 0, LOGOUT = 1, START = 2, STOP = 3
        }

        /// <summary>
        /// Class that runs the project in simulation mode.
        /// </summary>
        /// <param name="PLCProject">The PLC project to run.</param>
        public Runner(Project PLCProject, EnvDTE80.DTE2 dte)
        {
            this.project = PLCProject;
            this.dte = dte;
        }

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

        private void printErrors()
        {
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
        }

        /// <summary>
        /// Checks that a solution contains at least one PLC project.
        /// </summary>
        public void startPLC()
        {
            Console.WriteLine("Simulating " + project.Name);
            ITcSysManager systemManager = (ITcSysManager4)(project.Object);

            Console.WriteLine("Activating Config");
            systemManager.ActivateConfiguration();
            Console.WriteLine("Starting twinCAT");
            systemManager.StartRestartTwinCAT();

            ITcSmTreeItem plcProjectItem = systemManager.LookupTreeItem("TIPC");

            Console.WriteLine("Logging in");
            plcProjectItem.ConsumeXml(createXMLString(PLCAction.LOGIN));
            Console.WriteLine("Starting PLC");
            plcProjectItem.ConsumeXml(createXMLString(PLCAction.START));

            printErrors();
        }
    }
}
