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
  scannerState2: string;
  scannerState3: string;
  scannerStateDatasets: string;
  scannerStateDatasets2: string;
  legacyZFSPool: string;
  legacyZFSDataset: string;
  legacyBlockDev: string;
}

let fixtures:Fixtures = importAll "./fixtures.js"
