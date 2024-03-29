﻿//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseTask.cs">(c) 2017 Mike Fourie and Contributors (https://github.com/mikefourie/MSBuildExtensionPack) under MIT License. See https://opensource.org/licenses/MIT </copyright>
//-------------------------------------------------------------------------------------------------------------------------------------------------------------------
namespace MSBuild.ExtensionPack
{
    using System;
    using System.Globalization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Provides a common task for all the MSBuildExtensionPack Tasks
    /// </summary>
    public abstract class BaseTask : Task
    {
        private string machineName;

        /// <summary>
        /// Sets the TaskAction.
        /// </summary>
        public virtual string TaskAction { get; set; }

        /// <summary>
        /// Sets the MachineName.
        /// </summary>
        public virtual string MachineName
        {
            get { return this.machineName ?? Environment.MachineName; }
            set { this.machineName = value; }
        }

        /// <summary>
        /// Sets the UserName
        /// </summary>
        public virtual string UserName { get; set; }

        /// <summary>
        /// Sets the UserPassword.
        /// </summary>
        public virtual string UserPassword { get; set; }

        /// <summary>
        /// Sets the authority to be used to authenticate the specified user.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Set to true to log the full Exception Stack to the console.
        /// </summary>
        public bool LogExceptionStack { get; set; }

        /// <summary>
        /// Set to true to suppress all Message logging by tasks. Errors and Warnings are not affected.
        /// </summary>
        public bool SuppressTaskMessages { get; set; }

        /// <summary>
        /// Set to true to error if the task has been deprecated
        /// </summary>
        public bool ErrorOnDeprecated { get; set; }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns>bool</returns>
        public override sealed bool Execute()
        {
            this.DetermineLogging();
            try
            {
                this.InternalExecute();
                return !this.Log.HasLoggedErrors;
            }
            catch (Exception ex)
            {
                this.GetExceptionLevel();
                this.Log.LogErrorFromException(ex, this.LogExceptionStack, true, null);
                return !this.Log.HasLoggedErrors;
            }
        }

        /// <summary>
        /// Determines whether the task is targeting the local machine
        /// </summary>
        /// <returns>bool</returns>
        internal bool TargetingLocalMachine()
        {
            return this.TargetingLocalMachine(false);
        }

        /// <summary>
        /// Determines whether the task is targeting the local machine
        /// </summary>
        /// <param name="canExecuteRemotely">True if the current TaskAction can run against a remote machine</param>
        /// <returns>bool</returns>
        internal bool TargetingLocalMachine(bool canExecuteRemotely)
        {
            if (string.Compare(this.MachineName, Environment.MachineName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (!canExecuteRemotely)
                {
                    this.Log.LogError(string.Format(CultureInfo.CurrentCulture, "This task does not support remote execution. Please remove the MachineName: {0}", this.MachineName));
                }

                return false;
            }

            return true;
        }

        internal void LogTaskWarning(string message)
        {
            this.Log.LogWarning(message);
        }

        internal void LogTaskMessage(MessageImportance messageImportance, string message)
        {
            this.LogTaskMessage(messageImportance, message, null);
        }

        internal void LogTaskMessage(string message, object[] arguments)
        {
            this.LogTaskMessage(MessageImportance.Normal, message, arguments);
        }

        internal void LogTaskMessage(string message)
        {
            this.LogTaskMessage(MessageImportance.Normal, message, null);
        }

        internal void LogTaskMessage(MessageImportance messageImportance, string message, object[] arguments)
        {
            if (!this.SuppressTaskMessages)
            {
                if (arguments == null)
                {
                    this.Log.LogMessage(messageImportance, message);
                }
                else
                {
                    this.Log.LogMessage(messageImportance, message, arguments);
                }
            }
        }

        /// <summary>
        /// This is the main InternalExecute method that all tasks should implement
        /// </summary>
        /// <remarks>
        /// LogError should be thrown in the event of errors
        /// </remarks>
        protected abstract void InternalExecute();

        private void GetExceptionLevel()
        {
            string s = Environment.GetEnvironmentVariable("LogExceptionStack", EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrEmpty(s))
            {
                this.LogExceptionStack = Convert.ToBoolean(s, CultureInfo.CurrentCulture);
            }
        }

        private void DetermineLogging()
        {
            string s = Environment.GetEnvironmentVariable("SuppressTaskMessages", EnvironmentVariableTarget.Machine);

            if (!string.IsNullOrEmpty(s))
            {
                this.SuppressTaskMessages = Convert.ToBoolean(s, CultureInfo.CurrentCulture);
            }
        }
    }
}