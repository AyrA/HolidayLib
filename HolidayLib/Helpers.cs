using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace HolidayLib
{
    /// <summary>
    /// Helper functions
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Checks if an enum type is defined
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <param name="value">Enum value</param>
        /// <returns>true, if value is defined</returns>
        public static bool IsDefined<T>(this T value) where T : Enum
        {
            return Enum.IsDefined(typeof(T), value);
        }

        /// <summary>
        /// Performs a calculation made with reverse polish notation (RPN)
        /// </summary>
        /// <param name="instructions">Instructions to process</param>
        /// <param name="initialValues">
        /// Initial stack values. Values are pushed in the order they're defined here,
        /// meaning the last value is at the top of the stack
        /// </param>
        /// <returns>Top value of the stack after computation</returns>
        /// <exception cref="ArgumentException">Invalid instruction</exception>
        public static double RPN(string[] instructions, params double[] initialValues)
        {
            if (instructions == null || instructions.Length == 0)
            {
                return 0.0;
            }
            Dictionary<char, double> storage = new Dictionary<char, double>();
            Stack<double> doubles = new Stack<double>();
            foreach (var v in initialValues)
            {
                doubles.Push(v);
            }
            foreach (var instruction in instructions.Select(m => m.Trim().ToUpper()))
            {
                if (double.TryParse(instruction, out double newValue))
                {
                    doubles.Push(newValue);
                }
                else
                {
                    //Add
                    if (instruction == "+")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(a + b);
                    }
                    //Subtract
                    else if (instruction == "-")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(b - a);
                    }
                    //Multiply
                    else if (instruction == "*")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(a * b);
                    }
                    //Divide
                    else if (instruction == "/")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(b / a);
                    }
                    //Divide as integers
                    else if (instruction == "\\")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(Math.Floor(b / a));
                    }
                    //Power
                    else if (instruction == "**")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(Math.Pow(a, b));
                    }
                    //Replace value with 0 if it's NaN
                    else if (instruction == "NAN0")
                    {
                        var a = doubles.Pop();
                        doubles.Push(double.IsNaN(a) ? 0.0 : a);
                    }
                    //Replace value with largest possible double value if it's infinite
                    else if (instruction == "INFMAX")
                    {
                        var a = doubles.Pop();
                        doubles.Push(a == double.PositiveInfinity ? double.MaxValue : (a == double.NegativeInfinity ? double.MinValue : a));
                    }
                    //Math.Floor
                    else if (instruction == "FLOOR")
                    {
                        var a = doubles.Pop();
                        doubles.Push(Math.Floor(a));
                    }
                    //Math.Ceil
                    else if (instruction == "CEIL")
                    {
                        var a = doubles.Pop();
                        doubles.Push(Math.Ceiling(a));
                    }
                    //Math.Round with given number of decimals
                    else if (instruction == "ROUND")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(Math.Round(a, (int)b));
                    }
                    //Modulo
                    else if (instruction == "MOD" || instruction == "%")
                    {
                        var a = (int)doubles.Pop();
                        var b = (int)doubles.Pop();
                        doubles.Push(b % a);
                    }
                    //Duplicate top stack value
                    else if (instruction == "DUP")
                    {
                        var a = doubles.Pop();
                        doubles.Push(a);
                        doubles.Push(a);
                    }
                    //Swap top stack values
                    else if (instruction == "SWAP")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(a);
                        doubles.Push(b);
                    }
                    //Constant E
                    else if (instruction == "E")
                    {
                        doubles.Push(Math.E);
                    }
                    //Constant PI
                    else if (instruction == "PI")
                    {
                        doubles.Push(Math.PI);
                    }
                    //Comparison operator: >
                    else if (instruction == ">")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(b > a ? 1 : 0);
                    }
                    //Comparison operator: <
                    else if (instruction == "<")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(b < a ? 1 : 0);
                    }
                    //Comparison operator: >=
                    else if (instruction == ">=")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(b >= a ? 1 : 0);
                    }
                    //Comparison operator: <=
                    else if (instruction == "<=")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(b <= a ? 1 : 0);
                    }
                    //Comparison operator: ==
                    else if (instruction == "=")
                    {
                        var a = doubles.Pop();
                        var b = doubles.Pop();
                        doubles.Push(b == a ? 1 : 0);
                    }
                    //Comparison operator: "Approximately equal"
                    else if (instruction == "~=")
                    {
                        var a = (float)doubles.Pop();
                        var b = (float)doubles.Pop();
                        doubles.Push(b == a ? 1 : 0);
                    }
                    //Complex instructions in "COMMAND:OPERATOR" format
                    else
                    {
                        var cmd = Regex.Match(instruction, @"^([^:]+):(.+)$");
                        if (cmd.Success)
                        {
                            var command = cmd.Groups[1].Value;
                            var argument = cmd.Groups[2].Value;

                            switch (command)
                            {
                                //Store a value in memory (overwrites existing value if any)
                                case "STO":
                                    if (argument.Length > 1)
                                    {
                                        throw new ArgumentException($"Argument of `{instruction}` must be a single char only");
                                    }
                                    storage[argument[0]] = doubles.Pop();
                                    break;
                                //Recall a value from memory
                                case "RCL":
                                    if (argument.Length > 1)
                                    {
                                        throw new ArgumentException($"Argument of `{instruction}` must be a single char only");
                                    }
                                    doubles.Push(storage[argument[0]]);
                                    break;
                                //Delete value from memory
                                case "DEL":
                                    if (argument.Length > 1)
                                    {
                                        throw new ArgumentException($"Argument of `{instruction}` must be a single char only");
                                    }
                                    storage.Remove(argument[0]);
                                    break;
                                default:
                                    throw new ArgumentException($"Command '{instruction}' is invalid");
                            }
                        }
                        else
                        {
                            throw new ArgumentException($"Instruction '{instruction}' is invalid");
                        }
                    }
                }
            }
            return doubles.Pop();
        }

        /// <summary>
        /// Checks if the given values are valid RPN instructions,
        /// and throws if not.
        /// </summary>
        /// <param name="instructions">RPN Instructions</param>
        /// <exception cref="ArgumentException">Invalid RPN instruction</exception>
        /// <remarks>
        /// This doesn't checks if the instructions is valid
        /// in the sense that it's possible to actually execute at the given time.
        /// For example, this will not throw if the first instruction is "+"
        /// even though the stack is empty at that point.
        /// </remarks>
        public static void EnsureValidRPN(string[] instructions)
        {
            var knownInstructions = "+ - * / \\ ** % NAN0 INFMAX FLOOR CEIL ROUND MOD DUP SWAP " +
                "E PI > < <= >= = ~=".Split(' ');
            foreach (var instruction in instructions.Select(m => m.Trim().ToUpper()))
            {
                //Number
                if (double.TryParse(instruction, out double newValue))
                {
                    continue;
                }
                //Simple instructions
                else if (knownInstructions.Contains(instruction))
                {
                    continue;
                }
                //Complex instructions in "COMMAND:OPERATOR" format
                else
                {
                    var cmd = Regex.Match(instruction, @"^([^:]+):(.+)$");
                    if (cmd.Success)
                    {
                        var command = cmd.Groups[1].Value;
                        var argument = cmd.Groups[2].Value;

                        switch (command)
                        {
                            //Store a value in memory (overwrites existing value if any)
                            case "STO":
                                if (argument.Length > 1)
                                {
                                    throw new ArgumentException($"Argument of `{instruction}` must be a single char only");
                                }
                                break;
                            //Recall a value from memory
                            case "RCL":
                                if (argument.Length > 1)
                                {
                                    throw new ArgumentException($"Argument of `{instruction}` must be a single char only");
                                }
                                break;
                            //Delete value from memory
                            case "DEL":
                                if (argument.Length > 1)
                                {
                                    throw new ArgumentException($"Argument of `{instruction}` must be a single char only");
                                }
                                break;
                            default:
                                throw new ArgumentException($"Command '{instruction}' is invalid");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Instruction '{instruction}' is invalid");
                    }
                }

            }
        }

        /// <summary>
        /// Serializes a nullable integer
        /// </summary>
        /// <param name="BW">Binary writer</param>
        /// <param name="i">number</param>
        public static void SerializeNullable(BinaryWriter BW, int? i)
        {
            BW.Write((byte)(i == null ? 0 : 1));
            if (i.HasValue)
            {
                BW.Write(i.Value);
            }
        }

        /// <summary>
        /// Deserializes a nullable item from data
        /// </summary>
        /// <param name="BR">Data reader</param>
        /// <returns>Deserialized number</returns>
        /// <exception cref="InvalidDataException">Invalid deserialized data</exception>
        public static int? DeserializeNullableInt(BinaryReader BR)
        {
            switch (BR.ReadByte())
            {
                case 0:
                    return null;
                case 1:
                    return BR.ReadInt32();
                default:
                    throw new InvalidDataException("Invalid serialized data. Expected value 0 or 1, but got something different.");
            }
        }
    }
}
