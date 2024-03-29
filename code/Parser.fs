module Parser

open Combinator
open AST

let map, mapImpl = recparser()
let psentence, psentenceImpl = recparser()

let pad =
    fun p ->
        pbetween
            (pws0)
            (p)
            (pws0)

let pstrhelper = pletter <|> pchar ' '

let punknownstr = 
    pbetween
        (pchar '\"')
        (pmany1 (psat (fun c -> c <> '\"')))    
        (pchar '\"') 
        |>> stringify 
    
let pnumber = pmany1 pdigit |>> (fun ds -> stringify ds) |>> int

let ptype = 
    pleft
                (pad (punknownstr)) 
                (pad (pstr "character"))
    |>> (fun t -> Type t) <!> "type"

let pname =
    pright
        (pad (pstr "named"))
            (pad (punknownstr))
    |>> (fun n -> Name n) <!> "name"

let php =
    pright
        (pad (pstr "hp = "))
        pnumber

let pmp =
    pright
        (pad (pstr ", mp = "))
        pnumber

let patk =
    pright
        (pad (pstr ", atk = "))
        pnumber

let pdef =
    pright
        (pad (pstr ", def = "))
        pnumber

let pmatk =
    pright
        (pad (pstr ", matk = "))
        pnumber

let pmdef =
    pright
        (pad (pstr ", mdef = "))
        pnumber

let pspd =
    pright
        (pad (pstr ", spd = "))
        pnumber   

let pstats =
    pright
        (pad (pstr "with stats:"))
        (pbind php (fun h ->
            pbind pmp (fun m ->
                pbind patk (fun a ->
                    pbind pdef (fun d ->
                        pbind pmatk (fun ma ->
                            pbind pmdef (fun md ->
                                pbind pspd (fun s ->
                                    presult { hp = h; mp = m; atk = a; def = d; matk = ma; mdef = md; spd = s }
                                    )
                                )
                            )
                        )
                    )
                )
            )
        )
    |>> (fun s -> s)  <!> "stats"

let peffect =
    pbetween
        (pad (pstr "has effect"))
            (pad (punknownstr))
            ((pstr ",") <|> (pstr ""))
    |>> (fun e -> Effect e) <!> "effect"
    
let pability: Parser<Ability> =
    pbetween
            (pad (pstr "ability"))
            (pseq
                (pname)
                (peffect)
                (fun a ->  {name = fst a; effect = snd a})  <!> "ability"
            )
            ((pstr ",") <|> (pstr ""))

let pabilities: Parser<Abilities> =
    pright
        (pad (pstr "and abilities:"))
        (pmany0 pability)
    |>> (fun ps -> ps) <!> "abilities"

// Character of Type * Name * Stats * Abilities
let character = 
    pright
        (pws0)
        (pbind ptype (fun t ->
            pbind pname (fun n ->
                    (pbind pstats (fun s ->
                        pbind pabilities (fun a ->
                            presult { typep = t; name = n; stats = s; abilities = a; }
                            )
                        )
                    )
                )
            )  
        )
    |>> (fun c -> Character c) <!> "character"

let pdescriptor = 
    pright
        (pad (pstr "with description:"))
        (pad (punknownstr))
    |>> (fun d -> Descriptor d) <!> "descriptor"

let pobjects: Parser<Sentence list> =
    pright
        (pad (pstr "with objects:"))
        (pmany0 psentence)  
    |>> (fun os -> os) <!> "objects"

let pconnection =
    pseq
        (pleft 
            (pad (punknownstr))
            (pad (pstr "is"))
        )
        (pleft 
            (pad (punknownstr))
        (pstr "," <|> pstr ".")
        )
        (fun c -> Connection c) <!> "connection"

let pconnections: Parser<Connections> =
    pright
        (pad (pstr "and connections:"))
        (pmany0 pconnection)
    |>> (fun cs -> cs) <!> "connections"

let room: Parser<Room> =
    pbetween
        (pws0)
        (pright 
            (pad (pstr "There is a room"))
            (pbind pname (fun n ->
                pbind pdescriptor (fun d->
                    pbind pobjects (fun os ->
                        pbind pconnections (fun c ->
                            presult { name = n; descriptor = d; objects = os; connections = c} 
                            )
                        )
                    )
                )
            )
        )
        (pstr "")
    |>> (fun r -> r) <!> "room"

let pinteraction =
    pbetween
            (pad (pstr "interaction"))
            (pseq
                (pname)
                (peffect)
                (fun a ->  {name = fst a; effect = snd a})  <!> "interaction"
            )
            ((pstr ",") <|> (pstr ""))

let pinteractions =
    pright
        (pad (pstr "and interactions:"))
        (pmany0 pinteraction)
    |>> (fun is -> is) <!> "interactions"

let object =
    pbetween
        (pws0)
        (pright
            (pad (pstr "object"))
            (pbind pname (fun n ->
                pbind pdescriptor (fun d ->
                    pbind pinteractions (fun i ->
                        presult { name = n; descriptor = d; interactions = i; }
                        )
                    )
                )
            )
        )
        (pstr "")
    |>> (fun o -> Object o)

psentenceImpl := 
    pbetween
        (pad (pstr "("))
        (character <|> object) 
        ((pad (pstr "),")) <|> (pad (pstr ")")) )

mapImpl := pmany1 room |>> (fun (ss) -> ss)
let grammar = pleft map peof

let parse =
    fun input ->
        let i = prepare input
        match grammar i with
        | Success(ast, _) -> 
            Some ast
        | Failure(p, r) -> 
            printfn "%s" r
            printfn "Error located at pos %d" p
            let code, _, _ =  i
            printfn "%s" code[0..p]
            None