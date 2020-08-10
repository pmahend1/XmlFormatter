﻿using System;

namespace XmlFormatter
{
    public static class SymbolConstants
    {
        public const string StartTagStart = "<";

        public const string StartTagEnd = ">";

        public const string EndTagStart = "</";

        public const string EndTagEnd = ">";

        public static readonly char Space = ' ';

        public const string CommentTagStart = "<!--";

        public const string CommentTagEnd = "-->";

        public const string AssignmentStart = @"=""";
        
        public const string AssignmentEnd = @"""";

        public static readonly string Newline = Environment.NewLine;

    }
}
