using System;
using System.Collections.Generic;
using ExpressionType = System.Linq.Expressions.ExpressionType;
using Crispy.Ast;

namespace Crispy.Parsing
{
    class Parser
    {
        private readonly Tokenizer _tokenizer;
        private Token _nextToken;

        public Parser(Tokenizer tokenizer)
        {
            _tokenizer = tokenizer;
            NextToken();
        }

        public Token PeekToken()
        {
            return _nextToken;
        }

        public Token NextToken()
        {
            var ret = _nextToken;
            _nextToken = _tokenizer.NextToken();
            return ret;
        }

        public bool MaybeEatToken(TokenType tokenType)
        {
            if (PeekToken().Type == tokenType)
            {
                NextToken();
                return true;
            }
            return false;
        }

        public Token EatToken(TokenType tokenType)
        {
            var ret = NextToken();
            if (ret.Type != tokenType)
            {
                throw new ParserException(string.Format("Expected {0} but found {1}", tokenType, ret));
            }
            return ret;
        }

        public Token ParseId()
        {
            return EatToken(TokenType.Identifier);
        }

        public NodeExpression[] ParseFile()
        {
            var statements = new List<NodeExpression>();

            do
            {
                statements.Add(ParseStatement());
            } while (!_tokenizer.EndOfFile);

            return statements.ToArray();
        }

        public NodeExpression Parse()
        {
            var statements = new List<NodeExpression>();
            do
            {
                statements.Add(ParseStatement());
            } while (!_tokenizer.EndOfFile);

            return new BlockStatement(statements);
        }

        private NodeExpression ParseStatement()
        {
            var token = PeekToken();

            switch (token.Type)
            {
                case TokenType.SemiColon:
                    return NullStatement.Instance;
                case TokenType.KeywordThen:
                    return ParseBlockStatement();
                case TokenType.KeywordLoop:
                    return ParseLoopStatement();
                case TokenType.KeywordIf:
                    return ParseIfStatement();
                case TokenType.KeywordFunction:
                    return ParseFunctionDefStatement();
                case TokenType.KeywordReturn:
                    return ParseReturnStatement();
                case TokenType.KeywordVar:
                    return ParseVarStatement();
                case TokenType.KeywordImport:
                    return ParseImportStatement();
                case TokenType.KeywordBreak:
                    return ParseBreakStatement();
                default:
                    return ParseExpressionStatement();
            }
        }

        private NodeExpression ParseBlockStatement()
        {
            EatToken(TokenType.KeywordThen);
            var statements = new List<NodeExpression>();
            for (; ; )
            {
                var close = PeekToken();
                if (close.Type == TokenType.KeywordEnd)
                {
                    NextToken();
                    break;
                }
                statements.Add(ParseStatement());
            }
            return new BlockStatement(statements);
        }

        private NodeExpression ParseLoopStatement()
        {
            EatToken(TokenType.KeywordLoop);
            NodeExpression body = ParseStatement();
            return new LoopStatement(body);
        }

        /// <summary>
        ///     if (a) then
        ///     # do something
        ///     elseif (b) then
        ///     # do something else
        ///     else
        ///     # or do this
        ///     end
        /// </summary>
        /// <returns></returns>
        private NodeExpression ParseIfStatement()
        {
            EatToken(TokenType.KeywordIf);

            var ifStatements = new List<IfStatementTest> { ParseIfStatementTest() };

            while (MaybeEatToken(TokenType.KeywordElseIf))
            {
                ifStatements.Add(ParseIfStatementTest());
            }

            NodeExpression elseStatement = null;

            if (MaybeEatToken(TokenType.KeywordElse))
            {
                elseStatement = ParseStatement();
            }

            return new IfStatement(ifStatements, elseStatement);
        }

        private IfStatementTest ParseIfStatementTest()
        {
            // (expressions)
            MaybeEatToken(TokenType.OpenParen);
            var test = ParseExpression();
            MaybeEatToken(TokenType.CloseParen);

            var body = ParseStatement();

            return new IfStatementTest(test, body);
        }

