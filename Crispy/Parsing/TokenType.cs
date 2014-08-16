namespace Crispy.Parsing
{
    /// <summary>
    ///     TokenType
    ///     We tokenize pretty aggresively.  Not all tokens are
    ///     actually used in Crispy yet.
    /// </summary>
    public enum TokenType
    {
        // !
        Exclamation,

        // !=
        ExclamationEqual,

        // %
        Percent,

        // &
        Amphersand,

        // &&, AND
        DoubleAmphersand,

        // (
        OpenParen,

        // )
        CloseParen,

        // *
        Asterisk,

        // +
        Plus,

        // ,
        Comma,

        // -
        Minus,

        // .
        Dot,

        // /
        Slash,

        // :
        Colon,

        // <=
        LessThanOrEqual,

        // <>
        LessThanOrGreater,

        // <
        LessThan,

        // ==
        DoubleEqual,

        // =
        Equal,

        // >=
        GreaterThanOrEqual,

        // >
        GreaterThan,

        // <<
        LeftShift,

        // >>
        RightShift,

        // ?
        Question,

        // ;
        SemiColon,

        // ||, OR
        DoubleBar,

        // |
        Bar,

        // ~
        Tilde,

        // 'string' or "string"
        StringLiteral,

        // ident
        Identifier,

        // 1
        NumberInteger,

        // 1.0
        NumberFloat,

        // ^ - POW
        Caret,

        // ^^ - XOR
        DoubleCaret,

        // Nothing here.  Virtual last token.
        End,

        // IF
        KeywordIf,

        // THEN
        KeywordThen,

        // ELSE
        KeywordElse,

        // ELSEIF
        KeywordElseIf,

        // END, ENDIF
        KeywordEnd,

        // TRUE
        KeywordTrue,

        // FALSE
        KeywordFalse,

        // FUNCTION
        KeywordFunction,

        // LAMBDA
        KeywordLambda,

        // RETURN
        KeywordReturn,

        // BREAK
        KeywordBreak,

        // LOOP
        KeywordLoop,

        // VAR
        KeywordVar,

        // IMPORT
        KeywordImport,

        // AS - import System as s
        KeywordAs,

        // NEW
        KeywordNew,

        // [
        OpenBracket,

        // ]
        CloseBracket
    }
}
