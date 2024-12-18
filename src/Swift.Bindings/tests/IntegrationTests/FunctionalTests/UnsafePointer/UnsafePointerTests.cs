// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using System.Diagnostics;
using System.Security.Cryptography;

namespace BindingsGeneration.Tests
{
    public class UnsafePointerTests: IClassFixture<UnsafePointerTests.TestFixture>
    {
        private readonly TestFixture _fixture;

        public UnsafePointerTests(TestFixture fixture)
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

        private static unsafe void ChaCha20Poly1305Encrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> aad)
        {
            fixed (void* keyPtr = key)
            fixed (void* noncePtr = nonce)
            fixed (void* plaintextPtr = plaintext)
            fixed (byte* ciphertextPtr = ciphertext)
            fixed (byte* tagPtr = tag)
            fixed (void* aadPtr = aad)
            {
                const int Success = 1;

                Swift.Runtime.UnsafeMutableRawPointer _keyPtr = new Swift.Runtime.UnsafeMutableRawPointer(keyPtr);
                Swift.Runtime.UnsafeMutableRawPointer _noncePtr = new Swift.Runtime.UnsafeMutableRawPointer(noncePtr);
                Swift.Runtime.UnsafeMutableRawPointer _plaintextPtr = new Swift.Runtime.UnsafeMutableRawPointer(plaintextPtr);
                Swift.Runtime.UnsafeMutablePointer<Byte> _ciphertextPtr = new Swift.Runtime.UnsafeMutablePointer<Byte>(ciphertextPtr);
                Swift.Runtime.UnsafeMutablePointer<Byte> _tagPtr = new Swift.Runtime.UnsafeMutablePointer<Byte>(tagPtr);
                Swift.Runtime.UnsafeMutableRawPointer _aadPtr = new Swift.Runtime.UnsafeMutableRawPointer(aadPtr);

                int result = Swift.UnsafePointerTests.UnsafePointerTests.AppleCryptoNative_ChaCha20Poly1305Encrypt(
                                    _keyPtr, key.Length,
                                    _noncePtr, nonce.Length,
                                    _plaintextPtr, plaintext.Length,
                                    _ciphertextPtr, ciphertext.Length,
                                    _tagPtr, tag.Length,
                                    _aadPtr, aad.Length);

                if (result != Success)
                {
                    Debug.Assert(result == 0);
                    Console.WriteLine("Encryption failed");
                }
            }
        }

        private static unsafe void ChaCha20Poly1305Decrypt(
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> aad)
        {
            fixed (void* keyPtr = key)
            fixed (void* noncePtr = nonce)
            fixed (void* ciphertextPtr = ciphertext)
            fixed (void* tagPtr = tag)
            fixed (byte* plaintextPtr = plaintext)
            fixed (void* aadPtr = aad)
            {
                const int Success = 1;
                const int AuthTagMismatch = -1;

                Swift.Runtime.UnsafeMutableRawPointer _keyPtr = new Swift.Runtime.UnsafeMutableRawPointer(keyPtr);
                Swift.Runtime.UnsafeMutableRawPointer _noncePtr = new Swift.Runtime.UnsafeMutableRawPointer(noncePtr);
                Swift.Runtime.UnsafeMutableRawPointer _ciphertextPtr = new Swift.Runtime.UnsafeMutableRawPointer(ciphertextPtr);
                Swift.Runtime.UnsafeMutableRawPointer _tagPtr = new Swift.Runtime.UnsafeMutableRawPointer (tagPtr);
                Swift.Runtime.UnsafeMutablePointer<Byte> _plaintextPtr = new Swift.Runtime.UnsafeMutablePointer<Byte>(plaintextPtr);
                Swift.Runtime.UnsafeMutableRawPointer _aadPtr = new Swift.Runtime.UnsafeMutableRawPointer(aadPtr);

                int result = Swift.UnsafePointerTests.UnsafePointerTests.AppleCryptoNative_ChaCha20Poly1305Decrypt(
                    _keyPtr, key.Length,
                    _noncePtr, nonce.Length,
                    _ciphertextPtr, ciphertext.Length,
                    _tagPtr, tag.Length,
                    _plaintextPtr, plaintext.Length,
                    _aadPtr, aad.Length);

                if (result != Success)
                {
                    CryptographicOperations.ZeroMemory(plaintext);

                    if (result == AuthTagMismatch)
                    {
                        throw new AuthenticationTagMismatchException();
                    }
                    else
                    {
                        Debug.Assert(result == 0);
                        throw new CryptographicException();
                    }
                }
            }
        }

        [Fact]
        public static void TestUnsafePointerCryptoKit()
        {
            byte[] key = RandomNumberGenerator.GetBytes(32); // Generate a 256-bit key
            byte[] nonce = RandomNumberGenerator.GetBytes(12); // Generate a 96-bit nonce
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
            byte[] aad = System.Text.Encoding.UTF8.GetBytes("Additional Authenticated Data");

            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16]; // ChaCha20Poly1305 tag size
            Console.WriteLine($"Plaintext: {BitConverter.ToString(plaintext)}");

            ChaCha20Poly1305Encrypt(
                key,
                nonce,
                plaintext,
                ciphertext,
                tag,
                aad);

            Console.WriteLine($"Ciphertext: {BitConverter.ToString(ciphertext)}");
            Console.WriteLine($"Tag: {BitConverter.ToString(tag)}");

            Array.Clear(plaintext, 0, plaintext.Length);

            ChaCha20Poly1305Decrypt(
                key,
                nonce,
                ciphertext,
                tag,
                plaintext,
                aad
            );

            string decryptedMessage = System.Text.Encoding.UTF8.GetString(plaintext);
            Assert.Equal("Hello, World!", decryptedMessage);
        }
    }
}