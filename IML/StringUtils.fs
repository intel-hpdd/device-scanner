module IML.StringUtils
    let split (x:char []) (s:string) = s.Split(x)
    let trim (y:string) = y.Trim()
    let emptyStrToNone x = if x = "" then None else Some(x)
