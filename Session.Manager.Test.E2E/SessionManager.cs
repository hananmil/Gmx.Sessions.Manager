using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Session.Manager.Test.E2E
{
    internal class SessionManager
    {
        private bool IsEqueal(byte[] bufferA, byte[] bufferB)
        {
            if (bufferA.Length != bufferB.Length)
            {
                return false;
            }
            for (int i = 0; i < bufferA.Length; i++)
            {
                if (bufferA[i] != bufferB[i])
                {
                    return false;
                }
            }
            return true;
        }

        public byte[] RandomBuffer(int size)
        {
            var buffer = new byte[size];
            new Random().NextBytes(buffer);
            return buffer;
        }
    }
}
