lexer grammar CommonLexerRules;

CLEAR: 'clear' ; // 匹配clear
ID: [a-zA-Z]+ ; // 匹配标识符
INT: [0-9]+ ; // 匹配整数
NEWLINE: '\r'? '\n' ; // 告诉语法分析器一个新行的开始
WS: [ \t]+ -> skip ; // 丢弃空白字符