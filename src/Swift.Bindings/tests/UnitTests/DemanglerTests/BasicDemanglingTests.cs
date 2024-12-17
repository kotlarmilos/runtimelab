// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using BindingsGeneration.Demangling;
using Xunit;

namespace BindingsGeneration.Tests;

public class BasicDemanglingTests : IClassFixture<BasicDemanglingTests.TestFixture>
{
    private readonly TestFixture _fixture;

    public BasicDemanglingTests(TestFixture fixture)
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

    [Fact]
    public void TestProtocolWitnessTable()
    {
        var symbol = "_$s20GenericTestFramework6ThingyCAA7StanleyAAWP";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var protoWitnessReduction = result as ProtocolWitnessTableReduction;
        Assert.NotNull(protoWitnessReduction);
        Assert.Equal("GenericTestFramework.Thingy", protoWitnessReduction.ImplementingType.Name);
        Assert.Equal("GenericTestFramework.Stanley", protoWitnessReduction.ProtocolType.Name);
    }

    [Fact]
    public void TestOtherProtocolWitnessTable()
    {
        var symbol = "_$s20GenericTestFramework6ThingyCAA8IsItRealAAWP";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var protoWitnessReduction = result as ProtocolWitnessTableReduction;
        Assert.NotNull(protoWitnessReduction);
        Assert.Equal("GenericTestFramework.Thingy", protoWitnessReduction.ImplementingType.Name);
        Assert.Equal("GenericTestFramework.IsItReal", protoWitnessReduction.ProtocolType.Name);
    }

    [Fact]
    public void TestFailDemangleNonsense()
    {
        var symbol = "_$ThisIsJustGarbage";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var err = result as ReductionError;
        Assert.NotNull(err);
        Assert.Equal("No rule for node FirstElementMarker", err.Message);
    }

    [Fact]
    public void TestNestedProtocolWitnessTable()
    {
        var symbol = "_$s20GenericTestFramework3FooC6ThingyCAA8IsItRealAAWP";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var protoWitnessReduction = result as ProtocolWitnessTableReduction;
        Assert.NotNull(protoWitnessReduction);
        Assert.Equal("GenericTestFramework.Foo.Thingy", protoWitnessReduction.ImplementingType.Name);
        Assert.Equal("GenericTestFramework.IsItReal", protoWitnessReduction.ProtocolType.Name);
    }

    [Fact]
    public void TestOtherProtocolConformanceDescriptor()
    {
        var symbol = "_$s10someclient14CSAgeableProxyCAA7AgeableAAMc";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var conf = result as ProtocolConformanceDescriptorReduction;
        Assert.NotNull(conf);
        Assert.Equal("someclient.CSAgeableProxy", conf.ImplementingType.Name);
        Assert.Equal("someclient.Ageable", conf.ProtocolType.Name);
        Assert.Equal("someclient", conf.Module);
    }

    [Fact]
    public void TestFuncWithTuple()
    {
        var symbol = "_$s17unitHelpFrawework10ReturnsInt3arg1cS2u1a_Si1bt_SitF";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var func = result as FunctionReduction;
        Assert.NotNull (func);
        Assert.Equal ("ReturnsInt", func.Function.Name);
        var returnType = func.Function.Return as NamedTypeSpec;
        Assert.NotNull (returnType);
        Assert.Equal ("Swift.UInt", returnType.Name);
        var args = func.Function.ParameterList;
        Assert.Equal (2, args.Elements.Count);
        var firstarg = args.Elements [0] as TupleTypeSpec;
        Assert.NotNull (firstarg);
        Assert.Equal ("arg", firstarg.TypeLabel);
        Assert.Equal (2, firstarg.Elements.Count);
        Assert.Equal ("arg: (a: Swift.UInt, b: Swift.Int)", firstarg.ToString());
        var secondarg = args.Elements [1] as NamedTypeSpec;
        Assert.NotNull (secondarg);
        Assert.Equal ("c", secondarg.TypeLabel);
        Assert.Equal ("Swift.Int", secondarg.Name);
    }

    [Fact]
    public void TestDispatchThunkFunc()
    {
        var symbol = "_$s22GeneralHackingNonsense12ThisIsAClassC12identityFunc1aS2i_tFTj";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var thunk = result as DispatchThunkFunctionReduction;
        Assert.NotNull (thunk);
        var provenance = thunk.Function.Provenance;
        Assert.True (provenance.IsInstance);
        Assert.Equal ("GeneralHackingNonsense.ThisIsAClass", provenance.InstanceType!.Name);
        Assert.Equal ("identityFunc", thunk.Function.Name);
        Assert.Single (thunk.Function.ParameterList.Elements);
        var parameter = thunk.Function.ParameterList.Elements [0];
        Assert.Equal ("a: Swift.Int", parameter.ToString ());
        Assert.Equal ("Swift.Int", thunk.Function.Return.ToString ());
    }

    [Fact]
    public void TestDispatchThunkFuncAllocator()
    {
        var symbol = "_$s22GeneralHackingNonsense12ThisIsAClassCACycfCTj";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var thunk = result as DispatchThunkFunctionReduction;
        Assert.NotNull (thunk);
        var provenance = thunk.Function.Provenance;
        Assert.True (provenance.IsInstance);
        Assert.Equal ("GeneralHackingNonsense.ThisIsAClass", provenance.InstanceType!.Name);
        Assert.Equal ("__allocating_init", thunk.Function.Name);
        Assert.Empty (thunk.Function.ParameterList.Elements);
        var returnType = thunk.Function.Return as NamedTypeSpec;
        Assert.NotNull (returnType);
        Assert.Equal ("GeneralHackingNonsense.ThisIsAClass", returnType.Name);
    }

