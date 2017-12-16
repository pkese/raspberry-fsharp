namespace Shared

type Channel = int

type Led = {
  channel: Channel
  color: string
  trigger: string
  triggers: string list
}

type Leds = Led list

type Command = // browsers sends commands to Raspberry
  | SetTrigger of Channel * string

type Notification = // Raspberry sends notifications to all connected browsers
  | Leds of Leds

