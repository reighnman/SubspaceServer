﻿//HintName: Foo.Bar.PlayerActionCallback.g.cs
// <auto-generated>
//     Generated by the CallbackHelperGenerator
// </auto-generated>
using SS.Core.ComponentInterfaces;
using System;

#nullable enable

namespace Foo.Bar
{
    public static partial class PlayerActionCallback
    {
        /// <summary>
        /// Registers a callback <paramref name="handler"/> on a <paramref name="broker"/>.
        /// </summary>
        /// <param name="broker">The broker to register on.</param>
        /// <param name="handler">The handler to register.</param>
        public static void Register(IComponentBroker broker, PlayerActionDelegate handler)
        {
            broker?.RegisterCallback(handler);
        }

        /// <summary>
        /// Unregisters a callback <paramref name="handler"/> from a <paramref name="handler"/>.
        /// </summary>
        /// <param name="broker">The broker to unregister from.</param>
        /// <param name="handler">The handler to unregister.</param>
        public static void Unregister(IComponentBroker broker, PlayerActionDelegate handler)
        {
            broker?.UnregisterCallback(handler);
        }

        /// <summary>
        /// Invokes a callback's registered handlers on a <paramref name="broker"/> and any parent broker(s).
        /// </summary>
        /// <param name="broker">The broker to invoke callback handlers on.</param>
        /// <param name="player"><inheritdoc cref="PlayerActionDelegate" path="/param[@name='player']"/></param>
        /// <param name="action"><inheritdoc cref="PlayerActionDelegate" path="/param[@name='action']"/></param>
        /// <param name="arena"><inheritdoc cref="PlayerActionDelegate" path="/param[@name='arena']"/></param>
        public static void Fire(IComponentBroker broker, global::SS.Core.Player player, global::SS.Core.PlayerAction action, global::SS.Core.Arena? arena)
        {
            if (broker is null)
                return;

            PlayerActionDelegate? callbacks = broker.GetCallback<PlayerActionDelegate>();
            if (callbacks is not null)
                InvokeCallbacks(broker, callbacks, player, action, arena);

            if (broker.Parent is not null)
                Fire(broker.Parent, player, action, arena);

            // local helper (for recursion)
            static void InvokeCallbacks(IComponentBroker broker, PlayerActionDelegate callbacks, global::SS.Core.Player player, global::SS.Core.PlayerAction action, global::SS.Core.Arena? arena)
            {
                if (callbacks.HasSingleTarget)
                {
                    try
                    {
                        callbacks.Invoke(player, action, arena);
                    }
                    catch (Exception ex)
                    {
                        ILogManager? logManager = broker.GetInterface<ILogManager>();
                        if (logManager is not null)
                        {
                            try
                            {
                                logManager.Log(LogLevel.Error, $"Exception caught while processing callback {nameof(PlayerActionDelegate)}. {ex}");
                            }
                            finally
                            {
                                broker.ReleaseInterface(ref logManager);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine($"Exception caught while processing callback {nameof(PlayerActionDelegate)}. {ex}");
                        }
                    }
                }
                else
                {
                    foreach (PlayerActionDelegate callback in Delegate.EnumerateInvocationList(callbacks))
                    {
                        InvokeCallbacks(broker, callback, player, action, arena);
                    }
                }
            }
        }
    }
}
