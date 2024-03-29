﻿using System.Collections.Generic;
using System.IO;
using Caber.ConsoleSupport;

namespace Caber.Service
{
    public class ArgumentParser
    {
        public void Parse(IEnumerable<string> arguments, Service service)
        {
            using (var iterator = arguments.GetEnumerator())
            while (iterator.MoveNext())
            {
                switch (iterator.Current)
                {
                    default:
                        throw new InvalidArgumentsException($"Unrecognised argument: {iterator.Current}");
                }
            }
        }

        public void WriteUsage(TextWriter error)
        {
        }
    }
}
