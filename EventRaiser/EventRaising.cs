/*
   Copyright 2014 Michael Melancon

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventRaiser
{
    public static class EventRaising
    {
        /// <summary>
        /// Returns a new <see cref="System.EventHandler{T}"/> equivilent to a delegate compatible with the event handler pattern, <c>void Method(object sender, EventArgs arg)</c>. i.e. <see cref="System.ComponentModel.PropertyChangedEventHandler"/>.
        /// </summary>
        /// <remarks>The new delegate will have an equivilent invocation list, it will not be simply wrapped.</remarks>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">a delegate compatible with the event handler pattern.</param>
        /// <exception cref="System.ArgumentException">If the delegate is not compatible with the event handler pattern.</exception>
        /// <returns>a new <see cref="System.EventHandler{T}"/> equivilent to <paramref name="handler"/></returns>
        public static EventHandler<T> ToHandlerOf<T>(this Delegate handler) where T : EventArgs
        {
            if (handler == null)
                return null;
            try
            {
                return handler.GetInvocationList().Select(d => (EventHandler<T>)Delegate.CreateDelegate(typeof(EventHandler<T>), d.Target, d.Method))
                    .Combine();
            }
            catch (ArgumentException)
            {
                throw new ArgumentException(string.Format("The signature of the method referenced by this delegate is incompatible with System.EventHandler<{0}>.", typeof(T).FullName));
            }
        }

        /// <summary>
        /// Returns a new <see cref="System.EventHandler{T}"/> equivilent to a <see cref="System.EventHandler"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">the event handler to convert.</param>
        /// <returns>a new <see cref="System.EventHandler{T}"/> equivilent to <paramref name="handler"/></returns>
        public static EventHandler<T> ToHandlerOf<T>(this EventHandler handler) where T : EventArgs
        {
            return ((Delegate)handler).ToHandlerOf<T>();
        }

        /// <summary>
        /// Returns a new <see cref="System.EventHandler{T}"/> equivilent to a <see cref="System.EventHandler{TS}"/>, providing contravariance support.
        /// </summary>
        /// <typeparam name="TS">The type of <see cref="System.EventArgs"/> used by the input handler.</typeparam>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the output handler.</typeparam>
        /// <param name="handler">the event handler to convert.</param>
        /// <returns>a new <see cref="System.EventHandler{T}"/> equivilent to <paramref name="handler"/></returns>
        public static EventHandler<T> ToHandlerOf<TS, T>(this EventHandler<TS> handler)
            where TS : EventArgs
            where T : TS
        {
            return handler.ToHandlerOf<T>();
        }

        /// <summary>
        /// Converts an EventHandler to an EventHandler&lt;EventArgs&gt;.
        /// </summary>
        /// <param name="handler">the event handler to convert.</param>
        /// <returns>An equivilent EventHandler&lt;EventArgs&gt;</returns>
        public static EventHandler<EventArgs> ToGeneric(this EventHandler handler)
        {
            return handler.ToHandlerOf<EventArgs>();
        }

        /// <summary>
        /// Combines a sequence of <see cref="System.EventHandler{T}"/> into a multicast <see cref="System.EventHandler{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handlers">A sequence of <see cref="System.EventHandler{T}"/></param>
        /// <returns>A multicast <see cref="System.EventHandler{T}"/> with an invocation list of handlers.</returns>
        public static EventHandler<T> Combine<T>(this IEnumerable<EventHandler<T>> handlers) where T : EventArgs
        {
            return handlers.Aggregate((EventHandler<T>)null, (result, h) => result + h);
        }

        /// <summary>
        /// Creates a new <see cref="System.EventHandler{T}"/> with an invocation list equivilent to that of <paramref name="handler"/> except that the invocations have been wrapped with exception handling logic defined by <paramref name="exceptionHandler"/>.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">A <see cref="System.EventHandler{T}"/> whose invocation list should be invoked using <paramref name="exceptionHandler"/> to handle any exceptions. </param>
        /// <param name="exceptionHandler">Exception handling logic to be invoked. The original <paramref name="handler"/> and the exception will be passed to this delegate.</param>
        /// <returns></returns>
        public static EventHandler<T> Resilient<T>(this EventHandler<T> handler, Action<EventHandler<T>, Exception> exceptionHandler) where T : EventArgs
        {
            return handler == null ? null : handler.GetInvocationList().OfType<EventHandler<T>>().Select(h =>
                new EventHandler<T>(
                    (sender, args) =>
                    {
                        try
                        {
                            h(sender, args);
                        }
                        catch (Exception e)
                        {
                            exceptionHandler(h, e);
                        }
                    })).Combine();
        }

        /// <summary>
        /// Creates a new <see cref="System.EventHandler{T}"/> with an invocation list equivilent to that of <paramref name="handler"/> except that the invocations have been wrapped with exception handling and will not throw exceptions.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">A <see cref="System.EventHandler{T}"/> whose invocation list should be made to not throw exceptions.</param>
        /// <returns>a new <see cref="System.EventHandler{T}"/> that will not throw an exception</returns>
        public static EventHandler<T> Resilient<T>(this EventHandler<T> handler) where T : EventArgs
        {
            return handler.Resilient((h, exception) => { });
        }

        /// <summary>
        /// Creates a new <see cref="System.EventHandler{T}"/> that invokes the invocation list of <paramref name="handler"/> in parallel.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">A <see cref="System.EventHandler{T}"/> whose invocation list should be invoked in parallel.</param>
        /// <returns>A new <see cref="System.EventHandler{T}"/> that invokes the invocation list of <paramref name="handler"/> in parallel.</returns>
        public static EventHandler<T> Parallel<T>(this EventHandler<T> handler) where T : EventArgs
        {
            return handler == null ? null : new EventHandler<T>((sender, args) =>
                System.Threading.Tasks.Parallel.Invoke(handler.GetInvocationList()
                    .OfType<EventHandler<T>>()
                    .Select(i => new Action(() => i(sender, args))).ToArray()));
        }

        /// <summary>
        /// Creates a new <see cref="System.EventHandler{T}"/> that invokes the <paramref name="handler"/> as a <see cref="System.Threading.Tasks.Task"/>. Exceptions must be handled in the continuation.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">A <see cref="System.EventHandler{T}"/> that should be invoked as a <see cref="System.Threading.Tasks.Task"/>.</param>
        /// <param name="continuation">A continuation to execute when the <see cref="System.Threading.Tasks.Task"/> has completed.</param>
        /// <returns>a new <see cref="System.EventHandler{T}"/> that invokes the <paramref name="handler"/> as a <see cref="System.Threading.Tasks.Task"/>.</returns>
        public static EventHandler<T> Async<T>(this EventHandler<T> handler, Action<Task> continuation) where T : EventArgs
        {
            return (sender, args) => handler.RaiseAsync(sender, args).ContinueWith(continuation);
        }

        /// <summary>
        /// Creates a new <see cref="System.EventHandler{T}"/> that invokes the <paramref name="handler"/> as a <see cref="System.Threading.Tasks.Task"/>. Exceptions are handled, but ignored.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">A <see cref="System.EventHandler{T}"/> that should be invoked as a <see cref="System.Threading.Tasks.Task"/>.</param>
        /// <returns>a new <see cref="System.EventHandler{T}"/> that invokes the <paramref name="handler"/> as a <see cref="System.Threading.Tasks.Task"/>.</returns>
        public static EventHandler<T> Async<T>(this EventHandler<T> handler) where T : EventArgs
        {
            return handler.Async(t => { if (t.IsFaulted) t.Exception.Handle(e => true); });
        }

        /// <summary>
        /// Invokes the <paramref name="handler"/> if it is not null.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">The <see cref="System.EventHandler{T}"/> to invoke.</param>
        /// <param name="sender">The object that contains the event.</param>
        /// <param name="args">The argument to be passed to the event.</param>
        public static void Raise<T>(this EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            if (handler != null)
                handler(sender, args);
        }

        /// <summary>
        /// Invokes the <paramref name="handler"/> if it is not null.
        /// </summary>
        /// <param name="handler">The <see cref="System.EventHandler"/> to invoke.</param>
        /// <param name="sender">The object that contains the event.</param>
        /// <param name="args">The argument to be passed to the event.</param>
        public static void Raise(this EventHandler handler, object sender, EventArgs args)
        {
            if (handler != null)
                handler(sender, args);
        }

        /// <summary>
        /// Starts a new <see cref="System.Threading.Tasks.Task"/> that invokes the <paramref name="handler"/> if it is not null. This method returns immediately.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="System.EventArgs"/> used by the handler.</typeparam>
        /// <param name="handler">The <see cref="System.EventHandler{T}"/> to invoke.</param>
        /// <param name="sender">The object that contains the event.</param>
        /// <param name="args">The argument to be passed to the event.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that represents invoking of the handler.</returns>
        public static Task RaiseAsync<T>(this EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            return Task.Factory.StartNew(() => handler.Raise(sender, args));
        }
    }
}