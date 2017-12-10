namespace Shared

type Counter = int

type Init =
  { getCounter : unit -> Async<Counter> }
