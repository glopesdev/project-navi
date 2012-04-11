using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra.Generic;

namespace ProjectNavi.Localization
{
    public static class MatrixExtensions
    {
        public static Matrix<T> Expand<T>(this Matrix<T> matrix, int rows, int columns, int[] indices) where T : struct, IEquatable<T>, IFormattable
        {
            return Expand(matrix, rows, columns, indices, indices);
        }

        public static Matrix<T> Expand<T>(this Matrix<T> matrix, int rows, int columns, int[] indicesI, int[] indicesJ) where T : struct, IEquatable<T>, IFormattable
        {
            var result = matrix.CreateMatrix(rows, columns);
            for (int i = 0; i < indicesI.Length; i++)
            {
                for (int j = 0; j < indicesJ.Length; j++)
                {
                    result[indicesI[i], indicesJ[j]] = matrix[i, j];
                }
            }

            return result;
        }

        public static Vector<T> Take<T>(this Vector<T> vector, params int[] indices) where T : struct, IEquatable<T>, IFormattable
        {
            var result = vector.CreateVector(indices.Length);
            for (int i = 0; i < indices.Length; i++)
            {
                result[i] = vector[indices[i]];
            }

            return result;
        }

        public static Matrix<T> Take<T>(this Matrix<T> matrix, params int[] indices) where T : struct, IEquatable<T>, IFormattable
        {
            return Take(matrix, indices, indices);
        }

        public static Matrix<T> Take<T>(this Matrix<T> matrix, int[] indicesI, int[] indicesJ) where T : struct, IEquatable<T>, IFormattable
        {
            var result = matrix.CreateMatrix(indicesI.Length, indicesJ.Length);
            for (int i = 0; i < indicesI.Length; i++)
            {
                for (int j = 0; j < indicesJ.Length; j++)
                {
                    result[i, j] = matrix[indicesI[i], indicesJ[j]];
                }
            }

            return result;
        }
    }
}
