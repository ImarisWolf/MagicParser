using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicParser.CodeParsing
{
    public abstract class Token
    {
        public string identifier { get; protected set; }
        public int priority { get; protected set; }

        public static List<Token> list = new List<Token>();
    }

    public abstract class StringValueToken : Token
    {
        public string value { get; protected set; }
    }

    public abstract class NumberValueToken : Token
    {
        public float value { get; protected set; }
    }

    public abstract class BoolValueToken : Token
    {
        public bool value { get; protected set; }
    }

    #region priority 0
    public class Terminal : Token
    {
        public char value;
        public Terminal(char value) { this.value = value; identifier = null; priority = 0; }
    }

    public class BracketOpenToken : Token
    {
        public BracketOpenToken() { this.identifier = "("; priority = 0; }
    }

    public class BracketCloseToken : Token
    {
        public BracketCloseToken() { this.identifier = ")"; priority = 0; }
    }

    public class WhiteSpaceToken : StringValueToken
    {
        public WhiteSpaceToken(string value) { this.value = value; priority = 0; }
    }
    #endregion

    #region priority 1
    public class QuoteToken : Token
    {
        public QuoteToken() { identifier = "\""; priority = 1; }
    }

    public class AttributeToken : StringValueToken
    {
        public AttributeToken(string value) { this.value = value; identifier = null; priority = 1; }
    }

    public class IfToken : StringValueToken
    {
        public IfToken() { identifier = "if"; priority = 1; }
    }

    public class StringToken : StringValueToken
    {
        public StringToken (string value) { this.value = value; identifier = null; priority = 1; }
    }

    public class NumberToken : NumberValueToken
    {
        public NumberToken(float value) { this.value = value; priority = 1; }
    }

    public class BoolToken : BoolValueToken
    {
        public BoolToken(bool value) { this.value = value; priority = 1; }
    }
    #endregion

    #region priority 2
    public class MultiplyToken : NumberValueToken
    {
        public MultiplyToken () { identifier = "*"; priority = 2; }
    }

    public class DivisionToken : NumberValueToken
    {
        public DivisionToken() { identifier = "/"; priority = 2; }
    }

    #endregion

    #region priority 3
    public class AdditionToken : NumberValueToken
    {
        public AdditionToken() { identifier = "+"; priority = 3; }
    }

    public class SubtractionToken : NumberValueToken
    {
        public SubtractionToken() { identifier = "-"; priority = 3; }
    }
    #endregion

    #region priority 4
    public class GreaterToken : NumberValueToken
    {
        public GreaterToken() { identifier = ">"; priority = 4; }
    }

    public class GreaterOrEqualToken : NumberValueToken
    {
        public GreaterOrEqualToken() { identifier = ">="; priority = 4; }
    }

    public class LessToken : NumberValueToken
    {
        public LessToken() { identifier = "<"; priority = 4; }
    }
    #endregion

    #region priority 5
    public class EqualToken : BoolValueToken
    {
        public EqualToken() { identifier = "="; priority = 5; }
    }

    public class NotEqualToken : BoolValueToken
    {
        public NotEqualToken() { identifier = "!="; priority = 5; }
    }
    #endregion

    #region priority 6-8
    public class ContainsToken : BoolValueToken
    {
        public ContainsToken() { identifier = "contains"; priority = 6; }
    }

    public class OrToken : BoolValueToken
    {
        public OrToken() { identifier = "|"; priority = 7; }
    }

    public class AndToken : BoolValueToken
    {
        public AndToken() { identifier = "&"; priority = 8; }
    }
    #endregion


    #region priority 9
    public class DatabasesToken : Token
    {
        public DatabasesToken() { identifier = "dbs"; priority = 9; }
    }

    public class ParseCommentsToken : Token
    {
        public ParseCommentsToken() { identifier = "parseComments"; priority = 9; }
    }

    public class FilterToken : Token
    {
        public FilterToken() { identifier = "filter"; priority = 9; }
    }

    public class GroupingToken : Token
    {
        public GroupingToken() { identifier = "group"; priority = 9; }
    }

    public class SortingToken : Token
    {
        public SortingToken() { identifier = "sort"; priority = 9; }
    }

    public class FormattingToken : Token
    {
        public FormattingToken() { identifier = "format"; priority = 9; }
    }

    public class EndLineToken : Token
    {
        public EndLineToken() { identifier = ";"; priority = 9; }
    }

    public class EnumerationToken : Token
    {
        public EnumerationToken() { identifier = ","; priority = 9; }
    }
    #endregion

    #region priority 10
    public class DeclarationDBOpenToken : Token
    {
        public DeclarationDBOpenToken() { identifier = "<dbs>"; priority = 10; }
    }

    public class DeclarationDBCloseToken : Token
    {
        public DeclarationDBCloseToken() { identifier = "</dbs>"; priority = 10; }
    }

    public class CodeBeginToken : Token
    {
        public CodeBeginToken() { identifier = "<code>"; priority = 10; }
    }

    public class CodeEndToken : Token
    {
        public CodeEndToken() { identifier = "</code>"; priority = 10; }
    }
    #endregion
}
