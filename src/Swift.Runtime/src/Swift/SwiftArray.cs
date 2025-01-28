// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Swift.Runtime;
using Swift.Runtime.InteropServices;

namespace Swift;

/// <summary>
/// Represents a Swift array payload.
/// 
/// The following diagram illustrates the hierarchy of a Swift array type. 
/// The actual implementation may differ:
/// 
///    struct Array
///    +-----------------------------------------------------------------------+
///    |   struct ArrayBuffer                                                 |
///    |   +--------------------------------------------------------------+   |
///    |   |   struct BridgeStorage                                       |   |
///    |   |   +------------------------------------------------------+   |   |
///    |   |   | var rawValue: IntPtr                                 |   |   |
///    |   |   +------------------------------------------------------+   |   |
///    |   +--------------------------------------------------------------+   |
///    +-----------------------------------------------------------------------+
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 8)]
public struct ArrayBuffer
{
    public IntPtr storage;
}

/// <summary>
/// Represents a Swift array.
/// </summary>
/// <typeparam name="Element">The element type contained in the array.</typeparam>
public unsafe class SwiftArray<Element> : IDisposable, ISwiftObject
{
    static nuint _payloadSize = SwiftObjectHelper<SwiftArray<Element>>.GetTypeMetadata().Size;
    static nuint _elementSize = ElementTypeMetadata.Size;

    // Swift array is a value type and doesn't contain an IntPtr payload
    public ArrayBuffer buffer;
    bool _disposed = false;
    public void Dispose()
    {
        if (!_disposed)
        {
            Arc.Release(*(IntPtr*)buffer.storage);
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
    
    ~SwiftArray()
    {
        Arc.Release(*(IntPtr*)buffer.storage);
    }
    public static nuint PayloadSize => _payloadSize;
    public static nuint ElementSize => _elementSize;
    static TypeMetadata ISwiftObject.GetTypeMetadata()
    {
        return TypeMetadata.Cache.GetOrAdd(typeof(SwiftArray<Element>), _ => SwiftArrayPInvokes.PInvoke_getMetadata(TypeMetadataRequest.Complete, ElementTypeMetadata));
    }

    static TypeMetadata ElementTypeMetadata
    {
        get => TypeMetadata.GetTypeMetadataOrThrow<Element>();
    }

    static ISwiftObject ISwiftObject.NewFromPayload(SwiftHandle handle)
    {
        return new SwiftArray<Element>(handle);
    }

    IntPtr ISwiftObject.MarshalToSwift(IntPtr swiftDest)
    {
        var metadata = SwiftObjectHelper<SwiftArray<Element>>.GetTypeMetadata();
        unsafe
        {
            fixed (void* _payloadPtr = &buffer)
            {
                metadata.ValueWitnessTable->InitializeWithCopy((void*)swiftDest, (void*)_payloadPtr, metadata);
            }
        }
        return swiftDest;
    }

    /// <summary>
    /// Constructs a new SwiftArray from the given handle.
    /// </summary>
    SwiftArray(SwiftHandle handle)
    {
        this.buffer = *(ArrayBuffer*)(handle);
    }

    /// <summary>
    /// Constructs a new empty SwiftArray.
    /// </summary>
    public SwiftArray()
    {
        buffer = SwiftArrayPInvokes.Init(ElementTypeMetadata);
    }

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count
    {
        get
        {
            unsafe
            {
                return (int)SwiftArrayPInvokes.Count(buffer, ElementTypeMetadata);
            }
        }
    }

    /// <summary>
    /// Appends the given element to the array.
    /// </summary>
    public void Append(Element item)
    {
        unsafe
        {
            IntPtr T0Payload = IntPtr.Zero;
            var metadata = SwiftObjectHelper<SwiftArray<Element>>.GetTypeMetadata();
            try
            {
                T0Payload = (IntPtr)NativeMemory.Alloc(ElementSize);
                SwiftMarshal.MarshalToSwift(item, (IntPtr)T0Payload);

                fixed (void* _payloadPtr = &buffer)
                {
                    SwiftArrayPInvokes.Append(T0Payload, metadata, new SwiftSelf(_payloadPtr));
                }
            }
            finally
            {
                NativeMemory.Free((void*)T0Payload);
            }
        }
    }

    /// <summary>
    /// Inserts the given element at the given index.
    /// </summary>
    public void Insert(int index, Element item)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        unsafe
        {
            IntPtr T0Payload = IntPtr.Zero;
            var metadata = SwiftObjectHelper<SwiftArray<Element>>.GetTypeMetadata();
            try
            {
                T0Payload = (IntPtr)NativeMemory.Alloc(ElementSize);
                SwiftMarshal.MarshalToSwift(item, (IntPtr)T0Payload);
                fixed (void* _payloadPtr = &buffer)
                {
                    SwiftArrayPInvokes.Insert(new SwiftHandle(T0Payload), (nint)index, metadata, new SwiftSelf(_payloadPtr));
                }
            }
            finally
            {
                NativeMemory.Free((void*)T0Payload);
            }
        }
    }

    /// <summary>
    /// Removes the element at the given index.
    /// </summary>
    public void Remove(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        unsafe
        {
            IntPtr T0Payload = IntPtr.Zero;
            var metadata = SwiftObjectHelper<SwiftArray<Element>>.GetTypeMetadata();
            try
            {
                T0Payload = (IntPtr)NativeMemory.Alloc(ElementSize);
                SwiftMarshal.MarshalToSwift(index, (IntPtr)T0Payload);

                fixed (void* _payloadPtr = &buffer)
                {
                    SwiftArrayPInvokes.Remove(new SwiftIndirectResult((void*)T0Payload), (nint)index, metadata, new SwiftSelf(_payloadPtr));
                }
            }
            finally
            {
                NativeMemory.Free((void*)T0Payload);
            }
        }
    }

    /// <summary>
    /// Removes all elements from the array.
    /// </summary>
    public void RemoveAll()
    {
        unsafe
        {
            var metadata = SwiftObjectHelper<SwiftArray<Element>>.GetTypeMetadata();
            
            fixed (void* _payloadPtr = &buffer)
            {
                SwiftArrayPInvokes.RemoveAll(1, metadata, new SwiftSelf(_payloadPtr));
            }
        }
    }

    /// <summary>
    /// Gets or sets the element at the given index.
    /// </summary>
    public Element this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();

            unsafe
            {
                IntPtr T0Payload = (IntPtr)NativeMemory.Alloc(ElementSize);
                SwiftArrayPInvokes.Get(new SwiftIndirectResult((void*)T0Payload), (nint)index, buffer, ElementTypeMetadata);
                return SwiftMarshal.MarshalFromSwift<Element>(new SwiftHandle((IntPtr)T0Payload));
            }
        }
        set
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException();

