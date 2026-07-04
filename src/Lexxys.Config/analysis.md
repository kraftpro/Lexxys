**Critical Analysis**

YCL has a strong core idea: it tries to be a compact, readable configuration DSL that keeps YAML-like indentation, JSON-like flow values, and schema/type hints in one language. That is useful. The language is expressive enough for dense state-machine-style config, and the type application pattern system gives it a lot of power without forcing every node to carry explicit structure.

The main risk is that YCL is now close to being two languages: a simple configuration syntax and a declarative typing/matching language. That can work, but it raises the cost of understanding “what this line means” from local syntax alone.

**Strengths**

YCL’s biggest strength is ergonomic density. These are readable and compact:

```ycl
%:transition event -> target -> guard=("true") -> actions
%transition :transition

transition Submit -> Reviewing -> "ctx.valid" -> [audit, notify]
```

The indentation plus flow syntax mix is practical. Users can choose readable blocks for structure and inline collections for compact data. Multiline values are now much cleaner with `"""` and `<<< >>>`.

Lazy substitutions are also a good direction. They make variable and type defaults more composable:

```ycl
%$endpoint = "${base}/${region|us-east}"
%$base = "/${environment}/${service}"
```

That matches how people expect templates to behave.

**Weaknesses**

The biggest weakness is ambiguity pressure. YCL intentionally makes punctuation optional or overloaded:

- `:` and `=` are both assignment separators.
- Spaces can separate name/value.
- `,` and `;` can be optional.
- Custom separators can affect parsing later.
- `% pattern :type` can change how future nodes are interpreted.

This makes the language pleasant to write but harder to reason about, parse, format, lint, or teach. Small whitespace changes may alter meaning.

The type application pattern feature is powerful but subtle. This rule in particular is surprising:

```ycl
%* :pair
a one two
b one two
a three four
```

The final `*` mutates after first match. That is clever, but stateful matching is not obvious. It also means directives are not just declarations; they are active parser state machines.

Unnamed-node naming is useful, but potentially magical:

```ycl
%* :nv
- Petter 10
```

Becoming `nv = { name = "Petter", value = "10" }` is convenient, but the source no longer visibly contains the resulting node name.

**Design Risks**

Custom separators are probably the riskiest feature. They change tokenization/interpretation after the directive and can conflict with ordinary scalar text. `%!set-separator '->'` is great for state machines, but separators should probably be scoped, documented heavily, and maybe forbidden inside some contexts.

Directives “can be anywhere” is flexible, but it weakens locality. A directive indented under a scalar value visually looks like a child, but semantically affects following lines. That may surprise users:

```ycl
name demo
  %!set-separator '->'
```

This is legal, but visually odd.

Lazy substitution also needs clear cycle behavior. Leaving recursive references unresolved is safe, but users may expect an error. A linter should probably warn.

**Best Use Cases**

YCL is best for human-authored operational configuration where compactness matters:

- state machines
- routing/workflow definitions
- service configs with schema validation
- configs with repeated positional records
- legacy config migration where punctuation-light syntax helps

It is less ideal for:

- interchange format between systems
- security-sensitive config edited by many teams
- data where exact lexical preservation matters
- config that needs generic tooling support today

**Recommendations**

I would keep YCL, but define a “recommended subset”:

- Prefer explicit `:` or `=` for normal nodes.
- Use custom separators only near the type that needs them.
- Prefer named type application patterns like `%transition :transition`; use `%*` sparingly.
- Avoid placing directives under scalar values except when intentionally scoping readability.
- Require schema/type declarations near the top of a file or section.
- Add a formatter/linter early, because the language is too flexible to leave style entirely informal.

The language is promising, but it needs guardrails. Its power comes from parser state, and parser state is where configuration languages become hard to debug. A good linter, formatter, and “strict mode” would make YCL much safer without losing its compact DSL feel.



### improvements

1. `%!set-something`<br>
   without parameter resets "something" settings into the original state.
   remove special handling for `%!set-escape '\0'`, since it became equivalent to `%!set-escape`

2. Remove special rule for mathing star when it is the last item in pattern.
   So `%* :one` will match all the nodes below without locking to the first matched name and without renaming the unnamed nodes.

   


