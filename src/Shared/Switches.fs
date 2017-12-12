
namespace Switches

type Channel = Sw1 | Sw2 | Sw3 | Sw4 | SwAll
type State = On | Off
type Toggle = Channel * State
type SwitchMode = Manual | Auto | Presence

type Switch = {
    channel: Channel
    state: State
    name: string
    mode: SwitchMode
  } 

type Switches = Switch list
