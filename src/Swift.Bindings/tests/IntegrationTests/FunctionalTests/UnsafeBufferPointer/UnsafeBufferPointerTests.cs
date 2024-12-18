// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Swift.Runtime;
using Swift.UnsafeBufferPointerTests;

namespace BindingsGeneration.Tests
{
    public class UnsafeBufferPointerTests : IClassFixture<UnsafeBufferPointerTests.TestFixture>
    {
        private readonly TestFixture _fixture;

        public UnsafeBufferPointerTests(TestFixture fixture)
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

                Swift.Runtime.UnsafeRawBufferPointer keyBuffer = new Swift.Runtime.UnsafeRawBufferPointer(keyPtr, key.Length);
                Swift.Runtime.UnsafeRawBufferPointer nonceBuffer = new Swift.Runtime.UnsafeRawBufferPointer(noncePtr, nonce.Length);
                Swift.Runtime.UnsafeRawBufferPointer plaintextBuffer = new Swift.Runtime.UnsafeRawBufferPointer(plaintextPtr, plaintext.Length);
                Swift.Runtime.UnsafeMutableBufferPointer<Byte> ciphertextBuffer = new Swift.Runtime.UnsafeMutableBufferPointer<Byte>(ciphertextPtr, ciphertext.Length);
                Swift.Runtime.UnsafeMutableBufferPointer<Byte> tagBuffer = new Swift.Runtime.UnsafeMutableBufferPointer<Byte>(tagPtr, tag.Length);
                Swift.Runtime.UnsafeRawBufferPointer aadBuffer = new Swift.Runtime.UnsafeRawBufferPointer(aadPtr, aad.Length);

                int result = Swift.UnsafeBufferPointerTests.UnsafeBufferPointerTests.AppleCryptoNative_ChaCha20Poly1305Encrypt(
                                    keyBuffer,
                                    nonceBuffer,
                                    plaintextBuffer,
                                    ciphertextBuffer,
                                    tagBuffer,
                                    aadBuffer);

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

                Swift.Runtime.UnsafeRawBufferPointer keyBuffer = new Swift.Runtime.UnsafeRawBufferPointer(keyPtr, key.Length);
                Swift.Runtime.UnsafeRawBufferPointer nonceBuffer = new Swift.Runtime.UnsafeRawBufferPointer(noncePtr, nonce.Length);
                Swift.Runtime.UnsafeRawBufferPointer ciphertextBuffer = new Swift.Runtime.UnsafeRawBufferPointer(ciphertextPtr, ciphertext.Length);
                Swift.Runtime.UnsafeRawBufferPointer tagBuffer = new Swift.Runtime.UnsafeRawBufferPointer (tagPtr, tag.Length);
                Swift.Runtime.UnsafeMutableBufferPointer<Byte> plaintextBuffer = new Swift.Runtime.UnsafeMutableBufferPointer<Byte>(plaintextPtr, plaintext.Length);
                Swift.Runtime.UnsafeRawBufferPointer aadBuffer = new Swift.Runtime.UnsafeRawBufferPointer(aadPtr, aad.Length);

                int result = Swift.UnsafeBufferPointerTests.UnsafeBufferPointerTests.AppleCryptoNative_ChaCha20Poly1305Decrypt(
                    keyBuffer,
                    nonceBuffer,
                    ciphertextBuffer,
                    tagBuffer,
                    plaintextBuffer,
                    aadBuffer);

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
        public static void TestUnsafeBufferPointerCryptoKit()
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