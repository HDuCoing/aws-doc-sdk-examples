using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dashboard
{
    internal class Machine_Definitions
    {
        public string MachineSpace { get; set; }
        public int CPU_Cores { get; set; }
        public long RAM_MB { get; set; }
        public int MaxDiskCount { get; set; }
        public string VMRegion { get; set; }

    }
}
