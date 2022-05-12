using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

/*-------------------------------------------------------------------------------------------------------------------
                                                Perceptron Assignment
                                                Course:     AM6007
                                                Lecturer:   Dr Kieran Mulchrone 

                                                Name:       Mattis Voss
                                                Student #:  121128764
                                                Date:       29/10/2021
-------------------------------------------------------------------------------------------------------------------*/


namespace Perceptron
{
    class Program
    {
        /* This program creates a perceptron. It reads in data from a CSV file, trains weights on the data 
         * and outputs the weights vector. 
         * It then asks the user to input data for an engine and returns which group the engine falls in.*/
        static void Main(string[] args)
        {
            Perceptron p = new Perceptron();
            p.ReadData("data.csv");
            p.TrainData();
            p.Output();
            p.ClassifyPoint();
        }
    }



    /*-------------------------------------------------------------------------------------------------------------------*/
    /*                                          PERCEPTRON CLASS                                                         */
    /*-------------------------------------------------------------------------------------------------------------------*/




    /// <summary>
    /// Creates a perceptron to linearly separate engine RPM and vibration datapoints.
    /// The constructor takes a path to a CSV file as its only argument. If no file is specified, data may be 
    /// read in at a later point using public method "ReadData()". Properties include "Iters", the iterations taken to reach convergence
    /// "Alpha", the learning rate, and "MaxIters", the maximum number of iterations allowed.
    /// 
    /// Public methods:
    /// ReadData()         Reads in data from csv file.
    /// SetLearnRate()     Sets learning Rate based on console input. Can also set learning rate by directly modifying property "Alpha"
    /// TrainData()        Trains the perceptron on the data from the csv file
    /// Output()           Outputs the trained weight matrix
    /// ClassifyPoint()    Classifies an engine based on RPM and vibration input via console
    /// 
    /// </summary>
    public class Perceptron
    {
        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Private data                                                             */
        /*-------------------------------------------------------------------------------------------------------------------*/

        // "data" is an array of tuples based on a csv file. Each tuple will contain an input x-vector and the corresponding y-value
        private (Vector, double)[] data;
        // "w" is the vector of weights to train. It has 3 weight entries: bias, RPM, and vibration, all initialised to 0.
        private Vector w = new Vector(new double[] { 0, 0, 0 });
        // Learning rate, should be small and positive
        private double alpha = 0.01;
        // Maximum iterations allowed
        private int maxIters = 5000000;
        // Counter for total iterations to convergence
        private int iters = 0;


        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Properties                                                               */
        /*-------------------------------------------------------------------------------------------------------------------*/
        public double Alpha //Learning rate
        {
            get
            {
                return alpha;
            }
            set
            {
                if (alpha > 0 && alpha < 1) alpha = value;
                else Console.WriteLine("Learning rate should be between 0 and 1.");
            }
        }
        public int MaxIters //Maximum iterations allowed
        {
            get
            {
                return maxIters;
            }
            set
            {
                if (maxIters > 0) maxIters = value;
                else Console.WriteLine("Maximum iterations should be greater than 0.");
            }
        }

        public int Iters { get => iters; } // Counter for iterations

        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Constructors                                                             */
        /*-------------------------------------------------------------------------------------------------------------------*/

        // Constructor with CSV file path and learning rate as arguments

        public Perceptron(string path, double alpha = 0.01)
        {
            this.data = ReadData(path);
        }
        // Default constructor
        public Perceptron()
        {
        }

        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Private methods                                                          */
        /*-------------------------------------------------------------------------------------------------------------------*/

        /*Checks a row array for non-numeric entries, i.e. headers or footers.
         Returns true if all entries in a row can be converted to doubles*/
        private bool NotHeader(string[] rowArray)
        {
            bool res = true;
            foreach (string entry in rowArray)
            {
                res = res && double.TryParse(entry, out double NA);
            }
            return res;
        }

        // Definition of Heaviside step function, returns 1 if input is positive and 0 otherwise 
        private double H(double input)
        {
            return input > 0 ? 1 : 0;
        }

        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Public methods                                                           */
        /*-------------------------------------------------------------------------------------------------------------------*/

        /* Set learning rate "alpha" for the perceptron based on console input*/
        public void SetLearnRate()
        {
            double alpha;
            Console.WriteLine("Please enter learning rate (positive number less than 1):");
            string input = Console.ReadLine();
            bool success = double.TryParse(input, out alpha) && alpha > 0 && alpha < 1;
            while (!success)
            {
                Console.WriteLine("Please try again, positive integer less than 1 required:");
                input = Console.ReadLine();
                success = double.TryParse(input, out alpha) && alpha > 0 && alpha < 1;
            }
            this.alpha = alpha;
        }

