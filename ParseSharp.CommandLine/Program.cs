using System;
using System.Collections.Generic;
using System.IO;
using ParseSharp.BackusNaur;

namespace ParseSharp.CommandLine
{
    class Program
    {
        static void WriteError(string format, params object[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, args);
            Console.ResetColor();
        }

        static bool TryReadArg(string[] args, ref int i, out string value)
        {
            value = null;

            if (i >= args.Length - 1) {
                WriteError("Unexpected end of arguments");
                return false;
            }

            value = args[++i];
            return true;
        }

        static bool TryParse(Parser parser, string filePath, string outputFormat = null)
        {
            filePath = Path.GetFullPath(filePath);

            if (!File.Exists(filePath)) {
                WriteError("File not found: {0}", filePath);
                return false;
            }

            var result = parser.Parse(File.ReadAllText(filePath));
            if (result.Error != null) {
                WriteError("{0}({2},{3}): {1}", filePath, result.Error.Message, result.Error.LineNumber, result.Error.ColumnNumber);
                return false;
            }

            Console.WriteLine("Parsed {0} successfully", filePath);

            if (outputFormat != null) {
                var destPath = outputFormat
                    .Replace("$dir", Path.GetDirectoryName(filePath))
                    .Replace("$file", Path.GetFileNameWithoutExtension(filePath))
                    .Replace("$ext", Path.GetExtension(filePath));

                File.WriteAllText(destPath, result.ToString());
            }

            return true;
        }

        static int ParseFiles(string grammarFile, string rootParserName, List<string> sourceFiles, string outputFormat)
        {
            if (string.IsNullOrEmpty(grammarFile)) {
                WriteError("Expected a grammar file path");
                return 1;
            }
            
            Parser parser = new ExtendedBackusNaurFormParser();

            if (!TryParse(parser, grammarFile)) return 1;

            try {
                parser = ParserGenerator.FromEBnf(File.ReadAllText(grammarFile), rootParserName);
            } catch (ParserGenerationException e) {
                foreach (var semanticError in e.Errors) {
                    WriteError("{0}({2},{3}): {1}", grammarFile, semanticError.Message, semanticError.LineNumber, semanticError.ColumnNumber);
                }
            }

            var error = false;

            foreach (var file in sourceFiles) {
                error |= TryParse(parser, file, outputFormat);
            }

            return error ? 1 : 0;
        }

        static int Main(string[] args)
        {
            string grammarFile = null;
            string rootParserName = null;
            string outputFormat = null;

            var waitForInput = false;
            var sourceFiles = new List<string>();

            for (var i = 0; i < args.Length; ++i) {
                switch (args[i]) {
                    case "-g":
                    case "--grammar": {
                        if (!TryReadArg(args, ref i, out grammarFile)) return 1;
                    } break;
                    case "-p":
                    case "--root-parser": {
                        if (!TryReadArg(args, ref i, out rootParserName)) return 1;
                        } break;
                    case "-o":
                    case "--output-format": {
                        if (!TryReadArg(args, ref i, out outputFormat)) return 1;
                    } break;
                    case "-w":
                    case "--wait-for-input": {
                        waitForInput = true;
                    } break;
                    default: {
                        sourceFiles.Add(args[i]);
                    } break;
                }
            }

            var result = ParseFiles(grammarFile, rootParserName, sourceFiles, outputFormat);

            if (waitForInput) Console.ReadKey();

            return result;
        }
    }
}
