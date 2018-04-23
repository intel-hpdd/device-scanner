module IML.Types.Fixtures

open Fable.Core.JsInterop

type Fixtures = {
  add: string;
  change: string;
  addDm: string;
  addMdRaid: string;
  remove: string;
  pool: string;
  pools: string;
  mount: string;
  mounts: string;
  scannerState: string;
  legacyZFSPool: string;
  legacyZFSDataset: string;
  legacyBlockDev: string;
}

let fixtures:Fixtures = importAll "./fixtures.js"
