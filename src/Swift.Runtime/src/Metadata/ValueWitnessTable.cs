// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml;
using System;
using System.Runtime.InteropServices;

namespace Swift.Runtime
{
    /// <summary>
	/// https://github.com/apple/swift/blob/main/include/swift/ABI/MetadataValues.h#L117
	/// </summary>
	[Flags]
	public enum ValueWitnessFlags
	{
		AlignmentMask = 0x0000FFFF,
		IsNonPOD = 0x00010000,
		IsNonInline = 0x00020000,
		HasSpareBits = 0x00080000,
		IsNonBitwiseTakable = 0x00100000,
		HasEnumWitnesses = 0x00200000,
		Incomplete = 0x00400000,
	}

	/// <summary>
	/// See https://github.com/apple/swift/blob/main/include/swift/ABI/ValueWitness.def
	/// </summary>
	[StructLayout (LayoutKind.Sequential)]
	public ref struct ValueWitnessTable
	{
		public IntPtr InitializeBufferWithCopyOfBuffer;
		public IntPtr Destroy;
		public IntPtr InitWithCopy;
		public IntPtr AssignWithCopy;
		public IntPtr InitWithTake;
		public IntPtr AssignWithTake;
		public IntPtr GetEnumTagSinglePayload;
		public IntPtr StoreEnumTagSinglePayload;
		public nuint Size;
		public nuint Stride;
		public ValueWitnessFlags Flags;
		public uint ExtraInhabitantCount;
		public int Alignment => (int)((Flags & ValueWitnessFlags.AlignmentMask) + 1);
		public bool IsNonPOD => (Flags & ValueWitnessFlags.IsNonPOD) != 0;
		public bool IsNonBitwiseTakable => (Flags &ValueWitnessFlags.IsNonBitwiseTakable) != 0;
		public bool HasExtraInhabitants => ExtraInhabitantCount != 0;
	}
}