        /// <summary>
        ///     Parses function definitions
        ///     function add(a, b)
        ///     return a + b
        ///     end
        /// </summary>
        /// <returns>The define function statement.</returns>
        private NodeExpression ParseFunctionDefStatement()
        {
            EatToken(TokenType.KeywordFunction);
            var name = ParseId().Value;
            EatToken(TokenType.OpenParen);
            var parameters = new List<string>();
            for (; ; )
            {
                var close = PeekToken();
                if (close.Type == TokenType.CloseParen)
                {
                    NextToken();
                    break;
                }
                // Comma is optional
                MaybeEatToken(TokenType.Comma);

                string parameter = ParseId().Value;
                //TODO add default values
                parameters.Add(parameter);
            }

            var body = ParseStatement();

            return new FunctionDefStatement(name, parameters.ToArray(), body);
        }

        private NodeExpression ParseReturnStatement()
        {
            EatToken(TokenType.KeywordReturn);
            if (MaybeEatToken(TokenType.SemiColon))
            {
                return new ReturnStatement();
            }
            var expression = ParseExpression();
            MaybeEatToken(TokenType.SemiColon);
            return new ReturnStatement(expression);
        }

        private NodeExpression ParseVarStatement()
        {
            EatToken(TokenType.KeywordVar);
            var name = ParseId();

            if (MaybeEatToken(TokenType.Equal))
            {
                var value = ParseExpression();
                MaybeEatToken(TokenType.SemiColon);
                return new VarStatement(name.Value, value);
            }
            MaybeEatToken(TokenType.SemiColon);
            return new VarStatement(name.Value, null);
        }

        private NodeExpression ParseImportStatement()
        {
            EatToken(TokenType.KeywordImport);

            var names = new List<string>();
            names.Add(ParseId().Value);

            while (MaybeEatToken(TokenType.Dot))
            {
                names.Add(ParseId().Value);
            }

            Token nameAs = null;

            if (MaybeEatToken(TokenType.KeywordAs))
            {
                nameAs = ParseId();
            }

            MaybeEatToken(TokenType.SemiColon);
            return new ImportStatement(names, nameAs != null ? nameAs.Value : null);
        }

        private NodeExpression ParseBreakStatement()
        {
            NodeExpression expr = null;

            EatToken(TokenType.KeywordBreak);
            if (MaybeEatToken(TokenType.OpenParen))
            {
                expr = ParseExpression();
                EatToken(TokenType.CloseParen);
            }
            MaybeEatToken(TokenType.SemiColon);
            return new BreakExpression(expr);
        }

        private NodeExpression ParseExpressionStatement()
        {
            var expression = ParseExpression();
            MaybeEatToken(TokenType.SemiColon);
            return new ExpressionStatement(expression);
        }
 
        public NodeExpression ParseExpression()
        {
            var left = ParseLogicalOrExpression();
            var token = PeekToken();
            if (token.Type == TokenType.Equal)
            {
                left = FinishAssignment(left);
            }
            return left;
        }

        // ||, or operator
        private NodeExpression ParseLogicalOrExpression()
        {
            var left = ParseLogicalAndExpression();
            var token = PeekToken();
            if (token.Type == TokenType.DoubleBar)
            {
                NextToken();
                var right = ParseLogicalAndExpression();
                left = new BinaryExpression(ExpressionType.Or, left, right);
            }
            return left;
        }

        // &&, and operator
        private NodeExpression ParseLogicalAndExpression()
        {
            var left = ParseComparativeExpression();
            var token = PeekToken();
            if (token.Type == TokenType.DoubleAmphersand)
            {
                NextToken();
                var right = ParseComparativeExpression();
                left = new BinaryExpression(ExpressionType.And, left, right);
            }
            return left;
        }

