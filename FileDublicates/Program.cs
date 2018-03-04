using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace FileDublicates
{
    class Program
    {
        Thread IOThread;

        static readonly object cashLocker = new object();
        static readonly object dataLocker = new object();
        static readonly object cLogicLocker = new object();
        static readonly object debugLocker = new object();

        static AutoResetEvent NewData = new AutoResetEvent(false);
        static AutoResetEvent waitEnd = new AutoResetEvent(false);

        static Dictionary<string, byte[]> Cash = new Dictionary<string, byte[]>();

        static string fileNames;

        static List<string> debug = new List<string>();

        static ThreadLocal<int> localCount = new ThreadLocal<int>(() => { return 0; });
        static ThreadLocal<int> localIndex = new ThreadLocal<int>(() => { return 0; });

        static ContextObject context;

        static int numberOfThreads = 1;

        static void Main(string[] args)
        {
            new Thread(() => WriteToFileAsync()).Start();
            Crawler(FindEqualLengthFiles(@"C:\Users\Skomantas\OneDrive\visual studio\FileDublicates\FileDublicates\bin\Debug\test"));
            Console.ReadKey();
        }

        static Dictionary<long, List<string>> FindEqualLengthFiles(string directory)
        {
            Directory.SetCurrentDirectory(directory);
            string[] files = Directory.GetFiles(directory);
            List<FileInfo> fileInfos = new List<FileInfo>();
            Dictionary<long, List<string>> listsOfSLFiles = new Dictionary<long, List<string>>();

            foreach (string s in files)
            {
                FileInfo fileInfo = new FileInfo(s);

                if (fileInfos.Count == 0)
                {
                    fileInfos.Add(fileInfo);
                    continue;
                }

                foreach (FileInfo i in fileInfos)
                {
                    if (i.Length == fileInfo.Length)
                    {
                        if (true == listsOfSLFiles.ContainsKey(i.Length))
                        {
                            listsOfSLFiles[i.Length].Add(fileInfo.Name);
                            AddToCashAsync(fileInfo.Name);
                            break;
                        }
                        else
                        {
                            listsOfSLFiles.Add(i.Length, new List<string>());
                            listsOfSLFiles[i.Length].Add(i.Name);
                            listsOfSLFiles[i.Length].Add(fileInfo.Name);
                            AddToCashAsync(i.Name);
                            AddToCashAsync(fileInfo.Name);
                        }
                    }
                }
                fileInfos.Add(fileInfo);
            }
            return listsOfSLFiles;
        }

        static void Crawler(Dictionary<long, List<string>> listsOfSLFiles)
        {
            foreach (KeyValuePair<long, List<string>> listOfSLFile in listsOfSLFiles)
            {
                crawl(listOfSLFile.Value);
            }

        }

        static void crawl(List<string> fileNames)
        {
            context = new ContextObject(fileNames.Count, fileNames.Count, 0, numberOfThreads);

            if (context.Count < numberOfThreads)
                context.NumberOfThreads = fileNames.Count;


            for (int i = 0; i < numberOfThreads; i++)
            {
                // could use thread pool
                new Thread(() => threadTask(fileNames)).Start();
            }
            waitEnd.WaitOne();
        }

        static void threadTask(List<string> fileNames)
        {
            // index ir count turi perduoti turi perduoti reiksmes nuo context.iteration ir context.count
            int count = 0;
            int index = 0;

            lock (cLogicLocker)
            {
                if (context.Count >= 2)
                {
                    if (context.Length != 2)
                   {
                        if (context.Iteration < context.Length - 1)
                        {
                            context.Iteration++;
                            index = context.Iteration - context.Length + context.Count;
                        }
                        else
                        {
                            if(context.Count == 2)
                            {
                                waitEnd.Set();
                                return;
                            }
                            context.Count--;
                            count = context.Length - context.Count;
                            index = 1;
                            context.Iteration = count + 1;
                        }
                   }
                    else
                    {
                        count = context.Length-2;
                        index = 1;
                        context.Count--;
                    }
                }
                else
                {
                    waitEnd.Set();
                    return;
                }
            }
            compareFromCash(fileNames[localCount.Value], fileNames[localCount.Value + localIndex.Value]);
            Console.WriteLine(fileNames[count] + fileNames[count + index]);
            threadTask(fileNames);
        }



        static void compareFromCash(string fileName1, string fileName2)
        {
           OneMore:
            if (!Cash.ContainsKey(fileName1) || !Cash.ContainsKey(fileName2))
            {
                Thread.Sleep(10);
                goto OneMore;
            }
           else
            {
                if (Cash[fileName1].SequenceEqual(Cash[fileName2]))
                    UpdateFileNames(fileName1 + '\n' + fileName2);
            }
            
        }

        void compareFromDisk(string fileName1, string fileName2)
        {
            byte[] file1Bytes;

            using (Stream file = new FileStream(fileName1, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                file1Bytes = new byte[file.Length];
                file.Read(file1Bytes, 0, file1Bytes.Length);
            }

            byte[] file2Bytes;

            using (Stream file = new FileStream(fileName2, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                file2Bytes = new byte[file.Length];
                file.Read(file2Bytes, 0, file2Bytes.Length);
            }

            if (Cash[fileName1].SequenceEqual(Cash[fileName2]))
                UpdateFileNames(fileName1 + '\n' + fileName2);
        }

        static async void WriteToFileAsync()
        {
            FileStream fileStream = File.Open("data.txt", FileMode.Append);
            StreamWriter textWriter = new StreamWriter(fileStream);
            textWriter.AutoFlush = true;
            string newOne = null;

            while (true)
            {
                NewData.WaitOne();
                lock (dataLocker)
                {
                    if (fileNames != null)
                        newOne = string.Copy(fileNames);
                }
                await textWriter.WriteLineAsync(newOne);
            }
        }

        static void UpdateFileNames(string newOne)
        {
            lock (dataLocker)
            {
                fileNames = newOne;
            }
            NewData.Set();
        }

        static async void AddToCashAsync(string fileName)
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
                    Cash.Add(fileName, fileBytes);
                }
                catch(ArgumentException e)
                {
                   
                }
            }

        }
    }
}
