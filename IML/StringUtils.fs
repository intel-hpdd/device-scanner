module IML.StringUtils
    let concat (x:string) y = x + y
    let split (x:char []) (s:string) = s.Split(x)
    let trim (y:string) = y.Trim()
    let startsWith (x:string) y = x.StartsWith(y)
    let emptyStrToNone x = if x = "" then None else Some(x)
