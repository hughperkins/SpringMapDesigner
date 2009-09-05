using System;
using System.Collections.Generic;
using System.Text;

namespace MapDesigner
{
    class CommandQueueFactory
    {
        static CommandQueue commandqueuefromui = new CommandQueue();
        static CommandQueue commandqueuetoui = new CommandQueue();

        public static CommandQueue ToUI { get { return commandqueuetoui; } }
        public static CommandQueue FromUI { get { return commandqueuefromui; } }
    }
}
