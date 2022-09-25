using System;
using System.Text.RegularExpressions;

namespace FoxLang
{

    // SyntaxError
    public class SyntaxError : Exception
    {
        public SyntaxError(string message) : base(message){}
    }

    public class Tokenizer
    {
        private readonly string source;
        private int cursor;
        private int tokenCounter = 0;
        private TokenType lastToken;

        private readonly Tuple<string, TokenType>[] Spec = { 

            // -----------------------------------------------------------
            // Whitespace:
            new Tuple<string, TokenType>(@"^[ \t\r\f]+", TokenType.IGNORE),

            // -----------------------------------------------------------
            // NewLine:
            new Tuple<string, TokenType>(@"^\n+", TokenType.SEMICOLON),

            // -----------------------------------------------------------
            // Comments:

            // Skip single-line comments
            new Tuple<string, TokenType>(@"^\/\/.*", TokenType.IGNORE),

            // Skip multi-line comments
            new Tuple<string, TokenType>(@"^\/\*[\s\S]*?\*\/", TokenType.IGNORE),

            // -----------------------------------------------------------
            // Relational Operators:
            new Tuple<string, TokenType>(@"^[<>]=?", TokenType.RELATIONAL_OPERATOR),
            new Tuple<string, TokenType>(@"^[=!]=", TokenType.EQUALITY_OPERATOR),

            // -----------------------------------------------------------
            // Logical Operators:
            new Tuple<string, TokenType>(@"^\.and\.|^\band\b", TokenType.LOGICAL_AND),
            new Tuple<string, TokenType>(@"^\.or\.|^\bor\b", TokenType.LOGICAL_OR),
            new Tuple<string, TokenType>(@"^!", TokenType.LOGICAL_NOT),

            // -----------------------------------------------------------
            // Keywords:
            new Tuple<string, TokenType>(@"^\bas\b", TokenType.AS),
            new Tuple<string, TokenType>(@"^\blocal\b", TokenType.LOCAL),
            new Tuple<string, TokenType>(@"^\bpublic\b", TokenType.PUBLIC),
            new Tuple<string, TokenType>(@"^\bif\b", TokenType.IF),
            new Tuple<string, TokenType>(@"^\bthen\b", TokenType.THEN),
            new Tuple<string, TokenType>(@"^\belse\b", TokenType.ELSE),
            new Tuple<string, TokenType>(@"^\bendif\b", TokenType.ENDIF),
            new Tuple<string, TokenType>(@"^\.t\.|\btrue\b", TokenType.TRUE),
            new Tuple<string, TokenType>(@"^\.f\.|\bfalse\b", TokenType.FALSE),
            new Tuple<string, TokenType>(@"^\.null\.|\bnull\b", TokenType.NULL),
            new Tuple<string, TokenType>(@"^\breturn\b", TokenType.RETURN),
            new Tuple<string, TokenType>(@"^\bwhile\b", TokenType.WHILE),
            new Tuple<string, TokenType>(@"^\bendwhile\b", TokenType.ENDWHILE),
            new Tuple<string, TokenType>(@"^\brepeat\b", TokenType.REPEAT),
            new Tuple<string, TokenType>(@"^\buntil\b", TokenType.UNTIL),
            new Tuple<string, TokenType>(@"^\bclass\b", TokenType.CLASS),
            new Tuple<string, TokenType>(@"^\bendclass\b", TokenType.ENDCLASS),
            new Tuple<string, TokenType>(@"^\bthis\b", TokenType.THIS),
            new Tuple<string, TokenType>(@"^\bcreateobject\b", TokenType.CREATEOBJECT),
            new Tuple<string, TokenType>(@"^\bfor\b", TokenType.FOR),
            new Tuple<string, TokenType>(@"^\bto\b", TokenType.TO),
            new Tuple<string, TokenType>(@"^\bstep\b", TokenType.STEP),
            new Tuple<string, TokenType>(@"^\bendfor\b", TokenType.ENDFOR),
            new Tuple<string, TokenType>(@"^\bdodefault\b", TokenType.DODEFAULT),
            new Tuple<string, TokenType>(@"^\bfunction\b", TokenType.FUNCTION),
            new Tuple<string, TokenType>(@"^\blparameters\b", TokenType.LPARAMETERS),
            new Tuple<string, TokenType>(@"^\bendfunc\b", TokenType.ENDFUNC),



            // -----------------------------------------------------------
            // Assignment operators: =, +=, -=, *=, /=
            new Tuple<string, TokenType>(@"^=", TokenType.SIMPLE_ASSIGN),
            new Tuple<string, TokenType>(@"^[\+\-\*\/]=", TokenType.COMPLEX_ASSIGN),

            // -----------------------------------------------------------
            // Math operators: +, -, *, /
            new Tuple<string, TokenType>(@"^[\+\-]", TokenType.TERM_OPERATOR),
            new Tuple<string, TokenType>(@"^[\*\/]", TokenType.FACTOR_OPERATOR),

            // -----------------------------------------------------------
            // Numbers:
            new Tuple<string, TokenType>(@"^\d+", TokenType.NUMBER),

            // -----------------------------------------------------------
            // Double quoted string:
            new Tuple<string, TokenType>("^\"[^\"]*\"", TokenType.STRING),

            // -----------------------------------------------------------
            // Single quoted string:
            new Tuple<string, TokenType>("^'[^']*'", TokenType.STRING),

            // -----------------------------------------------------------
            // Identifier
            new Tuple<string, TokenType>(@"^\w+", TokenType.IDENTIFIER),

            // -----------------------------------------------------------
            // Symbols and delimiters:
            // new Tuple<string, TokenType>(@"^;", TokenType.SEMICOLON),
            new Tuple<string, TokenType>(@"^\(", TokenType.LPAREN),
            new Tuple<string, TokenType>(@"^\)", TokenType.RPAREN),
            new Tuple<string, TokenType>(@"^\[", TokenType.LBRACKET),
            new Tuple<string, TokenType>(@"^\]", TokenType.RBRACKET),
            new Tuple<string, TokenType>(@"^\.", TokenType.DOT),
            new Tuple<string, TokenType>(@"^,", TokenType.COMMA),

    };

