// <copyright file="BufferedCipherBaseExtensions.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Core.Cryptography
{
    using System;
    using Org.BouncyCastle.Crypto;

    internal static class BufferedCipherBaseExtensions
    {
        internal static byte[] Process(this BufferedCipherBase cipher, byte[] input)
        {
            cipher.Reset();

            int inputOffset = 0;
            int maximumOutputLength = cipher.GetOutputSize(input.Length);
            byte[] output = new byte[maximumOutputLength];
            int outputOffset = 0;
            int outputLength = 0;
            int bytesProcessed = cipher.ProcessBytes(input, inputOffset, input.Length, output, outputOffset);
            outputOffset += bytesProcessed;
            outputLength += bytesProcessed;
            bytesProcessed = cipher.DoFinal(output, outputOffset);
            outputOffset += bytesProcessed;
            outputLength += bytesProcessed;

            if (outputLength == output.Length)
            {
                return output;
            }
            else
            {
                byte[] truncatedOutput = new byte[outputLength];
                Buffer.BlockCopy(output, 0, truncatedOutput, 0, outputLength);
                return truncatedOutput;
            }
        }
    }
}