        /*Reads data from a CSV file using a StreamReader. Filters out lines with non-numeric entries. For each line it creates
        a Vector with entries (1, RPM, VIBRATIONS), and a double with the value of STATUS. It combines the Vector and double into a Tuple and
        adds it to the list "data". Finally it returns an array conversion of "data". This array is the main input to "TrainData".*/
        public (Vector, double)[] ReadData(string path)
        {
            // Check the CSV file exists
            FileInfo f = new FileInfo(@path);
            while (!f.Exists)
            {
                Console.WriteLine("File not found, please enter another path:");
                path = Console.ReadLine();
                f = new FileInfo(@path);
            }

            // Read in each line of file
            List<(Vector, double)> data = new List<(Vector, double)>();
            char[] separators = { ' ', ',', '.', ':', '\t' };
            StreamReader sr = new StreamReader(@path);
            string line = sr.ReadLine();
            while (line != null)
            {
                string[] lineArray = line.Split(separators);
                // Check that the line is not a header or footer, and doesn't contain NAN 
                if (NotHeader(lineArray))
                {
                    Vector xi = new Vector(new double[] { 1, Convert.ToDouble(lineArray[1]), Convert.ToDouble(lineArray[2]) });
                    double yi = Convert.ToDouble(lineArray[3]);
                    (Vector, double) xy = (xi, yi);
                    data.Add(xy);
                }
                line = sr.ReadLine();
            }
            sr.Close();
            // Return array of Tuples
            this.data = data.ToArray();
            return data.ToArray();
        }
        /* Trains the perceptron on the vectors of inputs and corresponding status in the array of Tuples "data". */
        public Vector TrainData()
        {
            if (data == null)
                Console.WriteLine("Training data has not been supplied");
            else
            {
                double yhat;
                int error;
                do
                {
                    this.iters++;
                    error = 0;
                    // Loop through all training data points, updating weights and error
                    for (int i = 0; i < data.Length; i++)
                    {
                        Vector xi = new Vector(data[i].Item1);
                        double yi = data[i].Item2;
                        yhat = H(xi * w);
                        if (yi != yhat)
                        {
                            // Apply update function and increment error
                            w = w + this.Alpha * (yi - yhat) * xi;
                            error++;
                        }
                    }
                } while (error != 0 && iters < this.maxIters);
                // Check for convergence
                if (error != 0) { w = null; }

            }
            return w;
        }
        /* Asks user to input RPM and vibration for an engine and check the predicted status.*/
        public void ClassifyPoint()
        {
            // Check that solution converged before trying to classify point
            if (w == null)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("--Cannot classify point--");
                Console.ResetColor();
            }
            else
            {
                double RPM;
                Vector x = new Vector(3);  //Storage for point to check
                Console.Write("Please enter RPM of engine to check: ");
                string input = Console.ReadLine();
                bool success = double.TryParse(input, out RPM) && RPM > 0;
                while (!success)
                {
                    Console.WriteLine("Please try again, positive number is required: ");
                    input = Console.ReadLine();
                    success = double.TryParse(input, out RPM) && RPM > 0;
                }
                double vibration;
                Console.Write("Please enter vibration of engine to check: ");
                input = Console.ReadLine();
                success = double.TryParse(input, out vibration) && vibration > 0;
                while (!success)
                {
                    Console.WriteLine("Please try again, positive number is required: ");
                    input = Console.ReadLine();
                    success = double.TryParse(input, out vibration) && vibration > 0;
                }
                // Assign inputs to x-Vector
                x[0] = 1;
                x[1] = RPM;
                x[2] = vibration;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n------------------------------------------------------");
                Console.WriteLine("|  The perceptron predicts that the engine is {0} |", H(x * w) <= 0 ? "faulty" : "good  ");
                Console.Write("------------------------------------------------------");
                Console.ResetColor();
                Console.WriteLine("\n");
                Console.WriteLine("\n");
            }

        }
        /* Output Vector of weights calculated by perceptron */
        public void Output()
        {
            if (w == null) // Check for convergence
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\nSolution did not converge");
                Console.ResetColor();
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nSolution converged in {0} iterations", Iters);
                Console.ResetColor();
                Console.WriteLine("\nTable of Trained Weights:");
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n--------------------------");
                Console.WriteLine("|  w0: {0,16}  |", w[0].ToString("#.###"));
                Console.WriteLine("|  w1: {0,16}  |", w[1].ToString("#.###"));
                Console.WriteLine("|  w2: {0,16}  |", w[2].ToString("#.###"));
                Console.WriteLine("--------------------------\n");
                Console.ResetColor();
                Console.WriteLine("Equation of Line of Separation:");
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n--------------------------------------");
                Console.WriteLine("|  RPM = {0} x Vibration + {1}  |", (-w[1] / w[2]).ToString("0.00"), (-w[0] / w[2]).ToString("0.00"));
                Console.Write("--------------------------------------");
                Console.ResetColor();
                Console.WriteLine("\n");
                Console.WriteLine("\n");
            }

        }
    }


    /*-------------------------------------------------------------------------------------------------------------------*/
    /*                                          VECTOR CLASS                                                             */
    /*-------------------------------------------------------------------------------------------------------------------*/


    /// <summary>
    /// Represents a vector. Length is fixed after initialistaion, but elements can be reassigned. 
    /// Allows use of [] notation for indexing.
    /// Default constructor asks user for input of vector length via console, creates a zero-Vector.
    /// 
    /// Overloaded operators:
    /// * Vector dot product (Vector * Vector)
    /// * Scaling of a vector by a double (double * Vector)
    /// + Binary and unary Vector addition
    /// - Binary and unary Vector subtraction
    /// 
    /// Public methods:
    /// ToString(): Returns vector as a row in square brackets.
    /// 
    /// </summary>
    public class Vector
    {
        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Private data                                                             */
        /*-------------------------------------------------------------------------------------------------------------------*/
        // Array of vector elements
        private double[] elems;
        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Properties                                                               */
        /*-------------------------------------------------------------------------------------------------------------------*/
        public double[] Elems
        {
            get
            {
                return elems;
            }
            set
            {
                if (value.Length == this.Length) elems = value;
                else Console.WriteLine("Input array must have same length as existing vector");
            }
        }
        public int Length { get => elems.Length; }

        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Constructors                                                             */
        /*-------------------------------------------------------------------------------------------------------------------*/

        // Default constructor, asks for length input from console
        public Vector()
        {
            int length;
            Console.WriteLine("Please enter length of vector required (positive integer):");
            string input = Console.ReadLine();
            bool success = int.TryParse(input, out length) && length >= 0;
            while (!success)
            {
                Console.WriteLine("Please try again, a positive integer is required:");
                input = Console.ReadLine();
                success = int.TryParse(input, out length) && length >= 0;
            }
            elems = new double[length];
        }
        // This constructor takes an array of doubles representing elements as its argument
        public Vector(double[] elems)
        {
            this.elems = elems;
        }
        // This constructor takes an integer defining length as its argument
        public Vector(int length)
        {
            bool success = length > 0 ? true : false;
            while (!success)
            {
                Console.WriteLine("Please enter a vector length greater than 0:");
                string input = Console.ReadLine();
                success = int.TryParse(input, out length) && length > 0;
            }

            double[] elems = new double[length];
            this.elems = elems;
        }
        // Copy constructor
        public Vector(Vector other)
        {
            this.elems = other.elems;
        }

        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Operator overloading                                                     */
        /*-------------------------------------------------------------------------------------------------------------------*/

        // Overload * operator for inner product
        public static double operator *(Vector left, Vector right)
        {
            double dotProd = 0;
            if (left.Length != right.Length)
                throw new ArgumentException("Inner product only possible for vectors of equal dimension");
            for (int i = 0; i < left.Length; i++)
            {
                dotProd += left.Elems[i] * right.Elems[i];
            }
            return dotProd;
        }
        // Overload + operator for binary addition
        public static Vector operator +(Vector left, Vector right)
        {
            Vector sum = new Vector(left.Elems.Length);
            if (left.Length != right.Length)
                throw new ArgumentException("Addition only possible for vectors of equal dimension");
            for (int i = 0; i < left.Length; i++)
            {
                sum.Elems[i] = left.Elems[i] + right.Elems[i];
            }
            return sum;
        }
        // Overload - operator for binary subtraction
        public static Vector operator -(Vector left, Vector right)
        {
            Vector diff = new Vector(left.Elems.Length);
            if (left.Length != right.Length)
                throw new ArgumentException("Subtraction only possible for vectors of equal dimension");
            for (int i = 0; i < left.Length; i++)
            {
                diff.Elems[i] = left.Elems[i] - right.Elems[i];
            }
            return diff;
        }

        // Overload + operator for unary addition
        public static Vector operator +(Vector right)
        {
            Vector tmp = new Vector(right.Elems.Length);
            for (int i = 0; i < right.Elems.Length; i++)
            {
                tmp.Elems[i] = right.Elems[i];
            }
            return tmp;
        }
        // Overload - operator for unary subtraction
        public static Vector operator -(Vector right)
        {
            int i = 0;
            Vector tmp = new Vector(right.Elems.Length);
            for (i = 0; i < right.Elems.Length; i++)
            {
                tmp.Elems[i] = -right.Elems[i];
            }
            return tmp;
        }
        // Overload * operator to scale a vector by a double
        public static Vector operator *(double x, Vector stretch)
        {
            Vector result = new Vector(stretch.Elems.Length);
            for (int i = 0; i < stretch.Length; i++)
            {
                result.Elems[i] = stretch.Elems[i] * x;
            }
            return result;
        }

        /*-------------------------------------------------------------------------------------------------------------------*/
        /*                                          Indexing and overrides                                                   */
        /*-------------------------------------------------------------------------------------------------------------------*/

        // Indexer to allow use of [] notation
        public double this[int i]
        {
            get { return this.Elems[i]; }
            set { this.Elems[i] = value; }
        }
        // Override array string representation to return a row vector in square brackets
        public override string ToString()
        {
            string tmp = string.Join(", ", elems);
            return string.Format("[{0}]", tmp);
        }

    }
}




