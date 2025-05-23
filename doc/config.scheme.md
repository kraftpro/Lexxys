# Configuration TXT

## Grammar

### Document

```bnfc
document          ::= (dirrective | node)+
node              ::= node_item [subnodes]
subnodes          ::= NEWLINE INDENT ( node )+ DEDENT
node_item         ::= node_name_value
                    | flow_collection
node_name_value   ::= name [EQUAL_SP] value
flow_collection   ::= array
                    | object
                    | parameters
array             ::= "[" value (COMMA_SP? value)* "]"
object            ::= "{" name_value (COMMA_SP? name_value)* "}"
parameters        ::= "(" value_name_value (COMMA_SP? value_name_value)* ")"
name_value        ::= name EQUAL_SP value
value_name_value  ::= value
                    | name EQUAL_SP value

name              ::= NAME | string
value             ::= flow_collection | PLAIN_VALUE | SRING | TEXT_BLOCK

PLAIN_VALUE       ::= any sequence of chars excluding new line character, doesn't start with "[", "(", or "{" and doesn't contain "\s<?#" sequence.
                      Characters except the new line can be escaped by grave "`".
NAME              ::= PLAIN_VALUE excluding white space characters.
STRING            ::= C style string (PowerShell style string ?)
TEXT_BLOCK        ::= "<"{N} NEWLINE [.\n]*? NEWLINE \s* ">"{N} NEWLINE


# node_name_value   ::= name [EQUAL_SP] node_parameters
# node_parameters   ::= value
#                     | name EQUAL_SP [node_parameters]
#                     | value [COMMA_SP node_parameters]
# plain_value       ::= any sequence of chars excluding new line character, "[:=,;]\s", "\s[#[({[]", and not starting with "-\s".
                       Characters except the new line can be escaped by "`".
```

### Directives

```bnfc
dirrective        ::= type_declaration
                    | var_definition
                    | object_definition
                    | type_declaration
                    | option_definition
type_declaration  ::= "%" type_name name_list      # Type declaration
var_definition    ::= "%" var_name ":" value       # Variable declaration
object_definition ::= "%" pattern ( type_name | name_list )
name_list         ::= name ( delimiter name )*
delimiter         ::= SPC | "," | ";"
option_definition ::= "%" "!" option
option            ::= "js-comments"
                    | "js-string"
                    | "include" file_path
                    | "separators" value ( ',' value }*


type_name         ::= ":".name
var_name          ::= "$".name

var_reference     := "${{" name [ "|" default_value ] "}}"   # Reference to the variable
```


### Elements

```bnfc

comments          ::= ( SPACE | BOL ) "#" TEXT NEWLINE
                    | ( SPACE | BOL ) "#<" ML_TEXT ">#"

name              ::= name_char+
                    | string
