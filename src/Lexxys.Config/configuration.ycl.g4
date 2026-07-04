grammar Ycl;

/*
 * ANTLR4 reference grammar for YCL.
 *
 * YCL is indentation-sensitive. A production lexer should emit INDENT and
 * DEDENT tokens from leading whitespace, using tab size 4. This grammar
 * declares those tokens and focuses on the syntactic structure consumed by
 * the parser.
 */

tokens { INDENT, DEDENT }

document
	: line* EOF
	;

line
	: NEWLINE
	| statement NEWLINE?
	;

statement
	: directive
	| node
	;

node
	: nodeItem childBlock?
	;

nodeItem
	: unnamedNode
	| namedNode
	;

namedNode
	: name blockValue?
	;

unnamedNode
	: DASH value?
	;

blockValue
	: blockSeparator value?
	| value
	;

blockSeparator
	: COLON
	| EQUAL
	;

childBlock
	: NEWLINE INDENT line+ DEDENT
	;

value
	: flowCollection
	| multilineValue
	| string
	| plainValue
	;

flowCollection
	: array
	| object
	| mixedCollection
	;

array
	: LBRACK flowItems? RBRACK
	;

object
	: LBRACE objectItems? RBRACE
	;

mixedCollection
	: LPAREN parameterItems? RPAREN
	;

flowItems
	: value (itemSeparator? value)* itemSeparator?
	;

objectItems
	: nameValue (itemSeparator? nameValue)* itemSeparator?
	;

parameterItems
	: parameterItem (itemSeparator? parameterItem)* itemSeparator?
	;

parameterItem
	: nameValue
	| value
	;

nameValue
	: name flowSeparator value
	;

flowSeparator
	: COLON
	| EQUAL
	;

itemSeparator
	: COMMA
	| SEMI
	| CUSTOM_SEPARATOR
	;

multilineValue
	: TRIPLE_TEXT
	| ANGLE_TEXT
	;

directive
	: PERCENT directiveBody
	;

directiveBody
	: COLON schemaOrTypeDeclaration
	| DOLLAR variableDeclaration
	| BANG metaStatement
	| typeApplicationOrAnonymousType
	;

schemaOrTypeDeclaration
	: DOT SLASH schemaPath schemaFieldSpec?
	| typeName typeFields?
	;

schemaPath
	: name (SLASH name)*
	;

schemaFieldSpec
	: schemaType? REQUIRED? defaultValueAssignment?
	;

schemaType
	: scalarType arrayMarker?
	| COLON typeName
	| structuredType
	;

arrayMarker
	: LBRACK RBRACK
	;

variableDeclaration
	: name assignment? value
	;

metaStatement
	: INCLUDE value
	| SET_SEPARATOR value
	| SET_ESCAPE value
	| name value?
	;

typeApplicationOrAnonymousType
	: typePattern assignment typeName
	| typePattern typeFields
	;

typePattern
	: patternElement (SLASH patternElement)*
	;

patternElement
	: STARSTAR
	| STAR
	| DASH
	| name
	;

typeFields
	: typeField (itemSeparator? typeField)* itemSeparator?
	;

typeField
	: name fieldShape? defaultValueAssignment?
	;

fieldShape
	: COLON structuredType
	;

structuredType
	: LPAREN typeFields? RPAREN
	;

defaultValueAssignment
	: EQUAL value
	;

assignment
	: COLON
	| EQUAL
	;

typeName
	: name
	;

scalarType
	: STRING_TYPE
	| INT_TYPE
	| INTEGER_TYPE
	| NUMBER_TYPE
	| DECIMAL_TYPE
	| BOOL_TYPE
	| BOOLEAN_TYPE
	| DATE_TYPE
	| DATE_TIME_TYPE
	| DATETIME_TYPE
	| TIMESTAMP_TYPE
	| PERIOD_TYPE
	| DURATION_TYPE
	| TIMESPAN_TYPE
	| TIME_SPAN_TYPE
	;

plainValue
	: plainAtom+
	;

plainAtom
	: name
	| substitution
	| scalarSymbol
	;

substitution
	: SUBST_OPEN name (PIPE plainValue)? RBRACE
	;

name
	: string
	| NAME
	;

string
	: STRING
	;

scalarSymbol
	: STARSTAR
	| STAR
	| DASH
	| DOT
	| SLASH
	| BANG
	| DOLLAR
	| COLON
	| EQUAL
	| COMMA
	| SEMI
	| PIPE
	| CUSTOM_SEPARATOR
	;

PERCENT: '%';
BANG: '!';
DOLLAR: '$';
COLON: ':';
EQUAL: '=';
COMMA: ',';
SEMI: ';';
PIPE: '|';
DOT: '.';
SLASH: '/';
DASH: '-';
STARSTAR: '**';
STAR: '*';
LPAREN: '(';
RPAREN: ')';
LBRACK: '[';
RBRACK: ']';
LBRACE: '{';
RBRACE: '}';
SUBST_OPEN: '${';

INCLUDE: 'include';
SET_SEPARATOR: 'set-separator';
SET_ESCAPE: 'set-escape';
REQUIRED: 'required';

STRING_TYPE: 'string';
INT_TYPE: 'int';
INTEGER_TYPE: 'integer';
NUMBER_TYPE: 'number';
DECIMAL_TYPE: 'decimal';
BOOL_TYPE: 'bool';
BOOLEAN_TYPE: 'boolean';
DATE_TIME_TYPE: 'date-time';
DATETIME_TYPE: 'datetime';
TIMESTAMP_TYPE: 'timestamp';
DATE_TYPE: 'date';
TIME_SPAN_TYPE: 'time-span';
TIMESPAN_TYPE: 'timespan';
PERIOD_TYPE: 'period';
DURATION_TYPE: 'duration';

TRIPLE_TEXT
	: '"""' .*? '"""'
	;

ANGLE_TEXT
	: '<<' '<'* .*? '>>' '>'*
	;

STRING
	: '"' (ESC | ~["\r\n])* '"'
	| '\'' (ESC | ~['\r\n])* '\''
	;

CUSTOM_SEPARATOR
	: '->'
	;

NAME
	: NAME_CHAR+
	;

NEWLINE
	: '\r'? '\n'
	| '\r'
	;

WS
	: [ \t]+ -> channel(HIDDEN)
	;

LINE_COMMENT
	: ( '#' | '//' ) ~[\r\n]* -> channel(HIDDEN)
	;

BLOCK_COMMENT
	: '/*' .*? '*/' -> channel(HIDDEN)
	| '<#' .*? '#>' -> channel(HIDDEN)
	;

fragment ESC
	: '\\' .
	| '`' .
	;

fragment NAME_CHAR
	: ~[ \t\r\n\[\]\{\}\(\),;:=/%!$|*]
	;
