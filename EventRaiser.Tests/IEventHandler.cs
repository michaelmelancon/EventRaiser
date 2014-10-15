using System;

namespace EventRaiser.Tests
{
    public interface IEventHandler<in T> where T : EventArgs
    {
        void HandleEvent(object sender, T args);
    }
}
