using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TimeSeriesAnalysis.Utility
{
    /// <summary>
    /// Enum to set the log level of ParserFeedback
    /// </summary>
    public enum ParserfeedbackMessageLevel
    {
        /// <summary>
        /// Show only fatal error messages
        /// </summary>
        fatal = 5,
        /// <summary>
        /// Show error messages and above
        /// </summary>
        error = 4,
        /// <summary>
        /// Show warning messages and above
        /// </summary>
        warn = 3,
        /// <summary>
        /// Show information messages and above
        /// </summary>
        info = 2,
       //no debug messages not here!
    }

    /// <summary>
    /// Struct for each log message of ParserFeedback
    /// </summary>
    public struct ParserFeedbackLogLine
    {
        /// <summary>
        /// Time of message
        /// </summary>
        public DateTime time;
        /// <summary>
        /// Message Text
        /// </summary>
        public String message;
        /// <summary>
        /// The level of the message
        /// </summary>
        public ParserfeedbackMessageLevel messageLevel;
    }



    /// <summary>
    /// Utility class is responsible for collecting feedback lines, such as warnings,error or info text to either the console window,
    /// visual-studio output/debug window, to a file structure or all. 
    /// <para>
    /// The class makes it easy to switch between displaying output to a console while debugging while
    /// to outputting to file when code moves to a server. 
    /// Suitable for collecting debugging info from services that run many cases repeatedly.
    /// </para>
    /// <para>
    ///  log levels:  INFO,WARN,ERROR,FATAL  (no debug messages here)
    ///  </para>
    /// </summary>
    public class ParserFeedback
    {
       
        // if you are very unlucky you could have a while loop spitting out millions of error messages or warnings
        // which would spam server disks, this is a fail-safe
        const int MaxNumberOfErrorsToLog = 1000;

        private bool doOutputAlsoToConsole = false;
        private bool doOutputAlsoToDebug = false;

        int nFatalErrors;
        int nErrors;
        int nWarnings;
      //  int nInfo;

        private string loggDir                 = "log";
        private const  string loggName         = "ParserFeedback";
        private string logfilename;

        /// <summary>
        /// Returns the name of the current log file
        /// </summary>
        /// <returns></returns>
        public string GetLogFilename() { return logfilename;}
        private string fullLogFileName;

        /// <summary>
        /// Returns the path to which log files are written
        /// </summary>
        /// <returns></returns>
        public string GetLogFilePath() { return fullLogFileName; }

        private FileStream commonFilestream;
        private FileStream caseFilestream;
        private static System.IO.StreamWriter commonLogFile = null;
        private static System.IO.StreamWriter caseLogFile = null;

        private List<ParserFeedbackLogLine> logList;
        private string[] caseArray;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="doOutputAlsoToConsole"></param>
        public ParserFeedback(bool doOutputAlsoToConsole = false)
        {
            doOutputAlsoToConsole = false;
            logList = new List<ParserFeedbackLogLine>();
            ResetCounters();
        }

        /// <summary>
        /// Enable (or disable) console output - i.e. writing messages directly to screen
        /// (useful for debugging, but leave off if running on a server)
        /// </summary>
        /// <param name="doEnable"></param>
        public void EnableConsoleOutput(bool doEnable=true)
        {
            this.doOutputAlsoToConsole = doEnable;

        }

        /// <summary>
        /// Enables or disables the output to Visual Studio debug window and to console out
        /// </summary>
        /// <param name="doEnable"></param>
        public void EnableDebugOutput(bool doEnable=true)
        {
            this.doOutputAlsoToDebug = doEnable;
            this.doOutputAlsoToConsole = doEnable;
        }

        /// <summary>
        /// Close a log file belong to a perticular case
        /// </summary>
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
            logList  = new List<ParserFeedbackLogLine>();
        }

        /// <summary>
        /// If output is to be divided into multiple log files, set the names of each "case"
        /// </summary>
        /// <param name="caseArray"></param>
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
                commonLogFile = new System.IO.StreamWriter(commonFilestream)
                {
                    AutoFlush = true
                };
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
                caseLogFile = new System.IO.StreamWriter(caseFilestream)
                {
                    AutoFlush = true
                };
            }
            catch (Exception e)
            {
                Console.WriteLine("ParserFeedback exception:" + fullLogFileName + ":" + e.Message);
            }
        }

        /// <summary>
        /// Reset all  error and warning counters
        /// </summary>
        public void ResetCounters()
        {
            nFatalErrors    = 0;
            nErrors         = 0;
            nWarnings       = 0;
        }

        private void StoreMessage(string msgString, ParserfeedbackMessageLevel msgLevel)
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
                    DateTime timestamp = DateTime.Now;

                    ParserFeedbackLogLine currentLogLine = new ParserFeedbackLogLine
                    {
                        time = timestamp,
                        message = msgString,
                        messageLevel = msgLevel
                    };

                    logList.Add(currentLogLine);
                    //message also output to console if appdebug==2
                    if (doOutputAlsoToConsole)
                    {
                        Console.WriteLine(msgString);
                    }
                    if (doOutputAlsoToDebug)
                    {
                        string pad = "*****";
                        Debug.WriteLine(pad + msgString + pad);
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

        /// <summary>
        /// Adds a fatal error 
        /// </summary>
        /// <param name="message"></param>
        public void AddFatalError(string message)
        {
            nFatalErrors++;
            StoreMessage("Fatal Error :" + message+ "\r\n", ParserfeedbackMessageLevel.fatal);
        }

        /// <summary>
        /// Add an error message 
        /// </summary>
        /// <param name="message"></param>
        public void AddError(string message)
        {
            nErrors++;
            StoreMessage("Error:" + message, ParserfeedbackMessageLevel.error);
        }

        ///<summary>
        ///  For testing, this is a way to check that no errors or warnings have been given.
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
            StoreMessage("Warning:" + message, ParserfeedbackMessageLevel.warn);
        }

        ///<summary>
        ///     Adds an info message
        ///</summary>
        public void AddInfo(string message)
        {
            StoreMessage("Info:"+message, ParserfeedbackMessageLevel.info);
        }

        ///<summary>
        ///     Intended for unit tests, get the first error or warning message
        ///</summary>
        public string GetFirstErrorOrWarning()
        {
            List<string> list = GetListOfAllLogLinesAtOrAboveLevel(ParserfeedbackMessageLevel.warn);

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

        public List<string> GetListOfAllLogLinesOfLevel(ParserfeedbackMessageLevel desiredLevel)
        {
            IEnumerable <string> ret = from a in logList
                      where a.messageLevel == desiredLevel
                      select a.message;
            return ret.ToList() ;
        }

        ///<summary>
        ///     Returns all log lines at or above a specified level
        ///</summary>
        public List<string> GetListOfAllLogLinesAtOrAboveLevel(ParserfeedbackMessageLevel desiredLevel = ParserfeedbackMessageLevel.warn)
        {
            IEnumerable<string> ret = from a in logList
                      where a.messageLevel >= desiredLevel 
                      select a.message;
            return ret.ToList();
        }

        ///<summary>
        ///     Returns all log lines at or belowe a specified level
        ///</summary>
        public List<string> GetListOfAllLogLinesAtOrBelowLevel(ParserfeedbackMessageLevel desiredLevel = ParserfeedbackMessageLevel.warn)
        {
            IEnumerable<string> ret = from a in logList
                                    where a.messageLevel <= desiredLevel
                                    select a.message;
            return ret.ToList();
        }

    }
}
