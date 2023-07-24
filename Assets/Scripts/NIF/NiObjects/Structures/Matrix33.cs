using System.IO;
using UnityEngine;


namespace NIF.NiObjects.Structures
{
    /// <summary>
    /// A 3x3 rotation matrix; M^T M=identity, det(M)=1.    Stored in OpenGL column-major format.
    /// </summary>
    public class Matrix33
    {
        public float[,] Matrix { get; private set; }

        private Matrix33(float[,] matrix)
        {
            Matrix = matrix;
        }

        public static Matrix33 Parse(BinaryReader binaryReader)
        {
            var matrix = new float[3, 3];
            //Top left
            matrix[0, 0] = binaryReader.ReadSingle();
            matrix[1, 0] = binaryReader.ReadSingle();
            //Bottom left
            matrix[2, 0] = binaryReader.ReadSingle();
            matrix[0, 1] = binaryReader.ReadSingle();
            matrix[1, 1] = binaryReader.ReadSingle();
            matrix[2, 1] = binaryReader.ReadSingle();
            //Top right
            matrix[0, 2] = binaryReader.ReadSingle();
            matrix[1, 2] = binaryReader.ReadSingle();
            //Bottom right
            matrix[2, 2] = binaryReader.ReadSingle();
            return new Matrix33(matrix);
        }

        public Matrix4x4 ToMatrix4x4()
        {
            var matrix = new Matrix4x4();

            for (var rowIndex = 0; rowIndex < 4; rowIndex++)
            {
                for (var columnIndex = 0; columnIndex < 4; columnIndex++)
                {
                    if ((rowIndex <= 2) && (columnIndex <= 2))
                    {
                        matrix[rowIndex, columnIndex] = Matrix[rowIndex, columnIndex];
                    }
                    else
                    {
                        matrix[rowIndex, columnIndex] = (rowIndex == columnIndex) ? 1 : 0;
                    }
                }
            }

            return matrix;
        }
    }
}