        // ==, !=, <>, >, >=, <, <= operators
        private NodeExpression ParseComparativeExpression()
        {
            var left = ParseAdditiveExpression();
            while (
                PeekToken().Type == TokenType.DoubleEqual || // ==
                PeekToken().Type == TokenType.ExclamationEqual || // !=
                PeekToken().Type == TokenType.LessThanOrGreater || // <>
                PeekToken().Type == TokenType.GreaterThan || // >
                PeekToken().Type == TokenType.GreaterThanOrEqual || // >=
                PeekToken().Type == TokenType.LessThan || // <
                PeekToken().Type == TokenType.LessThanOrEqual // <=
            ) {

                var t = PeekToken();
                NextToken();
                var right = ParseAdditiveExpression();

                switch (t.Type)
                {
                    case TokenType.DoubleEqual:
                        left = new BinaryExpression(ExpressionType.Equal, left, right);
                        break;
                    case TokenType.ExclamationEqual:
                        left = new BinaryExpression(ExpressionType.NotEqual, left, right);
                        break;
                    case TokenType.LessThanOrGreater:
                        left = new BinaryExpression(ExpressionType.NotEqual, left, right);
                        break;
                    case TokenType.GreaterThan:
                        left = new BinaryExpression(ExpressionType.GreaterThan, left, right);
                        break;
                    case TokenType.GreaterThanOrEqual:
                        left = new BinaryExpression(ExpressionType.GreaterThanOrEqual, left, right);
                        break;
                    case TokenType.LessThan:
                        left = new BinaryExpression(ExpressionType.LessThan, left, right);
                        break;
                    case TokenType.LessThanOrEqual:
                        left = new BinaryExpression(ExpressionType.LessThanOrEqual, left, right);
                        break;
                    default:
                        throw new ParserException(t.Value);
                }
            }
            return left;
        }

        // +, -, & operators
        private NodeExpression ParseAdditiveExpression()
        {
            var left = ParseMultiplicativeExpression();
            while (PeekToken().Type == TokenType.Plus || PeekToken().Type == TokenType.Minus)
            {
                var t = PeekToken();
                NextToken();
                var right = ParseMultiplicativeExpression();
                switch (t.Type)
                {
                    case TokenType.Plus:
                        left = new BinaryExpression(ExpressionType.Add, left, right);
                        break;
                    case TokenType.Minus:
                        left = new BinaryExpression(ExpressionType.Subtract, left, right);
                        break;
                    default:
                        throw new ParserException(t.Value);
                }
            }
            return left;
        }

        // *, /, %, mod operators
        private NodeExpression ParseMultiplicativeExpression()
        {
            var left = ParseUnaryExpression();
            while (PeekToken().Type == TokenType.Asterisk || PeekToken().Type == TokenType.Slash || PeekToken().Type == TokenType.Percent || PeekToken().Type == TokenType.Caret)
            {
                Token t = PeekToken();
                NextToken();
                var right = ParseMultiplicativeExpression();

                switch (t.Type)
                {
                    case TokenType.Asterisk:
                        left = new BinaryExpression(ExpressionType.Multiply, left, right);
                        break;
                    case TokenType.Slash:
                        left = new BinaryExpression(ExpressionType.Divide, left, right);
                        break;
                    case TokenType.Percent:
                        left = new BinaryExpression(ExpressionType.Modulo, left, right);
                        break;
                    case TokenType.Caret:
                        left = new BinaryExpression(ExpressionType.Power, left, right);
                        break;
                    default:
                        throw new ParserException(t.Value);
                }
            }
            return left;
        }

        // -, !, not unary operators
        private NodeExpression ParseUnaryExpression()
        {
            return ParsePostfixExpression();
        }

        private NodeExpression FinishAssignment(NodeExpression left)
        {
            EatToken(TokenType.Equal);
            var right = ParseExpression();
            return new AssignmentExpression(left, right);
        }