name_char         ::= !special_char | escape_shar special_char
special_char      ::= escape_char | "=" | ":" | ";" | "," | "[" | "]" | "{" | "}" | "(" | ")" | "/" | "\" | "'" | """ | SPACE
escape_char       ::= "`"
string            ::= """ ( string_char | "'" | escape_seq )* """
                    | "'" ( string_char | """ | escape_seq )* '"'
string_char       ::= !( "\r" | "\n" | escape_char | "'" | '"' )
escape_seq        ::= escape_char ( "r" | "n" | "t" | "f" | "v" | "a" | "b" | "0" | "`" | "h" HEX HEX | "x" HEX HEX (HEX HEX)? | "u" HEX HEX HEX HEX )

# todo: add unicode
TEXT              ::= /[^\r\n]*/
ML_TEXT           ::= /[.\r\n]*/
SPACE             ::= /[ \t]/
SP                ::= /[ \t]*/
SPC               ::= /[ \t]+/
NEWLINE           ::= /\r?\n/
BOL               ::= /^/
WSPACE            ::= /[ \t\r\n]/
EQUAL_SP          ::= /[:=](?=[ \t\r\b\n])/
COMMA_SP          ::= /[,;](?=[ \t\r\b\n])/
SP_COMCHAR        ::= /(?<>=[ \t\r\b\n])#/
SP_COMBLCK        ::= /(?<>=[ \t\r\b\n])<#/

```

```bnfc
pattern           ::= ( ( wildcard | name_part ) "/" )* name_part
name_part         ::= name aray_mark?
wildcard          ::= '*'      # matches any letter except path separator
                    | '**'     # matches any letter including path separator
array_mark        ::= json_array_mark | xml_array_mark
json_array_mark   ::= "[" "]"
xml_array_mark    ::= "[" name "]"   # defines a name of node to be repeated (by default "item")
```

### Sample

```
% school/buildings[] id name area
% school/buildings[]/location lat long

school
  buildings
    - 123 "Meadows Charter School" XY2323
      location: 42.34 80.45
    - 124 "Stonewall College" XX5134
    - 125 "Oak Park School for Girls"       # No area
    -    , "Grand Ridge School" YY1331      # No ID
    - 122,                    , YY1331      # No name
```

```
# Simple config

statecharts
  statechart
    name: Elevator
    description: <<
      The statechart of an Elevator mothion
      >>
    attributes: (item-1: value, item-2 = value, item3=value)
    states
      state
        name: Iddle
        id: 1
        transitions
          transition
            target: MovingUp
            event: Call
            guard: obj.CurrentLocation < obj.RequeredLocation
          transition
            target: MovingDown
            event: Call
            guard: obj.CurrentLocation > obj.RequeredLocation
          transition
            target: Stopped
            event: Call
            guard: obj.CurrentLocation == obj.RequeredLocation
      state
        name: MovingUp
        id: 2
        action: MoveUp();
        transitions
          %[transition]
          -
            target: Stopped
            condition: obj.CurrentLocation == obj.RequeredLocation
          -
            target: MovingUp
            condition: obj.CurrentLocation < obj.RequeredLocation
          -
            target: MovingDown
            condition: obj.CurrentLocation > obj.RequeredLocation

        # produces:
        # XML:
        #   <state name="MovingUp" id="2" action="MoveUp();">
        #     <transitions>
        #       <transition target="Stopped" condition="obj.CurrentLocation == obj.RequeredLocation"/>
        #       <transition target="MovingUp" condition="obj.CurrentLocation < obj.RequeredLocation"/>
        #       <transition target="MovingDown" condition="obj.CurrentLocation > obj.RequeredLocation"/>
        #     </transitions>
        #   </state>
        # JSON:
        #   state: {
        #     name: "MovingUp",
        #     id: 2,
        #     action: "MoveUp();",
        #     transitions: [
        #       {"target": "Stopped", "condition": "obj.CurrentLocation == obj.RequeredLocation"},
        #       {"target": "MovingUp", "condition": "obj.CurrentLocation < obj.RequeredLocation"},
        #       {"target": "MovingDown", "condition": "obj.CurrentLocation > obj.RequeredLocation"}
        #     ]
        #   }

      state
        name: MovingDown
        id: 3
        action: MoveDown();
        transition
          target: Stopped
          condition: obj.CurrentLocation == obj.RequeredLocation
        transition
          target: MovingUp
          condition: obj.CurrentLocation < obj.RequeredLocation
        transition
          target: MovingDown
          condition: obj.CurrentLocation > obj.RequeredLocation
        [1,2,3,5]

        # produces:
        # XML:
        #   <state name="MovingDown" id="3" action="MoveDown();">
        #     <transition target="Stopped" condition="obj.CurrentLocation == obj.RequeredLocation"/>
        #     <transition target="MovingUp" condition="obj.CurrentLocation < obj.RequeredLocation"/>
        #     <transition target="MovingDown" condition="obj.CurrentLocation > obj.RequeredLocation"/>
        #     <item>1</item><item>2</item><item>3</item><item>5</item>
        #   </state>
        # JSON:
        #  state: {
        #    name: "MovingDown",
        #    id: 3,
        #    action: "MoveDown();",
        #    transition: [
        #      {"target": "Stopped", "condition": "obj.CurrentLocation == obj.RequeredLocation"},
        #      {"target": "MovingUp", "condition": "obj.CurrentLocation < obj.RequeredLocation"},
        #      {"target": "MovingDown", "condition": "obj.CurrentLocation > obj.RequeredLocation"},
        #      [1, 2, 3, 5]
        #    ]
        #  }

      state
        name: Stopped
        id: 4
        transitions
          transition
            

        



```

#### Sample

```
% $state: AL                         # value
% $zip: 32432                        # value
% $stateZip: ${state}, ${zip}        # list (???)

% :address street city state zip     # declare address type
% :location lat long                 # declare location type
% :building name area address        # declare building type

school
  %:address                         # use address type for the next node
  %. :address                       # (the same?) use address type for the next node (and siblings with the same name and level (???))
  address: "1214 One Way Ave." Mantgomery, ${state} ${zip}
  buildings
    -
      id: 98
      %:location                    # use location type, `location` is a default node name
      - 45.48576 56.54875
    -
      id: 99
      %. lat long                   # folowing object declaration
      %: lat long                   # (the same; delete?) anonymous declaration
      location: 45.48576 56.54875
    -
      id: 95
      %location lat long                   # folowing object declaration
      - 45.48576 56.54875

# mapping node to type
%school/address :address               # node mathes `school/address` has an `address` type
%school/buildings[]/location :location # node mathes `school/buildings[]/location` has a `location` type

school
  address: "1214 One Way Ave." Mantgomery, ${stateZip}
  buildings
    -
      id: 98
      - 45.48576 56.54875             # node name `location' has got from the node match
    -
      id: 99
      location: 45.48576 56.54875
    %:building
    - "Light tower" XX4343 { street: "1214 One Way Ave.", city: Montgomery, state: AL, zip: 32432 }
      id: 95
      location: 45.48544 56.54855
```

#### Sample 2

```
% $state: AL                         # value
% $zip: 32432                        # value
% $stateZip: ${state} ${zip}         # list (???)
% $stateZip: [${state} ${zip}]       # list
% $sz: (state:${state} zip:${zip})   # object

% $address: street city state zip    # declare address type
% $location: lat long                # declare location type
% $building: name area address       # declare building type

school
  %. $address                       # use address type for the next node (and siblings with the same name and level (???))
  address: "1214 One Way Ave." Mantgomery, ${state} ${zip}
  buildings
    -
      id: 98
      %. $location                    # use location type, `location` is a default node name
      - 45.48576 56.54875
    -
      id: 99
      %. lat long                   # folowing object declaration
      %: lat long                   # (the same; delete?) anonymous declaration
      location: 45.48576 56.54875
    -
      id: 95
      %location lat long            # folowing object declaration
      - 45.48576 56.54875

# mapping node to type
%school/address $address               # node mathes `school/address` has an `address` type
%school/buildings[]/location $location # node mathes `school/buildings[]/location` has a `location` type

school
  address: "1214 One Way Ave." Mantgomery, ${stateZip}
  buildings
    -
      id: 98
      - 45.48576 56.54875             # node name `location' has got from the node match
    -
      id: 99
      location: 45.48576 56.54875
    %. $building
    - "Light tower" XX4343 { street: "1214 One Way Ave.", city: Montgomery, state: AL, zip: 32432 }
      id: 95
      location: 45.48544 56.54855
```

#### Sample

```
% school/locations[] street city state zip
% school/locations[location] street city state zip
% school/buildings[building]

school
  %$ city: Montgomery
  name    I.K. High School
  address
    street  1214 One Way Ave.
    city    Montgomery
    state   AL
    zip     32432
  buildings
    building
      address
        street
        ...
      area
      name
    building
      ...

  locations
    - "1212 One Way Ave." Montgomery, AL 32432
    - "1214 One Way Ave." Montgomery, AL 32432
    - "1216 One Way Ave." Montgomery, AL 32432

  buildings
    -
      address
        street  1214 One Way Ave.
        city    Montgomery
        state   AL
        zip     32432
      area XY2323
      name null

    - address: {street: "1214 One Way Ave.", city: ${city}, state: AL, zip: 32432}
      area: XY2323
      name: null

    - address (street: "1214 One Way Ave.", city: Montgomery, state: AL):
        zip 32432
      area XY2323
      name null

%- /school/buildings[] name, area, address
    - "Light tower" XX4343 { street: "1214 One Way Ave.", city: Montgomery, state: AL, zip: 32432 }
    - name: Light tower
      area: XX4343
      address:
        street: "1214 One Way Ave."
        city: Montgomery
        state: AL
        zip: 32432

%!js-comments
%!js-string
%!include config.js
%!separators ->; =>

```

```json
{
  "school": {
    "name": "I.K. High School",
    "address": {
      "street": "1214 One Way Ave.",
      "city": "Main city",
      "state": "AL",
      "zip": "32432"
    },
    "buildings": [
      {
        "address": {},
        "area": 123453,
        "name": null
      }
    ]
      
  }
}

```