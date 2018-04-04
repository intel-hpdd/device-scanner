module Fixtures

open Fable.Core.JsInterop

type Fixtures = {
  add: string;
  change: string;
  addDm: string;
  addMdRaid: string;
  remove: string;
  pool: string;
  mount: string;
}

let fixtures:Fixtures = importAll "./fixtures.js"
