grammar yacl;

// Parser rules
config              : (declaration | node)* EOF;

declaration         : '%' LINE;

node                : node_element [subnodes];
node_element        : node_name_value | sequence;
node_name_value     : name [EQUAL_SP] node_continues;
node_continues      : name EQUAL_SP [node_continues]
                    | value [COMMA_SP node_continues];

sequence            : array | object;
object              : map | attribs;
array               : '[' value ( COMMA_SP? node_continues )* ']';
map                 : '{' name_value ( COMMA_SP? name_value )* '}';
attribs             : '(' name_value ( COMMA_SP? name_value )* ')';
name_value          : name EQUAL_SP value;

name                : NAME;

value               : sequence | INLINE_VALUE | INLINE_TEXT | STRING;


comments            : COMMENTS;


subnodes            : ; //NEWLINE INDENT nodes DEDENT;
nodes               : ( node )+;


// node                : name [EQUAL_SP] node_value [subnodes];



fragment BOL : { Column == 0 };

// Lexer rules
COMMENTS            : (BOL | WSPACE)'#' ~[\r\n]*
                    | (BOL | WSPACE)'<#' .* '#>' -> skip;

LINE                : ~[\r\n]+;
WSPACE              : [ \t\r\n];
SPACE               : [ \t]+;
EQUAL_SP            : ('='|':') SPACE;
COMMA_SP            : (','|';') SPACE;
NAME                : [a-zA-Z_][a-zA-Z0-9_]*;
STRING              : '"' ~["]* '"';
SP                  : [ \t];
INLINE_VALUE        : (~[ \t\r\n,;=:\{\}\[\]\(\)]+|~SP ':'|~SP '='|~SP ','|~SP ';')+;
INLINE_TEXT         : '<<<' ~[\r\n]* '\n' .* '>>>';

// BOL : '\r'? '\n';
