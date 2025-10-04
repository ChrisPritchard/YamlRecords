# YamlRecords

![.NET](https://github.com/ChrisPritchard/YamlRecords/actions/workflows/dotnet.yml/badge.svg) [![NuGet](https://img.shields.io/nuget/v/YamlRecords.svg)](https://www.nuget.org/packages/YamlRecords/)

A small script that can deserialize and serialize to YAML from dotnet classes; it supports C# 9 [records with primary constructors](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record), and can also figure out inheritance with some derived type heuristics.

I built this for a Godot game I was working on, where I wanted to use minimal Record type definitions and inheritance. The incumbent dotnet project for YAML is [YamlDotNet](https://github.com/aaubry/YamlDotNet) but at the time of writing (September 2025) that project, while being vastly more sophisticated and tested than this humble script, had not adapted to Records with primary constructors yet (there are workarounds that include adding parameterless constructors to each record, a bit ugly). Additionally it didn't support inheritance very well when deserializing, a perennial issue with serializers. I needed both.

To use, either use the Nuget package (link above), or just copy [YamlRecords.cs](./YamlRecords.cs) into your project somewhere (change namespaces or trim down as needed) - its been built to just use the standard library, no external dependencies.

> **Note**: I built this for my needs, and its possible it won't cover all edge cases - I've tried to make it fairly generic for things like lists and collections, nullable types and enums etc, but don't expect it to be perfect.

## Example of use

> **Note**: All of the below steps are performed in [YamlRecords.Tests.cs](./YamlRecords.Tests.cs); the only file you need for your own projects is [YamlRecords.cs](./YamlRecords.cs) - the Nuget package also excludes all this testing stuff and their dependencies.

Using the record types defined in [YamlRecords.Tests.cs](./YamlRecords.Tests.cs), you can define a structure like this:

```c#
var test = new GameConfig(new()
{
    { "funds", new CardType("Funds", "res://assets/wealth_icon.png") },
    { "health", new CardType("Health", "res://assets/reputation_icon.png") },
},
new()
{
        { "work", new GameFlow("res://assets/authority_icon.png", "choose_path", new() {
            {
                "choose_path", new SocketState(new ()
                {
                    {"default", new StateVariant("Work", "Choose your path to earn funds", "Start", null)},
                    {"labour", new StateVariant("Work", "Physical work for small pay", "Start", new TransitionAction("labour"))}
                }
                , "default", [
                    new SocketConfig("work", ["reason","health","passion"], new() {
                        { "health", new VariantAction("labour") }
                    })
                ])
            },
            {
                "labour", new TimerState(new ()
                {
                    {"default", new StateVariant("Work", "The day stretches long, your hand's burn", "Running...", null)},
                }
                , "default", 60, null, new TransitionAction("choose_path")) // repeat for test
            },
        }) }
});
```

Note that this structure includes abstract types and their derived types, as well as record types using exclusively primary constructors.

You can then serialize this with:

```c#
var yaml = YamlRecords.Serialize(test);
Console.WriteLine(yaml);
```

> Note the namespace is `YamlRecordsSerializer`

Which will produce:

```yaml
cardTypes:
  funds:
    title: Funds
    iconPath: "res://assets/wealth_icon.png"
  health:
    title: Health
    iconPath: "res://assets/reputation_icon.png"
gameFlows:
  work:
    iconPath: "res://assets/authority_icon.png"
    startState: choose_path
    states:
      choose_path:
        sockets:
          - title: work
            accepts:
              - reason
              - health
              - passion
            onAccept:
              health:
                newVariant: labour
        variants:
          default:
            title: Work
            description: Choose your path to earn funds
            actionLabel: Start
            onAction:
          labour:
            title: Work
            description: Physical work for small pay
            actionLabel: Start
            onAction:
              newState: labour
        defaultVariant: default
      labour:
        seconds: 60
        socket:
        onElapsed:
          newState: choose_path
        variants:
          default:
            title: Work
            description: "The day stretches long, your hand's burn"
            actionLabel: Running...
            onAction:
        defaultVariant: default
```

Which you can then deserialize with:

```c#
var test = YamlRecords.Deserialize<GameConfig>(yaml);
```

And have no issues (re-serialize if necessary, to prove that the populated classes will once again generate identical YAML).

## Schemas

The tool can also generate a json schema for a type, complete with support for derived types. For example:

```c#
var schema = YamlRecords.GenerateSchema<GameConfig>()
```

will generate something like:

```json
{
  "type": "object",
  "title": "gameConfig",
  "nullable": true,
  "properties": {
    "cardTypes": {
      "type": "object",
      "additionalProperties": {
        "$ref": "#/defs/CardType"
      }
    },
    "gameFlows": {
      "type": "object",
      "additionalProperties": {
        "$ref": "#/defs/GameFlow"
      }
    },
    "startingCards": {
      "type": "array",
      "items": {
        "type": "string"
      }
    },
    "startingFlows": {
      "type": "array",
      "items": {
        "type": "string"
      }
    }
  },
  "additionalProperties": false,
  "defs": {
    "CardType": {
      "type": "object",
      "title": "cardType",
      "nullable": true,
      "properties": {
        "title": {
          "type": "string"
        },
        "iconPath": {
          "type": "string"
        }
      },
      "additionalProperties": false
    },
    "GameFlow": {
      "type": "object",
      "title": "gameFlow",
      "nullable": true,
      "properties": {
        "iconPath": {
          "type": "string"
        },
        "startState": {
          "type": "string"
        },
        "states": {
          "type": "object",
          "additionalProperties": {
            "$ref": "#/defs/FlowState"
          }
        }
      },
      "additionalProperties": false
    },
    "FlowState": {
      "type": "object",
      "title": "flowState",
      "nullable": true,
      "properties": {
        "variants": {
          "type": "object",
          "additionalProperties": {
            "$ref": "#/defs/StateVariant"
          }
        },
        "defaultVariant": {
          "type": "string"
        }
      },
      "oneOf": [
        {
          "$ref": "#/defs/SocketState"
        },
        {
          "$ref": "#/defs/TimerState"
        },
        {
          "$ref": "#/defs/CardState"
        }
      ]
    },
    "StateVariant": {
      "type": "object",
      "title": "stateVariant",
      "nullable": true,
      "properties": {
        "title": {
          "type": "string"
        },
        "description": {
          "type": "string"
        },
        "actionLabel": {
          "type": "string"
        },
        "onAction": {
          "$ref": "#/defs/StateAction"
        }
      },
      "additionalProperties": false
    },
    "SocketState": {
      "type": "object",
      "title": "socketState",
      "nullable": true,
      "properties": {
        "sockets": {
          "type": "array",
          "items": {
            "$ref": "#/defs/SocketConfig"
          }
        },
        "variants": {
          "type": "object",
          "additionalProperties": {
            "$ref": "#/defs/StateVariant"
          }
        },
        "defaultVariant": {
          "type": "string"
        }
      },
      "additionalProperties": false
    },
    "TimerState": {
      "type": "object",
      "title": "timerState",
      "nullable": true,
      "properties": {
        "seconds": {
          "type": "number"
        },
        "socket": {
          "$ref": "#/defs/SocketConfig"
        },
        "onElapsed": {
          "$ref": "#/defs/StateAction"
        },
        "variants": {
          "type": "object",
          "additionalProperties": {
            "$ref": "#/defs/StateVariant"
          }
        },
        "defaultVariant": {
          "type": "string"
        }
      },
      "additionalProperties": false
    },
    "CardState": {
      "type": "object",
      "title": "cardState",
      "nullable": true,
      "properties": {
        "newCards": {
          "type": "array",
          "items": {
            "type": "string"
          }
        },
        "variants": {
          "type": "object",
          "additionalProperties": {
            "$ref": "#/defs/StateVariant"
          }
        },
        "defaultVariant": {
          "type": "string"
        }
      },
      "additionalProperties": false
    },
    "StateAction": {
      "type": "object",
      "title": "stateAction",
      "nullable": true,
      "oneOf": [
        {
          "$ref": "#/defs/TransitionAction"
        },
        {
          "$ref": "#/defs/VariantAction"
        }
      ]
    },
    "SocketConfig": {
      "type": "object",
      "title": "socketConfig",
      "nullable": true,
      "properties": {
        "title": {
          "type": "string"
        },
        "accepts": {
          "type": "array",
          "items": {
            "type": "string"
          }
        },
        "onAccept": {
          "type": "object",
          "additionalProperties": {
            "$ref": "#/defs/StateAction"
          }
        }
      },
      "additionalProperties": false
    },
    "TransitionAction": {
      "type": "object",
      "title": "transitionAction",
      "nullable": true,
      "properties": {
        "newState": {
          "type": "string"
        }
      },
      "additionalProperties": false
    },
    "VariantAction": {
      "type": "object",
      "title": "variantAction",
      "nullable": true,
      "properties": {
        "newVariant": {
          "type": "string"
        }
      },
      "additionalProperties": false
    }
  }
}
```
