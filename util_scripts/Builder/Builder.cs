using System;
using System.Collections.Generic;
using EnvDTE;
using EnvDTE80;
using TCatSysManagerLib;

namespace BeckhoffBuilder
{
    class Builder
    {
        private Solution solution;
        private EnvDTE80.DTE2 dte;

        /// <summary>
        /// Class that builds the project.
        /// </summary>
        /// <param name="solution">The solution to build.</param>
        /// <param name="dte">The DTE (used to gather build errors).</param>
        public Builder(Solution solution, EnvDTE80.DTE2 dte)
        {
            this.solution = solution;
            this.dte = dte;
        }

        /// <summary>
        /// Builds the specified solution.
        /// </summary>
        /// <returns>true if build was successful, false otherwise</returns>
        public Boolean buildSolution() {
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

        /// <summary>
        /// Checks that a solution contains at least one PLC project.
        /// </summary>
        /// <returns>The project that can be run</returns> 
        public Project findPLCProject()
        {
            foreach (Project project in solution.Projects)
            {
                try
                {
                    ITcSysManager4 twinCatProject = (ITcSysManager4)project.Object;
                    ITcSmTreeItem plcConfig = twinCatProject.LookupTreeItem("TIPC");
                    Console.WriteLine("Found PLC Project: " + project.Name + "." + plcConfig.Name);
                    return project;
                } catch {
                    Console.WriteLine(project.Name + " is not a Twincat project");
                }
            }
            return null;
        }
    }
}
