using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using TCatSysManagerLib;
using System.Xml;

namespace AutomationTools
{
    class Runner
    {
        private Project project;
        private Utils utils;
        private int START_TWINCAT_TIMEOUT = 30000; //Twincat start timeout in ms.
        private int COMMAND_TIMEOUT = 30000; //Login/start/stop timeout in ms.

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

        private Boolean checkWithTimeout(int timeout, Func<Boolean> checkMethod)
        {
			const int TIME_BETWEEN_CHECKS = 500;
            for (int i = 0; i < timeout / TIME_BETWEEN_CHECKS; i++)
            {
                if (checkMethod())
                {
                    return true;
                }
                this.utils.printErrors();
                System.Threading.Thread.Sleep(TIME_BETWEEN_CHECKS);
            }
            return checkMethod();
        }

        /// <summary>
        /// Checks that the specified tag in the XML from the project tree matches the expected value. 
        /// </summary>
        /// <param name="plcProjectItem">The tree to search.</param> 
        /// <param name="tag">The tag to search for.</param> 
        /// <param name="expected">The expected value.</param>
        /// <returns>True if the value is found, else false.</returns>
        private Boolean checkXmlIsString(ITcSmTreeItem plcProjectItem, String tag, String expected)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(plcProjectItem.ProduceXml(true));
            XmlNodeList loggedIn = doc.SelectNodes(String.Format("//{0}", tag));
            return loggedIn.Item(0).InnerText.Equals(expected);
        }

        private Boolean login(ITcSmTreeItem plcApp)
        {
            Console.WriteLine("Logging in");
            plcApp.ConsumeXml(createXMLString(PLCAction.LOGIN));

            if (!checkWithTimeout(COMMAND_TIMEOUT, () => checkXmlIsString(plcApp, "LoggedIn", "true")))
            {
                Console.WriteLine("Failed to login!");
                return false;
            }
            return true;
        }

        private Boolean isRunning(ITcSmTreeItem plcProjectItem)
        {
            return checkXmlIsString(plcProjectItem, "PlcAppState", "Run");
        }

        /// <summary>
        /// Get the PLC App, assuming only one PLC.
        /// </summary>
        /// <param name="systemManager">The system manager to get the app from.</param>
        /// <returns>The PLC app</returns>
        private ITcSmTreeItem getPLCApp(ITcSysManager4 systemManager)
        {
            IEnumerable<ITcSmTreeItem> plcConfigs = systemManager.LookupTreeItem("TIPC").Child[1].Cast<ITcSmTreeItem>();
            return plcConfigs.Where(child => child.ItemSubTypeName == "TREEITEMTYPE_PLCAPP").Single();
        }

        /// <summary>
        /// Checks that a solution contains at least one PLC project and activates the configuration.
        /// </summary>
        public Boolean activatePLC()
        {
            Console.WriteLine("Simulating " + project.Name);
            ITcSysManager4 systemManager = (ITcSysManager4)(project.Object);

            Console.WriteLine("Activating Config");
            systemManager.ActivateConfiguration();
            System.Threading.Thread.Sleep(500);

            Console.WriteLine("Starting twinCAT");
            systemManager.StartRestartTwinCAT();
            System.Threading.Thread.Sleep(1000);

            if (!checkWithTimeout(START_TWINCAT_TIMEOUT, systemManager.IsTwinCATStarted))
            {
                Console.WriteLine("Twincat not starting, check that you have valid licenses!!");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks that a solution contains at least one PLC project and starts the project running.
        /// </summary>
        public Boolean startPLC()
        {
            Console.WriteLine("Starting " + project.Name);
            ITcSysManager4 systemManager = (ITcSysManager4)(project.Object);

            ITcSmTreeItem plcApp = getPLCApp(systemManager);

            System.Threading.Thread.Sleep(1000);
            if (!login(plcApp))
            {
                return false;
            }

            if (isRunning(plcApp))
            {
                Console.WriteLine("PLC is running");
                return true;
            }

            System.Threading.Thread.Sleep(1000);
            Console.WriteLine("Starting PLC");
            plcApp.ConsumeXml(createXMLString(PLCAction.START));

            if (!checkWithTimeout(COMMAND_TIMEOUT, () => isRunning(plcApp)))
            {
                Console.WriteLine("Failed to start!");
                return false;
            }

            return true;
        }
		
		/// <summary>
        /// Stops the PLC, assuming that you already have a running PLC.
        /// </summary>
        public Boolean stopPLC()
        {
            Console.WriteLine("Stopping " + project.Name);
            ITcSysManager4 systemManager = (ITcSysManager4)(project.Object);

			if (!systemManager.IsTwinCATStarted())
			{
				Console.WriteLine("No Twincat project running!!");
				return false;
			}

            ITcSmTreeItem plcApp = getPLCApp(systemManager);

            System.Threading.Thread.Sleep(1000);
            if (!login(plcApp))
            {
                return false;
            }

            System.Threading.Thread.Sleep(1000);
            Console.WriteLine("Stopping PLC");
            plcApp.ConsumeXml(createXMLString(PLCAction.STOP));

            if (!checkWithTimeout(COMMAND_TIMEOUT, () => checkXmlIsString(plcApp, "PlcAppState", "Stop")))
            {
                Console.WriteLine("Failed to stop!");
                return false;
            }

            return true;
        }
    }
}
