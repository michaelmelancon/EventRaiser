using System;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Should;

namespace EventRaiser.Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void ThrowsWhenDelegatePatternToGenericConversionIsUsedWithAnIncompatibleDelegate()
        {
            var target = new object();
            new Action(() => new Func<object, bool>(target.Equals).ToHandlerOf<PropertyChangedEventArgs>()).ShouldThrow<ArgumentException>();
        }

        [TestMethod]
        public void AllowsDelegatePatternToGenericConversion()
        {
            var handlerMock = new Mock<IEventHandler<PropertyChangedEventArgs>>();
            var sender = new object();
            var args = new PropertyChangedEventArgs("test");
            handlerMock.Setup(o => o.HandleEvent(sender, args)).Verifiable();
            var newHandler = new PropertyChangedEventHandler(handlerMock.Object.HandleEvent).ToHandlerOf<PropertyChangedEventArgs>();
            newHandler(sender, args);
            handlerMock.Verify(h => h.HandleEvent(sender, args), Times.Once);
        }

        [TestMethod]
        public void AllowsPlainToGenericConversion()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            var sender = new object();
            var args = EventArgs.Empty;
            handlerMock.Setup(o => o.HandleEvent(sender, args)).Verifiable();
            var newHandler = new EventHandler(handlerMock.Object.HandleEvent).ToGeneric();
            newHandler(sender, args);
            handlerMock.Verify(h => h.HandleEvent(sender, args), Times.Once);
        }


        [TestMethod]
        public void AllowsContravariantConversion()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            var sender = new object();
            var args = new PropertyChangedEventArgs("test");
            handlerMock.Setup(o => o.HandleEvent(sender, args)).Verifiable();
            var newHandler = new EventHandler<EventArgs>(handlerMock.Object.HandleEvent).ToHandlerOf<EventArgs, PropertyChangedEventArgs>();
            newHandler(sender, args);
            handlerMock.Verify(h => h.HandleEvent(sender, args), Times.Once);
        }

        [TestMethod]
        public void DoesNotThrowWhenResilient()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            var sender = new object();
            var args = new PropertyChangedEventArgs("test");
            handlerMock.Setup(o => o.HandleEvent(sender, args)).Throws(new ApplicationException()).Verifiable();
            var newHandler = new EventHandler<EventArgs>(handlerMock.Object.HandleEvent).ToHandlerOf<EventArgs, PropertyChangedEventArgs>();
            newHandler.Resilient().Raise(sender, args);
            handlerMock.Verify(h => h.HandleEvent(sender, args), Times.Once);
        }

        [TestMethod]
        public void ThrowsWhenNotResilient()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            var sender = new object();
            var args = new PropertyChangedEventArgs("test");
            handlerMock.Setup(o => o.HandleEvent(sender, args)).Throws(new ApplicationException()).Verifiable();
            var newHandler = new EventHandler<EventArgs>(handlerMock.Object.HandleEvent).ToHandlerOf<EventArgs, PropertyChangedEventArgs>();

            new Action(() => newHandler.Raise(sender, args)).ShouldThrow<ApplicationException>();

            handlerMock.Verify(h => h.HandleEvent(sender, args), Times.Once);
        }
    }
}