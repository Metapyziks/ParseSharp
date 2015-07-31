using System;
using System.Linq;

namespace ParseSharp
{
    public class ParseError : ParseLocatable, IEquatable<ParseError>
    {
        public static implicit operator ParseResult(ParseError error)
        {
            return new ParseResult(error, true);
        }

        internal readonly int Priority;
        internal readonly ParseContext Context;

        public readonly String Message;
        
        protected internal ParseError(ParseContext ctx, string message, int priority = 0)
        {
            Priority = priority;
            Context = ctx;

            Message = message;
        }

        protected override string GetInput()
        {
            return Context.Input;
        }

        protected override int GetOffset()
        {
            return Context.Offset;
        }

        public override string ToString()
        {
            return String.Format("{0} at line {1}, column {2}", Message, LineNumber, ColumnNumber);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ParseError);
        }

        public bool Equals(ParseError other)
        {
            return other != null
                && other.Context.Offset == Context.Offset
                && other.Context.Parser == Context.Parser;
        }

        public override int GetHashCode()
        {
            return Context.Offset.GetHashCode() ^ Context.Parser.GetHashCode();
        }
    }

    public class SymbolExpectedError : ParseError
    {
        private static String FormatOptionList(String[] options)
        {
            if (options.Length == 0) {
                throw new ArgumentException("Expected one or more option.", "options");
            }

            if (options.Length == 1) {
                return String.Format("Expected {0}", options[0]);
            }

            return String.Format("Expected {0} or {1}",
                String.Join(", ", options.Take(options.Length - 1).ToArray()),
                options.Last());
        }

        public readonly String[] Options;

        internal SymbolExpectedError(ParseContext ctx, String[] options, int priority)
            : base(ctx, FormatOptionList(options), priority)
        {
            Options = options;
        }
    }
}
