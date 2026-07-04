# Configuration YCL Samples

This file shows practical YCL patterns supported by the parser.

## Basic Application Settings

```ycl
app:
  name Orders
  environment production
  enabled true
  endpoints [health, metrics, api]

database:
  provider sqlserver
  connection-name OrdersDb
  retry:
    attempts 3
    backoff 2s
```

## Flow Collections

```ycl
server { host=localhost, port=8080, tls=true }
users [
  { id=1211, name="John Doe", roles=[admin, ops] },
  { id=1212, name="Jane Smith", roles=[reader] }
]
route (method: GET, path: /orders, cacheable: true)
```

## Variables and Defaults

```ycl
%$service = orders
%$region = us-east

paths:
  logs /var/log/${service}
  data /srv/${service}/${region}
  temp /tmp/${service}-${slot|blue}
```

Variable declarations are lazy templates, so they may contain substitutions resolved later when the variable is used.

```ycl
%$endpoint = "${base}/${region|us-east}"
%$base = "/${environment}/${service}"
%$environment = prod
%$service = billing

api:
  root ${endpoint}
```

## Includes

```ycl
%$environment = production

%!include common/logging.ycl
%!include services/orders.ycl
```

Included files are resolved relative to the including file. Includes stay inside the root directory of the initially parsed file, and recursive include chains are rejected.

## Custom Separators

```ycl
%!set-separator '->'

pipeline [validate -> enrich -> persist -> publish]

%:transition event -> target -> guard=("true") -> actions
%transition :transition

transition Submit -> Reviewing -> "ctx.valid" -> [audit, notify-reviewer]
transition Cancel -> Cancelled
```

Directives can appear inside nested config text and affect only following lines.

```ycl
workflow:
  before [validate->publish]
    % ! set-separator = '->'
  after [validate->publish]
```

`before` is a single scalar item, while `after` is split by the newly configured separator.
Use `%!set-separator` without a value to reset separators to `,` and `;`.

## Inline Positional Types

```ycl
%:user id, name, age=0, roles
%user :user

user 1001 "Ada Lovelace" 36 [admin, analyst]
user 1002 "Grace Hopper"
```

Result shape:

```ycl
user = [
  { id = "1001", name = "Ada Lovelace", age = "36", roles = [ "admin", "analyst" ] },
  { id = "1002", name = "Grace Hopper", age = "0", roles = null }
]
```

## Type Application Patterns

```ycl
%:pair left right
%* :pair

a one two
b one two
a three four

%:endpoint host port
%services/**/endpoint :endpoint

services:
  public:
    http:
      endpoint api.example.test 443
```

Path patterns use `/`; `*` matches one path element, `**` matches one or more path elements, and `-` matches unnamed nodes.

## Expanded Schemas

Every expanded schema field starts with `%:`. Use `%:./field` for direct fields, `%:./parent/child` for nested fields, and flow syntax for compact nested definitions.

```ycl
%:service
%:./name string required
%:./replicas int = 1
%:./enabled bool = true
%:./started-at date-time required
%:./timeout period = 30s
%:./tags string[] = [api]
%:./endpoint (host string required, port int = 443)
%:./limits/min int = 1
%:./limits/max int = 10

%service :service

service Orders 3 true 2026-06-27T10:15:30Z 45s [api, public]
  endpoint api.internal
```

Schema defaults are lazy templates and resolve when the type is applied.

```ycl
%:service-url
%:./name string = "${service-name}"
%:./url string = "https://${domain}/${service-name}"

%$service-name = orders
%$domain = example.test

%service :service-url
service:
```

## Nested Blocks and Lists

```ycl
queues:
  -
    name orders-created
    durable true
    consumers:
      - billing
      - fulfillment
      - analytics
  -
    name orders-cancelled
    durable true
    consumers [billing, fulfillment]
```

## Multiline Values

Use C#-style `"""` or legacy-compatible angle markers. Both preserve line breaks. Longer angle markers allow shorter close sequences inside the text.

```ycl
release-notes """
  Added YCL parser support.

  Fixed schema validation and includes.
  """

legacy-notes <<
  This syntax is compatible
  with legacy configuration text.
  >>

template <<<
  This text can contain >> without ending the value.
  >>>
```

## Comments

```ycl
app Orders # line comment

/*
  C-style block comment.
  Markers such as # and // are ignored inside the block.
*/

<# PowerShell-style block comment #>

logging:
  level info // slash line comment
```

## Escape Character

Backtick is the default escape character in quoted strings. `%!set-escape` changes it for subsequent strings. Use `\0` with the current escape character to disable escaping. Use `%!set-escape` without a value to reset the default backtick escape.

```ycl
message "Hello `"YCL`""

%!set-escape '\'
windows-path "C:\\Temp\\Orders"

%!set-escape '\0'
literal "Backslash sequences stay literal: \n \t \0"
```

## State Machine

```ycl
%!set-separator '->'
%:transition event -> target -> guard=("true") -> actions
%transition :transition

machine:
  name OrderFlow
  initial Draft
  states:
    -
      name Draft
      on:
        transition Submit -> Reviewing -> "ctx.total > 0" -> [validate, audit]
        transition Cancel -> Cancelled

    -
      name Reviewing
      on:
        transition Approve -> Approved -> "ctx.approved" -> [reserve, notify]
        transition Reject -> Draft -> "true" -> [append-note]

    -
      name Approved
      final true

    -
      name Cancelled
      final true
```

## Mixed Collections

Parentheses can carry named and unnamed values together.

```ycl
filter (status: open, priority: high, assigned-to-me, limit: 50)
```

## Scalar Value Notes

YCL stores parsed scalar values as strings in `ConfigNode`. Schema types validate shape but do not convert storage:

```ycl
%:job
%:./run-date date required
%:./started-at date-time required
%:./timeout period = 5s

%job :job
job 2026-06-27 2026-06-27T08:30:00Z
```

The resulting values remain `"2026-06-27"`, `"2026-06-27T08:30:00Z"`, and `"5s"`.
