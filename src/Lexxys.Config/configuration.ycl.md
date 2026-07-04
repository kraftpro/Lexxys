# Configuration language

The goal:
- Compact and readable. Omit most punctuation.
- Easy integration.


### Grammar rules

1. A document is a sequence of nodes and directives.
2. A node is either a plain name, a name with a value, or a flow collection.
3. Indentation defines parent-child structure.
4. A child list is formed by one or more indented sibling nodes.
5. `:` and `=` both separate a name from a value in node syntax.
6. A newline or space after `:` or `=` is required when the separator is used as syntax; otherwise it is part of the item name.
7. `:` inside a type declaration introduces a structured field or nested type.
8. `=` inside a type declaration assigns a default value.
9. A default value is used when the corresponding item value is omitted.
10. A structured field may contain nested positional values or a nested collection.
11. `-` starts an unnamed item in a list context.
12. `{}` denotes a map, `[]` denotes an array, and `()` denotes a collection that can mix named and unnamed items.
13. `,` and `;` are optional separators inside flow collections.
14. `%` introduces a directive.
15. `%:` declares a type.
16. `%$` declares a variable.
17. `%!` declares a meta statement.
18. Directives may appear at any indentation level. They are processed at their location and affect only following lines.


### space indentation like YAML or Python

#### sample 1.a
```ycl
users
  user
    id    1214
    name  John Travolta
    age   31
    courses
      course 1
      course 8
      course 12

  user
    id    1213
    name  Alfred J. Peterson
    age   23
    courses
      course 5
      course 8
      course 12

```
converted to JSON
```json
"users": [
  {
    "id": 1214,
    "name": "John Travolta",
    "age": 31,
    "courses": [1, 8, 12]
  },
  {
    "id": 1213,
    "name": "Alfred J. Peterson",
    "age": 23,
    "courses": [5, 8, 12]
  }
]
```
#### sample 1.b
```ycl
users
  user
    id    1214
    name  John Travolta
    age   31
    courses
      course 1
      course 8
      course 12
```
converted to JSON
```json
"users": {
  "user": {
    "id": 1214,
    "name": "John Travolta",
    "age": 31,
    "courses": [1, 8, 12]
  }
}
```
> problem: The system doesn't understand that "user" is an element of the "users" array
converted to XML
```xml
<users>
  <user>
    <id>1214</id>
    ...
    <courses>
      <course>1</course>
      <course>8</course>
      <course>12</course>
    </courses>
  </user>
  <user>
    <id>1213</id>
    ...
  </user>
</users>
```

#### sample 1.c
```ycl
users
  -
    id    1214
    name  John Travolta
    age   31
    courses
      -   1
      -   8
      -   12
```
converted to JSON
```json
"users": [
  {
    "id": 1214,
    "name": "John Travolta",
    "age": 31,
    "courses": [1, 8, 12]
  }
]
```
converted to XML
```xml
<users>
  <item>
    <id>1214</id>
    ...
    <courses>
      <item>1</item>
      <item>8</item>
      <item>12</item>
    </courses>
  </item>
</users>
```

### Delimiters ':' or '=' are equivalent

Space (new line) after the delimiter is required, otherwise it is a part of item name.
Space before comments character is required.
```ycl
users:
  -
    id:   1214
    name: John Travolta # comments
    age=  31
    courses:
      -   1
      -   8
      -   12
```
converted to XML
```xml
<users>
  <item id="1214" name="John Travolta" age="31">
    <courses>
      <item>1</item>
      <item>8</item>
      <item>12</item>
    </courses>
  </item>
</users>

```
> system tries to convert items with `=` and `:` into XML attributes.

### JSON/PowerShell like constructions
> using `{}` for maps, `[]` for arrays, and `()` for collections with both named and unnamed items.
> delimiters `,` and `;` can be omitted.
> signs `:` and `=` are equivalent.

```ycl
users: [
  {
    "id": 1214,
    "name": "John Travolta",
    "age": 31,
    "courses": [1, 8, 12]
  },
  {
    id = 1213; name = "Alfred J. Peterson"; age=23; courses=[5,8,12]
  }
  ( id: 1212, name: "John Lennon", age: 23, courses: (3, 5, 8), extra-value: true )
]
more-users
  - (name: Bucifal id: 1211)
    age 13
    courses (1, 11, 5)
```

### Multiline values

Use C#-style `"""` or legacy-compatible angle markers. Both preserve line breaks. Longer angle markers allow shorter close sequences inside the text.

