﻿using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;
using System.Diagnostics;

namespace SS.Utilities.ObjectPool
{
    /// <summary>
    /// A policy for pooling of <see cref="LinkedListNode{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">The element type of the nodes.</typeparam>
    public class LinkedListNodePooledObjectPolicy<T> : IPooledObjectPolicy<LinkedListNode<T>>
    {
        public LinkedListNode<T> Create()
        {
            return new LinkedListNode<T>(default!);
        }

        public bool Return(LinkedListNode<T> obj)
        {
            if (obj is null)
                return false;

            Debug.Assert(obj.List is null);

            if (obj.List is not null)
                return false;

            obj.ValueRef = default!;
            return true;
        }
    }
}
