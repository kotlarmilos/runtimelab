// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography;
using Swift.Runtime;

namespace BindingsGeneration.FunctionalTests
{
    /// <summary>
    /// Represents ChaChaPoly in C#.
    /// </summary>
    public unsafe struct ChaChaPoly
    {
        /// <summary>
        /// Represents Nonce in C#.
        /// </summary>
        public sealed unsafe class Nonce : IDisposable, ISwiftObject
        {
            private static nuint PayloadSize = Metadata.ValueWitnessTable->Size;

            private readonly void* _payload;

            private bool _disposed = false;

            public Nonce()
            {
                _payload = NativeMemory.Alloc(PayloadSize);
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(_payload);
                CryptoKit.PInvoke_ChaChaPoly_Nonce_Init(swiftIndirectResult);
            }

            public Nonce(Data data)
            {
                _payload = NativeMemory.Alloc(PayloadSize);
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(_payload);

                TypeMetadata metadata = Runtime.GetMetadata(data);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

                CryptoKit.PInvoke_ChaChaPoly_Nonce_Init2(swiftIndirectResult, &data, metadata, witnessTable, out SwiftError error);

                if (error.Value != null)
                {
                    NativeMemory.Free(_payload);
                    throw new CryptographicException();
                }
            }

            public void* Payload => _payload;

            public static TypeMetadata Metadata => CryptoKit.PInvoke_ChaChaPoly_Nonce_GetMetadata();

            public void Dispose()
            {
                if (!_disposed)
                {
                    NativeMemory.Free(_payload);
                    _disposed = true;
                    GC.SuppressFinalize(this);
                }
            }

            ~Nonce()
            {
                NativeMemory.Free(_payload);
            }
        }

        /// <summary>
        /// Represents SealedBox in C#.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        public unsafe struct SealedBox
        {
            private readonly Data _combined;

            public SealedBox(ChaChaPoly.Nonce nonce, Data ciphertext, Data tag)
            {
                TypeMetadata ciphertextMetadata = Runtime.GetMetadata(ciphertext);
                TypeMetadata tagMetadata = Runtime.GetMetadata(tag);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* ciphertextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, ciphertextMetadata, null);
                void* tagWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, tagMetadata, null);

                this = CryptoKit.PInvoke_ChaChaPoly_SealedBox_Init(
                    nonce.Payload,
                    &ciphertext,
                    &tag,
                    ciphertextMetadata,
                    tagMetadata,
                    ciphertextWitnessTable,
                    tagWitnessTable,
                    out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }

            public Data Ciphertext => CryptoKit.PInvoke_ChaChaPoly_SealedBox_GetCiphertext(this);

            public Data Tag => CryptoKit.PInvoke_ChaChaPoly_SealedBox_GetTag(this);
        }

