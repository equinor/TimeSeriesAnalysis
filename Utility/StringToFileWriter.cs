﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TimeSeriesAnalysis.Utility
{


    ///<summary>
    /// IO Utility class to write to file that implements IDisposable interface. 
    /// <para>
    /// Suggest to use this objects of this class within the <c>using</c> keyword
    /// so that file-resources are automatically freed in case your code is terminated before
    /// it has completed. 
    /// </para>
    ///</summary>

    public class StringToFileWriter : IDisposable
        {
            StreamWriter sw;
            System.IO.MemoryStream memStream;
            string file;
            Encoding localEncoding = Encoding.UTF8;
 
            /// <summary>
            /// Constructor with local encodign
            /// </summary>
            /// <param name="filename"></param>
            public StringToFileWriter(string filename)
            {
                memStream = new System.IO.MemoryStream();
                sw = new StreamWriter(memStream, localEncoding)
                {
                    NewLine = "\r\n"
                };
                file = filename;
            }

            /// <summary>
            /// Constructor with specific encoding
            /// </summary>
            /// <param name="filename">file to be creates</param>
            /// <param name="encoding">text encoding format</param>
            public StringToFileWriter(string filename, Encoding encoding)
            {
                localEncoding = encoding;
                memStream = new System.IO.MemoryStream();
                sw = new StreamWriter(memStream, localEncoding);
                file = filename;
            }

            /// <summary>
            /// Write a single line of text 
            /// </summary>
            /// <param name="text"></param>
            public void Write(string text)
            {
                sw.Write(text);
            }

            /// <summary>
            /// Close file handles
            /// </summary>
            public bool Close()
            {
                sw.Flush();
                memStream.Position = 0;

                string binPath = System.IO.Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath);
                binPath = binPath.Replace("file:\\", "");

                StreamReader sr = new StreamReader(memStream, localEncoding);
                string result = sr.ReadToEnd();

                sw.Close();
                sw = null;

                // Check if file name contans \
                try
                {
                    string filePath = file;
                    if (file.IndexOf("\\") < 0 || file.IndexOf(@"\") < 0)
                    {

                        filePath = binPath + "\\" + file;
                    }
                    else
                    {
                        CreateDirectoryStructure(file);
                        filePath = file;
                    }

                    FileStream fileStream = new FileStream(filePath, FileMode.Create);
                    StreamWriter sw2 = new StreamWriter(fileStream, Encoding.UTF8);
                    sw2.Write(result);
                    sw2.Flush();
                    sw2.Close();
                    fileStream.Close();
                    sw = null;

                    return true;
                }
                catch
                {
                return false;
                }
                //sw = null;
            }

            /// <summary>
            /// Dispose
            /// </summary>
            /// <param name="disposing"></param>
            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // dispose managed resources
                    try
                    {
                    if (memStream!=null)
                        memStream.Close();
                    if(sw != null)
                        sw.Close();
                    }
                    catch
                    {

                    }
                }
                // free native resources
            }

            /// <summary>
            /// Disposes 
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// creates directory on path if it does not exist
            /// </summary>
            /// <param name="FilePath"></param>
            public void CreateDirectoryStructure(string FilePath)
            {
                // Create results directory if not present
                if (System.IO.Directory.Exists(FilePath.Substring(0, FilePath.LastIndexOf("\\") + 1)) == false)
                {
                    System.IO.Directory.CreateDirectory(FilePath.Substring(0, FilePath.LastIndexOf("\\") + 1));
                }
            }

        }


  


}
