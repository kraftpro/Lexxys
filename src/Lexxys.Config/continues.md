
# Part 1 (2026-07-02)

## Clearence

- [x] Compound result (Lexxys.ResultValue<Configuration.NodeCollection> ?) should be returned instead of throwing exceptions.
- [x] Add warning and error messages to the result.
- [x] Warn/Error if the recursive substitution circle is detected.
- [x] Disable the special behavior of the last star in the pattern of type application.
- [x] Add a warning if repeated nodes are splitted. ie 
     > ```
     > node-A
     > node-B
     > node-A
     > ```
     > will produce [[node-A, node-A], node-B] instead of [node-A, node-B, node-A] which is unclear.

- [x] Use `set-separator` and `set-escape` without value to reset to default values.
- [x] Add parser position information to ConfigNode and ConfigNodeCollection (line, column, file name, etc.)
- [x] Add the ability to attach a handler for a meta statement, including external parsers for `include`.
- [ ] more clearence items ...


## Meta statement

update

```bnf
meta_statement        ::= '%!' name value?
include_statement     ::= '%!include' value
separator_statement   ::= '%!set-separator' value
escape_statement      ::= '%!set-escape' value
```
as
```bnf
meta_statement        ::= '%!' meta_name value?
meta_name             ::= 'include' | 'set-separator' | 'set-escape'
```


- Meta statements communicate with the parser and may modify its behavior.
- The include statement takes a Configuration.NodeCollection object and inserts
  it into the current parsing result.
  > Depending on the parameter, the include statement may parse another YCL file (current functionality),
  > parse other formats, or generate the Configuration.NodeCollection object.
- Add support for custom meta statements by the meta statements factory. The factory should be able to create a meta statement object by its name.


## Substitution

- Add support of external sources for substitutions (ie. environment variables, command line arguments, configurations, etc.)
- Use factory pattern for substitution source objects.
- Use ':' separator to indicate the type of the substitution source.
  - `${env:name}` gets the value of the environment variable `name`
  - `${arg:name}` gets the value of the command line argument `name`
  - `${config:reference}` gets the value at the specified `reference` from the current configuration
  - `${:reference}` gets the value at the specified `reference` from current parsing YCL configuration


## Declaration

Use the ':' prefix for the parameter type in the type definition. Apply a default value at the end of node construction to prevent parameter duplication.


#### Sample declaration:

declaration logic:

```ycl
# declare a type `rmq-broadcast` with positional paramteres `prefix` and `route`
%:rmq-broadcast prefix :string, route :string
# declare a non positional parameter `priority` in the type 'rmq-broadcast`
%:rmq-broadcast/priority :int = 100
# declare subnodes 'group' and then 'queue' in the type 'rmq-broadcast`
# declare positional parameters name, durable, and auto-delete in the rmq-broadcast/group/queue
%:rmq-broadcast/group/queue name :string, durable :bool, auto-delete :bool


# `./` - copies the path element from the previous declaration.
# declare positional parameters name, durable, and auto-delete in the rmq-broadcast/group/exchange
%:././exchange name :string, type :string, durable :bool
# declare non positional parameter `aout-delete`
%:./././auto-delete :bool

# `bind` section
%:././bind exchange :string, queue :string      # only exchange and queue are positional parameters
%:./././routing-key :string

#declare node `parameters` in `rmq-broadcase/group/(queue|exchange|bind)` nodes
%:rmq-broadcast/group/*/parameters

%. :rmq-broadcast	# declares the next item      # . applies the type to the next node only
broadcast
  prefix:	dpa-broadcast-
  route: r#transact     # not a comment because '#' doesn't precede a space
  priority: 75

  -                     # group
    exchange que-dlx fanout
    queue que-dlx true false
    bind que-dlx que-dlx

  -                     # group
    exchange exch
      type: fanout
      durable: true
      auto-delete: false

    queue que
      durable: true
      auto-delete: false
      parameters        # can be replaced with a '-' since 'parameter' is the only node according to declaration.
        x-message-ttl: 60000
        x-max-length: 1000
        x-dead-letter-exchange: que-dlx

    bind exch que

```

