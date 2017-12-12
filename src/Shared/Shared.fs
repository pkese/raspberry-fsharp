namespace Shared

open Switches

type SwApp = {
    getSwitches: unit -> Async<Switches>
    setSwitch: (Channel*State) -> Async<Switches>
    setMode: (Channel*SwitchMode) -> Async<Switches>
  }
