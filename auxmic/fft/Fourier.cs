using System;
using System.Numerics;

namespace auxmic.fft
{
    // based on
    // AForge Math Library
    // AForge.NET framework
    // http://www.aforgenet.com/framework/
    //
    // Copyright © Andrew Kirillov, 2005-2009
    // andrew.kirillov@aforgenet.com
    //
    // FFT idea from Exocortex.DSP library
    // http://www.exocortex.org/dsp/
    //
    // http://code.google.com/p/aforge/source/browse/trunk/Sources/Math/FourierTransform.cs
    internal sealed class Fourier
    {    
        private const int MAX_LENGTH = 4096; //16384;
        private const int MIN_LENGTH = 1; //2;
        private const int MAX_BITS = 12; //14;
        private const int MIN_BITS = 0; //1;

        private static int[][] _reversedBits = new int[MAX_BITS][];
        private static Complex[,][] _complexRotation = new Complex[MAX_BITS, 2][];

        /// <summary>
        /// Compute a 1D fast Fourier transform of a dataset of complex numbers.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="direction"></param>
        internal static void FFT(Complex[] data, Direction direction)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            int length = data.Length;

            if (Tools.IsPowerOf2(length) == false)
            {
                throw new ArgumentOutOfRangeException("data length", length, "must be a power of 2");
            }

            int ln = Tools.Log2(length);

            // reorder array
            ReorderArray(data);

            // successive doubling
            int N = 1;
            int M;

            for (int level = 1; level <= ln; level++)
            {
                M = N;
                N <<= 1;

                Complex[] rotation = GetComplexRotation(level, direction);

                for (int j = 0; j < M; j++)
                {
                    Complex t = rotation[j];

                    for (int even = j; even < length; even += N)
                    {
                        int odd = even + M;

                        Complex evenElement = data[even];
                        Complex oddElement = data[odd];

                        double odduR = oddElement.Real * t.Real - oddElement.Imaginary * t.Imaginary;
                        double odduI = oddElement.Real * t.Imaginary + oddElement.Imaginary * t.Real;

                        data[even] = new Complex(evenElement.Real + odduR, evenElement.Imaginary + odduI);
                        data[odd] = new Complex(evenElement.Real - odduR, evenElement.Imaginary - odduI);
                    }
                }
            }

            if (direction == Direction.Forward)
            {
                for (int i = 0; i < length; i++)
                {
                    data[i] = new Complex(data[i].Real / (double)length, data[i].Imaginary / (double)length);
                }
            }
        }

        private static void ReorderArray(Complex[] data)
        {
            int length = data.Length;

            if ((length < MIN_LENGTH) || (length > MAX_LENGTH) || (!Tools.IsPowerOf2(length)))
                throw new ArgumentException("Incorrect data length.");

            int[] reversedBits = GetReversedBits(Tools.Log2(length));

            for (int i = 0; i < length; i++)
            {
                int swap = reversedBits[i];

                if (swap > i)
                {
                    Complex temp = data[i];
                    data[i] = data[swap];
                    data[swap] = temp;
                }
            }
        }

        private static int[] GetReversedBits(int numberOfBits)
        {
            if ((numberOfBits < MIN_BITS) ||
                (numberOfBits > MAX_BITS))
            {
                throw new ArgumentOutOfRangeException();
            }

            if (_reversedBits[numberOfBits - 1] == null)
            {
                int maxBits = Tools.Pow2(numberOfBits);
                int[] reversedBits = new int[maxBits];

                for (int i = 0; i < maxBits; i++)
                {
                    int oldBits = i;
                    int newBits = 0;

                    for (int j = 0; j < numberOfBits; j++)
                    {
                        newBits = (newBits << 1) | (oldBits & 1);
                        oldBits = (oldBits >> 1);
                    }

                    reversedBits[i] = newBits;
                }

                _reversedBits[numberOfBits - 1] = reversedBits;
            }

            return _reversedBits[numberOfBits - 1];
        }

        private static Complex[] GetComplexRotation(int levels, Direction direction)
        {
            int directionIndex = (direction == Direction.Forward) ? 0 : 1;

            // check if the array is already calculated
            if (_complexRotation[levels - 1, directionIndex] == null)
            {
                int M = 1 << (levels - 1);
                double uR = 1.0;
                double uI = 0.0;
                double angle = Math.PI / M * (int)direction;
                double wR = Math.Cos(angle);
                double wI = Math.Sin(angle);
                double uwI;
                Complex[] rotation = new Complex[M];

                for (int j = 0; j < M; j++)
                {
                    rotation[j] = new Complex(uR, uI);

                    uwI = uR * wI + uI * wR;
                    uR = uR * wR - uI * wI;
                    uI = uwI;
                }

                _complexRotation[levels - 1, directionIndex] = rotation;
            }

            return _complexRotation[levels - 1, directionIndex];
        }
    }
}
