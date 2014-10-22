using System;
using System.Collections.Generic;
using System.Linq;

namespace EventRaiser.Tests
{
    public interface IEventHandler<in T> where T : EventArgs
    {
        void HandleEvent(object sender, T args);
    }
}
