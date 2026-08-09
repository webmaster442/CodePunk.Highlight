using CodePunk.Highlight.Core.SyntaxHighlighting.Abstractions;
using CodePunk.Highlight.Core.SyntaxHighlighting.Tokenization;

namespace CodePunk.Highlight.Core.SyntaxHighlighting.Languages;

/// <summary>
/// C++ language definition for syntax highlighting.
/// Provides a tokenizer for C++23 with support for keywords, types, preprocessor
/// directives, raw string literals, and more.
/// </summary>
public class CPPLanguageDefinition : ILanguageDefinition
{
    public string Name => "cpp";
    public string[] Aliases => new[] { "c++", "cc", "cxx", "hpp", "hh", "hxx", "ipp" };

    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        // Control flow
        "if", "else", "for", "while", "do", "switch", "case", "default", "break",
        "continue", "return", "goto",
        // Declarations / storage
        "typedef", "struct", "union", "enum", "class", "namespace", "template",
        "typename", "using", "static", "extern", "auto", "register", "const",
        "volatile", "mutable", "inline", "explicit", "friend", "virtual",
        "override", "final", "public", "private", "protected", "constexpr",
        "consteval", "constinit", "thread_local", "concept", "requires", "export",
        // Operators / expressions
        "new", "delete", "sizeof", "alignof", "alignas", "typeid", "decltype",
        "noexcept", "static_assert", "static_cast", "dynamic_cast",
        "reinterpret_cast", "const_cast", "operator", "this",
        // Exceptions / coroutines
        "try", "catch", "throw", "co_await", "co_return", "co_yield",
        // Alternative tokens
        "and", "and_eq", "bitand", "bitor", "compl", "not", "not_eq", "or",
        "or_eq", "xor", "xor_eq",
        // Misc
        "asm"
    };

    private static readonly HashSet<string> Types = new(StringComparer.Ordinal)
    {
        // Fundamental types
        "void", "bool", "char", "char8_t", "char16_t", "char32_t", "wchar_t",
        "short", "int", "long", "float", "double", "signed", "unsigned",
        // Fixed-width / library integer types
        "size_t", "ssize_t", "ptrdiff_t", "intptr_t", "uintptr_t",
        "int8_t", "int16_t", "int32_t", "int64_t", "uint8_t", "uint16_t",
        "uint32_t", "uint64_t", "int_least8_t", "int_least16_t", "int_least32_t",
        "int_least64_t", "uint_least8_t", "uint_least16_t", "uint_least32_t",
        "uint_least64_t", "int_fast8_t", "int_fast16_t", "int_fast32_t",
        "int_fast64_t", "uint_fast8_t", "uint_fast16_t", "uint_fast32_t",
        "uint_fast64_t", "intmax_t", "uintmax_t",
        // Common standard library types
        "string", "string_view", "wstring", "u8string", "u16string", "u32string",
        "vector", "array", "map", "unordered_map", "set", "unordered_set",
        "multimap", "multiset", "pair", "tuple", "optional", "variant", "any",
        "span", "list", "forward_list", "deque", "stack", "queue",
        "priority_queue", "shared_ptr", "unique_ptr", "weak_ptr", "function",
        "initializer_list", "byte", "nullptr_t",
        "FILE"
    };

    private static readonly HashSet<string> Literals = new(StringComparer.Ordinal)
    {
        "true", "false", "nullptr", "NULL"
    };

    public bool Matches(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId)) return false;
        var normalized = languageId.ToLowerInvariant();
        return normalized == Name || Aliases.Contains(normalized);
    }

    public IEnumerable<Token> Tokenize(ReadOnlySpan<char> source)
    {
        var tokens = new List<Token>();
        var pos = 0;

        while (pos < source.Length)
        {
            var ch = source[pos];

            // Whitespace
            if (char.IsWhiteSpace(ch))
            {
                var start = pos;
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Text, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Preprocessor directives
            if (ch == '#')
            {
                var start = pos;
                pos++;
                // Skip whitespace after #
                while (pos < source.Length && char.IsWhiteSpace(source[pos]))
                    pos++;
                // Read directive name
                while (pos < source.Length && (char.IsLetterOrDigit(source[pos]) || source[pos] == '_'))
                    pos++;
                // Read rest of line (including line continuations with \)
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length && source[pos + 1] == '\n')
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == '\n')
                        break;
                    pos++;
                }
                tokens.Add(new Token(TokenType.Preprocessor, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Single-line comments
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '/')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length && source[pos] != '\n')
                    pos++;
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Multi-line comments
            if (ch == '/' && pos + 1 < source.Length && source[pos + 1] == '*')
            {
                var start = pos;
                pos += 2;
                while (pos < source.Length - 1)
                {
                    if (source[pos] == '*' && source[pos + 1] == '/')
                    {
                        pos += 2;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.Comment, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Raw string literals: R"delim( ... )delim" (optionally prefixed with L, u8, u, U)
            if (TryReadRawStringPrefix(source, pos, out var rawStart))
            {
                var start = pos;
                pos = ReadRawString(source, rawStart);
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // String literals (with optional L, u8, u, U prefix)
            if (ch == '"' || TryReadStringPrefix(source, pos, '"', out _))
            {
                var start = pos;
                // Skip encoding prefix
                while (pos < source.Length && source[pos] != '"')
                    pos++;
                pos++; // opening quote
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == '"')
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Character literals (with optional L, u8, u, U prefix)
            if (ch == '\'' || TryReadStringPrefix(source, pos, '\'', out _))
            {
                var start = pos;
                while (pos < source.Length && source[pos] != '\'')
                    pos++;
                pos++; // opening quote
                while (pos < source.Length)
                {
                    if (source[pos] == '\\' && pos + 1 < source.Length)
                    {
                        pos += 2;
                        continue;
                    }
                    if (source[pos] == '\'')
                    {
                        pos++;
                        break;
                    }
                    pos++;
                }
                tokens.Add(new Token(TokenType.String, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Numbers
            if (char.IsDigit(ch))
            {
                var start = pos;
                var isHex = false;
                var isOctal = false;
                var isBinary = false;

                // Check for hex (0x), octal (0), or binary (0b)
                if (ch == '0' && pos + 1 < source.Length)
                {
                    if (source[pos + 1] == 'x' || source[pos + 1] == 'X')
                    {
                        isHex = true;
                        pos += 2;
                    }
                    else if (source[pos + 1] == 'b' || source[pos + 1] == 'B')
                    {
                        isBinary = true;
                        pos += 2;
                    }
                    else if (char.IsDigit(source[pos + 1]))
                    {
                        isOctal = true;
                        pos++;
                    }
                }

                while (pos < source.Length)
                {
                    var current = source[pos];

                    if (char.IsDigit(current))
                    {
                        pos++;
                        continue;
                    }

                    // Digit separators (C++14+)
                    if (current == '\'' && pos + 1 < source.Length &&
                        (char.IsLetterOrDigit(source[pos + 1]) || source[pos + 1] == '\''))
                    {
                        pos++;
                        continue;
                    }

                    if (isHex && IsHexDigit(current))
                    {
                        pos++;
                        continue;
                    }

                    if (!isHex && !isBinary && !isOctal && current == '.')
                    {
                        pos++;
                        continue;
                    }

                    if (!isHex && !isBinary && !isOctal && (current == 'e' || current == 'E'))
                    {
                        pos++;
                        if (pos < source.Length && (source[pos] == '+' || source[pos] == '-'))
                            pos++;
                        continue;
                    }

                    // Suffixes: L, LL, U, UL, ULL, F, etc.
                    if (current == 'L' || current == 'U' || current == 'F' || current == 'l' || current == 'u' || current == 'f')
                    {
                        pos++;
                        // Handle LL, ULL, etc.
                        if (pos < source.Length && (source[pos] == 'L' || source[pos] == 'l' || source[pos] == 'U' || source[pos] == 'u'))
                            pos++;
                        break;
                    }

                    break;
                }

                tokens.Add(new Token(TokenType.Number, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Identifiers and keywords
            if (IsIdentifierStart(ch))
            {
                var start = pos;
                pos++;
                while (pos < source.Length && IsIdentifierPart(source[pos]))
                    pos++;

                var text = source.Slice(start, pos - start).ToString();

                TokenType type = TokenType.Identifier;
                if (Keywords.Contains(text))
                    type = TokenType.Keyword;
                else if (Types.Contains(text))
                    type = TokenType.Type;
                else if (Literals.Contains(text))
                    type = TokenType.Keyword;

                tokens.Add(new Token(type, text));
                continue;
            }

            // Operators
            if (IsOperatorStart(ch))
            {
                var start = pos;
                pos++;
                // Multi-character operators
                while (pos < source.Length && IsOperatorPart(source[pos]))
                    pos++;
                tokens.Add(new Token(TokenType.Operator, source.Slice(start, pos - start).ToString()));
                continue;
            }

            // Punctuation
            if (IsPunctuation(ch))
            {
                tokens.Add(new Token(TokenType.Punctuation, ch.ToString()));
                pos++;
                continue;
            }

            // Everything else
            tokens.Add(new Token(TokenType.Text, ch.ToString()));
            pos++;
        }

        return tokens;
    }

    private static bool TryReadStringPrefix(ReadOnlySpan<char> source, int pos, char quote, out int quotePos)
    {
        // Matches encoding prefixes L, u, U, u8 immediately followed by the quote.
        quotePos = pos;
        var i = pos;
        if (i < source.Length && (source[i] == 'L' || source[i] == 'u' || source[i] == 'U'))
        {
            if (source[i] == 'u' && i + 1 < source.Length && source[i + 1] == '8')
                i += 2;
            else
                i++;

            if (i < source.Length && source[i] == quote)
            {
                quotePos = i;
                return true;
            }
        }
        return false;
    }

    private static bool TryReadRawStringPrefix(ReadOnlySpan<char> source, int pos, out int rStart)
    {
        // Matches R"..., LR"..., u8R"..., uR"..., UR"...
        rStart = pos;
        var i = pos;
        if (i < source.Length && (source[i] == 'L' || source[i] == 'u' || source[i] == 'U'))
        {
            if (source[i] == 'u' && i + 1 < source.Length && source[i + 1] == '8')
                i += 2;
            else
                i++;
        }

        if (i + 1 < source.Length && source[i] == 'R' && source[i + 1] == '"')
        {
            rStart = i;
            return true;
        }
        return false;
    }

    private static int ReadRawString(ReadOnlySpan<char> source, int rPos)
    {
        // rPos points at 'R'. Format: R"delimiter( ... )delimiter"
        var pos = rPos + 2; // skip R and opening "
        var delimStart = pos;
        while (pos < source.Length && source[pos] != '(' && source[pos] != '"')
            pos++;

        var delimiter = source.Slice(delimStart, pos - delimStart).ToString();
        var terminator = ")" + delimiter + "\"";

        if (pos < source.Length && source[pos] == '(')
            pos++; // skip opening (

        while (pos < source.Length)
        {
            if (source[pos] == ')' &&
                pos + terminator.Length <= source.Length &&
                source.Slice(pos, terminator.Length).SequenceEqual(terminator.AsSpan()))
            {
                pos += terminator.Length;
                break;
            }
            pos++;
        }

        return pos;
    }

    private static bool IsIdentifierStart(char ch) =>
        char.IsLetter(ch) || ch == '_';

    private static bool IsIdentifierPart(char ch) =>
        char.IsLetterOrDigit(ch) || ch == '_';

    private static bool IsOperatorStart(char ch) =>
        ch == '+' || ch == '-' || ch == '*' || ch == '/' || ch == '%' ||
        ch == '=' || ch == '!' || ch == '<' || ch == '>' || ch == '&' ||
        ch == '|' || ch == '^' || ch == '~' || ch == '?' || ch == ':' ||
        ch == '.';

    private static bool IsOperatorPart(char ch) =>
        ch == '+' || ch == '-' || ch == '=' || ch == '&' || ch == '|' ||
        ch == '<' || ch == '>' || ch == '?' || ch == '!' || ch == '*' ||
        ch == '/' || ch == '%' || ch == '.' || ch == ':';

    private static bool IsPunctuation(char ch) =>
        ch == '{' || ch == '}' || ch == '(' || ch == ')' || ch == '[' || ch == ']' ||
        ch == ';' || ch == ',' || ch == ':';

    private static bool IsHexDigit(char ch) =>
        char.IsDigit(ch) || (ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F');
}
