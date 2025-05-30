﻿//HintName: Foo.Bar.MultipleParametersCallback.g.cs
// <auto-generated>
//     Generated by the CallbackHelperGenerator
// </auto-generated>
using SS.Core.ComponentInterfaces;
using System;

#nullable enable

namespace Foo.Bar
{
    public static partial class MultipleParametersCallback
    {
        /// <summary>
        /// Registers a callback <paramref name="handler"/> on a <paramref name="broker"/>.
        /// </summary>
        /// <param name="broker">The broker to register on.</param>
        /// <param name="handler">The handler to register.</param>
        public static void Register(IComponentBroker broker, MultipleParametersDelegate handler)
        {
            broker?.RegisterCallback(handler);
        }

        /// <summary>
        /// Unregisters a callback <paramref name="handler"/> from a <paramref name="handler"/>.
        /// </summary>
        /// <param name="broker">The broker to unregister from.</param>
        /// <param name="handler">The handler to unregister.</param>
        public static void Unregister(IComponentBroker broker, MultipleParametersDelegate handler)
        {
            broker?.UnregisterCallback(handler);
        }

        /// <summary>
        /// Invokes a callback's registered handlers on a <paramref name="broker"/> and any parent broker(s).
        /// </summary>
        /// <param name="broker">The broker to invoke callback handlers on.</param>
        /// <param name="x"><inheritdoc cref="MultipleParametersDelegate" path="/param[@name='x']"/></param>
        /// <param name="y"><inheritdoc cref="MultipleParametersDelegate" path="/param[@name='y']"/></param>
        /// <param name="z"><inheritdoc cref="MultipleParametersDelegate" path="/param[@name='z']"/></param>
        public static void Fire(IComponentBroker broker, int x, string y, ref readonly double z)
        {
            if (broker is null)
                return;

            MultipleParametersDelegate? callbacks = broker.GetCallback<MultipleParametersDelegate>();
            if (callbacks is not null)
                InvokeCallbacks(broker, callbacks, x, y, in z);

            if (broker.Parent is not null)
                Fire(broker.Parent, x, y, in z);

            // local helper (for recursion)
            static void InvokeCallbacks(IComponentBroker broker, MultipleParametersDelegate callbacks, int x, string y, ref readonly double z)
            {
                if (callbacks.HasSingleTarget)
                {
                    try
                    {
                        callbacks.Invoke(x, y, in z);
                    }
                    catch (Exception ex)
                    {
                        ILogManager? logManager = broker.GetInterface<ILogManager>();
                        if (logManager is not null)
                        {
                            try
                            {
                                logManager.Log(LogLevel.Error, $"Exception caught while processing callback {nameof(MultipleParametersDelegate)}. {ex}");
                            }
                            finally
                            {
                                broker.ReleaseInterface(ref logManager);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine($"Exception caught while processing callback {nameof(MultipleParametersDelegate)}. {ex}");
                        }
                    }
                }
                else
                {
                    foreach (MultipleParametersDelegate callback in Delegate.EnumerateInvocationList(callbacks))
                    {
                        InvokeCallbacks(broker, callback, x, y, in z);
                    }
                }
            }
        }
    }
}
