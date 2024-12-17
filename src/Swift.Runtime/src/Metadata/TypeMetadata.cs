// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Swift.Runtime;

/// <summary>
/// Flags used to describe types
/// </summary>
[Flags]
public enum TypeMetadataFlags {
    None = 0,
    /// <summary>
    /// The metadata is not an actual type
    /// </summary>
    IsNonType = 0x400,
    /// <summary>
    /// The metadata doesn't live on the heap
    /// </summary>
    IsNonHeap = 0x200,
    /// <summary>
    /// The type is private to the runtime
    /// </summary>
    IsRuntimePrivate = 0x100,
}

/// <summary>
/// The type represented by the metadata
/// </summary>
public enum TypeMetadataKind {
    /// <summary>
    /// None - errror
    /// </summary>
    None = 0,
    /// <summary>
    /// The metadata represents a struct
    /// </summary>
    Struct = 0 | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents an enum
    /// </summary>
    Enum = 1 | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents an optional type
    /// </summary>
    Optional = 2 | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents an non-swift class
    /// </summary>
    ForeignClass = 3 | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents a foreign reference type
    /// </summary>
    ForeignReferenceType = 4 | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents an opaque type
    /// </summary>
    Opaque = 0 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents a tuple
    /// </summary>
    Tuple = 1 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents a closure/function
    /// </summary>
    Function = 2 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents a protocol
    /// </summary>
    Existential = 3 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents a type of a TypeMetadata type
    /// </summary>
    Metatype = 4 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents an Objective C wrapper
    /// </summary>
    ObjCClassWrapper = 5 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents a type of an existential container
    /// </summary>
    ExistentialMetatype = 6 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents an extended existential type
    /// </summary>
    ExtendedExistential = 7 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents the type Builtin.FixedArray
    /// </summary>
    FixedArray = 8 | TypeMetadataFlags.IsRuntimePrivate | TypeMetadataFlags.IsNonHeap,
    /// <summary>
    /// The metadata represents a heap local variable
    /// </summary>
    HeapLocalVariable = 0 | TypeMetadataFlags.IsNonType,
    /// <summary>
    /// The metadata represents a generic heap local variable
    /// </summary>
    HeapGenericLocalVariable = 0 | TypeMetadataFlags.IsNonType | TypeMetadataFlags.IsRuntimePrivate,
    /// <summary>
    /// The metadata represents an error
    /// </summary>
    ErrorObject = 1 | TypeMetadataFlags.IsNonType | TypeMetadataFlags.IsRuntimePrivate,
    /// <summary>
    /// The metadata represents a heap-allocated task
    /// </summary>
    Task = 2 | TypeMetadataFlags.IsNonType | TypeMetadataFlags.IsRuntimePrivate,
    /// <summary>
    /// The metadata represents a non-task async job
    /// </summary>
    Job = 3 | TypeMetadataFlags.IsNonType | TypeMetadataFlags.IsRuntimePrivate,
    // Swift source code says that for fixed values, this will never exceed 0x7ff,
    // but all class types will be 0x800 and above
    /// <summary>
    /// The metadata represents a class
    /// </summary>
    Class = 0x800 
}


/// <summary>
/// Represents the type metadata for a Swift type
/// </summary>
public readonly struct TypeMetadata : IEquatable<TypeMetadata> {
    readonly IntPtr handle;

    static TypeMetadata ()
    {
        // TODO - add metadata for common built-in types like scalars and strings
        cache = new TypeMetadataCache();
    }

    /// <summary>
    /// An empty/invalid TypeMetadata object
    /// </summary>
    public readonly static TypeMetadata Zero = default (TypeMetadata);

    /// <summary>
    /// Construct a TypeMetadata object
    /// </summary>
    /// <param name="handle">The handle for the type</param>
    TypeMetadata (IntPtr handle)
    {
        this.handle = handle;
    }

    /// <summary>
    /// Returns true if and only if the TypeMetadata is valid.
    /// </summary>
    public bool IsValid => handle != IntPtr.Zero;

    /// <summary>
    /// Throws a NotSupportedException if the TypeMetadata is invalid
    /// </summary>
    /// <exception cref="NotSupportedException"></exception>
    void ThrowOnInvalid ()
    {
        if (!IsValid)
            throw new NotSupportedException ();
    }

    // This comes from the Swift ABI documentation - https://github.com/swiftlang/swift/blob/23e3f5f5de2ed046f3183264589be1f9a54f7e1e/include/swift/ABI/MetadataValues.h#L117
    const long kMaxDiscriminator = 0x7ff;

    /// <summary>
    /// Returns the kind of this TypeMetadata
    /// </summary>
    public TypeMetadataKind Kind {
        get {
            ThrowOnInvalid ();
            long val = ReadPointerSizedInt (handle);
            if (val == 0)
            return TypeMetadataKind.None;
            if (val > kMaxDiscriminator)
                return TypeMetadataKind.Class;
            return (TypeMetadataKind)val;
        }
    }

    /// <summary>
    /// Returns a pointer to the value witness table for the given type
    /// </summary>
    public unsafe ValueWitnessTable *ValueWitnessTable => IsValid ? (ValueWitnessTable*)(*((IntPtr*)handle - 1)) : throw new NullReferenceException ("TypeMetadata is null");

    /// <summary>
    /// Returns the size of the Swift type in bytes
    /// </summary>
    public unsafe nuint Size => this.ValueWitnessTable->Size;

    /// <summary>
    /// Returns the stride of the Swift type in bytes
    /// </summary>
    public unsafe nuint Stride => this.ValueWitnessTable->Stride;

    /// <summary>
    /// Returns the alignment of the Swift type
    /// </summary>
    public unsafe int Alignment => this.ValueWitnessTable->Alignment;

    /// <summary>
    /// Reads a pointer sized integer from the location supplied
    /// </summary>
    /// <param name="p">a pointer to memory</param>
    /// <returns></returns>
    unsafe static nint ReadPointerSizedInt (IntPtr p)
    {
        // Check for debug only. This calling code should always do the null
        // checking.
#if DEBUG
        if (p == IntPtr.Zero)
            throw new ArgumentOutOfRangeException (nameof (p));
#endif
        return *((nint*)p);
    }

    /// <summary>
    /// Returns true if other is the same as this
    /// </summary>
    /// <param name="other">a TypeMetadata object to compare</param>
    /// <returns>true if the other is the same, false otherwise</returns>
    public bool Equals(TypeMetadata other)
    {
        return other.handle == handle;
    }

    /// <summary>
    /// Returns true if and only if o is a TypeMetadata object and is equal to this
    /// </summary>
    /// <param name="o">an object to compare</param>
    /// <returns>true if the other is the same, false otherwise</returns>
    public override bool Equals (object? o)
    {
        if (o is TypeMetadata tm)
            return tm.handle == this.handle;
        return false;
    }
    
    /// <summary>
    /// Returns a hashcode for this TypeMetadata object
    /// </summary>
    /// <returns>A hashcode for this TypeMetadata object</returns>
    public override int GetHashCode ()
    {
        return handle.GetHashCode ();
    }

    static readonly TypeMetadataCache cache;
    /// <summary>
    /// Gets the type metadata cache for the runtime.
    /// </summary>
    public static ITypeMetadataCache Cache => cache;
}