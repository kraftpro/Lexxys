# Configuration YCL Grammar

This file formalizes the language described in [configuration.ycl.md](configuration.ycl.md). When examples and grammar appear to differ, the examples in that file are authoritative.

## 1. Lexical Conventions

YCL is indentation-sensitive. Leading spaces and tabs both contribute to indentation; tabs advance to the next tab stop with tab size 4.

```bnf
NEWLINE        ::= '\n' | '\r\n' | '\r'
INDENT         ::= increased visual indentation after NEWLINE
DEDENT         ::= decreased visual indentation after NEWLINE
WS             ::= ' ' | '\t'
BLOCK_SEP      ::= (':' | '=') (WS | NEWLINE)
FLOW_SEP       ::= ':' | '='
ITEM_SEP       ::= ',' | ';' | configured separator
NAME           ::= unquoted text not starting a separator, comment, collection delimiter, or directive
PLAIN_VALUE    ::= unquoted text ending at NEWLINE or comment
MULTILINE_OPEN ::= '"""' | '<' '<'+
MULTILINE_CLOSE ::= '"""' | matching '>' sequence
STRING         ::= single-quoted or double-quoted string with escapes
COMMENT        ::= line comment or block comment
```

`:` and `=` are syntactic separators in block node syntax only when followed by whitespace or a newline; otherwise they remain part of the item name. Inside flow collections, type declarations, and directives, compact assignments such as `age=23` and `courses=[5,8,12]` are valid.

Quoted strings use the backtick character as the default escape character, for example `"Hello `"YCL`""`. `%!set-escape value` changes the escape character for subsequent quoted strings. Use the current escape character followed by `0` to disable escaping, for example `%!set-escape '`0'` with the default escape or `%!set-escape '\0'` after setting the escape to backslash.

`#` starts a comment when it appears at the beginning of a line or is preceded by whitespace. Directives start with `%` only.

## 2. Document

A document is a sequence of directives and nodes.

```bnf
document        ::= statement*
statement       ::= directive | node
```

Directives may appear at any indentation level, including between nested sibling lines. A directive is processed at its physical location and affects only following lines. It does not reinterpret the node that appears before it.

## 3. Nodes

A node can be a plain name, a name with a value, or an unnamed list item. Flow collections appear as values.

```bnf
node            ::= node_item subnodes?
node_item       ::= named_node | unnamed_node
named_node      ::= name (BLOCK_SEP? value)?
unnamed_node    ::= '-' value?
subnodes        ::= NEWLINE INDENT node+ DEDENT
name            ::= NAME | STRING
value           ::= flow_collection | STRING | PLAIN_VALUE | multiline_value
multiline_value ::= ('"""' NEWLINE text_line* NEWLINE '"""')
                  | ('<<' NEWLINE text_line* NEWLINE '>>')
```

Examples:

```ycl
users
  -
    id: 1214
    name John Travolta
```

In block syntax, `name value` is valid without `:` or `=`; the first token is the node name and the remainder is its value. In this form, `,` and `;` remain part of the scalar unless the value is later interpreted by an applied type.

Multiline values use explicit opening and closing markers. C#-style `"""` and legacy-compatible angle markers both preserve line breaks and blank lines. Angle markers must close with the same length, so `<<<` closes with `>>>` and can contain plain `>>` text:

```ycl
description """
  first line

  second line
  """

legacy <<
  first
  text
  >>

template <<<
  value >> appears here
  >>>
```

## 4. Flow Collections

Flow collections provide compact inline arrays, maps, and mixed collections.

```bnf
flow_collection ::= array | object | parameters
array           ::= '[' flow_items? ']'
object          ::= '{' object_items? '}'
parameters      ::= '(' parameter_items? ')'
flow_items      ::= value (ITEM_SEP? value)*
object_items    ::= name_value (ITEM_SEP? name_value)*
parameter_items ::= parameter_item (ITEM_SEP? parameter_item)*
parameter_item  ::= value | name_value
name_value      ::= name FLOW_SEP value
```

`[]` is an array, `{}` is a map, and `()` is a mixed collection that can contain named and unnamed values. `,` and `;` are optional separators when token boundaries are otherwise clear. A custom separator added by `%!set-separator` is also recognized:

```ycl
{ id = 1213; name = "Alfred J. Peterson"; age=23; courses=[5,8,12] }
( id: 1212, name: "John Lennon", courses: (3, 5, 8), true )
%!set-separator '->'
path [start -> running -> done]
```

## 5. Lists and Unnamed Items

`-` starts an unnamed item in a list context. The item may contain an inline value, nested properties, or both.

```bnf
unnamed_node ::= '-' value?
```

When a type is applied, unnamed items may inherit the active type name or positional context.

