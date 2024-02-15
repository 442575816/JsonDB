grammar Expr;
import CommonLexerRules; // 引入通用规则

// 起始规则，词法分析的起点
prog: stat+ ;

stat: expr NEWLINE          # printExpr
    | ID '=' expr NEWLINE   # assign
    | CLEAR NEWLINE         # clear
    | NEWLINE               # blank
    ;

MUL : '*';
DIV : '/';
ADD : '+';
SUB : '-';

expr: expr op=('*'|'/') expr # MulDiv
    | expr op=('+'|'/') expr # AddSub
    | INT                    # int
    | ID                     # id
    | '(' expr ')'           # parens
    ;

