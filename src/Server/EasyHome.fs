namespace EasyHome

open System
open System.IO
open System.Diagnostics
open System.Threading

open Switches // On/Off, Sw1..SwAll, Toggle

module EasyHome = 
    module private SwitchImpl = 
        let switchStreams =
            Map [
                Sw1,   (["e83fac";"e2f36c";"ef881c";"e00b5c"],["e6567c";"eb702c";"e9248c";"e341bc"])
                Sw2,   (["e19905";"e4aa35";"eec5f5";"ed6ee5"],["ece795";"ea12d5";"e5dd45";"e7bcc5"])
                Sw3,   (["e6567e";"eb702e";"e9248e";"e341be"],["ef881e";"e00b5e";"e83fae";"e2f36e"])
                Sw4,   (["ece797";"ea12d7";"e5dd47";"e7bcc7"],["e19907";"e4aa37";"eec5f7";"ed6ee7"])
                SwAll, (["e65672";"eb7022";"e92482";"e341b2"],["ef8812";"e00b52";"e83fa2";"e2f362"])
            ]

        let pin =
            if not (File.Exists("/sys/class/gpio/gpio14/value"))
            then
                File.WriteAllText("/sys/class/gpio/export","14", Text.Encoding.ASCII)
                File.WriteAllText("/sys/class/gpio/gpio14/direction","out", Text.Encoding.ASCII)
            let file = File.OpenWrite "/sys/class/gpio/gpio14/value"
            let toggleFn (toState:OnOff) =
                match toState with On -> '1' | Off -> '0'
                    |> byte
                    |> file.WriteByte
                file.Flush()
            toggleFn

        let sleep us =
            let sw = Stopwatch.StartNew()
            let mutable op = 0
            let waitTicks = Math.Round((float (int64 us * Stopwatch.Frequency) / 1000000.0)) |> int64
            if us > 1500 then
                let ms = (us - 500) / 1000
                Thread.Sleep ms
            while sw.ElapsedTicks < waitTicks do
                op <- op + 1
            sw.Stop()

        let txRf433 (data:string) =
            let tick = 377 // microseconds
            let n = Int32.Parse(data, Globalization.NumberStyles.HexNumber)
            let bits =
                [1..24]
                |> Seq.fold (fun (n,res) _ -> n/2,n%2::res) (n,[]) 
                |> fun (_,lst) -> lst
                |> List.map (function 0 -> 1,3 | 1 -> 3,1 | _ -> failwith "only 0 or 1 expected")
            let tickstream =
                [[0,362];
                 [1,6];bits; [1,6];bits; [1,6];bits; [1,6];bits;   // switch
                 [8,19];bits;[8,19];bits;[8,19];bits;[8,19];bits // (programming)
                 [500,0]]
                |> Seq.concat

            let txBit (a,b) = 
                a*tick |> sleep
                Off |> pin
                b*tick |> sleep
                On |> pin

            let savedPriority = Thread.CurrentThread.Priority
            try
                Thread.CurrentThread.Priority  <- ThreadPriority.AboveNormal
                tickstream |> Seq.iter txBit
            finally
                Thread.CurrentThread.Priority <- savedPriority
                Off |> pin

        let toggle sw state =
            GC.Collect()
            GC.WaitForPendingFinalizers()
            let (on,off) = switchStreams.[sw]
            let onoffstream = match state with | On -> on | Off -> off

            //todo: run this in a separate thread
            onoffstream |> Seq.iter (txRf433 >> (fun _ -> Thread.Sleep(10)))

    let switches = MailboxProcessor.Start(fun inbox-> 
        // the message processing function

        let handle cmd =
            printfn "> %A" cmd
            // process a message
            match cmd with
            | switch,state -> SwitchImpl.toggle switch state

        let rec messageLoop() = async {
            let! cmd = inbox.Receive()
            handle cmd
            // loop to top
            return! messageLoop()
        }

        // initialize and start the loop
        async {
            // make sure we're uptime more than 30 seconds
            // in case of power outage, this will prevent switches to reprogram
            let uptime =
                let upstr = File.ReadAllText("/proc/uptime").Split().[0]
                Single.Parse(upstr) |> int
            if uptime < 30 then
                printfn "Waiting for 30 seconds uptime"
                do! Async.Sleep((30 - uptime) * 1000)

            do! Async.SwitchToNewThread()
            return! messageLoop()
        }
    )