## 6. Directives

Directives configure parsing, variables, type declarations, and type application.

```bnf
directive             ::= type_declaration
                        | schema_field_decl
                        | variable_declaration
                        | meta_statement
                        | type_application
                        | anonymous_type_declaration

type_declaration      ::= '%:' type_name type_fields?
schema_field_decl     ::= '%:' schema_path schema_body?
variable_declaration  ::= '%$' name (FLOW_SEP | BLOCK_SEP)? value
meta_statement        ::= '%!' meta_name value?
meta_name             ::= built_in_meta_name | registered_meta_name
built_in_meta_name    ::= 'include' | 'set-separator' | 'set-escape'
type_application      ::= '%' type_pattern WS? ':' type_name
anonymous_type_declaration ::= '%' name type_fields
type_pattern          ::= pattern_item ('/' pattern_item)*
pattern_item          ::= name | '*' | '**' | '-' | '.'
```

Semantics:

- `%:user ...` declares a named type.
- `%$user = Gerry` declares a variable template.
- `%!include path` includes another YCL file at the directive location. Relative paths are resolved from the including file when using `ParseFile`. Includes are confined to the root directory of the initially parsed file, missing include files are errors, and recursive include chains are rejected.
- `%!set-separator value` adds an item separator used in flow collections, positional values, and type field lists. Longer separators are matched before shorter separators. `%!set-separator` without a value resets separators to `,` and `;`.
- `%!set-escape value` changes the quoted-string escape character. The default is backtick; `\0` or `` `0`` according to the current escape character disables escaping. `%!set-escape` without a value resets the escape character to backtick.
- Additional `%!name value` forms are valid only when a `YclParser` meta handler is registered for `name`. Unknown meta names are invalid.
- Meta handlers may return a `ConfigNodeCollection`; returned nodes are inserted at the directive location. Replacing the `include` handler allows external parsers while retaining safe include path resolution.
- `% pattern :type` applies a referenced type to the nearest following node matched by `pattern` and matching siblings.
- `% pattern fields...` declares an anonymous type and applies it using the same pattern rule.
- `%. :type` is a one-shot application: it applies `type` to the next node only and preserves that node's name.

Whitespace may appear between directive special characters and names, so `% ! set-escape = '\'`, `% $name = value`, and `% : type field` are equivalent to their compact forms.

## 7. Variables and Substitution

Variables can be referenced in plain values and strings.

```bnf
substitution ::= '${' (name | source_reference) default_value? '}'
source_reference ::= source_name ':' reference
source_name ::= 'env' | 'arg' | 'config' | '' | registered_source_name
default_value ::= '|' value
```

`${user}` substitutes an existing variable. `${sha-type|256}` substitutes `sha-type` when defined, otherwise `256`. Variables are parser-wide, so values declared before an include are visible inside included files. Recursive substitutions are left unresolved and reported as warnings by the non-throwing parse result API.

`${env:name}` reads an environment variable, `${arg:name}` reads a configured command-line argument, `${config:reference}` reads a value from an external `ConfigNodeCollection`, and `${:reference}` reads a value from the YCL document parsed so far. Applications may register additional substitution sources on `YclParser`. Missing sources or missing values leave the substitution expression unchanged unless a default value is supplied.

Variable declarations are lazy templates. Substitution expressions inside a variable declaration are preserved until the variable is used:

```ycl
%$endpoint = "${base}/${region|us-east}"
%$base = "/${environment}/${service}"
%$environment = prod
%$service = billing

path ${endpoint}
```

The `path` value is `/prod/billing/us-east`. Recursive variable references are left unresolved instead of causing infinite expansion.

## 8. Type Declarations

Type declarations define positional fields in order. Trailing values may be omitted; skipped middle values must be represented explicitly by an empty slot. Field lists may be whitespace-separated only when no explicit item separator appears in the declaration.

```bnf
type_fields      ::= type_field (ITEM_SEP? type_field)*
type_field       ::= field_name (parameter_type | structured_type)? required_marker? default_value_assignment?
schema_field     ::= field_name (parameter_type | structured_type)? required_marker? default_value_assignment?
schema_path      ::= schema_path_item ('/' schema_path_item)*
schema_path_item ::= field_name | '*' | '.'
schema_body      ::= type_fields | schema_field
parameter_type   ::= ':' (scalar_type | scalar_type '[]' | type_name)
field_name       ::= name
structured_type  ::= '(' type_fields ')'
scalar_type      ::= 'string' | 'int' | 'integer' | 'number' | 'decimal' | 'bool' | 'boolean'
                   | 'date' | 'date-time' | 'datetime' | 'timestamp' | 'period' | 'duration' | 'timespan' | 'time-span'
required_marker  ::= 'required'
default_value_assignment ::= '=' value
```

