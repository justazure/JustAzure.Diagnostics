using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.WindowsAzure.ServiceRuntime;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace NLogDemo.Worker
{
    public class MyLogger
    {
        private readonly Logger logger = LogManager.GetLogger("Main");

        public MyLogger()
        {
            InitializeLogging();
        }

        public void Info(object message)
        {
            logger.Info(message);
        }

        private void InitializeLogging()
        {
            var currentConfig = LogManager.Configuration;
            if (currentConfig == null)
            {
                var binDirectory =
                    new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase)).LocalPath;
                var configFile = Path.Combine(binDirectory, "NLog.config");

                if (File.Exists(configFile))
                {
                    var newConfig = new XmlLoggingConfiguration(configFile);
                    LogManager.Configuration = newConfig;
                    currentConfig = LogManager.Configuration;
                }
                else
                {
                    var localPath = GetLocalPath();
                    var logDirPath = Path.Combine(localPath, "logs");

                    SimpleConfigurator.ConfigureForFileLogging(Path.Combine(logDirPath, "ApplicationLog.log"));
                }
            }

            UpdateConfig(currentConfig);

            LogManager.Configuration = currentConfig;
        }

        private static string GetLocalPath()
        {
            if (!RoleEnvironment.IsAvailable)
            {
                var filePath = Path.GetTempPath();
                return filePath;
            }

            // Get the local storage information
            const string localResourceName = "LogStorage";

            // Override the file path name with the local resource 
            string localPath = String.Empty;
            try
            {
                var localResource = RoleEnvironment.GetLocalResource(localResourceName);
                localPath = localResource.RootPath;
                return localPath;
            }
            catch (Exception)
            {
                throw new ArgumentException(localResourceName + " is not a valid Azure package local resource name");
            }
        }

        private void UpdateConfig(LoggingConfiguration config)
        {
            // Get the local path directory
            var localPath = GetLocalPath();

            // Set up the ${logDir} and ${archiveDir} variables 
            var logDirPath = Path.Combine(localPath, "logs");
            if (!Directory.Exists(logDirPath))
                Directory.CreateDirectory(logDirPath);

            var archiveDirPath = Path.Combine(localPath, "archive");
            if (!Directory.Exists(archiveDirPath))
                Directory.CreateDirectory(archiveDirPath);

            // Set up the Azure role name variables
            var role = RoleEnvironment.CurrentRoleInstance.Role.Name;
            var instance = RoleEnvironment.CurrentRoleInstance.Id;

            // Update the file targets with the proper log storage directory base
            foreach (var ft in config.AllTargets.OfType<FileTarget>())
            {
                var name = ft.Name.Replace("_wrapped", "");

                var archiveFileName = String.Format("{0}Log_{1}_{2}_{3}_{{#####}}",
                    name, role, instance, @"${shortdate}.log");
                ft.ArchiveFileName = Path.Combine(archiveDirPath, archiveFileName);

                //      ft.ArchiveFileName = Path.Combine(localPath, archiveFileName);

                var fileName = String.Format("{0}Log_{1}_{2}.log", name, role, instance);
                ft.FileName = Path.Combine(logDirPath, fileName);
            }
        }
    }
}