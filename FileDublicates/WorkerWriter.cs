using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileDublicates
{
    class WorkerWriter: IDisposable
    {
        public string fileNames;
    
        static AutoResetEvent NewData = new AutoResetEvent(false);
        static AutoResetEvent DataSet = new AutoResetEvent(false);

        public WorkerWriter()
        {

        }

        async void WriteToFileAsync()
        {
            FileStream fileStream = File.Open("data.txt", FileMode.Append);
            StreamWriter textWriter = new StreamWriter(fileStream);
            textWriter.AutoFlush = true;
            string newOne = null;

            while (true)
            {
                NewData.Set();
                DataSet.WaitOne();
                if (fileNames != null)
                    newOne = string.Copy(fileNames);
                await textWriter.WriteLineAsync(newOne);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                  
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);

        }
        #endregion
    }
}
