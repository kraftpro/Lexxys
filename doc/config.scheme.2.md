# Statechart

### DSL
```DSL

statechart ElevatorSystem {

  initial state Idle

  state Idle {
    on CALL_UP -> MovingUp
    on CALL_DOWN -> MovingDown
    on MAINTENANCE_ACTIVATED -> Maintenance
    on EMERGENCY -> Emergency
  }

  state MovingUp {
    on FLOOR_REACHED -> DoorOpening
    on EMERGENCY -> Emergency
    on MAINTENANCE_ACTIVATED -> Maintenance
  }

  state MovingDown {
    on FLOOR_REACHED -> DoorOpening
    on EMERGENCY -> Emergency
    on MAINTENANCE_ACTIVATED -> Maintenance
  }

  state DoorOpening {
    on DOOR_OPENED -> DoorOpened
    on EMERGENCY -> Emergency
  }

  state DoorOpened {
    on DOOR_TIMEOUT -> DoorClosing
    on EMERGENCY -> Emergency
  }

  state DoorClosing {
    on DOOR_CLOSED {
      if pending_requests_above -> MovingUp
      if pending_requests_below -> MovingDown
      else -> Idle
    }
    on EMERGENCY -> Emergency
  }

  state Maintenance {
    on MAINTENANCE_DEACTIVATED -> Idle
    on EMERGENCY -> Emergency
  }

  state Emergency {
    on EMERGENCY_CLEARED -> Idle
  }
}


```

### YCL

```config.txt

%separators -> =>     # define '->' and '=>' as separators along with ',' and ';'
%type: StateChart     # set type of the next node

statechart ElevatorSystem

  # initial state Idle
  initial-state Idle

  state Idle
    on CALL_UP -> MovingUp
    on CALL_DOWN -> MovingDown
    on MAINTENANCE_ACTIVATED -> Maintenance
    on EMERGENCY -> Emergency

  state MovingUp
    on FLOOR_REACHED -> DoorOpening
    on EMERGENCY -> Emergency
    on MAINTENANCE_ACTIVATED -> Maintenance

  state MovingDown
    on FLOOR_REACHED -> DoorOpening
    on EMERGENCY -> Emergency
    on MAINTENANCE_ACTIVATED -> Maintenance

  state DoorOpening
    on DOOR_OPENED -> DoorOpened
    on EMERGENCY -> Emergency

  state DoorOpened
    on DOOR_TIMEOUT -> DoorClosing
    on EMERGENCY -> Emergency

  state DoorClosing
    on DOOR_CLOSED
      if pending_requests_above -> MovingUp
      if pending_requests_below -> MovingDown
      else -> Idle
    on EMERGENCY -> Emergency

  state DoorClosing
    on DOOR_CLOSED -> MovingUp
      if pending_requests_above
    on DOOR_CLOSED -> MovingDown
      if pending_requests_below
    on DOOR_CLOSED -> Idle
      otherwise
    on EMERGENCY -> Emergency

  state Maintenance
    on MAINTENANCE_DEACTIVATED -> Idle
    on EMERGENCY -> Emergency

  state Emergency
    on EMERGENCY_CLEARED -> Idle

# end statechart ElevatorSystem

```

### JSON

```json

statechart: {
  name: "ElevatorSystem",
  state: [
    {
      name: "Idle",
      on: [
        {
          event: "CALL_UP",
          state: "MovingUp"
        },
        {
          event: "CALL_DOWN",
          state: "MovingDown"
        }
      ]
      ...
    },
    {
      name: "DoorClosing",
      on: [
        {
          event: "DOOR_CLOSED",
          [
            if: [
              {
                condition: "pending_requests_above",
                event: MovingUp"
              },
              {
                condition: "pending_requests_below",
                event: MovingDown"
              }
            ],
            else: {
              state: "Idle"
            }
          ]
        },
        {
          event: "EMERGENCY",
          state: "Emergency"
        }
      ]
    }
    ...
  ]
}

```

### Connection String

```config.txt
%parser ConnectionString
connection
  server: db2.dev.contoso.com
  database: School
  user: sa
  password: Pa$$w0rd
  timeout: 10
  multiSubnetFailover: yes
```

### Type Definition

```config.txt

# type definition by the root name "statechart"

# defines types statechart, statechart/state, statechart/state/on ...

#:statechart name initial-state
#:statechart/state name
#:statechart/state/on event state
#:statechart/state/on/if condition state
#:statechart/state/on/else state

# use special symbol to reference up level

#:statechart name initial-state
#:./state name
#:././on event state
#:./././if condition state
#:./././else state

---

# use variable declaration. use braces to combine map and value  
%$StateChart: (name, initial-state              # root node has positional parameters name and initial-state
  state: (name,                                 # subnode state - name
    on: (event, state,                          # subnode state/on - event, state
      if: (condition, state)                    # subnode state/on/if - event, state
      else: (state)                             # subnode state/on/else - event
    )
  )
)

#type: $statechart



```

### Parser Definition

```csharp

class ConfigParser
{
  private string? _name;

  public virtual string Name => _name ?? GetName();

  public virtual Type Type => typeof(object);

  public abstract bool TryParse(ConfigNode node, out object? value);

  private string GetName()
  {
    string typeName = GetType().Name;
    return typeName.EndsWith("Parser") ? typeName[..^6]: typeName;
  }
}

class ConnectionStringParser: ConfigParser
{
  public override Type Type => typeof(ConnectionString);

  public virtual bool TryParse(ConfigNode node, out object? value)
  {

  }
}

configFactory.AddParser(new ConnectionStringParser());

```
