using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;

namespace Core.Services
{
    public class ProjectService : IProjectService
    {
        public int val { get; private set; }

        public int AddAssetsToProject()
        {
            return val;
        }
    }
}