            var metadata = SwiftObjectHelper<SwiftArray<Element>>.GetTypeMetadata();
            unsafe
            {
                IntPtr T0Payload = (IntPtr)NativeMemory.Alloc(ElementSize);
                SwiftMarshal.MarshalToSwift(value, T0Payload);

                fixed (void* _payloadPtr = &buffer)
                {
                    SwiftArrayPInvokes.Set(new SwiftHandle(T0Payload), (nint)index, metadata, new SwiftSelf(_payloadPtr));
                }
            }
        }
    }
}

internal static class SwiftArrayPInvokes
{
    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSaMa")]
    public static extern TypeMetadata PInvoke_getMetadata(TypeMetadataRequest request, TypeMetadata typeMetadata);

    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sS2ayxGycfC")]
    public static extern ArrayBuffer Init(TypeMetadata typeMetadata);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSayxSicig")]
    public static unsafe extern void Get(SwiftIndirectResult result, nint index, ArrayBuffer handle, TypeMetadata elementMetadata);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSayxSicis")]
    public static unsafe extern void Set(SwiftHandle value, nint index, TypeMetadata elementMetadata, SwiftSelf self);

    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSa5countSivg")]
    public static extern nint Count(ArrayBuffer handle, TypeMetadata elementMetadata);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSa6appendyyxnF")]
    public static unsafe extern void Append(IntPtr value, TypeMetadata metadata, SwiftSelf self);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSa9removeAll15keepingCapacityySb_tF")]
    public static unsafe extern void RemoveAll(byte keepCapacity, TypeMetadata metadata, SwiftSelf self);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSa6remove2atxSi_tF")]
    public static unsafe extern void Remove(SwiftIndirectResult result, nint index, TypeMetadata metadata, SwiftSelf self);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    [DllImport(KnownLibraries.SwiftCore, EntryPoint = "$sSa6insert_2atyxn_SitF")]
    public static unsafe extern void Insert(SwiftHandle value, nint index, TypeMetadata metadata, SwiftSelf self);
}
