using System;
using System.Collections.Generic;
using System.Globalization;
using Crispy.Ast;
using ExpressionType = System.Linq.Expressions.ExpressionType;

namespace Crispy.Parsing
{
    sealed class Parser
    {
        private readonly Tokenizer _tokenizer;
        private Token _lastConsumedToken = null!;
        private Token _nextToken = null!;

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
            _lastConsumedToken = ret;
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
            return ret.Type == tokenType
                ? ret
                : throw new ParserException(
                    string.Format(CultureInfo.InvariantCulture, "Expected {0} but found {1}", tokenType, ret));
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

            return [.. statements];
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
                case TokenType.KeywordForeach:
                    return ParseForeachStatement();
                case TokenType.KeywordTry:
                    return ParseTryStatement();
                case TokenType.KeywordIf:
                    return ParseIfStatement();
                case TokenType.KeywordFunction:
                    return ParseFunctionDefStatement();
                case TokenType.KeywordReturn:
                    return ParseReturnStatement();
                case TokenType.KeywordThrow:
                    return ParseThrowStatement();
                case TokenType.KeywordVar:
                    return ParseVarStatement();
                case TokenType.KeywordImport:
                    return ParseImportStatement();
                case TokenType.KeywordBreak:
                    return ParseBreakStatement();
                case TokenType.KeywordContinue:
                    return ParseContinueStatement();
                default:
                    return ParseExpressionStatement();
            }
        }

        private BlockStatement ParseBlockStatement()
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

        private LoopStatement ParseLoopStatement()
        {
            EatToken(TokenType.KeywordLoop);
            NodeExpression body = ParseStatement();
            return new LoopStatement(body);
        }

        private ForeachStatement ParseForeachStatement()
        {
            EatToken(TokenType.KeywordForeach);
            MaybeEatToken(TokenType.OpenParen);
            var itemName = ParseId().Value;
            EatToken(TokenType.KeywordIn);
            var sequence = ParseExpression();
            MaybeEatToken(TokenType.CloseParen);
            var body = ParseStatement();
            return new ForeachStatement(itemName, sequence, body);
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
        private IfStatement ParseIfStatement()
        {
            EatToken(TokenType.KeywordIf);

            var ifStatements = new List<IfStatementTest>();
            MaybeEatToken(TokenType.OpenParen);
            var firstTest = ParseExpression();
            MaybeEatToken(TokenType.CloseParen);

            var keywordStyle = IsKeywordIfBody();
            ifStatements.Add(new IfStatementTest(firstTest, ParseIfStatementBody(keywordStyle, true)));

            while (MaybeEatToken(TokenType.KeywordElseIf))
            {
                MaybeEatToken(TokenType.OpenParen);
                var test = ParseExpression();
                MaybeEatToken(TokenType.CloseParen);
                ifStatements.Add(new IfStatementTest(test, ParseIfStatementBody(keywordStyle, true)));
            }

            NodeExpression? elseStatement = null;

            if (MaybeEatToken(TokenType.KeywordElse))
            {
                elseStatement = ParseIfStatementBody(keywordStyle, false);
            }

            if (keywordStyle)
            {
                EatToken(TokenType.KeywordEnd);
            }

            return new IfStatement(ifStatements, elseStatement);
        }

        private bool IsKeywordIfBody()
        {
            return PeekToken().Type == TokenType.KeywordThen &&
                   PeekToken().Value == "then";
        }

        private NodeExpression ParseIfStatementBody(bool keywordStyle, bool expectThen)
        {
            if (!keywordStyle)
            {
                return ParseStatement();
            }

            if (expectThen)
            {
                EatToken(TokenType.KeywordThen);
            }

            var statements = new List<NodeExpression>();
            for (; ; )
            {
                var close = PeekToken();
                if (close.Type == TokenType.KeywordElseIf ||
                    close.Type == TokenType.KeywordElse ||
                    close.Type == TokenType.KeywordEnd)
                {
                    break;
                }
                statements.Add(ParseStatement());
            }
            return new BlockStatement(statements);
        }

        /// <summary>
        ///     Parses function definitions
        ///     function add(a, b)
        ///     return a + b
        ///     end
        /// </summary>
        /// <returns>The define function statement.</returns>
        private FunctionDefStatement ParseFunctionDefStatement()
        {
            EatToken(TokenType.KeywordFunction);
            var name = ParseId().Value;
            EatToken(TokenType.OpenParen);
            var parameters = ParseCallableParameters();
            var body = ParseStatement();

            return new FunctionDefStatement(name, parameters, body);
        }

        private ReturnStatement ParseReturnStatement()
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

        private ThrowStatement ParseThrowStatement()
        {
            EatToken(TokenType.KeywordThrow);
            if (PeekToken().Type == TokenType.SemiColon ||
                PeekToken().Type == TokenType.KeywordEnd ||
                PeekToken().Type == TokenType.KeywordCatch ||
                PeekToken().Type == TokenType.KeywordFinally)
            {
                MaybeEatToken(TokenType.SemiColon);
                return new ThrowStatement();
            }

            var expression = ParseExpression();
            MaybeEatToken(TokenType.SemiColon);
            return new ThrowStatement(expression);
        }

        private VarStatement ParseVarStatement()
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

        private ImportStatement ParseImportStatement()
        {
            EatToken(TokenType.KeywordImport);

            var names = new List<string> { ParseId().Value };

            while (MaybeEatToken(TokenType.Dot))
            {
                names.Add(ParseId().Value);
            }

            Token? nameAs = null;

            if (MaybeEatToken(TokenType.KeywordAs))
            {
                nameAs = ParseId();
            }

            MaybeEatToken(TokenType.SemiColon);
            return new ImportStatement(names, nameAs?.Value);
        }

        private BreakExpression ParseBreakStatement()
        {
            NodeExpression? expr = null;

            EatToken(TokenType.KeywordBreak);
            if (MaybeEatToken(TokenType.OpenParen))
            {
                expr = ParseExpression();
                EatToken(TokenType.CloseParen);
            }
            MaybeEatToken(TokenType.SemiColon);
            return new BreakExpression(expr);
        }

        private ContinueExpression ParseContinueStatement()
        {
            EatToken(TokenType.KeywordContinue);
            MaybeEatToken(TokenType.SemiColon);
            return new ContinueExpression();
        }

        private TryStatement ParseTryStatement(bool keywordConsumed = false)
        {
            if (!keywordConsumed)
            {
                EatToken(TokenType.KeywordTry);
            }
            var tryBody = ParseStatement();

            string? catchName = null;
            NodeExpression? catchBody = null;
            if (MaybeEatToken(TokenType.KeywordCatch))
            {
                if (MaybeEatToken(TokenType.OpenParen))
                {
                    catchName = ParseId().Value;
                    EatToken(TokenType.CloseParen);
                }

                catchBody = ParseStatement();
            }

            NodeExpression? finallyBody = null;
            if (MaybeEatToken(TokenType.KeywordFinally))
            {
                finallyBody = ParseStatement();
            }

            return catchBody == null && finallyBody == null
                ? throw new ParserException("try requires catch or finally")
                : new TryStatement(tryBody, catchName, catchBody, finallyBody);
        }

        private ExpressionStatement ParseExpressionStatement()
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
                token = PeekToken();
            }

            return token.Type == TokenType.Question
                ? throw new ParserException("Ternary operator is not supported.")
                : left;
        }

        // ||, or operator
        private NodeExpression ParseLogicalOrExpression()
        {
            var left = ParseLogicalAndExpression();
            while (PeekToken().Type == TokenType.DoubleBar)
            {
                NextToken();
                var right = ParseLogicalAndExpression();
                left = MakeLogicalOr(left, right);
            }
            return left;
        }

        // &&, and operator
        private NodeExpression ParseLogicalAndExpression()
        {
            var left = ParseComparativeExpression();
            while (PeekToken().Type == TokenType.DoubleAmphersand)
            {
                NextToken();
                var right = ParseComparativeExpression();
                left = MakeLogicalAnd(left, right);
            }
            return left;
        }

        private static IfStatement MakeLogicalAnd(NodeExpression left, NodeExpression right)
        {
            left = UnwrapParens(left);
            right = UnwrapParens(right);
            return new IfStatement(
                [new IfStatementTest(left, WrapExpressionAsBlock(right))],
                WrapExpressionAsBlock(new ConstantExpression(false)));
        }

        private static IfStatement MakeLogicalOr(NodeExpression left, NodeExpression right)
        {
            left = UnwrapParens(left);
            right = UnwrapParens(right);
            return new IfStatement(
                [new IfStatementTest(left, WrapExpressionAsBlock(new ConstantExpression(true)))],
                WrapExpressionAsBlock(right));
        }

        private static NodeExpression UnwrapParens(NodeExpression expression)
        {
            while (expression is ParentesizedExpression parentesizedExpression)
            {
                expression = parentesizedExpression.Expression;
            }

            return expression;
        }

        private static BlockStatement WrapExpressionAsBlock(NodeExpression expression)
        {
            return new BlockStatement([new ExpressionStatement(expression)]);
        }

        // ==, !=, <>, >, >=, <, <= operators
        private NodeExpression ParseComparativeExpression()
        {
            var left = ParseBitwiseOrExpression();
            while (
                PeekToken().Type == TokenType.DoubleEqual || // ==
                PeekToken().Type == TokenType.ExclamationEqual || // !=
                PeekToken().Type == TokenType.LessThanOrGreater || // <>
                PeekToken().Type == TokenType.GreaterThan || // >
                PeekToken().Type == TokenType.GreaterThanOrEqual || // >=
                PeekToken().Type == TokenType.LessThan || // <
                PeekToken().Type == TokenType.LessThanOrEqual // <=
            )
            {

                var t = PeekToken();
                NextToken();
                var right = ParseBitwiseOrExpression();

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

        // | operator
        private NodeExpression ParseBitwiseOrExpression()
        {
            var left = ParseBitwiseXorExpression();
            while (PeekToken().Type == TokenType.Bar)
            {
                NextToken();
                var right = ParseBitwiseXorExpression();
                left = new BinaryExpression(ExpressionType.Or, left, right);
            }

            return left;
        }

        // ^^ operator
        private NodeExpression ParseBitwiseXorExpression()
        {
            var left = ParseBitwiseAndExpression();
            while (PeekToken().Type == TokenType.DoubleCaret)
            {
                NextToken();
                var right = ParseBitwiseAndExpression();
                left = new BinaryExpression(ExpressionType.ExclusiveOr, left, right);
            }

            return left;
        }

        // & operator
        private NodeExpression ParseBitwiseAndExpression()
        {
            var left = ParseShiftExpression();
            while (PeekToken().Type == TokenType.Amphersand)
            {
                NextToken();
                var right = ParseShiftExpression();
                left = new BinaryExpression(ExpressionType.And, left, right);
            }

            return left;
        }

        // <<, >> operators
        private NodeExpression ParseShiftExpression()
        {
            var left = ParseAdditiveExpression();
            while (PeekToken().Type == TokenType.LeftShift || PeekToken().Type == TokenType.RightShift)
            {
                var t = PeekToken();
                NextToken();
                var right = ParseAdditiveExpression();
                switch (t.Type)
                {
                    case TokenType.LeftShift:
                        left = new BinaryExpression(ExpressionType.LeftShift, left, right);
                        break;
                    case TokenType.RightShift:
                        left = new BinaryExpression(ExpressionType.RightShift, left, right);
                        break;
                    default:
                        throw new ParserException(t.Value);
                }
            }

            return left;
        }

        // +, - operators
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
            return MaybeEatToken(TokenType.Minus)
                ? new UnaryExpression(ExpressionType.Negate, ParseUnaryExpression())
                : MaybeEatToken(TokenType.Tilde)
                    ? new UnaryExpression(ExpressionType.OnesComplement, ParseUnaryExpression())
                : MaybeEatToken(TokenType.Exclamation)
                    ? new UnaryExpression(ExpressionType.Not, ParseUnaryExpression())
                    : ParsePostfixExpression();
        }

        private AssignmentExpression FinishAssignment(NodeExpression left)
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
                    return new ConstantExpression(int.Parse(token.Value, CultureInfo.InvariantCulture));

                case TokenType.NumberFloat:
                    return new ConstantExpression(double.Parse(token.Value, CultureInfo.InvariantCulture));

                case TokenType.StringLiteral:
                    return new ConstantExpression(token.Value);

                case TokenType.KeywordTrue:
                    return new ConstantExpression(true);

                case TokenType.KeywordFalse:
                    return new ConstantExpression(false);

                case TokenType.KeywordNull:
                    return new ConstantExpression(null);

                case TokenType.OpenBracket:
                    return ParseListLiteral();

                case TokenType.KeywordDict:
                    return ParseDictionaryLiteral();

                case TokenType.KeywordTry:
                    return ParseTryStatement(true);

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

        private ListLiteralExpression ParseListLiteral()
        {
            var elements = new List<NodeExpression>();
            if (PeekToken().Type != TokenType.CloseBracket)
            {
                elements.Add(ParseExpression());
                while (MaybeEatToken(TokenType.Comma))
                {
                    if (PeekToken().Type == TokenType.CloseBracket)
                    {
                        break;
                    }

                    elements.Add(ParseExpression());
                }
            }

            EatToken(TokenType.CloseBracket);
            return new ListLiteralExpression([.. elements]);
        }

        private DictionaryLiteralExpression ParseDictionaryLiteral()
        {
            EatToken(TokenType.OpenBracket);

            var entries = new List<KeyValuePair<NodeExpression, NodeExpression>>();
            if (PeekToken().Type != TokenType.CloseBracket)
            {
                entries.Add(ParseDictionaryEntry());
                while (MaybeEatToken(TokenType.Comma))
                {
                    if (PeekToken().Type == TokenType.CloseBracket)
                    {
                        break;
                    }

                    entries.Add(ParseDictionaryEntry());
                }
            }

            EatToken(TokenType.CloseBracket);
            return new DictionaryLiteralExpression([.. entries]);
        }

        private KeyValuePair<NodeExpression, NodeExpression> ParseDictionaryEntry()
        {
            var key = ParseExpression();
            EatToken(TokenType.Colon);
            var value = ParseExpression();
            return new KeyValuePair<NodeExpression, NodeExpression>(key, value);
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

        private NewExpression ParseNew()
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

            return new NewExpression(constructor, [.. arguments]);
        }

        private LambdaExpression ParseLambda()
        {
            EatToken(TokenType.OpenParen);
            var parameters = ParseCallableParameters();
            var body = ParseStatement();

            return new LambdaExpression(parameters, body);
        }

        private CallableParameter[] ParseCallableParameters()
        {
            var parameters = new List<CallableParameter>();
            var sawDefaultValue = false;

            for (; ; )
            {
                var close = PeekToken();
                if (close.Type == TokenType.CloseParen)
                {
                    NextToken();
                    break;
                }

                // Comma is optional.
                MaybeEatToken(TokenType.Comma);
                if (PeekToken().Type == TokenType.CloseParen)
                {
                    NextToken();
                    break;
                }

                if (PeekToken().Type == TokenType.Dot)
                {
                    throw new ParserException("Variadic parameters are not supported.");
                }

                var parameterName = ParseId().Value;
                NodeExpression? defaultValue = null;
                if (MaybeEatToken(TokenType.Equal))
                {
                    defaultValue = ParseExpression();
                    sawDefaultValue = true;
                }
                else if (sawDefaultValue)
                {
                    throw new ParserException("Required parameters cannot follow optional parameters.");
                }

                parameters.Add(new CallableParameter(parameterName, defaultValue));
            }

            return [.. parameters];
        }

        private NodeExpression FinishExpressionTerminal(NodeExpression expr)
        {
            for (; ; )
            {
                Token tok = PeekToken();
                if (_lastConsumedToken.LineNumber > 0 &&
                    tok.LineNumber > _lastConsumedToken.LineNumber)
                {
                    return expr;
                }

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

        private MemberExpression ParseMember(NodeExpression expr, Boolean isNewExpression = false)
        {
            EatToken(TokenType.Dot);
            Token name = ParseId();
            return !isNewExpression && PeekToken().Type == TokenType.OpenParen
                ? new MemberExpression(expr, name.Value, MemberType.MethodCall)
                : new MemberExpression(expr, name.Value, MemberType.Member);
        }

        private FunctionCallExpression FinishCall(NodeExpression target)
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

            return new FunctionCallExpression(target, [.. args]);
        }

        private IndexExpression FinishIndex(NodeExpression target)
        {
            EatToken(TokenType.OpenBracket);
            NodeExpression index = ParseExpression();
            EatToken(TokenType.CloseBracket);
            return new IndexExpression(target, index);
        }

    }
}
