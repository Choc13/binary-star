open System
open System.Collections.Generic
open FSharp.Control
open FSharpPlus
open Microsoft.Azure.Cosmos
open Microsoft.Azure.Cosmos.Linq

let source = new CosmosClient("")
let target = new CosmosClient("", CosmosClientOptions(AllowBulkExecution=true))

let getAll (container: Container) =

    let query = container.GetItemLinqQueryable<Dictionary<string, Object>>()

    use feedIterator = query.ToFeedIterator()

    asyncSeq {
        while (feedIterator.HasMoreResults) do
            let! response = feedIterator.ReadNextAsync() |> Async.AwaitTask
            yield! response |> AsyncSeq.ofSeq
    }

let write (container: Container) (item: Dictionary<string, Object>) = 
    let requestOptions =
        ItemRequestOptions(EnableContentResponseOnWrite = false)
    container.UpsertItemAsync(item, item.["PartitionKey"].ToString() |> PartitionKey, requestOptions) |> Async.AwaitTask |> Async.Ignore

[<EntryPoint>]
let main argv =
    let sourceContainer = source.GetDatabase("Beacon").GetContainer("Beacon")
    let targetContainer = target.GetDatabase("Beacon").GetContainer("Beacon")
    let docs = getAll sourceContainer
    docs |> AsyncSeq.iterAsync (write targetContainer) |> Async.RunSynchronously
        
    0