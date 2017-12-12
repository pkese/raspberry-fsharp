
let fpath = "data/gfile006.data"

let fnames = [1..16] |> List.map (sprintf "data/gfile%03d.data")

let data = System.IO.File.ReadAllBytes fpath
printfn "avg = %d" ((data |> Seq.sumBy int) / data.Length)
printfn "len: %d, pairwise: %d" (data.Length) (data |> Seq.pairwise |> Seq.length)
printfn "%A" (data |> Seq.skip 9666 |> Seq.take 20 |> Seq.toList)

let sqr x = x * x

(*
detect signal and record each sample to separate file:

    /usr/local/bin/rtl_433 -g 20 -a -t -s 44100
    
 sample rate =  at 250 KHz
*)
let analyze (data: byte[]) = 
    data
    |> Seq.map (fun s -> int s - 128)
    |> Seq.chunkBySize 2
    |> Seq.map (fun chunk -> (chunk |> Seq.sumBy sqr) > 10000) // I/Q -> amplitudes + threshold
    |> Seq.scan (fun (last,_) p -> p,(last<>p)) (true,false) // flip detection
    |> Seq.map snd
    |> Seq.mapi (fun i diff -> if diff then i else -1) // -> change offsets
    |> Seq.filter (fun min -> min>1)
    |> Seq.scan (fun (last,_) p -> p,p-last) (0,0)
    |> Seq.map snd
    |> Seq.chunkBySize 2
    |> Seq.map (fun chunk ->
        let a,b =
            chunk |> function
            | [|a;b|] -> (a,b)
            | [|a|] -> (a,0)
            | _ -> failwith "unexpected"
        let scale x = (x+48) / 96 // (x+7) / 17 // (x+52) / 105
        //match a,b with
        match (scale a), (scale b) with
            | (1,3) -> "0"
            | (1,0) -> "0\n"
            | (3,1) -> "1"
            | (3,0) -> "1\n"
            | (0,0) -> "?"
            //| (_,_) -> sprintf "\n (%3d+%4d=%4d) " a b (a+b))
            | (a,b) -> sprintf "\n (%2d,%2d) " a b)
    |> Seq.append ["\n"]

let extractCode s = 
    s
    |> Seq.choose (function "0" -> Some 0 | "1" -> Some 1 | _ -> None)
    |> Seq.chunkBySize 4
    |> Seq.map (Seq.fold (fun n d -> n * 2 + d) 0) // to hex
    |> Seq.chunkBySize 6
    //|> Seq.map (Seq.fold (fun n d -> n * 16 + d) 0 >> sprintf "%x\n" )
    |> Seq.map (function [|a;b;c;d;e;f|] -> sprintf "%x %x%x%x%x %x\n" a b c d e f | _ -> "??" )
    |> Seq.skip 2
    |> Seq.take 1

"data/gfile002.data"
    |> System.IO.File.ReadAllBytes
    |> analyze
    //|> extractCode
    |> Seq.iter (printf "%s")

fnames
    |> Seq.map System.IO.File.ReadAllBytes
    |> Seq.iter (analyze >> extractCode >> Seq.iter (printf "%s"))


