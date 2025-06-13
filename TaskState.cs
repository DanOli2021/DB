using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelDB
{
    public class TaskState
    {
        public string Result { get; set; }
        public string Message { get; set; }
        public bool Finished { get; set; } = false;
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
