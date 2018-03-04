using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileDublicates
{
    class Cash
    {
        Dictionary<string, byte[]> cash = new Dictionary<string, byte[]>();
        static readonly object cashLocker = new object();

        public bool compareFromCash(string fileName1, string fileName2)
        {
            //danger of looplock
            OneMore:
            if (!cash.ContainsKey(fileName1) || !cash.ContainsKey(fileName2))
            {
                Thread.Sleep(10);
                goto OneMore;
            }
            else
            {
                return cash[fileName1].SequenceEqual(cash[fileName2]);             
            }

        }

        public async void AddFileAsync(string fileName)
        {
            byte[] fileBytes;

            using (Stream file = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileBytes = new byte[file.Length];
                await file.ReadAsync(fileBytes, 0, fileBytes.Length).ConfigureAwait(false);
            }
            lock (cashLocker)
            {
                try
                {
                    cash.Add(fileName, fileBytes);
                }
                catch (ArgumentException e)
                {

                }
            }

        }

        public void Clear()
        {
            cash.Clear();
        } 
    }
}
