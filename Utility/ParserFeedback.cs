﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TimeSeriesAnalysis
{
    public enum MessageLevel
    {
        fatal = 5,
        error = 4,
        warn = 3,
        info = 2,
       //no debug messages not here!
    }

    public struct LogLine
    {
        public DateTime time;
        public String message;
        public MessageLevel messageLevel;
    }

    /*
 

    */
    ///<summary>
    ///     This class is responsible for collecting feedback lines to a file structure 
    ///     Suitable for collecting debugging info from services that run many cases repeatedly.
    ///     log levels:  INFO<WARN<ERROR<FATAL  (no debug messages here)
    ///</summary>


    public class ParserFeedback
    {
       
        // if you are very unlucky you could have a while loop spitting out millions of error messages or warnings
        // which would spam server disks, this is a fail-safe
        const int MaxNumberOfErrorsToLog = 1000;

        // retained variables for info to pass to slack
        private string currentConfigFile = "";
        private string  shortConfigFileName = "";
        private string configModifiedDate = "";
        // 

        private bool doOutputAlsoToConsole = false;

        int nFatalErrors;
        int nErrors;
        int nWarnings;
        int nInfo;

        private int numberOfSlackErrorMessagesSent = 0;

        int nLogMessages;

        private string loggDir                 = "log";
        private const  string loggName         = "ParserFeedback";
        private string logfilename;
        public string GetLogFilename() { return logfilename;}
        private string fullLogFileName;
        public string GetLogFilePath() { return fullLogFileName; }

        private FileStream commonFilestream;
        private FileStream caseFilestream;
        private static System.IO.StreamWriter commonLogFile = null;
        private static System.IO.StreamWriter caseLogFile = null;

        private List<LogLine> logList;
        private string[] caseArray;
        private string[] eventArray;

        public ParserFeedback(bool doOutputAlsoToConsole = false)
        {
            doOutputAlsoToConsole = false;
            numberOfSlackErrorMessagesSent = 0;
            logList = new List<LogLine>();
            ResetCounters();
        }

        public void EnableConsoleOutput(bool doEnable)
        {
            this.doOutputAlsoToConsole = doEnable;

        }


        public void CloseCaseLogFile()
        {
            if (caseLogFile != null)
            {
                if (caseLogFile.BaseStream != null)
                {
                    caseLogFile.Flush();
                    caseLogFile.Close();
                }
            }
        }


        ///<summary>
        /// Flushes all messages to file and closes file handlers.
        ///</summary>

        public void Close()
        {
            CloseCaseLogFile();
            if (commonLogFile != null)
            {
                if (commonLogFile.BaseStream != null)
                {
                    commonLogFile.Flush();
                    commonLogFile.Close();
                }
            }
            logList  = new List<LogLine>();
        }

        public void SetCaseArray(string[] caseArray)
        {
            this.caseArray = caseArray;
        }

        ///<summary>
        ///     Creates a html file, where is "case" is presented in an iframe- useful for quickly viewing many cases
        ///     Needs SetCaseArray to be called first
        ///</summary>

        public void CreateCommonHTMLfile()
        {
            string commonlLogFileName = loggDir + "\\" + loggName + ".html";
            FileStream commonFilestream = new FileStream(commonlLogFileName, FileMode.Create, FileAccess.Write);
            StreamWriter commonHTMLFile = new System.IO.StreamWriter(commonFilestream);

            commonHTMLFile.Write(
                "<html> \r\n" +
                "<head></head>\r\n" +
                "<style>\r\n" +
                "iframe{width:100%;}\r\n" +
                "h1{ font-size:14; font:arial; }\r\n" +
                "</style>\r\n" +
                "<body>\r\n");

            commonHTMLFile.Write(
                    "<h1>most recent run:</h1>\r\n" +
                    "<iframe src=ParserFeedback.txt></iframe>\r\n"
                    );

            foreach (string caseName in caseArray)
            {
                commonHTMLFile.WriteLine("<h1>\'" + caseName.Trim() + "\' case</h1>");
                commonHTMLFile.WriteLine("<iframe src=\"" + loggName + "_" + caseName.Trim() + ".txt\"></iframe>\r\n");
            }
            if(eventArray != null)
            {
                foreach (string eventName in eventArray)
                {
                    string parserFeedbackFileName = "..\\"+ eventName.Trim() + "\\ParserFeedback.txt";
                    commonHTMLFile.WriteLine("<h1>\'" + eventName.Trim() + "\' event</h1>");
                    commonHTMLFile.WriteLine("<iframe src=\"" + parserFeedbackFileName + "\"></iframe>\r\n");
                }
            }
            commonHTMLFile.Write(
                    "</body>\r\n" +
                    "</html>\r\n"
                    );

            commonHTMLFile.Flush();
            commonHTMLFile.Close();
            
        }

        ///<summary>
        ///     Creates a new empty log file and, resets counters etc. This is a "common" file if no cases are specificed
        ///</summary>
        public void CreateCommonLogFile(string loggDir)
        {
            //cleanup old logfile if neccessary
            Close();

            this.loggDir = loggDir;
            logfilename = loggName; //+ "_node" + node;
            fullLogFileName = loggDir + "\\" + logfilename + ".txt";
            string fullOldLogFileName = loggDir + "\\" + logfilename + "_old.txt";

            try
            {
                System.IO.Directory.CreateDirectory(loggDir);
                if (System.IO.File.Exists(fullOldLogFileName))
                    System.IO.File.Delete(fullOldLogFileName);
                if (System.IO.File.Exists(fullLogFileName))
                    System.IO.File.Move(fullLogFileName, fullOldLogFileName);
                commonFilestream = new FileStream(fullLogFileName, FileMode.Create, FileAccess.Write,
                System.IO.FileShare.ReadWrite);
                commonLogFile = new System.IO.StreamWriter(commonFilestream);
                commonLogFile.AutoFlush = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("ParserFeedback exception:" + fullLogFileName + ":" + e.Message);
            }
            ResetCounters();
            
        }

        ///<summary>
        ///     Creates a new empty log file for a specific case name. Calling this function before
        ///     StoreMessage will cause all messages to be copied to it.
        ///</summary>

        public void CreateCaseLogFile(string caseName, int caseNum)
        {

            //cleanup old logfile if neccessary
            CloseCaseLogFile();
            caseName = caseName.Trim();
            if (caseName == null || caseName == "")
                caseName = caseNum.ToString();
            logfilename = loggName + "_" + caseName; 
            fullLogFileName = loggDir + "\\" + logfilename + ".txt";
            string fullOldLogFileName = this.loggDir + "\\" + logfilename + "_old.txt";

            try
            {
                System.IO.Directory.CreateDirectory(loggDir);
                if (System.IO.File.Exists(fullOldLogFileName))
                    System.IO.File.Delete(fullOldLogFileName);
                if (System.IO.File.Exists(fullLogFileName))
                    System.IO.File.Move(fullLogFileName, fullOldLogFileName);
                caseFilestream = new FileStream(fullLogFileName, FileMode.Create, FileAccess.Write,
                System.IO.FileShare.ReadWrite);
                caseLogFile = new System.IO.StreamWriter(caseFilestream);
                caseLogFile.AutoFlush = true;
            }
            catch (Exception e)
            {
                Console.WriteLine("ParserFeedback exception:" + fullLogFileName + ":" + e.Message);
            }
        }

        public void ResetCounters()
        {
            numberOfSlackErrorMessagesSent = 0;
            nFatalErrors    = 0;
            nErrors         = 0;
            nWarnings       = 0;
            nInfo           = 0;
            nLogMessages    = 0;
        }

        private void StoreMessage(string msgString, MessageLevel msgLevel)
        {
            try
            {
                // the buildup of softtags can sometimes give alot of the same error message,
                // one per sample, this check ensures each error is counted only once
                bool doLog = true;
                if (logList.Count > 0)
                    if (logList.Last().message == msgString)
                        doLog = false;
                if (logList.Count> MaxNumberOfErrorsToLog)
                    doLog = false;
                if (doLog)
                {
                    nLogMessages++;
                    DateTime timestamp = DateTime.Now;

                    LogLine currentLogLine = new LogLine();
                    currentLogLine.time = timestamp;
                    currentLogLine.message = msgString;
                    currentLogLine.messageLevel = msgLevel;

                    logList.Add(currentLogLine);
                    //message also output to console if appdebug==2
                    if (doOutputAlsoToConsole)
                    {
                        Console.WriteLine(msgString);
                    }
                    if (commonLogFile != null)
                    {
                        if (commonLogFile.BaseStream != null)
                        {
                            commonLogFile.WriteLine(timestamp.ToString("yyyy-MM-dd HH:mm:ss") + ">" + msgString);
                            commonLogFile.Flush();
                            commonFilestream.Flush();
                        }
                    }
                    if (caseLogFile != null)
                    {
                        if (caseLogFile.BaseStream != null)
                        {
                            caseLogFile.WriteLine(timestamp.ToString("yyyy-MM-dd HH:mm:ss") + ">" + msgString);
                            caseLogFile.Flush();
                            caseFilestream.Flush();
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ParserFeedback error:" + e.Message);
            }
        }

        public void AddFatalError(string message, string attachment=null)
        {
            nFatalErrors++;
            StoreMessage("Fatal Error :" + message+ "\r\n"+ attachment, MessageLevel.fatal);
        }

        public void AddError(string message,string attachment=null)
        {
            nErrors++;
            StoreMessage("Error:" + message, MessageLevel.error);
        }

        ///<summary>
        ///     For testing, this is a way to check that no errors or warnings have been given.
        ///</summary>
        public bool IsNumberOfErrorsAndWarningsZero()
        {
            if (nWarnings > 0)
                return false;
            if (nErrors > 0)
                return false;
            if (nFatalErrors > 0)
                return false;
            return true;

        }
        ///<summary>
        ///     Adds a warning
        ///</summary>
        public void AddWarning(string message)
        {
            nWarnings++;
            StoreMessage("Warning:" + message, MessageLevel.warn);
        }

        ///<summary>
        ///     Adds an info message
        ///</summary>
        public void AddInfo(string message)
        {
            nInfo++;
            StoreMessage("Info:"+message, MessageLevel.info);
        }

        ///<summary>
        ///     Intended for unit tests, get the first error or warning message
        ///</summary>
        public string GetFirstErrorOrWarning()
        {
            List<string> list = GetListOfAllLogLinesAtOrAboveLevel(MessageLevel.warn);

           if (list.Count > 0)
                return list.ElementAt(0).ToString();
            else
                return "";
        }

        /*
        public void SetCurrentConfigFile(string currentConfigFile, string configModifiedDate)
        {
            this.currentConfigFile = currentConfigFile;
            this.shortConfigFileName = Path.GetFileName(currentConfigFile);
            this.configModifiedDate = configModifiedDate;
            AddInfo("Parsing " + currentConfigFile+ " (modified:"+ configModifiedDate + ")");
        }
        */

        ///<summary>
        ///     Returns all log lines of a specified level
        ///</summary>

        public List<string> GetListOfAllLogLinesOfLevel(MessageLevel desiredLevel)
        {
            IEnumerable <string> ret = from a in logList
                      where a.messageLevel == desiredLevel
                      select a.message;
            return ret.ToList() ;
        }

        ///<summary>
        ///     Returns all log lines at or above a specified level
        ///</summary>
        public List<string> GetListOfAllLogLinesAtOrAboveLevel(MessageLevel desiredLevel = MessageLevel.warn)
        {
            IEnumerable<string> ret = from a in logList
                      where a.messageLevel >= desiredLevel 
                      select a.message;
            return ret.ToList();
        }

        ///<summary>
        ///     Returns all log lines at or belowe a specified level
        ///</summary>
        public List<string> GetListOfAllLogLinesAtOrBelowLevel(MessageLevel desiredLevel = MessageLevel.warn)
        {
            IEnumerable<string> ret = from a in logList
                                    where a.messageLevel <= desiredLevel
                                    select a.message;
            return ret.ToList();
        }

    }
}