```ycl
description """
  First paragraph.

  Second paragraph.
  """

summary <<
  This text spans
  several source lines
  and becomes one value.
  >>

template <<<
  This text can contain >> without ending the value.
  >>>
```

### Meta statements, variables and substitution

```ycl
%$user = Gerry                  # '$' define variable 'user' for substitution
%!include variables.config.txt  # '!' include configuration file (user can be redefined there)
%!set-separator '->'            # '!' use '->' as a list separator along with ',' and ';'
%!set-separator                 # reset separators to ',' and ';'
%!set-escape    '\'             # '!' use '\' as the quoted-string escape character instead of the default backtick.
%!set-escape                    # reset the escape character to the default backtick.
%!set-escape    '\0'            # disable quoted-string escaping when '\' is the current escape character

user
  name ${user}                                            # substitute
  public-key  /users/${user}-public.key
  checksum    "/sha/user-${user}-sha${sha-type|256}.bin"  # substitute 'sha-type' if defined, or else 256
  region      ${arg:region|us-east}                       # read configured command-line argument
  home        ${env:USERPROFILE}                          # read environment variable
  service     ${config:defaults/service}                  # read external ConfigNodeCollection
  alias       ${:user/name}                                # read current YCL document parsed so far

```

Variables are lazy templates. A variable declaration may contain substitutions, including variables declared later, as long as they are defined before the variable is used:

```ycl
%$endpoint = "${base}/${region|us-east}"
%$base = "/${environment}/${service}"
%$environment = prod
%$service = billing

app:
  path ${endpoint}              # /prod/billing/us-east
```

Directives can be placed inside nested config text for readability. They affect only the lines that follow them:

```ycl
pipeline [validate->publish]    # one scalar value before the separator is declared
  %!set-separator '->'
steps [validate->publish]       # parsed as two array items
```

### Type definition

Type declarations define positional fields in order. Trailing values may be omitted, but skipped middle values should be represented explicitly.
Structured fields can be declared inside a type when a value itself has nested parts.
Default item values can also be provided in the type declaration.
In a type declaration, `:` prefixes scalar and named parameter types, while structured types use bare flow syntax. `=` assigns a default value.
Defaults are lazy templates and are applied after node construction, so a child node value is not duplicated by an earlier default.

```ycl
%:user  id :int, name :string, age :int, courses :int[]  # ':' defines parameter types

%* :user                  # '*' applies the type to following sibling nodes
user 1211 "John Doe" 55   # defines user
  courses (11, 12)
user 2232 "Katerin Smoke" 18 [2, 13]
%- :user                  # '-' applies the type to following unnamed nodes
- 2212 "Petter Pen"       # the node inherits the type name, so the missing age is inferred from the declared positions
- 2200, , 67               # the user name is omitted; the empty slot keeps the age in the third position

%member :user            # applies the type user to the next 'member' node or an unnamed node

%element id :int, name :string, value :int  # anonymous type applied to the next element node.
- 1200, "Twelve months" 12
- 1300 Thirteen 13

%:contact  name :string, email :string, phone :string = "n/a"  # '=' assigns a default value for phone
contact "Gerry" "gerry@example.com"
contact "Mina" "mina@example.com" "+1-555-0101"

%:building  name :string, address (street :string, city :string, zip :int), area :int = 0
building "North Tower" ("1 Main St.", "Montgomery", 32432)
building "South Tower" ("2 Main St.", "Montgomery", 32432) 1200

%:service
%:./name :string = "${service-name}"
%:./url :string = "https://${domain}/${service-name}"

%:rmq-broadcast prefix :string, route :string
%:rmq-broadcast/priority :int = 100
%:rmq-broadcast/group/queue name :string, durable :bool, auto-delete :bool
%:././exchange name :string, type :string, durable :bool
%:./././auto-delete :bool
%:rmq-broadcast/group/*/parameters
```

Absolute schema paths such as `%:rmq-broadcast/group/queue ...` create structural child types. In schema paths, `.` copies the path element from the previous declaration at the same position, and `*` declares fields shared by any child at that path.

Type application directives use `% pattern (type-reference | type declaration)`. Pattern elements are separated with `/`; `*` matches one node, `**` matches one or more nodes, `-` matches an unnamed node, and `.` applies to the next node only while preserving its name.

```ycl
%:pair left right
%* :pair

a one two      # matches pair
b one two      # matches pair
a three four   # matches pair again

%xx name value
- Petter 10    # becomes xx = { name = "Petter", value = "10" }

%:nv name value
%* :nv
- Mina 20      # becomes nv = { name = "Mina", value = "20" }
```

