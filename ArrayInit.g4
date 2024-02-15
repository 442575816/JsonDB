grammar ArrayInit;

init : '{' value (',' value)* '}' ;

// 一个value可以是嵌套的花挎号结构，也可以是一个简单的整数
value : init
      | INT
      ;

INT : [0-9]+ ; // 定义词法符号INT， 数字类型
WS : [ \t\r\n]+ -> skip ; // 定义词法规则，空白字符丢弃