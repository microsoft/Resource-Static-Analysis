/* 
 * Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the MIT license.
 * See LICENSE in the project root for license information.
 */

using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.ResourceStaticAnalysis.Core.Output
{
    /// <summary>
    /// Use this object if you want to be able to asynchronously spit out text into a file.
    /// In the constructor you provide the target file name.
    /// StartDocument method writes the start of the document and FinalizeDocument writes the end (for example you can provide a header and a footer of an XML file). 
    /// Any BeginWriteEntry calls in between will queue up the strings you provide to be written directly to the file.
    /// </summary>

    internal class AsyncFileOutputWriter : IDisposable
    {
        /// <summary>
        /// Used to control access to the streams.
        /// </summary>
        private static readonly ReaderWriterLockSlim StreamLock = new ReaderWriterLockSlim();
        /// <summary>
        /// File stream is used to perform async I/O
        /// </summary>
        FileStream fs;
        /// <summary>
        /// Stream Writer is used to encode output
        /// </summary>
        StreamWriter sw;
        /// <summary>
        /// Memory stream is used to inteface between stream writer and file stream to enable asyc I/O
        /// </summary>
        MemoryStream ms;
        public AsyncFileOutputWriter(string filePath)
        {
            try
            {
                fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 8192, FileOptions.Asynchronous);
                ms = new MemoryStream(1024 * 1024);
                sw = new StreamWriter(ms, Encoding.UTF8);
                sw.AutoFlush = true;
            }
            catch (Exception e)
            {
                throw new OutputWriterInitializationException(
                    "Creation of file stream object failed for output writer of type: " + this.GetType().FullName,
                    e
                    );
            }
        }
        /// <summary>
        /// Adds newline to the end of whatever is specified in 'text'
        /// </summary>
        /// <param name="text"></param>
        private IAsyncResult WriteChunk(AsyncCallback callback, string text, object state)
        {
            byte[] bytes;
            IAsyncResult result;
            StreamLock.EnterWriteLock();
            try
            {
                sw.Write(text);
                bytes = ms.ToArray();
                result = fs.BeginWrite(bytes, 0, bytes.Length, callback, state);
                ms.Position = 0;
                ms.SetLength(0);
            }
            finally
            {
                StreamLock.ExitWriteLock();
            }

            return result;
        }
        private void EndChunk(IAsyncResult ar)
        {
            try
            {
                fs.EndWrite(ar);
            }
            catch (IOException e)
            {
                throw new Exception("IO operation failed when using " + this.GetType().FullName, e);
            }
        }
        public IAsyncResult StartDocument(string header)
        {
            return WriteChunk(this.EndChunk, header, null);
        }
        public IAsyncResult BeginWriteEntry(AsyncCallback callback, string xmlOutputEntry, object state)
        {
            return WriteChunk(callback, xmlOutputEntry, state);
        }

        public void EndWriteEntry(IAsyncResult ar)
        {
            EndChunk(ar);
            return;
        }

        public IAsyncResult FinalizeDocument(string footer)
        {
            return WriteChunk(this.EndChunk, footer, null);
        }

        #region IDisposable Members
        public void Dispose()
        {
            sw.Dispose();
            ms.Dispose();
            fs.Dispose();
        }
        #endregion
    } 

}
