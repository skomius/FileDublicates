using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDublicates
{
    class ContextObject
    {
        public int Length;
        public int Count;
        public int Iteration;
        public int NumberOfThreads;

        public ContextObject(int length, int count, int iteration, int numberOfThreads)
        {
            Length = length;
            Count = count;
            Iteration = iteration;
            NumberOfThreads = numberOfThreads;
        }
    }
}
