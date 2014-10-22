using System;
using System.ComponentModel;
using System.Linq;
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

            PropertyChangedEventHandler handler = handlerMock.Object.HandleEvent;
            EventHandler<PropertyChangedEventArgs> newHandler = handler.ToHandlerOf<PropertyChangedEventArgs>();

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

            EventHandler handler = handlerMock.Object.HandleEvent;
            EventHandler<EventArgs> newHandler = handler.ToGeneric();

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

            EventHandler<EventArgs> handler = handlerMock.Object.HandleEvent;
            EventHandler<PropertyChangedEventArgs> newHandler = handler.ToHandlerOf<EventArgs, PropertyChangedEventArgs>();

            newHandler(sender, args);
            handlerMock.Verify(h => h.HandleEvent(sender, args), Times.Once);
        }

        [TestMethod]
        public void CombineCreatesAMulticastEventHandlerFromASequenceOfEventHandlers()
        {
            var handlerMock1 = new Mock<IEventHandler<EventArgs>>();
            var handlerMock2 = new Mock<IEventHandler<EventArgs>>();
            var handlerMock3 = new Mock<IEventHandler<EventArgs>>();

            EventHandler<EventArgs>[] handlers =
            {
                handlerMock1.Object.HandleEvent, 
                handlerMock2.Object.HandleEvent,
                handlerMock3.Object.HandleEvent
            };

            EventHandler<EventArgs> handler = handlers.Combine();

            handler.GetInvocationList().ShouldEqual(handlers.Cast<Delegate>());
        }

        [TestMethod]
        public void RaiseInvokesGenericEventHandler()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            handlerMock.Setup(o => o.HandleEvent(this, EventArgs.Empty)).Verifiable();

            EventHandler<EventArgs> handler = handlerMock.Object.HandleEvent;
            handler.Raise(this, EventArgs.Empty);

            handlerMock.Verify();
        }

        [TestMethod]
        public void RaiseInvokesEventHandler()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            handlerMock.Setup(o => o.HandleEvent(this, EventArgs.Empty)).Verifiable();

            EventHandler handler = handlerMock.Object.HandleEvent;
            handler.Raise(this, EventArgs.Empty);

            handlerMock.Verify();
        }

        [TestMethod]
        public void RaiseDoesNotThrowIfGenericEventHandlerIsNull()
        {
            EventHandler<EventArgs> handler = null;
            handler.Raise(this, EventArgs.Empty);
        }

        [TestMethod]
        public void RaiseDoesNotThrowIfEventHandlerIsNull()
        {
            EventHandler handler = null;
            handler.Raise(this, EventArgs.Empty);
        }

        [TestMethod]
        public void DoesNotThrowWhenResilient()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            handlerMock.Setup(o => o.HandleEvent(this, EventArgs.Empty)).Throws(new ApplicationException()).Verifiable();

            EventHandler<EventArgs> newHandler = handlerMock.Object.HandleEvent;
            newHandler += handlerMock.Object.HandleEvent;
            newHandler += handlerMock.Object.HandleEvent;

            newHandler.Resilient().Raise(this, EventArgs.Empty);

            handlerMock.Verify(h => h.HandleEvent(this, EventArgs.Empty), Times.Exactly(3));
        }

        [TestMethod]
        public void ThrowsWhenNotResilient()
        {
            var handlerMock = new Mock<IEventHandler<EventArgs>>();
            handlerMock.Setup(o => o.HandleEvent(this, EventArgs.Empty)).Throws(new ApplicationException()).Verifiable();

            EventHandler<EventArgs> newHandler = handlerMock.Object.HandleEvent;
            newHandler += handlerMock.Object.HandleEvent;
            newHandler += handlerMock.Object.HandleEvent;

            new Action(() => newHandler.Raise(this, EventArgs.Empty)).ShouldThrow<ApplicationException>();

            handlerMock.Verify(h => h.HandleEvent(this, EventArgs.Empty), Times.Once);
        }
    }
}