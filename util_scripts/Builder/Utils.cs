using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using TCatSysManagerLib;

namespace BeckhoffBuilder
{
    class Utils
    {
        private DTE2 dte;
        private List<ErrorItem> lastErrors = new List<ErrorItem>();

        private class ErrorComparer : IEqualityComparer<ErrorItem>
        {
            public bool Equals(ErrorItem x, ErrorItem y)
            {
                return x.Description == y.Description;
            }

            public int GetHashCode(ErrorItem obj)
            {
                return obj.Description.GetHashCode();
            }
        }

        /// <summary>
        /// Utils for using the builder.
        /// </summary>
        /// <param name="dte">The DTE (used to gather build errors).</param>
        public Utils(EnvDTE80.DTE2 dte)
        {
            this.dte = dte;
            dte.ToolWindows.ErrorList.ShowMessages = true;
            dte.ToolWindows.ErrorList.ShowErrors = true;
            dte.ToolWindows.ErrorList.ShowWarnings = true;
        }

        public void printErrors()
        {
            Dictionary<vsBuildErrorLevel, String> errorLevel = new Dictionary<vsBuildErrorLevel, String>() {
                    {vsBuildErrorLevel.vsBuildErrorLevelHigh, "Error"},
                    {vsBuildErrorLevel.vsBuildErrorLevelMedium, "Warning"},
                    {vsBuildErrorLevel.vsBuildErrorLevelLow, "Info"}};
            ErrorItems errorItems = dte.ToolWindows.ErrorList.ErrorItems;

            var errors = from i in Enumerable.Range(1, errorItems.Count) select errorItems.Item(i);
            var newErrors = errors.Except(lastErrors, new ErrorComparer());

            newErrors.ToList().ForEach(error => Console.WriteLine("Build " + errorLevel[error.ErrorLevel] + ": " + error.Description));
            lastErrors = errors.ToList();
        }
    }
}
