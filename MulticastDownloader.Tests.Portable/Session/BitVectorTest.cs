// <copyright file="BitVectorTest.cs" company="MS">
// Copyright (c) 2016 MS.
// </copyright>

namespace MS.MulticastDownloader.Tests.Session
{
    using Core.Session;
    using Xunit;

    public class BitVectorTest
    {
        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(12)]
        public void BitVectorConstructs(long szVector)
        {
            BitVector bv = new BitVector(szVector);
            Assert.Equal(szVector, bv.LongCount);
            Assert.Equal(szVector, bv.Count);
            Assert.True(bv.RawBits.Length / 8 <= szVector);
        }

        [Theory]
        [InlineData(new byte[] { 1 }, 1)]
        [InlineData(new byte[] { 0xF }, 4)]
        [InlineData(new byte[] { 0xFF }, 8)]
        [InlineData(new byte[] { 0xF, 0xFF }, 12)]
        public void BitVectorConstructsAndInitializes(byte[] initData, long szVector)
        {
            BitVector bv = new BitVector(szVector, initData);
            Assert.Equal(szVector, bv.LongCount);
            Assert.Equal(szVector, bv.Count);
            Assert.Equal(initData, bv.RawBits);
        }

        [Theory]
        [InlineData(new bool[] { false, false, false })]
        [InlineData(new bool[] { true, true, true })]
        [InlineData(new bool[] { false, false, false, false, false, false, false })]
        [InlineData(new bool[] { true, true, true, true, true, true, true })]
        [InlineData(new bool[] { false, true, false })]
        [InlineData(new bool[] { false, true, false })]
        [InlineData(new bool[] { true, false, true, false, true, false, true })]
        [InlineData(new bool[] { false, true, false, true, false, true, false })]
        public void BitVectorSequenceEqualsAfterConstructionAndInitialization(bool[] sequence)
        {
            BitVector bv = new BitVector(sequence.Length);
            for (int i = 0; i < sequence.Length; ++i)
            {
                Assert.False(bv[i]);
                bv[i] = sequence[i];
                Assert.Equal(bv[i], sequence[i]);
            }
        }

        [Theory]
        [InlineData(new bool[] { false, false, false }, 0)]
        [InlineData(new bool[] { true, true, true }, 2)]
        [InlineData(new bool[] { false, false, false, false, false, false, false }, 2)]
        [InlineData(new bool[] { true, true, true, true, true, true, true }, 5)]
        [InlineData(new bool[] { false, true, false }, 1)]
        [InlineData(new bool[] { false, true, false }, 1)]
        [InlineData(new bool[] { true, false, true, false, true, false, true }, 2)]
        [InlineData(new bool[] { false, true, false, true, false, true, false }, 5)]
        public void BitVectorValueFlipsAfterConstructionAndInitialization(bool[] sequence, int flipIndex)
        {
            BitVector bv = new BitVector(sequence.Length);
            for (int i = 0; i < sequence.Length; ++i)
            {
                Assert.False(bv[i]);
                bv[i] = sequence[i];
                Assert.Equal(bv[i], sequence[i]);
            }

            bool expected = !bv[flipIndex];
            bv[flipIndex] = !bv[flipIndex];
            Assert.Equal(expected, bv[flipIndex]);
        }
    }
}
