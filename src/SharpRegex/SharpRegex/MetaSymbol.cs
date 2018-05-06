using System;

namespace SharpRegex
{
    /// <summary>
    /// 正则表达式特殊符号
    /// </summary>
    internal class MetaSymbol
    {
        public const char Concanate = '.';
        public const char Alternate = '|';
        public const char ZeroOrMore = '*';
        public const char OneOrMore = '+';
        public const char ZeroOrOne = '?';
        public const char OpenPren = '(';
        public const char ClosePren = ')';
        public const char Complement = '^';
        public const char AnyOneChar = '_';
        public const string AnyOneCharTrans = "AnyChar";
        public const char Escape = '\\';
        public const string Epsilon = "epsilon";
        public const char CharSetStart = '[';
        public const char CharSetEnd = ']';
        public const char Range = '-';
        public const string Dummy = "Dummy";
        public const char MatchStart = '^';
        public const char MatchEnd = '$';
        public const char NewLine = 'n';
        public const char Tab = 't';
    }
}
