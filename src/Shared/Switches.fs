
namespace Switches

type Channel = Sw1 | Sw2 | Sw3 | Sw4 | SwAll
type OnOff = Off | On
type Toggle = Channel * OnOff
type SwitchMode = Manual | Auto | Presence

type Switch = {
    channel: Channel
    state: OnOff
    name: string
    mode: SwitchMode
  } 

type Switches = Switch list
