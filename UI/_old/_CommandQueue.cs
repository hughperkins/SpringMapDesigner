using System;
using System.Collections.Generic;
using System.Text;

namespace MapDesigner
{
    // queue of commands, threadsafe, for comms between ui thread and opengl thread
    public class CommandQueue
    {
        public delegate void CommandHandler( UICommand command );

        Queue<UICommand> commandlist = new Queue<UICommand>();

        // call from any thread, doesnt do anything
        public CommandQueue()
        {
        }

        // call from reader thread only
        public void Tick()
        {
            lock (commandlist)
            {
                while (commandlist.Count > 0)
                {
                    UICommand command = commandlist.Dequeue();
                    if (consumersbycommand.ContainsKey(command.GetType()))
                    {
                        foreach (CommandHandler handler in consumersbycommand[command.GetType()])
                        {
                            handler(command);
                        }
                    }
                }
            }
        }

        // writer thread
        public void Enqueue( UICommand command )
        {
            lock (commandlist)
            {
                commandlist.Enqueue(command);
            }
        }

        public int Count
        {
            get
            {
                lock (commandlist)
                {
                    return commandlist.Count;
                }
            }
        }

        // reader thread
        public UICommand Dequeue()
        {
            lock (commandlist)
            {
                return commandlist.Dequeue();
            }
        }

        Dictionary<Type, List<CommandHandler>> consumersbycommand = new Dictionary<Type, List<CommandHandler>>();

        // reader thread only
        public void RegisterConsumer(Type commandtype, CommandHandler handler)
        {
            if (!consumersbycommand.ContainsKey(commandtype))
            {
                consumersbycommand.Add(commandtype, new List<CommandHandler>());
            }
            consumersbycommand[commandtype].Add(handler);
        }
    }
}
