namespace Shared

type Channel = int

type Led = {
  channel: Channel
  color: string
  trigger: string
  triggers: string list
}

type Leds = Led list

type Command = // sent from client to server
  | SetTrigger of Channel * string

type Notification = // server event notification
  | Leds of Leds