Examples:

```ycl
%:user id :int, name :string, age :int, courses :int[]
%:contact name :string, email :string, phone :string = "n/a"
%:building name :string, address (street :string, city :string, zip :int), area :int = 0

%:service
%:./name :string required
%:./replicas :int = 1
%:./started-at :date-time required
%:./timeout :period = 5s
%:./tags :string[] = [api, public]
%:./endpoint (host :string required, port :int = 443)
%:./limits/min :int = 1
%:./limits/max :int = 10

%:rmq-broadcast prefix :string, route :string
%:rmq-broadcast/priority :int = 100
%:rmq-broadcast/group/queue name :string, durable :bool, auto-delete :bool
%:././exchange name :string, type :string, durable :bool
%:./././auto-delete :bool
%:rmq-broadcast/group/*/parameters
```

In type declarations, `:` prefixes scalar and named parameter types, while structured types use bare flow syntax and must not use the `:` prefix. `=` assigns a default value. In an expanded schema declaration, every field line starts with `%:`. `%:./field` adds a field to the most recent type declaration, and `%:./parent/child` adds a nested field. Absolute schema paths such as `%:service/endpoint/host :string` create or extend structural child schemas. In schema paths, `.` copies the path element from the previous schema declaration at the same position, and `*` declares fields shared by any child at that path. Complex nested fields should use flow syntax such as `%:./endpoint (host :string required, port :int = 443)`. Scalar types validate supplied values, `required` rejects missing values, and `[]` requires an array. Date, date-time, and period validation use the core Lexxys string parsers. Defaults are lazy templates and are applied after node construction, so a default is used only when the corresponding item value is still omitted, not when the value is supplied as a child node or as an explicit empty string.

## 9. Type Application and Positional Values

After a type is applied, node values are interpreted positionally:

```ycl
%* :user
user 1211 "John Doe" 55
%- :user
- 2200, , 67
```

The first example maps values to `id`, `name`, and `age`. The second preserves the empty middle slot so `67` maps to `age`.

Structured fields may be supplied as nested positional values or as a nested collection:

```ycl
building "North Tower" ("1 Main St.", "Montgomery", 32432)
```

Type application patterns use `/` between path elements. `*` matches one node, `**` matches one or more nodes, `-` matches an unnamed node, and `.` applies to the next node only:

```ycl
%:pair left right
%* :pair

a one two      # typed as pair
b one two      # typed as pair
a three four   # typed as pair
```

Unnamed nodes can be named by the applied type or by the last literal pattern item:

```ycl
%xx name value
- Petter 10    # becomes xx = { name = "Petter", value = "10" }

%:nv name value
%* :nv
- Mina 20      # becomes nv = { name = "Mina", value = "20" }
```

## 10. Comments

Supported comments:

```bnf
line_comment  ::= '#' text_to_newline | '//' text_to_newline
block_comment ::= '/*' text '*/' | '<#' text '#>'
```

Block comments are removed before line parsing; nested block comments are not parsed recursively, but comment markers inside a block comment are ignored as text until the first matching closer.

## 11. Intermediate Model

The parser produces a `ConfigNodeCollection` root. The formal intermediate model is:

- `ConfigNode`: empty, scalar, array, object, mixed collection, or scalar with children.
- `ConfigNodeCollection`: ordered entries of `(string? key, ConfigNode value)`.
- A collection with only unnamed entries is an array.
- A collection with only named entries is an object/map.
- A collection with both named and unnamed entries is mixed.
- Repeated sibling names are promoted to a named array value by `ConfigNodeCollection`.

This model is exposed by `ConfigNode.Kind`, `ConfigNodeCollection.GetCollectionType()`, and shape helpers such as `IsArray`, `IsObject`, `IsMap`, and `IsMixed`.

## 12. Conversion Semantics

The master document describes conversion intent, not only syntax:

- Repeated sibling names may be promoted to arrays.
- Unnamed items serialize to JSON array items and XML `<item>` elements unless type context supplies a better name.
- Items written with `:` or `=` may be projected as XML attributes when they are scalar properties of an element.
- `{}` maps to an object/map, `[]` maps to an array, and `()` maps to a mixed collection.

Exact array promotion and XML projection are semantic decisions made after parsing, not lexical grammar rules.

## 13. Parsing Precedence

1. Recognize comments and directives.
2. Recognize collection delimiters.
3. In block nodes, treat `:` or `=` as a separator only when followed by whitespace or newline.
4. In flow collections and type declarations, allow compact `name=value` and `name:value`.
5. Establish node shape before applying type metadata.
6. Apply positional fields, structured fields, and defaults.
