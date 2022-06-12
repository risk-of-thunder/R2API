using System.Security;
using System.Security.Permissions;
using IVT = System.Runtime.CompilerServices.InternalsVisibleToAttribute;

// SecurityPermision set to minimum and SkipVerification set to true
// for skipping access modifiers check from the mono JIT
// The same attributes are added to the assembly when ticking
// Unsafe Code in the Project settings
// This is done here to allow an explanation of the trick and
// not in an outside source you could potentially miss.

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]

//This allows us to use the SystemInitializer attribute
//the attribute will call whatever method it's attached to once ror2 starts loading.
//We can add dependencies to the SystemInitializer attribute, an example would be run
//a piece of code that gets automatically ran once the ItemCatalog is initialized
[assembly: HG.Reflection.SearchableAttribute.OptIn]

[assembly: IVT("R2API.Items")]
[assembly: IVT("R2API.ContentManagement")]
[assembly: IVT("R2API.ArtifactCode")]
[assembly: IVT("R2API.Difficulty")]
[assembly: IVT("R2API.Elites")]
[assembly: IVT("R2API.RecalculateStats")]
[assembly: IVT("R2API.Prefab")]