        /// <summary>
        /// Encrypts the plaintext using the key, nonce, and authenticated data.
        /// </summary>
        public static unsafe SealedBox seal<Plaintext, AuthenticateData>(Plaintext plaintext, SymmetricKey key, Nonce nonce, AuthenticateData aad, out SwiftError error) where Plaintext : unmanaged, ISwiftObject where AuthenticateData : unmanaged, ISwiftObject
        {
            TypeMetadata plaintextMetadata = Runtime.GetMetadata(plaintext);
            TypeMetadata aadMetadata = Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* plaintextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, plaintextMetadata, null);
            void* aadWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, aadMetadata, null);

            SealedBox sealedBox = CryptoKit.PInvoke_ChaChaPoly_Seal(
                &plaintext,
                key.Payload,
                nonce.Payload,
                &aad,
                plaintextMetadata,
                aadMetadata,
                plaintextWitnessTable,
                aadWitnessTable,
                out error);

            return sealedBox;
        }

        /// <summary>
        /// Decrypts the sealed box using the key and authenticated data.
        /// </summary>
        public static unsafe Data open<AuthenticateData>(SealedBox sealedBox, SymmetricKey key, AuthenticateData aad, out SwiftError error) where AuthenticateData : unmanaged, ISwiftObject
        {
            TypeMetadata metadata = Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

            Data data = CryptoKit.PInvoke_ChaChaPoly_Open(
                sealedBox,
                key.Payload,
                &aad,
                metadata,
                witnessTable,
                out error);

            return data;
        }
    }

    /// <summary>
    /// Represents AesGcm in C#.
    /// </summary>
    public unsafe struct AesGcm
    {
        /// <summary>
        /// Represents Nonce in C#.
        /// </summary>
        public sealed unsafe class Nonce : IDisposable, ISwiftObject
        {
            private static nuint PayloadSize = Metadata.ValueWitnessTable->Size;

            private readonly void* _payload;

            private bool _disposed = false;

            public Nonce()
            {
                _payload = NativeMemory.Alloc(PayloadSize);
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(_payload);
                CryptoKit.PInvoke_AesGcm_Nonce_Init(swiftIndirectResult);
            }

            public Nonce(Data data)
            {
                _payload = NativeMemory.Alloc(PayloadSize);
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(_payload);

                TypeMetadata metadata = Runtime.GetMetadata(data);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

                CryptoKit.PInvoke_AesGcm_Nonce_Init2(swiftIndirectResult, &data, metadata, witnessTable, out SwiftError error);

                if (error.Value != null)
                {
                    NativeMemory.Free(_payload);
                    throw new CryptographicException();
                }
            }

            public void* Payload => _payload;

            public static TypeMetadata Metadata => CryptoKit.PInvoke_AesGcm_Nonce_GetMetadata();

            public void Dispose()
            {
                if (!_disposed)
                {
                    NativeMemory.Free(_payload);
                    _disposed = true;
                    GC.SuppressFinalize(this);
                }
            }

            ~Nonce()
            {
                NativeMemory.Free(_payload);
            }
        }

        /// <summary>
        /// Represents SealedBox in C#.
        /// </summary>
        public sealed unsafe class SealedBox : IDisposable, ISwiftObject
        {
            private static nuint PayloadSize = Metadata.ValueWitnessTable->Size;

            private readonly void* _payload;

            private bool _disposed = false;

            public SealedBox()
            {
                _payload = NativeMemory.Alloc(PayloadSize);
            }

            public SealedBox(AesGcm.Nonce nonce, Data ciphertext, Data tag)
            {
                _payload = NativeMemory.Alloc(PayloadSize);
                SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(_payload);

                TypeMetadata ciphertextMetadata = Runtime.GetMetadata(ciphertext);
                TypeMetadata tagMetadata = Runtime.GetMetadata(tag);
                void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
                void* ciphertextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, ciphertextMetadata, null);
                void* tagWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, tagMetadata, null);

                CryptoKit.PInvoke_AesGcm_SealedBox_Init(
                    swiftIndirectResult,
                    nonce.Payload,
                    &ciphertext,
                    &tag,
                    ciphertextMetadata,
                    tagMetadata,
                    ciphertextWitnessTable,
                    tagWitnessTable,
                    out SwiftError error);

                if (error.Value != null)
                {
                    NativeMemory.Free(_payload);
                    throw new CryptographicException();
                }
            }

            public void* Payload => _payload;

            public static TypeMetadata Metadata => CryptoKit.PInvoke_AesGcm_SealedBox_GetMetadata();

            public Data Ciphertext => CryptoKit.PInvoke_AesGcm_SealedBox_GetCiphertext(new SwiftSelf(_payload));

            public Data Tag => CryptoKit.PInvoke_AesGcm_SealedBox_GetTag(new SwiftSelf(_payload));

            public void Dispose()
            {
                if (!_disposed)
                {
                    NativeMemory.Free(_payload);
                    _disposed = true;
                    GC.SuppressFinalize(this);
                }
            }

            ~SealedBox()
            {
                NativeMemory.Free(_payload);
            }
        }

        /// <summary>
        /// Encrypts the plaintext using the key, nonce, and authenticated data.
        /// </summary>
        public static unsafe SealedBox seal<Plaintext, AuthenticateData>(Plaintext plaintext, SymmetricKey key, Nonce nonce, AuthenticateData aad, out SwiftError error) where Plaintext : unmanaged, ISwiftObject where AuthenticateData : unmanaged, ISwiftObject
        {
            AesGcm.SealedBox sealedBox = new AesGcm.SealedBox();
            SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(sealedBox.Payload);

            TypeMetadata plaintextMetadata = Runtime.GetMetadata(plaintext);
            TypeMetadata aadMetadata = Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* plaintextWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, plaintextMetadata, null);
            void* aadWitnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, aadMetadata, null);

            CryptoKit.PInvoke_AesGcm_Seal(
                swiftIndirectResult,
                &plaintext,
                key.Payload,
                nonce.Payload,
                &aad,
                plaintextMetadata,
                aadMetadata,
                plaintextWitnessTable,
                aadWitnessTable,
                out error);

            return sealedBox;
        }

        /// <summary>
        /// Decrypts the sealed box using the key and authenticated data.
        /// </summary>
        public static unsafe Data open<AuthenticateData>(SealedBox sealedBox, SymmetricKey key, AuthenticateData aad, out SwiftError error) where AuthenticateData : unmanaged, ISwiftObject
        {
            TypeMetadata metadata = Runtime.GetMetadata(aad);
            void* conformanceDescriptor = IDataProtocol.GetConformanceDescriptor;
            void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

            Data data = CryptoKit.PInvoke_AesGcm_Open(
                sealedBox.Payload,
                key.Payload,
                &aad,
                metadata,
                witnessTable,
                out error);

            return data;
        }
    }

    /// <summary>
    /// Represents SymmetricKey in C#.
    /// </summary>
    public sealed unsafe class SymmetricKey : IDisposable, ISwiftObject
    {
        private static nuint PayloadSize = Metadata.ValueWitnessTable->Size;

        public readonly void* _payload;

        private bool _disposed = false;

        public SymmetricKey(SymmetricKeySize symmetricKeySize)
        {
            _payload = NativeMemory.Alloc(PayloadSize);
            SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(_payload);
            CryptoKit.PInvoke_SymmetricKey_Init(swiftIndirectResult, &symmetricKeySize);
        }

        public SymmetricKey(Data data)
        {
            _payload = NativeMemory.Alloc(PayloadSize);
            SwiftIndirectResult swiftIndirectResult = new SwiftIndirectResult(_payload);

            TypeMetadata metadata = Runtime.GetMetadata(data);
            void* conformanceDescriptor = IContiguousBytes.GetConformanceDescriptor;
            void* witnessTable = Foundation.PInvoke_Swift_GetWitnessTable(conformanceDescriptor, metadata, null);

            CryptoKit.PInvoke_SymmetricKey_Init2(swiftIndirectResult, &data, metadata, witnessTable);
        }

        public void* Payload => _payload;

        public static TypeMetadata Metadata => CryptoKit.PInvoke_SymmetricKey_GetMetadata();

        public void Dispose()
        {
            if (!_disposed)
            {
                NativeMemory.Free(_payload);
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        ~SymmetricKey()
        {
            NativeMemory.Free(_payload);
        }
    }

    /// <summary>
    /// Represents SymmetricKeySize in C#.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public unsafe struct SymmetricKeySize
    {
        private readonly nint _bitCount;

        public SymmetricKeySize(nint bitCount)
        {
            SymmetricKeySize instance;
            CryptoKit.PInvoke_init(new SwiftIndirectResult(&instance), bitCount);
            this = instance;
        }
    }

    /// <summary>
    /// Swift CryptoKit PInvoke methods in C#.
    /// </summary>
    public static class CryptoKit
    {
        public const string Path = "/System/Library/Frameworks/CryptoKit.framework/CryptoKit";

        [DllImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO5NonceVAEycfC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_ChaChaPoly_Nonce_Init(SwiftIndirectResult result);

        [DllImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO5NonceV4dataAEx_tKc10Foundation12DataProtocolRzlufC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_ChaChaPoly_Nonce_Init2(SwiftIndirectResult result, void* data, TypeMetadata metadata, void* witnessTable, out SwiftError error);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO5NonceVMa")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern TypeMetadata PInvoke_ChaChaPoly_Nonce_GetMetadata();

        [DllImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO9SealedBoxV10ciphertext10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern Data PInvoke_ChaChaPoly_SealedBox_GetCiphertext(ChaChaPoly.SealedBox sealedBox);

        [DllImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO9SealedBoxV3tag10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern Data PInvoke_ChaChaPoly_SealedBox_GetTag(ChaChaPoly.SealedBox sealedBox);

        [DllImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO9SealedBoxV5nonce10ciphertext3tagAeC5NonceV_xq_tKc10Foundation12DataProtocolRzAkLR_r0_lufC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern ChaChaPoly.SealedBox PInvoke_ChaChaPoly_SealedBox_Init(void* nonce, void* ciphertext, void* tag, TypeMetadata ciphertextMetadata, TypeMetadata tagMetadata, void* ciphertextWitnessTable, void* tagWitnessTable, out SwiftError error);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO5NonceVAGycfC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_AesGcm_Nonce_Init(SwiftIndirectResult result);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO5NonceV4dataAGx_tKc10Foundation12DataProtocolRzlufC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_AesGcm_Nonce_Init2(SwiftIndirectResult result, void* data, TypeMetadata metadata, void* witnessTable, out SwiftError error);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO5NonceVMa")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern TypeMetadata PInvoke_AesGcm_Nonce_GetMetadata();

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO9SealedBoxVMa")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern TypeMetadata PInvoke_AesGcm_SealedBox_GetMetadata();

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO9SealedBoxV10ciphertext10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern Data PInvoke_AesGcm_SealedBox_GetCiphertext(SwiftSelf sealedBox);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO9SealedBoxV3tag10Foundation4DataVvg")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern Data PInvoke_AesGcm_SealedBox_GetTag(SwiftSelf sealedBox);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO9SealedBoxV5nonce10ciphertext3tagAgE5NonceV_xq_tKc10Foundation12DataProtocolRzAmNR_r0_lufC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_AesGcm_SealedBox_Init(SwiftIndirectResult result, void* nonce, void* ciphertext, void* tag, TypeMetadata ciphertextMetadata, TypeMetadata tagMetadata, void* ciphertextWitnessTable, void* tagWitnessTable, out SwiftError error);

        [DllImport(Path, EntryPoint = "$s9CryptoKit12SymmetricKeyV4sizeAcA0cD4SizeV_tcfC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_SymmetricKey_Init(SwiftIndirectResult result, SymmetricKeySize* symmetricKeySize);

        [DllImport(Path, EntryPoint = "$s9CryptoKit12SymmetricKeyV4dataACx_tc10Foundation15ContiguousBytesRzlufC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_SymmetricKey_Init2(SwiftIndirectResult result, void* data, TypeMetadata metadata, void* witnessTable);

        [DllImport(Path, EntryPoint = "$s9CryptoKit12SymmetricKeyVMa")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern TypeMetadata PInvoke_SymmetricKey_GetMetadata();

        [DllImport(Path, EntryPoint = "$s9CryptoKit16SymmetricKeySizeV8bitCountACSi_tcfC")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_init(SwiftIndirectResult result, nint bitCount);

        [DllImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO4seal_5using5nonce14authenticatingAC9SealedBoxVx_AA12SymmetricKeyVAC5NonceVSgq_tK10Foundation12DataProtocolRzAoPR_r0_lFZ")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern ChaChaPoly.SealedBox PInvoke_ChaChaPoly_Seal(void* plaintext, void* key, void* nonce, void* aad, TypeMetadata plaintextMetadata, TypeMetadata aadMetadata, void* plaintextWitnessTable, void* aadWitnessTable, out SwiftError error);

        [DllImport(Path, EntryPoint = "$s9CryptoKit03ChaC4PolyO4open_5using14authenticating10Foundation4DataVAC9SealedBoxV_AA12SymmetricKeyVxtKAG0I8ProtocolRzlFZ")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern Data PInvoke_ChaChaPoly_Open(ChaChaPoly.SealedBox sealedBox, void* key, void* aad, TypeMetadata metadata, void* witnessTable, out SwiftError error);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO4seal_5using5nonce14authenticatingAE9SealedBoxVx_AA12SymmetricKeyVAE5NonceVSgq_tK10Foundation12DataProtocolRzAqRR_r0_lFZ")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern void PInvoke_AesGcm_Seal(SwiftIndirectResult result, void* plaintext, void* key, void* nonce, void* aad, TypeMetadata plaintextMetadata, TypeMetadata aadMetadata, void* plaintextWitnessTable, void* aadWitnessTable, out SwiftError error);

        [DllImport(Path, EntryPoint = "$s9CryptoKit3AESO3GCMO4open_5using14authenticating10Foundation4DataVAE9SealedBoxV_AA12SymmetricKeyVxtKAI0I8ProtocolRzlFZ")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
        public static unsafe extern Data PInvoke_AesGcm_Open(void* sealedBox, void* key, void* aad, TypeMetadata metadata, void* witnessTable, out SwiftError error);
    }
}
