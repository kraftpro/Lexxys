# Configuration TXT

## Grammar

### Document

```bnfc
document          := node+
node              := declaration | name [":"] node-value [subnodes]
subnodes          := NEWLINE INDENT nodes DEDENT
nodes             := ( node | json-value )+
node-value        := json | eol-text
json-value        := json-array | json-obj | attr-list
json-array        := "[" array-item [ ","? array-item ]* "]"
json-item         := json-value | name
json-obj          := "{" obj-pair [ ","? obj-pair ]* "}"
obj-pair          := name (":" | "=") json-item
```

### Declaration

```bnfc
declaration       := type-declaration
                   | var-declaration
                   | include-def
                   | object-definition
type-declaration  := "%" type-name name-list      # Type declaration
var-definition    := "%" var-name ":" value       # Variable declaration
include-def       := "%" "%" file-name            # Include
object-definition := "%" pattern ( type-name | name-list )

type-name         := ":".name
var-name          := "$".name

var-reference     := "${{" name [ "|" default-value ] "}}"   # Reference to the variable
```


### Elements

```bnfc
comments          := ( SPACE | BOL ) "#" TEXT NEWLINE
                   | ( SPACE | BOL ) "#<" ML_TEXT ">#"
name-list         := name ( delimiter name )*
name              := name_char+
                   | string
name-char         := !special-char | escape-shar special-char
special-char      := escape-char | "=" | ":" | ";" | "," | "[" | "]" | "{" | "}" | "(" | ")" | "/" | "\" | "'" | """ | SPACE
delimiter         := SPC | "," | ";"
escape-char       := "`"
string            := """ ( string-char | "'" | escape-seq )* """
                   | "'" ( string-char | """ | escape-seq )* '"'
string-char       := !( "\r" | "\n" | escape-char | "'" | '"' )
escape-seq        := escape-char ( "r" | "n" | "t" | "f" | "v" | "a" | "b" | "0" | "`" | "h" HEX HEX | "x" HEX HEX (HEX HEX)? | "u" HEX HEX HEX HEX )

# todo: add unicode
TEXT              := /[^\r\n]*/
ML_TEXT           := /[.\r\n]*/
SPACE             := /[ \t]/
SP                := /[ \t]*/
SPC               := /[ \t]+/
NEWLINE           := /\r?\n/
BOL               := /^/
```

```bnfc
pattern           := ( ( wildcard | name-part ) "/" )* name-part
name-part         := name aray-mark?
wildcard          := '*'      # matches any letter except path separator
                   | '**'     # matches any letter including path separator
array-mark        := json-array-mark | xml-array-mark
json-array-mark   := "[" "]"
xml-array-mark    := "[" name "]"   # defines a name of node to be repeated (by default "item")
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