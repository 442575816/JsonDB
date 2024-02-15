grammar Java;

Identifier : [a-zA-Z]+ ;

type : 'public'
     | 'private'
     | 'protected'

classDeclaration
    : 'class' Identifier typeParameters? ('extends' type)? ('implements' typeList)? 
    classBody
    ;

methodDeclaration
    : type Identifier formalParameters ('[' ']')* methodDeclarationRest
    | 'void' Identifier formalParameters methodDeclarationRest
    ;