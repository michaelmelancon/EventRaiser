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
                throw new ArgumentException(string.Format("The method signature of the method referenced by this delegate is incompatible with System.EventHandler<{0}>.", typeof(T).FullName));
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="exceptionHandler"></param>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static EventHandler<T> Resilient<T>(this EventHandler<T> handler) where T : EventArgs
        {
            return handler.Resilient((h, exception) => { });
        }

        /// <summary>
        /// Creates a ne
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static EventHandler<T> Parallel<T>(this EventHandler<T> handler) where T : EventArgs
        {
            return handler == null ? null : new EventHandler<T>((sender, args) =>
                System.Threading.Tasks.Parallel.Invoke(handler.GetInvocationList()
                    .OfType<EventHandler<T>>()
                    .Select(i => new Action(() => i(sender, args))).ToArray()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="continuation"></param>
        /// <returns></returns>
        public static EventHandler<T> Async<T>(this EventHandler<T> handler, Action<Task> continuation) where T : EventArgs
        {
            return (sender, args) => Task.Factory.StartNew(() =>
            {
                if (handler != null)
                    handler(sender, args);
            }).ContinueWith(continuation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static EventHandler<T> Async<T>(this EventHandler<T> handler) where T : EventArgs
        {
            return handler.Async(t => { if (t.IsFaulted) t.Exception.Handle((e) => true); });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static void Raise<T>(this EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            if (handler != null)
                handler(sender, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static void Raise(this EventHandler handler, object sender, EventArgs args)
        {
            if (handler != null)
                handler(sender, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Task RaiseAsync<T>(this EventHandler<T> handler, object sender, T args) where T : EventArgs
        {
            return Task.Factory.StartNew(() =>
            {
                if (handler != null)
                    handler(sender, args);
            });
        }
    }
}