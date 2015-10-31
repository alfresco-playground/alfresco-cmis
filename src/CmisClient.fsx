
#r "../library/DotCmis.dll"

open DotCMIS
open DotCMIS.Client
open DotCMIS.Client.Impl
open DotCMIS.Data.Impl
open DotCMIS.Data.Extensions
open DotCMIS.Enums
open System.IO
open System
open System.Linq
open System.Collections.Generic

type Config = {
    UserName: string
    Password: string
    AtomPubUrl: string
}

type Document = {
    Name: string
    Title: string
    Description: string
    Content: byte array
    ContentType: string
}

type Folder = {
    Name: string
    Title: string
    Description: string
}

[<AutoOpen>]
module Default =
    let defaultConfig = {
        UserName = "admin"
        Password = "admin"
        AtomPubUrl = "http://127.0.0.1:8081/alfresco/api/-default-/public/cmis/versions/1.0/atom" }

    let createSession config =
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
        (session)

type Client(config: Config) =

    let session = createSession defaultConfig

    member this.CreateDescription description =
        let data = new ExtensionsData()
        data.Extensions <- List()

        let property = CmisExtensionElement()
        property.Name <- "cmis:propertyString"
        property.Namespace <- "http://docs.oasis-open.org/ns/cmis/core/200908/"
        property.Attributes <-
            dict[
                ("propertyDefinitionId", "cm:description")
                ("displayName", "Description")
                ("queryName", "cm:description")
            ] |> Dictionary<String,String>
        property.Children <- [
            CmisExtensionElement(
                Name = "cmis:value",
                Namespace = "http://docs.oasis-open.org/ns/cmis/core/200908/",
                Value = description )
        ].ToList().Cast<ICmisExtensionElement>().ToList()

        let aspect = CmisExtensionElement()
        aspect.Name <- "P:cm:titled"
        aspect.Namespace <- "http://www.alfresco.org"
        aspect.Children <- [
                CmisExtensionElement(
                    Name = "alf:properties",
                    Namespace = "http://www.alfresco.org",
                    Children = [ property ].ToList().Cast<ICmisExtensionElement>().ToList() )
            ].ToList().Cast<ICmisExtensionElement>().ToList()

        data.Extensions.Add aspect
        (data)

    member this.CreateExtension(doc: Document) =
        let child = List<CmisExtensionElement>()
        let add el = child.Add el
        add <| this.CreateElement "cm:title" PropertyType.String doc.Title
        add <| this.CreateElement "cm:description" PropertyType.String doc.Description

        let el = CmisExtensionElement()
        el.Name <- "P:cm:titled"
        el.Namespace <- "http://www.alfresco.org"
        el.Children <- child.Cast<ICmisExtensionElement>().ToList()

        let aspect = new CmisExtensionElement()
        aspect.Name <- "alf:setAspects"
        aspect.Namespace <- "http://www.alfresco.com"
        aspect.Children <- [el].ToList().Cast<ICmisExtensionElement>().ToList()

        let data = ExtensionsData()
        data.Extensions <- [].ToList()
        data.Extensions.Add aspect
        (data)

    member this.CreateElement (name: string) (propertyType :PropertyType) (value: string) =
        let ext = CmisExtensionElement()
        ext.Name <- String.Format("cmis:property{0}", Enum.GetName(typeof<PropertyType>, propertyType))
        ext.Namespace <- "http://docs.oasis-open.org/ns/cmis/core/200908"
        ext.Attributes <-
            dict [
                ("propertyDefinition", name)
                ("displayName", "")
                ("queryName", name)
            ]
        ext.Children <- [
            CmisExtensionElement(Name = "cmis:value", Value = value)
        ].ToList().Cast<ICmisExtensionElement>().ToList()
        (ext)

    member this.InsertDocument(doc: Document, folderId: string) =
        let parentFolder = session.GetObject(folderId) :?> IFolder
        let property = System.Collections.Generic.Dictionary<string,obj>()
        let add (key:string) (value) = property.Add(key, value)
        add PropertyIds.Name doc.Name
        add PropertyIds.ObjectTypeId "cmis:document"
        //add "cm:title" doc.Title
        //add "cm:description" doc.Description

        let stream = ContentStream()
        stream.FileName <- doc.Name
        stream.MimeType <- doc.ContentType
        stream.Length <- doc.Content.Length |> int64 |> Nullable
        stream.Stream <- new MemoryStream(doc.Content)

        let cmisDoc = parentFolder.CreateDocument(property, stream, VersioningState.Major |> Nullable)

        let extension = DotCMIS.Data.Impl.Properties()
        extension.Extensions <- this.CreateDescription("Hello, world").Extensions

        let documentId = ref cmisDoc.Id
        let repositoryId = session.RepositoryInfo.Id
        let checkToken = ref ""

        session.Binding.GetObjectService().UpdateProperties(repositoryId, documentId, checkToken, extension, null)
        (cmisDoc)

[<EntryPoint>]
let go args =
    printfn "|| params -> %A" args

    let document = {
        Name = args.[1]
        Title = "Hello, World!"
        Description= "Hello, World!"
        ContentType = "application/pdf"
        Content = File.ReadAllBytes("resource/limiting.pdf") }

    let client = Client(defaultConfig)
    let rs = client.InsertDocument(document, args.[0])
    0






