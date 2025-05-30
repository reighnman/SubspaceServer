﻿//HintName: Foo.Bar.NestedTypeCallback.g.cs
// <auto-generated>
//     Generated by the CallbackHelperGenerator
// </auto-generated>
using SS.Core.ComponentInterfaces;
using System;

#nullable enable

namespace Foo.Bar
{
    public static partial class NestedTypeCallback
    {
        /// <summary>
        /// Registers a callback <paramref name="handler"/> on a <paramref name="broker"/>.
        /// </summary>
        /// <param name="broker">The broker to register on.</param>
        /// <param name="handler">The handler to register.</param>
        public static void Register(IComponentBroker broker, NestedTypeDelegate handler)
        {
            broker?.RegisterCallback(handler);
        }

        /// <summary>
        /// Unregisters a callback <paramref name="handler"/> from a <paramref name="handler"/>.
        /// </summary>
        /// <param name="broker">The broker to unregister from.</param>
        /// <param name="handler">The handler to unregister.</param>
        public static void Unregister(IComponentBroker broker, NestedTypeDelegate handler)
        {
            broker?.UnregisterCallback(handler);
        }

        /// <summary>
        /// Invokes a callback's registered handlers on a <paramref name="broker"/> and any parent broker(s).
        /// </summary>
        /// <param name="broker">The broker to invoke callback handlers on.</param>
        /// <param name="x"><inheritdoc cref="NestedTypeDelegate" path="/param[@name='x']"/></param>
        public static void Fire(IComponentBroker broker, global::Foo.Bar.NestedTypeCallback.MyNestedType x)
        {
            if (broker is null)
                return;

            NestedTypeDelegate? callbacks = broker.GetCallback<NestedTypeDelegate>();
            if (callbacks is not null)
                InvokeCallbacks(broker, callbacks, x);

            if (broker.Parent is not null)
                Fire(broker.Parent, x);

            // local helper (for recursion)
            static void InvokeCallbacks(IComponentBroker broker, NestedTypeDelegate callbacks, global::Foo.Bar.NestedTypeCallback.MyNestedType x)
            {
                if (callbacks.HasSingleTarget)
                {
                    try
                    {
                        callbacks.Invoke(x);
                    }
                    catch (Exception ex)
                    {
                        ILogManager? logManager = broker.GetInterface<ILogManager>();
                        if (logManager is not null)
                        {
                            try
                            {
                                logManager.Log(LogLevel.Error, $"Exception caught while processing callback {nameof(NestedTypeDelegate)}. {ex}");
                            }
                            finally
                            {
                                broker.ReleaseInterface(ref logManager);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine($"Exception caught while processing callback {nameof(NestedTypeDelegate)}. {ex}");
                        }
                    }
                }
                else
                {
                    foreach (NestedTypeDelegate callback in Delegate.EnumerateInvocationList(callbacks))
                    {
                        InvokeCallbacks(broker, callback, x);
                    }
                }
            }
        }
    }
}
