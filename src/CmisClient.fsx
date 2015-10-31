
#r "../library/DotCmis.dll"

open DotCMIS
open DotCMIS.Client
open DotCMIS.Client.Impl
open DotCMIS.Data.Impl
open DotCMIS.Enums
open System.IO
open System

type Config = {
    UserName: string
    Password: string
    AtomPubUrl: string
}

type Document = {
        Title: string
        Content: byte array
}

type Client(config: Config) =
    let parameters = System.Collections.Generic.Dictionary<string,string>()
    do
        let add (key:string) (value:string) = parameters.Add(key, value)
        add SessionParameter.BindingType BindingType.AtomPub
        add SessionParameter.AtomPubUrl config.AtomPubUrl
        add SessionParameter.User config.UserName
        add SessionParameter.Password config.Password
        add SessionParameter.Compression  bool.TrueString

    let factory = SessionFactory.NewInstance()
    let session = factory.GetRepositories(parameters).[0].CreateSession()

    member this.InsertDocument(doc: Document, folderId: string) =

        let parentFolder = session.GetObject(folderId) :?> IFolder
        let property = System.Collections.Generic.Dictionary<string,obj>()
        let add (key:string) (value) = property.Add(key, value)
        add PropertyIds.Name doc.Title
        add PropertyIds.ObjectTypeId "cmis:document"

        let stream = ContentStream()
        stream.FileName <- sprintf "%s.pdf" doc.Title
        stream.MimeType <- "application/pdf"
        stream.Length <- doc.Content.Length |> int64 |> Nullable
        stream.Stream <- new MemoryStream(doc.Content)

        let cmisDoc = parentFolder.CreateDocument(property, stream, VersioningState.Major |> Nullable)
        (cmisDoc)

[<EntryPoint>]
let go args =

    printfn "|| params -> %A" args

    let config = {
        UserName = "admin"
        Password = "admin"
        AtomPubUrl = "http://127.0.0.1:8081/alfresco/api/-default-/public/cmis/versions/1.0/atom" }
    let document = {
        Title = "HelloWorld"
        Content = File.ReadAllBytes("resource/limiting.pdf") }

    let client = Client(config)
    let rs = client.InsertDocument(document, args.[0])
    0






