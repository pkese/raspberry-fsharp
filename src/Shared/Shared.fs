namespace Shared

open Switches

type Commands = 
  | GetSwitches
  | SetSwitch of Channel*OnOff
  | SetMode of Channel*SwitchMode

type Events =
  | Switches of Switches

type SwApp = {
    getSwitches: unit -> Async<Switches>
    setSwitch: (Channel*OnOff) -> Async<Switches>
    setMode: (Channel*SwitchMode) -> Async<Switches>
  }
