using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyFTP.Services
{
    public interface ILogger : IDisposable
    {
        void WriteLine(string message);
    }
}