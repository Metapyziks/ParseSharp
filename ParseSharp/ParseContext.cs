﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace ParseSharp
{
    public sealed class ParseContext
    {
        private readonly HashSet<Parser> _whitespaceParsers;

        public readonly ParseContext Parent;
        public readonly Parser Parser;

        public readonly WhitespacePolicy WhitespacePolicy;

        public readonly String Input;
        public readonly int InitialOffset;

        public int Offset { get; private set; }
        public int Remaining { get { return Input.Length - Offset; } }

        public ParseContext(String input, int offset)
        {
            Input = input;
            InitialOffset = Offset = offset;
            WhitespacePolicy = WhitespacePolicy.Match;
            _whitespaceParsers = null;
        }

        public ParseContext(ParseContext parent, Parser parser)
            : this(parent.Input, -1)
        {
            Parent = parent;
            Parser = parser;

            WhitespacePolicy = parent.WhitespacePolicy;

            var wsRule = parser as WhitespaceRuleParser;
            if (wsRule != null)
            {
                _whitespaceParsers = new HashSet<Parser>(parent._whitespaceParsers ?? Enumerable.Empty<Parser>())
                {
                    wsRule.WhitespaceParser
                };
            }
            else
            {
                _whitespaceParsers = parent._whitespaceParsers;
            }

            var policy = parser as WhitespacePolicyParser;
            if (policy != null) WhitespacePolicy = policy.Policy;

            InitialOffset = Offset = parent.WhitespacePolicy == WhitespacePolicy.Ignore
                ? SkipWhitespace(parent.Input, parent.Offset) : parent.Offset;
        }

        private int SkipWhitespace(String input, int offset)
        {
            while (offset < input.Length)
            {
                while (offset < input.Length && char.IsWhiteSpace(input[offset]))
                {
                    ++offset;
                }

                if (_whitespaceParsers == null) break;

                var matched = false;
                foreach (var whitespaceParser in _whitespaceParsers)
                {
                    var result = whitespaceParser.Parse(input, offset);
                    if (result.Success && result.End > offset)
                    {
                        matched = true;
                        offset = result.End;
                        break;
                    }
                }

                if (!matched) break;
            }

            return offset;
        }

        public bool IsUnique(ParseContext ctx)
        {
            if (Parser == ctx.Parser && InitialOffset == ctx.InitialOffset) return false;
            return Parent == null || Parent.IsUnique(ctx);
        }

        public ParseResult Peek(Parser parser)
        {
            return parser.Parse(this);
        }

        public ParseResult Parse(Parser parser)
        {
            var result = parser.Parse(this);
            Apply(result);
            return result;
        }

        public ParseResult Apply(ParseResult result)
        {
            if (result == null || !result.Success) return result;

            var dest = result.Index + result.Length;
            if (dest < Offset) {
                throw new ArgumentException("Cannot apply a match that occured earlier in the input.", "result");
            }

            Offset = result.Index + result.Length;

            if (WhitespacePolicy == WhitespacePolicy.Ignore) {
                Offset = SkipWhitespace(Input, Offset);
            }

            return result;
        }

        public void Advance(int amount)
        {
            if (amount < 0) {
                throw new ArgumentOutOfRangeException("amount");
            }

            Offset += amount;
        }

        public ParseError Error(String message, int priority = 0)
        {
            return new ParseError(this, message, priority);
        }

        public SymbolExpectedError Expected(Regex expected)
        {
            return new SymbolExpectedError(this, new []{ expected.ToString() }, -1);
        }

        public SymbolExpectedError Expected(params Parser[] expected)
        {
            return new SymbolExpectedError(this, expected.Select(x => x.ExpectingDescription).ToArray(), 0);
        }

        public SymbolExpectedError Expected(params String[] expected)
        {
            return new SymbolExpectedError(this, expected, 0);
        }

        public SymbolExpectedError Expected(params SymbolExpectedError[] expected)
        {
            var max = expected.Max(x => x != null ? x.Context.Offset : -1);
            return new SymbolExpectedError(
                expected
                    .FirstOrDefault(x => x != null && x.Context.Offset == max)
                    .Context,
                expected
                    .Where(x => x != null && x.Context.Offset == max)
                    .SelectMany(x => x.Options)
                    .Distinct()
                    .ToArray(), 0);
        }

        public Match Match(Regex regex)
        {
            var result = regex.Match(Input, Offset);

            if (result.Success) {
                Offset += result.Length;
            }

            return result;
        }

        public bool Match(String literal)
        {
            if (Remaining < literal.Length) return false;

// ReSharper disable once LoopCanBeConvertedToQuery
            for (var i = 0; i < literal.Length; ++i) {
                if (Input[Offset + i] != literal[i]) return false;
            }

            Offset += literal.Length;
            return true;
        }

        public override string ToString()
        {
            return string.Format("Offset: {0}, Next: \"{1}\"", Offset, Input.Substring(Offset, Math.Min(Remaining, 4)));
        }
    }
}
