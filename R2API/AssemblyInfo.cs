using System.Security;
using System.Security.Permissions;

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
