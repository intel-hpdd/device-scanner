module IML.StringUtils
    let startsWith (x:string) (y:string) = y.StartsWith(x)
    let endsWith (x:string) (y:string) = y.EndsWith(x)
    let split (x:char []) (s:string) = s.Split(x)
    let trim (y:string) = y.Trim()
    let emptyStrToNone x = if x = "" then None else Some(x)
