// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Swift.Runtime;
using System.Reflection;

namespace BindingsGeneration.Tests;

public class KnownMetadataTests : IClassFixture<KnownMetadataTests.TestFixture>
{
    private readonly TestFixture _fixture;

    public KnownMetadataTests(TestFixture fixture)
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

    [Theory]
    [InlineData(typeof(bool))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(int))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(long))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(nint))]
    [InlineData(typeof(nuint))]
    [InlineData(typeof(float))]
    [InlineData(typeof(double))]
    [InlineData(typeof(void))]
    public static void HasBool(Type type)
    {
        Assert.True(TypeMetadata.TryGetTypeMetadata(type, out var md));
    }
}