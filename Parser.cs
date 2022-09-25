using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Hosting;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace FoxLang
{
    public class Parser
    {
        private readonly Tokenizer tokenizer;
        private Token lookAhead;
        public Parser(string source)
        {
            tokenizer = new Tokenizer(source);            
        }

        public object[] Parse()
        {
            lookAhead = tokenizer.GetNextToken();
            return Program();
        }

        /**
         * Program ::= StatementList
         */
        private object[] Program()
        {
            return new object[]
            {
                "Program",
                StatementList(),
            };
        }
        /**
         * StatementList ::= (Statement)*
         */
        private List<object> StatementList(TokenType stopLookAhead = TokenType.EOF)
        {
            List<object> statementList = new List<object>();

            while (lookAhead != null && lookAhead.type != stopLookAhead)
            {
                statementList.Add(Statement());
            }

            return statementList;
        }

        /**
         * Statement ::= ExpressionStatement
         *              | LocalStatement
         *              | ReturnStatement
         *              | FunctionStatement
         */
        private object[] Statement()
        {
            switch (lookAhead.type)
            {
                case TokenType.LOCAL:
                    return VariableStatement(TokenType.LOCAL);
                case TokenType.PUBLIC:
                    return VariableStatement(TokenType.PUBLIC);
                case TokenType.RETURN:
                    return ReturnStatement();
                case TokenType.IF:
                    return IfStatement();
                case TokenType.FUNCTION:
                    return FunctionStatement();
                default:
                    return ExpressionStatement();
            }
        }

        /**
         * LocalStatement ::= ('LOCAL' | 'PUBLIC') identifier ('AS' Identifier | '=' Expression)?
         */
        private object[] VariableStatement(TokenType variableToken)
        {
            Eat(variableToken);
            var declarations = VariableDeclarationList();
            Eat(TokenType.SEMICOLON);

            return new object[]{
                "VariableStatement",
                variableToken.ToString().ToLower(),
                declarations,
            };
        }

        /**
         * VariableDeclarationList ::= VariableDeclaration (',' VariableDeclaration)*
         */
        private List<object> VariableDeclarationList()
        {
            List<object> declarations = new List<object>();

            do
            {
                declarations.Add(VariableDeclaration());
            } while (lookAhead.type == TokenType.COMMA && (Eat(TokenType.COMMA).type == TokenType.COMMA));

            return declarations;
        }

        /**
         * VariableDeclaration ::= Identifier ('AS' Identifier | '=' Expression)?
         *
         * List of valid examples:
         * 
         * 1. LOCAL NOMBRE AS STRING = "PEPE", EDAD AS NUMBER = 36, EMAIL AS STRING = "EMAIL"
         * 2. LOCAL NOMBRE = "PEPE", EDAD = 36, EMAIL = "EMAIL"
         * 3. LOCAL NOMBRE, EDAD, EMAIL
         * 
         * All examples above can be mixed between them and the grammar would be valid as well.
         */
        private object[] VariableDeclaration()
        {
            var name = Identifier();
            object[] type = null;
            object[] initializer = null;
            if (lookAhead.type == TokenType.AS)
            {
                Eat(TokenType.AS);
                type = Identifier();
            }
            if (lookAhead.type == TokenType.SIMPLE_ASSIGN)
            {
                Eat(TokenType.SIMPLE_ASSIGN);
                initializer = Expression();
            }
            return new object[]
            {
                "VariableDeclaration",
                name,
                type,
                initializer,
            };
        }

        /**
         * ReturnStatement ::= 'RETURN' (Expression)?
         */

        private object[] ReturnStatement()
        {
            Eat(TokenType.RETURN);
            var returnValue = (lookAhead.type != TokenType.SEMICOLON) ? Expression() : null;
            Eat(TokenType.SEMICOLON);
            return new object[]
            {
                "ReturnStatement",
                returnValue,
            };
        }

        /**
         * IfStatement ::= 'IF' Expression ('THEN')? StatementList 'ENDIF' | ('ELSE' StatementList)?
         */
        private object[] IfStatement()
        {
            Eat(TokenType.IF);
            if (lookAhead.type == TokenType.LPAREN)
            {
                Eat(TokenType.LPAREN);
            }
            var condition = Expression();
            if (lookAhead.type == TokenType.RPAREN)
            {
                Eat(TokenType.RPAREN);
            }

            if (lookAhead.type == TokenType.THEN)
            {
                Eat(TokenType.THEN);
            }
            Eat(TokenType.SEMICOLON);

            List<object> consequence = new List<object>();
            List<object> alternative = new List<object>();
            while (lookAhead.type != TokenType.ENDIF && lookAhead.type != TokenType.ELSE)
            {
                consequence.Add(Statement());
            }
            var thenBranch = new object[]
            {
                "BlockStatement",
                consequence,
            };

            if (lookAhead.type == TokenType.ELSE)
            {
                Eat(TokenType.ELSE);
                Eat(TokenType.SEMICOLON);
                while (lookAhead.type != TokenType.ENDIF)
                {
                    alternative.Add(Statement());
                }
            }
            var elseBranch = new object[]
            {
                "BlockStatement",
                alternative,
            };

            Eat(TokenType.ENDIF);
            if (lookAhead.type == TokenType.SEMICOLON)
            {
                Eat(TokenType.SEMICOLON);
            }

            return new object[]
            {
                "IfStatement",
                condition,
                thenBranch,
                elseBranch,
            };
        }

        /**
         * FunctionStatement ::= 'FUNCTION' Identifier ('(' ParameterList? ')')?
         * ParameterList := Identifier (',' Identifier)*
         */
        private object[] FunctionStatement()
        {
            Eat(TokenType.FUNCTION);
            var name = Identifier();
            List<object> parameters = new List<object>();
            List<object> stmtList = new List<object>();
            bool parametersParsed = false;
            if (lookAhead.type == TokenType.LPAREN)
            {
                Eat(TokenType.LPAREN);
                parameters = (lookAhead.type != TokenType.RPAREN) ? ParameterList() : null;
                parametersParsed = (parameters != null) ? true : false;
                Eat(TokenType.RPAREN);
            }
            Eat(TokenType.SEMICOLON);

            if (lookAhead.type == TokenType.LPARAMETERS)
            {
                if (parametersParsed) throw new SyntaxError("Function parameters must appear only once.");
                Eat(TokenType.LPARAMETERS);
                parameters = ParameterList();
                Eat(TokenType.SEMICOLON);
            }

            while (lookAhead.type != TokenType.ENDFUNC)
            {
                stmtList.Add(Statement());
            }

            Eat(TokenType.ENDFUNC);
            if (lookAhead.type == TokenType.SEMICOLON)
            {
                Eat(TokenType.SEMICOLON);
            }

            object[] body = new object[]
            {
                "BlockStatement",
                stmtList,
            };

            return new object[]
            {
                "FunctionStatement",
                name,
                parameters,
                body,
            };
        }

        /**
         * ParameterList := Identifier (',' Identifier)*
         */
        private List<object> ParameterList()
        {
            List<object> parameterList = new List<object>();
            do
            {
                parameterList.Add(Identifier());
            } while (lookAhead.type == TokenType.COMMA && Eat(TokenType.COMMA).type == TokenType.COMMA);

            return parameterList;
        }

        /**
         * ExpressionStatement ::= Expression
         */
        private object[] ExpressionStatement()
        {
            var expression = Expression();
            Eat(TokenType.SEMICOLON);

            return new object[] { 
                "ExpressionStatement",
                expression,
            };
        }

        /**
         * Expression ::= Assignment
         */
        private object[] Expression()
        {
            return Assignment();
        }

        /**
         * Assignment ::= LogicalOr ('='|'+='|'-='|'*='|'/=' Assignment)?
         */
        private object[] Assignment()
        {
            var left = LogicalOr();
            if (!IsAssignmentOperator(lookAhead.type))
            {
                return left;
            }
            return new object[]
            {
                "AssignmentExpression",
                AssignmentOperator().value,
                CheckValidAssignmentTarget(left),
                Assignment(),
            };
        }

        // IsAssignmentOperator: determina si el lookAhead es: '='|'+='|'-='|'*='|'/='
        private bool IsAssignmentOperator(TokenType t)
        {
            return t == TokenType.SIMPLE_ASSIGN || t == TokenType.COMPLEX_ASSIGN;
        }

        private Token AssignmentOperator()
        {
            if (lookAhead.type == TokenType.SIMPLE_ASSIGN)
            {
                return Eat(TokenType.SIMPLE_ASSIGN);
            }
            return Eat(TokenType.COMPLEX_ASSIGN);
        }

        private object[] CheckValidAssignmentTarget(object[] node)
        {
            if (node[0].ToString() == "Identifier" || node[0].ToString() == "MemberExpression")
            {
                return node;
            }
            throw new SyntaxError("Invalid left-hand side in assignment expression.");
        }

        /**
         * LogicalOr ::= LogicalAnd ('or' LogicalAnd)*
         */
        private object[] LogicalOr()
        {
            var left = LogicalAnd();
            while (lookAhead.type == TokenType.LOGICAL_OR)
            {
                var optor = Eat(TokenType.LOGICAL_OR).value;
                var right = LogicalAnd();
                left = new object[]
                {
                    "LogicalExpression",
                    optor,
                    left,
                    right,
                };
            }
            return left;
        }

        /**
         * LogicalAnd ::= Equality ('and' Equality)*
         */
        private object[] LogicalAnd()
        {
            var left = Equality();

            while (lookAhead.type == TokenType.LOGICAL_AND)
            {
                var optor = Eat(TokenType.LOGICAL_AND).value;
                var right = Equality();

                left = new object[]
                {
                    "LogicalExpression",
                    optor,
                    left,
                    right,
                };
            }

            return left;
        }

        /**
         * Equality ::= Comparison ('!='|'==' Comparison)*
         */
        private object[] Equality()
        {
            var left = Comparison();

            while (lookAhead.type == TokenType.EQUALITY_OPERATOR)
            {
                var optor = Eat(TokenType.EQUALITY_OPERATOR).value;
                var right = Comparison();

                left = new object[]
                {
                    "BinaryExpression",
                    optor,
                    left,
                    right,
                };
            }

            return left;
        }

        /**
         * Comparison ::= Term ('<'|'>'|'<='|'>=' Term)*
         */
        private object[] Comparison()
        {
            var left = Term();

            while (lookAhead.type == TokenType.RELATIONAL_OPERATOR)
            {
                var optor = Eat(TokenType.RELATIONAL_OPERATOR).value;
                var right = Term();
                left = new object[]
                {
                    "BinaryExpression",
                    optor,
                    left,
                    right,
                };
            }

            return left;
        }

        /**
         * Term ::= Factor ('+'|'-' Factor)*
         */
        private object[] Term()
        {
            var left = Factor();

            while (lookAhead.type == TokenType.TERM_OPERATOR)
            {
                var optor = Eat(TokenType.TERM_OPERATOR).value;
                var right = Factor();

                left = new object[]
                {
                    "BinaryExpression",
                    optor,
                    left,
                    right,
                };
            }

            return left;
        }

        /**
         * Factor ::= Unary ('*'|'/' Unary)*
         */
        private object[] Factor()
        {
            var left = Unary();

            while (lookAhead.type == TokenType.FACTOR_OPERATOR)
            {
                var optor = Eat(TokenType.FACTOR_OPERATOR).value;
                var right = Unary();

                left = new object[]
                {
                    "BinaryExpression",
                    optor,
                    left,
                    right,
                };
            }

            return left;
        }

        /**
         * Unary ::= ('+'|'-'|'!' Unary)* | LeftHandSideExp
         */
        private object[] Unary()
        {
            string optor = "";
            switch (lookAhead.type)
            {
                case TokenType.TERM_OPERATOR:
                    optor = Eat(TokenType.TERM_OPERATOR).value;
                    break;
                case TokenType.LOGICAL_NOT:
                    optor = Eat(TokenType.LOGICAL_NOT).value;
                    break;
            }
            if (optor.Length > 0)
            {
                return new object[]
                {
                    "UnaryExpression",
                    optor,
                    Unary(),
                };
            }
            return LeftHandSideExp();
        }

        /**
         * LeftHandSideExp ::= CallMemberExpression
         */
        private object[] LeftHandSideExp()
        {
            return CallMemberExpression();
        }

        /**
         * CallMemberExpression ::= 'super' CallExpression | MemberExpression | CallExpression
         */
        private object[] CallMemberExpression()
        {
            if (lookAhead.type == TokenType.DODEFAULT)
            {
                return CallExpression(DoDefault());
            }
            var member = MemberExpression();
            if (lookAhead.type == TokenType.LPAREN)
            {
                return CallExpression(member);
            }

            return member;
        }

        /**
         * MemberExpression ::= primary | ('.' identifier)* | ('[' expression ']')*
         */
        private object[] MemberExpression()
        {
            var parentObject = Primary();
            while (lookAhead.type == TokenType.DOT || lookAhead.type == TokenType.LBRACKET)
            {
                if (lookAhead.type == TokenType.DOT)
                {
                    Eat(TokenType.DOT);
                    var property = Identifier();
                    parentObject = new object[]
                    {
                        "MemberExpression",
                        false,
                        parentObject,
                        property,
                    };
                } else
                {
                    Eat(TokenType.LBRACKET);
                    var property = Expression();
                    parentObject = new object[]
                    {
                        "MemberExpression",
                        true,
                        parentObject,
                        property,
                    };
                }
            }
            return parentObject;
        }

        /**
         * CallExpression ::= callExpressionNode ('(' CallExpression ')')*
         */
        private object[] CallExpression(object[] callee)
        {
            var callExpression = new object[]
            {
                "CallExpression",
                callee,
                Arguments(),
            };

            if (lookAhead.type == TokenType.LPAREN)
            {
                callExpression = CallExpression(callExpression);
            }

            return callExpression;
        }

        /**
         * Arguments ::= ArgumentList
         */
        private List<object> Arguments()
        {
            Eat(TokenType.LPAREN);
            var argumentList = (lookAhead.type != TokenType.RPAREN) ? ArgumentList() : null;
            Eat(TokenType.RPAREN);

            return argumentList;
        }

        /**
         * ArgumentList ::= Expression (',' Expression)*
         */
        private List<object> ArgumentList()
        {
            List<object> argumentList = new List<object>();
            argumentList.Add(Expression());

            while (lookAhead.type == TokenType.COMMA)
            {
                Eat(TokenType.COMMA);
                argumentList.Add(Expression());
            }

            return argumentList;
        }


        /**
         * DoDeafult
         */
        private object[] DoDefault()
        {
            Eat(TokenType.DODEFAULT);
            return new object[]
            {
                "DoDefaultExpression",
            };
        }

        /**
         * Primary ::= Literal | ThisExp | CreateObject | Identifier | Grouped
         */
        private object[] Primary()
        {
            if (IsLiteral(lookAhead.type))
            {
                return Literal();
            }
            switch (lookAhead.type)
            {
                case TokenType.THIS:
                    return ThisExpression();
                case TokenType.CREATEOBJECT:
                    return CreateObjectExpression();
                case TokenType.IDENTIFIER:
                    return Identifier();
                case TokenType.LPAREN:
                    return GroupedExpression();
                default:
                    throw new SyntaxError("Unexpected primary expression: '" + lookAhead.type.ToString() + "'");
            }
        }

        private object[] ThisExpression()
        {
            Eat(TokenType.THIS);
            return new object[]
            {
                "ThisExpression",
            };
        }

        private object[] CreateObjectExpression()
        {
            Eat(TokenType.CREATEOBJECT);
            return new object[]
            {
                "CreateObjectExpression",
                MemberExpression(),
                Arguments(),
            };
        }

        private bool IsLiteral(TokenType t)
        {
            return t == TokenType.NUMBER || t == TokenType.STRING || t == TokenType.TRUE || t == TokenType.FALSE || t == TokenType.NULL;
        }

        private object[] Literal()
        {
            switch (lookAhead.type)
            {
                case TokenType.NUMBER:
                    return NumberLiteral();
                case TokenType.STRING:
                    return StringLiteral();
                case TokenType.TRUE:
                    return BooleanLiteral(true);
                case TokenType.FALSE:
                    return BooleanLiteral(false);
                case TokenType.NULL:
                    return NullLiteral();
            }
            return null;
        }

        private object[] NumberLiteral()
        {
            string literal = Eat(TokenType.NUMBER).value;
            return new object[]
            {
                "NumericLiteral",
                int.Parse(literal),
            };
        }

        private object[] StringLiteral()
        {
            string literal = Eat(TokenType.STRING).value;
            literal = literal.Substring(1, literal.Length - 2);
            return new object[]
            {
                "StringLiteral",
                literal,
            };
        }

        private object[] BooleanLiteral(bool value)
        {
            Eat(value ? TokenType.TRUE : TokenType.FALSE);
            return new object[]
            {
                "BooleanLiteral",
                value,
            };
        }

        private object[] NullLiteral()
        {
            Eat(TokenType.NULL);
            return new object[]
            {
                "NullLiteral",
                null,
            };
        }

        private object[] Identifier()
        {
            string name = Eat(TokenType.IDENTIFIER).value;
            return new object[]
            {
                "Identifier",
                name,
            };
        }

        private object[] GroupedExpression()
        {
            Eat(TokenType.LPAREN);
            var exp = Expression();
            Eat(TokenType.RPAREN);
            return exp;
        }

        private Token Eat(TokenType t)
        {
            var token = lookAhead;
            if (token.type == TokenType.EOF)
            {
                throw new SyntaxError(string.Format("Unexpected end of input, expected: {0}", t));
            }
            if (token.type != t)
            {
                throw new SyntaxError(string.Format("Unexpected token: \"{0}\", expected: \"{1}\"", token.type, t));
            }
            lookAhead = tokenizer.GetNextToken();

            return token;
        }
    }
}
