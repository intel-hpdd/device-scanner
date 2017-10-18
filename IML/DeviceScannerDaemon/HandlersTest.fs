module IML.DeviceScannerDaemon.HandlersTest

open IML.DeviceScannerDaemon.Handlers
open TestFixtures
open Fable.PowerPack
open Fable.Import.Jest
open Matchers


let private toJson =  Json.ofString >> Result.unwrapResult
let private mapToJson = Map.toArray >> Json.Object

let private addJson = mapToJson addObj
let private changeJson =
  addObj
    |> Map.add "ACTION" (Json.String "change")
    |> mapToJson
let private removeJson = mapToJson removeObj
let private infoJson = toJson """{ "ACTION": "info" }"""

testList "Data Handler" [
  let withSetup f ():unit =
    let ``end`` = Matcher<string option, unit>()

    let handler = dataHandler ``end``.Mock

    f (``end``, handler)

  yield! testFixture withSetup [
    "Should call end with map for info event", fun (``end``, handler) ->
      handler infoJson
      ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{}}");

    "Should call end for add event", fun (``end``, handler) ->
      handler addJson

      ``end`` <?> None;

    "Should call end for add event", fun (``end``, handler) ->
      handler changeJson

      ``end`` <?> None;

    "Should call end for remove event", fun (``end``, handler) ->
      handler removeJson

      ``end`` <?> None;

    "Should end on a bad match", fun (``end``, handler) ->
      expect.assertions 2

      expect.Invoke(fun () -> handler (toJson """{}""")).toThrowErrorMatchingSnapshot()

      ``end`` <?> None

    "Should add then remove a device path", fun (``end``, handler) ->
        expect.assertions(2)

        handler addJson

        handler infoJson

        expect.Invoke(``end``.LastCall).toMatchSnapshot()

        handler removeJson

        handler infoJson

        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{}}");

    "Should call end for remove pool zed event", fun (``end``, handler) ->
       handler destroyZpool
       ``end`` <?> None;

    "Should call end for add pool zed event", fun (``end``, handler) ->
       handler createZpool
       ``end`` <?> None;

    "Should call end for import pool zed event", fun (``end``, handler) ->
       handler importZpool
       ``end`` <?> None;

    "Should call end for export pool zed event", fun (``end``, handler) ->
       handler exportZpool
       ``end`` <?> None;

    "Should call end for remove dataset zed event", fun (``end``, handler) ->
       handler destroyZdataset
       ``end`` <?> None;

    "Should call end for add dataset zed event", fun (``end``, handler) ->
       handler createZdataset
       ``end`` <?> None;

    "Should add then remove a zpool", fun (``end``, handler) ->
        handler createZpool

        handler (toJson """{ "ACTION": "info" }""")

        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"ACTIVE\",\"PATH\":\"testPool1\",\"DATASETS\":{}}}}");

        handler destroyZpool

        handler (toJson """{ "ACTION": "info" }""")

        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{}}");

//    "Should import then export then import a zpool", fun (``end``, handler) ->
//        handler importZpool
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"ACTIVE\",\"PATH\":\"testPool1\",\"DATASETS\":{}}}}")
//
//        handler exportZpool
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"EXPORTED\",\"PATH\":\"testPool1\",\"DATASETS\":{}}}}")
//
//        handler importZpool
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"ACTIVE\",\"PATH\":\"testPool1\",\"DATASETS\":{}}}}");
//
//    "Should add then remove a zdataset", fun (``end``, handler) ->
//        handler createZpool
//        handler createZdataset
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"ACTIVE\",\"PATH\":\"testPool1\",\"DATASETS\":{\"testPool1/home\":{\"POOL_UID\":\"0x2D28F440E514007F\",\"DATASET_NAME\":\"testPool1/home\",\"DATASET_UID\":\"testPool1/home\"}}}}}")
//
//        handler destroyZdataset
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"ACTIVE\",\"PATH\":\"testPool1\",\"DATASETS\":{}}}}");
//
//    "Should export then import zpool with datasets", fun (``end``, handler) ->
//        handler createZpool
//        handler createZdataset
//        handler exportZpool
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"EXPORTED\",\"PATH\":\"testPool1\",\"DATASETS\":{\"testPool1/home\":{\"POOL_UID\":\"0x2D28F440E514007F\",\"DATASET_NAME\":\"testPool1/home\",\"DATASET_UID\":\"testPool1/home\"}}}}}")
//
//        handler importZpool
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"ACTIVE\",\"PATH\":\"testPool1\",\"DATASETS\":{\"testPool1/home\":{\"POOL_UID\":\"0x2D28F440E514007F\",\"DATASET_NAME\":\"testPool1/home\",\"DATASET_UID\":\"testPool1/home\"}}}}}")
//
//        handler destroyZdataset
//        handler exportZpool
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"EXPORTED\",\"PATH\":\"testPool1\",\"DATASETS\":{}}}}")
//
//        handler importZpool
//
//        handler (toJson """{ "ACTION": "info" }""")
//
//        ``end`` <?> Some("{\"BLOCK_DEVICES\":{},\"ZFSPOOLS\":{\"0x2D28F440E514007F\":{\"NAME\":\"testPool1\",\"UID\":\"0x2D28F440E514007F\",\"STATE_STR\":\"ACTIVE\",\"PATH\":\"testPool1\",\"DATASETS\":{\"testPool1/home\":{\"POOL_UID\":\"0x2D28F440E514007F\",\"DATASET_NAME\":\"testPool1/home\",\"DATASET_UID\":\"testPool1/home\"}}}}}");
  ]
]