        NodeExpression ParsePostfixExpression()
        {
            NodeExpression expr = ParsePrimaryExpression();
            return FinishExpressionTerminal(expr);
        }

        NodeExpression ParsePrimaryExpression()
        {
            var token = NextToken();

            switch (token.Type)
            {
                case TokenType.OpenParen:
                    var expr = ParseExpression();
                    EatToken(TokenType.CloseParen);
                    return new ParentesizedExpression(expr);

                case TokenType.NumberInteger:
                    return new ConstantExpression(Int32.Parse(token.Value));

                case TokenType.NumberFloat:
                    return new ConstantExpression(Double.Parse(token.Value));

                case TokenType.StringLiteral:
                    return new ConstantExpression(token.Value);

                case TokenType.Identifier:
                    return new NamedExpression(token.Value);

                case TokenType.KeywordNew:
                    return ParseNew();

                case TokenType.KeywordLambda:
                    return ParseLambda();

                default:
                    throw new ParserException(token.Value);
            }
        }

        NodeExpression ParseMemberExpression(Boolean isNewExpression = false)
        {
            NodeExpression expression = ParsePrimaryExpression();
            while (PeekToken().Type == TokenType.Dot)
            {
                expression = ParseMember(expression, isNewExpression);
            }
            return expression;
        }

        private NodeExpression ParseNew()
        {
            NodeExpression constructor = ParseMemberExpression(true);
            EatToken(TokenType.OpenParen);

            var arguments = new List<NodeExpression>();
            for (; ; )
            {
                Token end = PeekToken();
                if (end.Type == TokenType.CloseParen)
                {
                    NextToken();
                    break;
                }

                arguments.Add(ParseExpression());
            }

            return new NewExpression(constructor, arguments.ToArray());
        }

        private NodeExpression ParseLambda()
        {
            EatToken(TokenType.OpenParen);
            var parameters = new List<string>();
            for (; ; )
            {
                var close = PeekToken();
                if (close.Type == TokenType.CloseParen)
                {
                    NextToken();
                    break;
                }
                // Comma is optional
                MaybeEatToken(TokenType.Comma);

                string parameter = ParseId().Value;
                //TODO add default values
                parameters.Add(parameter);
            }

            var body = ParseStatement();

            return new LambdaExpression(parameters.ToArray(), body);
        }

        private NodeExpression FinishExpressionTerminal(NodeExpression expr)
        {
            for (; ; )
            {
                Token tok = PeekToken();
                switch (tok.Type)
                {
                    case TokenType.Dot:
                        expr = ParseMember(expr);
                        break;
                    case TokenType.OpenParen:
                        expr = FinishCall(expr);
                        break;
                    case TokenType.OpenBracket:
                        expr = FinishIndex(expr);
                        break;
                    default:
                        return expr;
                }
            }
        }

        private NodeExpression ParseMember(NodeExpression expr, Boolean isNewExpression = false)
        {
            EatToken(TokenType.Dot);
            Token name = ParseId();

            if (!isNewExpression && PeekToken().Type == TokenType.OpenParen)
            {
                return new MemberExpression(expr, name.Value, MemberType.MethodCall);
            }

            return new MemberExpression(expr, name.Value, MemberType.Member);
        }

        private NodeExpression FinishCall(NodeExpression target)
        {
            EatToken(TokenType.OpenParen);

            var args = new List<NodeExpression>();

            if (PeekToken().Type != TokenType.CloseParen)
            {
                args.Add(ParseExpression());
                while (MaybeEatToken(TokenType.Comma))
                {
                    args.Add(ParseExpression());
                }
            }

            EatToken(TokenType.CloseParen);

            return new FunctionCallExpression(target, args.ToArray());
        }

        private NodeExpression FinishIndex(NodeExpression target)
        {
            EatToken(TokenType.OpenBracket);
            NodeExpression index = ParseExpression();
            EatToken(TokenType.CloseBracket);
            return new IndexExpression(target, index);
        }

    }
}
