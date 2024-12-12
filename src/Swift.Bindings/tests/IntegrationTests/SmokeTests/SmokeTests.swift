// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import Foundation

@frozen
public struct FrozenStruct {
    let x: Int32
    let y: Int32

    public init(x: Int32, y: Int32) {
        self.x = x
        self.y = y
    }

    public func add() -> Int32 {
        return x + y
    }
}

// Add non-frozen struct