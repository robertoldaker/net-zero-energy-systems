namespace SmartEnergyLabDataApi.Common
{
    public static class Utilities
    {
        public static T[] CopyArray<T>(T[] source)
        {
            var dest = new T[source.Length];
            Array.Copy(source, dest, dest.Length);
            return dest;
        }

        public static T[,] CopyArray<T>(T[,] source)
        {
            var dest = new T[source.GetLength(0),source.GetLength(1)];
            Array.Copy(source, dest, dest.Length);
            return dest;
        }

        public static double Determinant(double[,] input)
        {
            int order = int.Parse(System.Math.Sqrt(input.Length).ToString());
            if (order > 2)
            {
                double value = 0;
                for (int j = 0; j < order; j++)
                {
                    double[,] Temp = createSmallerMatrix(input, 0, j);
                    value = value + input[0, j] * (signOfElement(0, j) * Determinant(Temp));
                }
                return value;
            }
            else if (order == 2)
            {
                return ((input[0, 0] * input[1, 1]) - (input[1, 0] * input[0, 1]));
            }
            else
            {
                return (input[0, 0]);
            }
        }

        //this method determines the sign of the elements
        private static int signOfElement(int i, int j)
        {
            if ((i + j) % 2 == 0)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        //this method determines the sub matrix corresponding to a given element
        private static double[,] createSmallerMatrix(double[,] input, int i, int j)
        {
            int order = int.Parse(System.Math.Sqrt(input.Length).ToString());
            double[,] output = new double[order - 1, order - 1];
            int x = 0, y = 0;
            for (int m = 0; m < order; m++, x++)
            {
                if (m != i)
                {
                    y = 0;
                    for (int n = 0; n < order; n++)
                    {
                        if (n != j)
                        {
                            output[x, y] = input[m, n];
                            y++;
                        }
                    }
                }
                else
                {
                    x--;
                }
            }
            return output;
        }


        public static double[,] MatrixInverse(double[,] matrix)
        {
            int n = matrix.GetLength(0);
            double[,] result = MatrixDuplicate(matrix);

            int[] perm;
            int toggle;
            double[,] lum = MatrixDecompose(matrix, out perm, out toggle);
            if (lum == null)
            {
                throw new Exception("Unable to compute inverse");
            }

            double[] b = new double[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0;
                    else
                        b[j] = 0.0;
                }
                double[] x = helperSolve(lum, b); // 

                for (int j = 0; j < n; ++j)
                {
                    result[j, i] = x[j];
                }
            }
            return result;
        }

        // --------------------------------------------------

        private static double[] helperSolve(double[,] luMatrix, double[] b)
        {
            // before calling this helper, permute b using the perm array
            // from MatrixDecompose that generated luMatrix
            int n = luMatrix.GetLength(0);
            double[] x = new double[n];
            b.CopyTo(x, 0);

            for (int i = 1; i < n; ++i)
            {
                double sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i,j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1,n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                double sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix[i,j] * x[j];
                x[i] = sum / luMatrix[i,i];
            }

            return x;
        }

        public static double[,] MatrixDuplicate(double[,] matrix)
        {
            // allocates/creates a duplicate of a matrix.
            double[,] result = new double[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); ++i)
            {
                // copy the values
                for (int j = 0; j < matrix.GetLength(1); ++j)
                {
                    result[i, j] = matrix[i, j];
                }
            }
            return result;
        }

        private static double[,] MatrixDecompose(double[,] matrix, out int[] perm, out int toggle)
        {
            // Doolittle LUP decomposition with partial pivoting.
            // rerturns: result is L (with 1s on diagonal) and U;
            // perm holds row permutations; toggle is +1 or -1 (even or odd)
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1); // assume square
            if (rows != cols)
                throw new Exception("Attempt to decompose a non-square m");

            int n = rows; // convenience

            double[,] result = MatrixDuplicate(matrix);

            perm = new int[n]; // set up row permutation result
            for (int i = 0; i < n; ++i)
            {
                perm[i] = i;
            }

            toggle = 1; // toggle tracks row swaps.
                        // +1 -greater-than even, -1 -greater-than odd. used by MatrixDeterminant

            for (int j = 0; j < n - 1; ++j) // each column
            {
                double colMax = Math.Abs(result[j, j]); // find largest val in col
                int pRow = j;

                // reader Matt V needed this:
                for (int i = j + 1; i < n; ++i)
                {
                    if (Math.Abs(result[i, j]) > colMax)
                    {
                        colMax = Math.Abs(result[i, j]);
                        pRow = i;
                    }
                }
                // Not sure if this approach is needed always, or not.

                if (pRow != j) // if largest value not on pivot, swap rows
                {
                    // smap rows
                    double temp;
                    for (int jj = 0; jj < matrix.GetLength(1); jj++)
                    {
                        temp = result[pRow, jj];
                        result[pRow, jj] = result[j, jj];
                        result[j, jj] = temp;
                    }

                    int tmp = perm[pRow]; // and swap perm info
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }

                // --------------------------------------------------
                // This part added later (not in original)
                // and replaces the 'return null' below.
                // if there is a 0 on the diagonal, find a good row
                // from i = j+1 down that doesn't have
                // a 0 in column j, and swap that good row with row j
                // --------------------------------------------------

                if (result[j, j] == 0.0)
                {
                    // find a good row to swap
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row, j] != 0.0)
                            goodRow = row;
                    }

                    if (goodRow == -1)
                    {
                        throw new Exception("Cannot use Doolittle's method");
                    }

                    // swap rows so 0.0 no longer on diagonal
                    double temp;
                    for (int jj = 0; jj < matrix.GetLength(1); jj++)
                    {
                        temp = result[goodRow, jj];
                        result[goodRow, jj] = result[j, jj];
                        result[j, jj] = temp;
                    }

                    int tmp = perm[goodRow]; // and swap perm info
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }
                // --------------------------------------------------
                // if diagonal after swap is zero . .
                //if (Math.Abs(result[j][j]) less-than 1.0E-20) 
                //  return null; // consider a throw

                for (int i = j + 1; i < n; ++i)
                {
                    result[i, j] /= result[j, j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i, k] -= result[i, j] * result[j, k];
                    }
                }


            } // main j column loop

            return result;
        } // MatrixDecompose        



    }

    class MatrixDeterminant
    {
        public static void Main(string[] args)
        {
            try
            {
                //get the order of determinant from the user
                Console.WriteLine("Enter the order of determinant: ");
                int n = int.Parse(Console.ReadLine().ToString());
                Console.WriteLine("Order of determinant entered: " + n.ToString());
                if (n > 0)
                {
                    double[,] myMatrix = new double[n, n];
                    //input the matrix elements
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            Console.WriteLine("Enter element [" + (i + 1) + "]" + "[" + (j + 1) + "]: ");
                            myMatrix[i, j] = double.Parse(Console.ReadLine().ToString());
                        }
                    }
                    //display the entered matrix
                    Console.WriteLine("Matrix entered: ");
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j < n; j++)
                        {
                            Console.Write(myMatrix[i, j].ToString() + " ");
                        }
                        Console.WriteLine();
                    }
                    Console.WriteLine("Value of the determinant is: " + Determinant(myMatrix));
                }
                else
                {
                    Console.WriteLine("Order should be a positive integer.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
            }
        }
        //this method determines the sign of the elements
        static int SignOfElement(int i, int j)
        {
            if ((i + j) % 2 == 0)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
        //this method determines the sub matrix corresponding to a given element
        static double[,] CreateSmallerMatrix(double[,] input, int i, int j)
        {
            int order = int.Parse(System.Math.Sqrt(input.Length).ToString());
            double[,] output = new double[order - 1, order - 1];
            int x = 0, y = 0;
            for (int m = 0; m < order; m++, x++)
            {
                if (m != i)
                {
                    y = 0;
                    for (int n = 0; n < order; n++)
                    {
                        if (n != j)
                        {
                            output[x, y] = input[m, n];
                            y++;
                        }
                    }
                }
                else
                {
                    x--;
                }
            }
            return output;
        }
        //this method determines the value of determinant using recursion
        static double Determinant(double[,] input)
        {
            int order = int.Parse(System.Math.Sqrt(input.Length).ToString());
            if (order > 2)
            {
                double value = 0;
                for (int j = 0; j < order; j++)
                {
                    double[,] Temp = CreateSmallerMatrix(input, 0, j);
                    value = value + input[0, j] * (SignOfElement(0, j) * Determinant(Temp));
                }
                return value;
            }
            else if (order == 2)
            {
                return ((input[0, 0] * input[1, 1]) - (input[1, 0] * input[0, 1]));
            }
            else
            {
                return (input[0, 0]);
            }
        }
    }
}