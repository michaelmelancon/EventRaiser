using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventRaiser
{
    public static class EventRaising
    {
        /// <summary>
        /// Be careful with this one. It should only be used for EventHandler style delegates (Action&lt;Object,EventArgs&gt;).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static EventHandler<T> ToHandlerOf<T>(this EventHandler handler) where T : EventArgs
        {
            return ((Delegate)handler).ToHandlerOf<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="S"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static EventHandler<T> ToHandlerOf<S, T>(this EventHandler<S> handler)
            where S : EventArgs
            where T : S
        {
            return handler.ToHandlerOf<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static EventHandler<EventArgs> ToGeneric(this EventHandler handler)
        {
            return handler.ToHandlerOf<EventArgs>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handlers"></param>
        /// <returns></returns>
        public static EventHandler<T> Combine<T>(this IEnumerable<EventHandler<T>> handlers) where T : EventArgs
        {
            return handlers.Aggregate((EventHandler<T>)null, (result, h) => result += h);
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
            return handler.GetInvocationList().OfType<EventHandler<T>>().Select(h =>
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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static EventHandler<T> Parallel<T>(this EventHandler<T> handler) where T : EventArgs
        {
            return (handler != null) ? new EventHandler<T>((sender, args) =>
                System.Threading.Tasks.Parallel.Invoke(handler.GetInvocationList()
                    .OfType<EventHandler<T>>()
                    .Select(i => new Action(() => i(sender, args))).ToArray())) : null;
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