module Evaluator

open AST
open GameEngine

let initGS =
    fun cE oE mE ->
        { characters = cE;
          rooms = mE; 
          objects = oE; 
          turn = 0;
          mcL = "";
          alive = true; }

let rec evalObjects =
    fun (ss: Sentence list) (charE: charEnv) (objE: objEnv) (mapE: mapEnv) ->
        match ss with
        | [] ->
            initGS charE objE mapE
        | s::ss ->
            match s with
            | Character c->
                let newEnv: Map<string,Character> = charE.Add (c.name, c)
                evalObjects ss newEnv objE mapE
            | Object o ->
                let newEnv = objE.Add (o.name, o)
                evalObjects ss charE objE mapE

let evalMap =
    fun (mapE: mapEnv) ->
        let charEnv = Map.empty
        let objEnv = Map.empty
        let values = mapE |> Map.values |> Seq.cast |> List.ofSeq
        let objects = List.map (fun (r: Room) -> r.objects) values |> List.concat
        evalObjects objects charEnv objEnv mapE

let rec evalParagraph = 
    fun rs (mapE: mapEnv)  ->
        match rs with 
        | [] -> 
            playGame (evalMap mapE)
        | r::rs -> 
            let newEnv = mapE.Add (r.name, r)
            evalParagraph rs newEnv