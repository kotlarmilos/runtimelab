// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Swift.SmokeTests;

namespace BindingsGeneration.Tests
{
    public class SmokeTests : IClassFixture<SmokeTests.TestFixture>
    {
        private readonly TestFixture _fixture;

        public SmokeTests(TestFixture fixture)
        {
            _fixture = fixture;
        }

        public class TestFixture
        {
            static TestFixture()
            {
                InitializeResources();
            }

            private static void InitializeResources()
            {
                // Initialize
            }
        }

        [Fact]
        public static void TestFrozenStruct()
        {
            FrozenStruct f = new FrozenStruct(40, 2);
            Assert.Equal(42, f.add());
        }
        [Fact]
        public static void TestNonFrozenStruct()
        {
            // Add test for non-frozen struct
        }
    }
}
