using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;

namespace Core.Services
{
    public class TestService : ITestService
    {
        public int val { get; private set; }
        public TestService()
        {
            val = Random.Shared.Next(1, 1001);
        }
        public int RetrieveProject()
        {
            return val;
        }
    }
}
