﻿namespace FuseMod
{
    using Sandbox.ModAPI;
    using System;
    using System.IO;

    public static class Logger
    {
        private readonly static string DebugFileName;
        private static TextWriter DebugWriter;

        private readonly static string ErrorLogFileName;
        private static TextWriter ErrorLogWriter;

        private static bool isInitialized = false;

        public static string ErrorFileName { get { return ErrorLogFileName; } }

        static Logger()
        {
            if (MyAPIGateway.Session != null)
            {
                DebugFileName = string.Format("Debug_{0}.log", Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
                ErrorLogFileName = string.Format("ErrorLog_{0}_{1:yyyy-MM-dd_HH-mm-ss}.log", Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath), DateTime.Now);
            }
            else
            {
                DebugFileName = string.Format("Debug_{0}.log", 0);
                ErrorLogFileName = string.Format("ErrorLog_{0}_{1:yyyy-MM-dd_HH-mm-ss}.log", 0, DateTime.Now);
            }
        }

        public static void Init()
        {
            if (!isInitialized)
            {


                DebugWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage(DebugFileName, typeof(Logger));
                isInitialized = true;

            }
        }

        public static void Debug(string text, params object[] args)
        {
            if (DebugWriter == null || !isInitialized)
                return;

            string msg = text;
            if (args.Length != 0)
                msg = string.Format(text, args);

            DebugWriter.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss:fff}] Debug - {1}", DateTime.Now, msg));
            DebugWriter.Flush();
        }
        /// <summary>
        /// Print the debug message to the chat and the log file.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="output">If you wish to toggle output to chat under certain circumstances set this</param>
        public static void DebugToChat(string text, bool output = true)
        {
            if (output)
            {
                MyAPIGateway.Utilities.ShowMessage("[Debug]", text);
            }
            Debug(text);
        }

        public static void LogException(Exception ex, string additionalInformation = null)
        {
            if (!isInitialized)
                return;

            // we create the writer when it is needed to prevent the creation of empty files
            if (ErrorLogWriter == null)
                ErrorLogWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage(ErrorLogFileName, typeof(Logger));

            ErrorLogWriter.WriteLine(string.Format("[{0:yyyy-MM-dd HH:mm:ss:fff}] Error - {1}", DateTime.Now, ex.ToString()));

            if (!string.IsNullOrEmpty(additionalInformation))
            {
                ErrorLogWriter.WriteLine(string.Format("Additional information on {0}:", ex.Message));
                ErrorLogWriter.WriteLine(additionalInformation);
            }

            ErrorLogWriter.Flush();
        }

        public static void Terminate()
        {
            if (DebugWriter != null)
            {
                DebugWriter.Flush();
                DebugWriter.Close();
                DebugWriter = null;
            }

            if (ErrorLogWriter != null)
            {
                ErrorLogWriter.Flush();
                ErrorLogWriter.Close();
                ErrorLogWriter = null;
            }

            isInitialized = false;
        }

        public static void WriteGameLog(string text, params object[] args)
        {
            string message = text;
            if (args != null && args.Length != 0)
                message = string.Format(text, args);

            if (MyAPIGateway.Utilities.IsDedicated)
                VRage.Utils.MyLog.Default.WriteLineAndConsole(message);
            else
                VRage.Utils.MyLog.Default.WriteLine(message);

            VRage.Utils.MyLog.Default.Flush();
        }
    }
}