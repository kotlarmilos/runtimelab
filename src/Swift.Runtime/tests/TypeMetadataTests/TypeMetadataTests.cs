// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Swift.Runtime;
using System.Reflection;

namespace BindingsGeneration.Tests;

public class TypeMetadataTests : IClassFixture<TypeMetadataTests.TestFixture>
{
    private readonly TestFixture _fixture;

    public TypeMetadataTests(TestFixture fixture)
    {
        _fixture = fixture;
    }

    public class TestFixture
    {
        static TestFixture()
        {
        }

        private static void InitializeResources()
        {
        }
    }

    static TypeMetadata MakePhonyMetadata(int value)
    {
        IntPtr p = new IntPtr(value);

        var t = typeof(TypeMetadata);
        var ci = t.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new Type[] { typeof(IntPtr) })!;

        return (TypeMetadata)(ci.Invoke(new object[] { p }));
    }

    [Fact]
    public static void CacheWorks()
    {
        var fakeMeta = MakePhonyMetadata(42);
        TypeMetadata.Cache.GetOrAdd(typeof(System.Convert), (t) =>
        {
            return fakeMeta;
        });
        Assert.True(TypeMetadata.Cache.TryGet(typeof(System.Convert), out var result));
    }

    [Fact]
    public static void TryGetFail()
    {
        var contains = TypeMetadata.Cache.TryGet(typeof(System.EventArgs), out var result);
        Assert.False(contains);
    }

    [Fact]
    public static void TryGetSucceed()
    {
        var fakeMeta = MakePhonyMetadata(43);
        TypeMetadata.Cache.GetOrAdd(typeof(System.Random), (t) =>
        {
            return fakeMeta;
        });
        var contains = TypeMetadata.Cache.TryGet(typeof(System.Random), out var result);
        Assert.True(contains);
    }
}
