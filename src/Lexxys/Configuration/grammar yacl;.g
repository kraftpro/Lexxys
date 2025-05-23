grammar yacl;

// Parser rules
config              : (declaration | node)* EOF;

declaration         : '%' LINE;

node                : node_element; //( subnodes )?;
node_element        : name_value_pair; // ( COMMA name_value_pair )*;
name_value_pair     : name EQUAL value;

name                : NAME;
value               : PLAIN_TEXT | STRING;


NAME                : [a-zA-Z_][a-zA-Z0-9_]*;
STRING              : '"' ~["]* '"';
PLAIN_TEXT          : ~[\r\n,;=:{}[\]()#]+;
EQUAL               : ('='|':');
COMMA               : (','|';');
LINE                : ~[\r\n]+;




node_name_value     : name EQUAL_SP ( value | sequence )?;

sequence            : array | object;
object              : map | attribs;
array               : '[' value ( COMMA_SP? value )* ']';
map                 : '{' name_value ( COMMA_SP? name_value )* '}';
attribs             : '(' name_value ( COMMA_SP? name_value )* ')';
name_value          : name EQUAL_SP value;

name                : NAME;

value               : NAME; // sequence | INLINE_VALUE | INLINE_TEXT | STRING;


comments            : SL_COMMENTS | ML_COMMENTS;


subnodes            : ; //NEWLINE INDENT nodes DEDENT;
nodes               : ( node )+;


// node                : name [EQUAL_SP] node_value [subnodes];


// fragment BOL : { Column == 0 };

BOL                 : [\r\n];

// Lexer rules
SL_COMMENTS         : (BOL | WSPACE)'#' ~[\r\n]*;
ML_COMMENTS         : (BOL | WSPACE)'<#' .*? '#>';

WSPACE              : [ \t\r\n];
SPACE               : [ \t]+;
EQUAL_SP            : ('='|':') SPACE;
COMMA_SP            : (','|';') SPACE;
NAME                : [a-zA-Z_][a-zA-Z0-9_]*;
STRING              : '"' ~["]* '"';
SP                  : [ \t];
INLINE_VALUE        : ( ~[\r\n,;=:{}[\]()#]+ | [:=][ \t] | [,;]+[ \t] | [ \t]'#' )*[\n\r];
INLINE_TEXT         : '<<<' ~[\r\n]* '\n' .*? [\n\r][ \t]* '>>>';

// BOL : '\r'? '\n';