    [Fact]
    public void TestFuncNoArgs()
    {
        var symbol = "_$s22GeneralHackingNonsense12ThisIsAClassC11returnSevenSiyF";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var func = result as FunctionReduction;
        Assert.NotNull (func);
        var provenance = func.Function.Provenance;
        Assert.True (provenance.IsInstance);
        Assert.Equal ("GeneralHackingNonsense.ThisIsAClass", provenance.InstanceType!.Name);
        Assert.Empty (func.Function.ParameterList.Elements);
        var returnType = func.Function.Return as NamedTypeSpec;
        Assert.NotNull (returnType);
        Assert.Equal ("Swift.Int", returnType.Name);
    }

    [Fact]
    public void TestFuncNoLabels()
    {
        var symbol = "_$s22GeneralHackingNonsense12ThisIsAClassC22noPublicParameterNamesyS2i_SitF";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var func = result as FunctionReduction;
        Assert.NotNull (func);
        var provenance = func.Function.Provenance;
        Assert.True (provenance.IsInstance);
        Assert.Equal ("GeneralHackingNonsense.ThisIsAClass", provenance.InstanceType!.Name);
        Assert.Equal (2, func.Function.ParameterList.Elements.Count);
        Assert.Equal ("(Swift.Int, Swift.Int)", func.Function.ParameterList.ToString ());
        var returnType = func.Function.Return as NamedTypeSpec;
        Assert.NotNull (returnType);
        Assert.Equal ("Swift.Int", returnType.Name);
    }


    [Fact]
    public void TestFuncFewLabels()
    {
        var symbol = "_$s22GeneralHackingNonsense12ThisIsAClassC23fewPublicParameterNames_1b_S2i_S2itF";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var func = result as FunctionReduction;
        Assert.NotNull (func);
        var provenance = func.Function.Provenance;
        Assert.True (provenance.IsInstance);
        Assert.Equal ("GeneralHackingNonsense.ThisIsAClass", provenance.InstanceType!.Name);
        Assert.Equal (3, func.Function.ParameterList.Elements.Count);
        Assert.Equal ("(Swift.Int, b: Swift.Int, Swift.Int)", func.Function.ParameterList.ToString ());
        var returnType = func.Function.Return as NamedTypeSpec;
        Assert.NotNull (returnType);
        Assert.Equal ("Swift.Int", returnType.Name);
    }


    [Fact]
    public void TestMetadataAccessor()
    {
        var symbol = "_$s22GeneralHackingNonsense12ThisIsAClassCMa";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var metadataAccessor = result as MetadataAccessorReduction;
        Assert.NotNull (metadataAccessor);
        Assert.Equal ("GeneralHackingNonsense.ThisIsAClass", metadataAccessor.TypeSpec.Name);
    }


    [Fact]
    public void TestGenericMetadataAccessor()
    {
        var symbol = "_$s22GeneralHackingNonsense6DupletVMa";
        var demangler = new Swift5Demangler ();
        var result = demangler.Run (symbol);
        var metadataAccessor = result as MetadataAccessorReduction;
        Assert.NotNull (metadataAccessor);
        Assert.Equal ("GeneralHackingNonsense.Duplet", metadataAccessor.TypeSpec.Name);
    }

    [Fact]
    public void TestUnknownEntry()
    {
        var symbol = "_$ss5print_9separator10terminatoryypd_S2StFfA0_"; // default argument initializer
        var demangler = new Swift5Demangler ();
        var result = demangler.Run(symbol);
        var error = result as ReductionError;
        Assert.NotNull (error);
        Assert.Equal (ReductionErrorSeverity.Low, error.Severity);
    }

    [Fact]
    public void TestConformanceWithGeneric()
    {
        var symbol = "_$sSayxGSlsMc";
        var result = new Swift5Demangler ().Run (symbol);
        var proto = result as ProtocolConformanceDescriptorReduction;
        Assert.NotNull (proto);
        Assert.Equal ("Swift.Array<T_0_0>", proto.ImplementingType.ToString ());
        Assert.Equal ("Swift.Collection", proto.ProtocolType.ToString ());
    }

    [Fact]
    public void TestGenericWithAssociatedType ()
    {
        var symbol = "_$ss16IndexingIteratorV4next7ElementQzSgyF";
        var result = new Swift5Demangler ().Run (symbol);
        var func = result as FunctionReduction;
        Assert.NotNull (func);
        Assert.Equal ("Swift.IndexingIterator.next() -> Swift.Optional<T_0_0.Element>", func.Function.ToString ());
    }

    [Fact]
    public void TestThrowFunc ()
    {
        var symbol = "_$ss15withUnsafeBytes2of_q_x_q_SWKXEtKr0_lF";
        var result = new Swift5Demangler ().Run (symbol);
        var func = result as FunctionReduction;
        Assert.NotNull (func);
        Assert.Equal ("Swift.withUnsafeBytes<T_0_0, T_0_1>(of: T_0_0, (Swift.UnsafeRawBufferPointer) throws -> T_0_1) -> T_0_1", func.Function.ToString ());
    }
}