        public Tokenizer(string source)
        {
            if (!source.EndsWith("\n"))
            {
                source += "\n";
            }
            this.source = source;
            cursor = 0;
        }
        
        private bool HasMoreTokens()
        {
            return cursor < source.Length;
        }

        public Token GetNextToken()
        {
            if (!HasMoreTokens())
            {
                return new Token(TokenType.EOF, "");
            }

            string input = source.Substring(cursor);

            foreach (var tuple in Spec)
            {
                string tokenValue = this.match(tuple.Item1, input);
                if (tokenValue == null)
                {
                    continue;
                }
                // check for the IGNORE token type
                if (tuple.Item2 == TokenType.IGNORE)
                {
                    return GetNextToken();
                }
                if (tuple.Item2 == TokenType.SEMICOLON) // new line
                {
                    if (lastToken == TokenType.SEMICOLON || tokenCounter == 0)
                    {
                        return GetNextToken();
                    }
                    lastToken = TokenType.SEMICOLON;
                    tokenValue = "";
                } else
                {
                    lastToken = tuple.Item2;
                }
                tokenCounter++;
                // return the token and value
                return new Token(tuple.Item2, tokenValue);
            }

            throw new SyntaxError(string.Format("Unexpected token: {0}", input));
        }

        private string match(string pattern, string input)
        {
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection matches = regex.Matches(input);
            if (matches.Count > 0)
            {
                cursor += matches[0].Length;
                return matches[0].Value;
            }
            return null;
        }
    }

    public enum TokenType
    {
        LPAREN,
        RPAREN,
        LBRACKET,
        RBRACKET,
        COMMA,
        SEMICOLON,
        DOT,

        // keywords
        AS,
        LOCAL,
        PUBLIC,
        IF,
        THEN,
        ELSE,
        ENDIF,
        ELSEIF,
        TRUE,
        FALSE,
        NULL,
        RETURN,
        DODEFAULT,
        THIS,
        CREATEOBJECT,
        FUNCTION,
        LPARAMETERS,
        ENDFUNC,

        // Iterators keywords
        WHILE,
        ENDWHILE,
        REPEAT,
        UNTIL,
        CLASS,
        ENDCLASS,
        FOR,
        TO,
        STEP,
        ENDFOR,        

        // Literals
        NUMBER,
        STRING,
        IDENTIFIER,

        // Operators
        SIMPLE_ASSIGN,
        COMPLEX_ASSIGN,
        RELATIONAL_OPERATOR,
        EQUALITY_OPERATOR,
        TERM_OPERATOR,
        FACTOR_OPERATOR,
        LOGICAL_OR,
        LOGICAL_AND,
        LOGICAL_NOT,
        IGNORE,
        EOF,
        ERROR,        
    }

    // Token class
    public class Token
    {
        public TokenType type;
        public string value;

        public Token(TokenType type, string value)
        {
            this.type = type;
            this.value = value;
        }

        public override string ToString()
        {
            return string.Format("Token({0}, '{1}')", type, value);
        }
    }
}
