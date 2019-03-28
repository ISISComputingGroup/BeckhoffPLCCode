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
        private Utils utils;
        private int START_TIMEOUT = 10000; //Twincat start timeout in ms.

        enum PLCAction
        {
            LOGIN = 0, LOGOUT = 1, START = 2, STOP = 3
        }

        /// <summary>
        /// Class that runs the project in simulation mode.
        /// </summary>
        /// <param name="PLCProject">The PLC project to run.</param>
        /// <param name="utils">A utils instance that has various utilities.</param>
        public Runner(Project PLCProject, EnvDTE80.DTE2 dte)
        {
            this.project = PLCProject;
            this.utils = new Utils(dte);
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

        /// <summary>
        /// Creates the xml required to interact with the PLC.
        /// The difference between each action is translated into a true/false list in the xml. e.g. a login has LoginCmd true and all other Cmds false.
        /// </summary>
        /// <param name="action">The action to perform</param>
        /// <returns>The constructed xml</returns>
        private String createXMLString(PLCAction action)
        {
            List<bool> options = Enumerable.Repeat(false, 4).ToList();
            options[(int)action] = true;
            List<String> strOptions = options.ConvertAll(o => o.ToString().ToLowerInvariant());
            return String.Format(xmlTemplate, strOptions.ToArray());
        }

        /// <summary>
        /// Checks that a solution contains at least one PLC project.
        /// </summary>
        public Boolean startPLC()
        {
            Console.WriteLine("Simulating " + project.Name);
            ITcSysManager4 systemManager = (ITcSysManager4)(project.Object);

            Console.WriteLine("Activating Config");
            systemManager.ActivateConfiguration();
            System.Threading.Thread.Sleep(500);

            Console.WriteLine("Starting twinCAT");
            systemManager.StartRestartTwinCAT();
            System.Threading.Thread.Sleep(500);

            for (int i=0; i < START_TIMEOUT/500; i++)
            {
                if (systemManager.IsTwinCATStarted())
                {
                    break;
                }
                this.utils.printErrors();
                System.Threading.Thread.Sleep(500);
            }

            if (!systemManager.IsTwinCATStarted())
            {
                Console.WriteLine("Twincat not starting, check that you have valid licenses!!");
                return false;
            }

            ITcSmTreeItem plcProjectItem = systemManager.LookupTreeItem("TIPC");

            Console.WriteLine("Logging in");
            plcProjectItem.ConsumeXml(createXMLString(PLCAction.LOGIN));
            Console.WriteLine("Starting PLC");
            plcProjectItem.ConsumeXml(createXMLString(PLCAction.START));

            return true;
        }
    }
}
