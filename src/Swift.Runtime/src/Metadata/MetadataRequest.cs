// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// <summary>
/// Represents the possible values for a MetadataRequest
/// </summary>
[Flags] 
public enum MetadataRequest {
        Complete = 0,
        NonTransitiveComplete = 1,
        LayoutComplete = 0x3f,
        Abstract = 0xff,
        IsNotBlocking = 0x100, 
}