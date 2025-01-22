# Swift code generation

Certain parts of the Swift ABI are poorly documented and too complex to project directly to .NET, such as metadata, async contexts, and dynamic dispatch thunks. In these cases, Swift wrappers offer the flexibility to encapsulate these poorly documented ABI components within Swift code, enabling more maintainable C#/Swift interop boundary.

The main tradeoff is between more maintainable C#/Swift interop boundary and the increased cost of shipping and trimming. To minimize complexity, we propose generating Swift wrappers only when absolutely necessary, keeping them as thin as possible.

Below, we outline the specific scenarios where Swift wrappers are required.

## Async

Swift async function is projected into a function with C# Task. For each Swift function, there is a Swift wrapper function that calls into C# callback upon completion of the async operation. Here is an example:

Swift code:
```swift
public func waitFor(seconds: UInt64) async -> Int32 {
    do {
        try await Task.sleep(nanoseconds: seconds * 1_000_000_000)
    } catch {
        print("Failed to sleep: \(error)")
        return -1
    }
    return returnValue
}
```

Generated Swift wrapper:
```swift
extension TimerStruct {
    @_silgen_name("waitFor_async")
    public func waitFor_async(callback: @escaping (Swift.Int32) -> Void, seconds: Swift.UInt64) {
        Task {
            let result = await waitFor(seconds: seconds)
            callback(result)
        }
    }
}
```

Generated C# code:
```csharp
private GCHandle? _callbackHandle;
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
private delegate void CallbackDelegate(Int32 result);

public unsafe  Task<Int32> waitFor( UInt64 seconds)
{
    TaskCompletionSource<Int32> task = new TaskCompletionSource<Int32>();
    CallbackDelegate callbackDelegate = null;
    callbackDelegate = (Int32 result) =>
    {
        try
        {
            task.TrySetResult(result);
        }
        finally
        {
            if (_callbackHandle.HasValue)
            {
                _callbackHandle.Value.Free();
                _callbackHandle = null;
            }
        }
    };
    _callbackHandle = GCHandle.Alloc(callbackDelegate);
    var self = new SwiftSelf((void*)_payload);
    
    waitFor(Marshal.GetFunctionPointerForDelegate(callbackDelegate), IntPtr.Zero, seconds, self);
    
    return task.Task;
}
```