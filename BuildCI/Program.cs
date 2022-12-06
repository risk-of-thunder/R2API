// We do two things in there:
// - Update all toml files that reference another local toml package versionNumber in their respective package.dependencies toml array
//   so that its always matching with the versionNumber from the referenced local toml package.
// - Constructs a dependency tree so that we know which packages to upload first to the website.

using Nett;

const string ThunderstoreTomlFileName = "thunderstore.toml";

const string PackageTomlKey = "package";
const string NamespaceTomlKey = "namespace";
const string NameTomlKey = "name";
const string VersionNumberTomlKey = "versionNumber";
const string DependencyTomlKey = "dependencies";

var packages = new Dictionary<string, R2APIPackage>();

var currentFolder = Directory.GetCurrentDirectory();
while (true)
{
    var hasSolutionFile = Directory.GetFiles(currentFolder, "*.sln", SearchOption.TopDirectoryOnly).Length == 1;
    if (hasSolutionFile)
        break;

    var parentFolder = Directory.GetParent(currentFolder);
    if (parentFolder == null || !parentFolder.Exists)
    {
        Console.WriteLine("Could not find a solution file.");
        return;
    }

    currentFolder = parentFolder.FullName;
}

var tomlFiles = Directory.GetFiles(currentFolder, ThunderstoreTomlFileName, SearchOption.AllDirectories);
foreach (var tomlFile in tomlFiles)
{
    var parsedToml = Toml.ReadFile(tomlFile);
    var packageTomlTable = parsedToml.Get<TomlTable>(PackageTomlKey);

    var package = new R2APIPackage(packageTomlTable.Get<string>(NamespaceTomlKey), packageTomlTable.Get<string>(NameTomlKey),
        packageTomlTable.Get<string>(VersionNumberTomlKey),
        Directory.GetParent(tomlFile)!.FullName,
        packageTomlTable.ContainsKey(DependencyTomlKey) ? packageTomlTable.Get<TomlTable>(DependencyTomlKey) : null);

    var dictKey = packageTomlTable.Get<string>(NamespaceTomlKey) + packageTomlTable.Get<string>(NameTomlKey);
    packages.Add(dictKey, package);
}

foreach (var (_, package) in packages)
{
    package.InitFullDependencyReferences(packages);
}

// Update the package.dependencies array versioNumbers of the toml files
foreach (var tomlFile in tomlFiles)
{
    var parsedToml = Toml.ReadFile(tomlFile);
    var packageTomlTable = parsedToml.Get<TomlTable>(PackageTomlKey);

    var dictKey = packageTomlTable.Get<string>(NamespaceTomlKey) + packageTomlTable.Get<string>(NameTomlKey);
    var package = packages[dictKey];

    if (package.Dependencies != null)
    {
        var dependencyTomlArray = packageTomlTable.Get<TomlTable>(DependencyTomlKey);

        foreach (var r2apiModuleDependency in package.Dependencies)
        {
            foreach (var (namespaceAndName, versionNumber) in dependencyTomlArray)
            {
                const char NamespaceAndNameSeparator = '-';
                var namespaceAndNameSplit = namespaceAndName.Split(NamespaceAndNameSeparator);

                var @namespace = namespaceAndNameSplit[0];
                var name = namespaceAndNameSplit[1];

                if (@namespace == r2apiModuleDependency.Namespace &&
                    name == r2apiModuleDependency.Name)
                {
                    TomlObjectFactory.Update(dependencyTomlArray, namespaceAndName, r2apiModuleDependency.Version.ToString());
                }
            }
        }

        packageTomlTable[DependencyTomlKey] = dependencyTomlArray;
        parsedToml[PackageTomlKey] = packageTomlTable;
        Toml.WriteFile(parsedToml, tomlFile);
    }
}

var packageTree = new List<Node<R2APIPackage>>();

void AddToDependencyTree(R2APIPackage package)
{
    Stack<Node<R2APIPackage>> stack = new();
    stack.Push(new(package, 0));

    while (stack.Count > 0)
    {
        var node = stack.Pop();
        packageTree.Add(node);

        if (node.Value.Dependencies != null)
        {
            foreach (var dependency in node.Value.Dependencies)
            {
                stack.Push(new(dependency, node.Depth + 1));
            }
        }
    }
}

// Constructs a dependency tree so that we know which packages to upload first to the website.
foreach (var (_, package) in packages)
{
    AddToDependencyTree(package);
}

foreach (var dependency in packageTree.
    OrderByDescending(n => n.Depth).
    DistinctBy(n => n.Value.Name))
{
    const char separator = '|';
    var output =
        dependency.Value.Namespace.ToString() + separator +
        dependency.Value.Name.ToString() + separator +
        dependency.Value.Version.ToString() + separator +
        dependency.Value.CsProjDirectoryFullPath;
    Console.WriteLine(output);
}

internal class Node<T>
{
    internal T Value;
    internal int Depth;

    internal Node(T value, int depth = 0)
    {
        Value = value;
        Depth = depth;
    }
}

internal class R2APIPackage
{
    internal string Namespace { get; }
    internal string Name { get; }
    internal Version Version { get; }

    internal string CsProjDirectoryFullPath { get; }

    const string RootNamespace = "RiskofThunder";
    const string PackageNamePrefix = "R2API_";

    /// <summary>
    /// Contains only the dependencies that start with <see cref="PackageNamePrefix"/> in their name and
    /// are under the <see cref="RootNamespace"/> namespace
    /// </summary>
    internal List<R2APIPackage>? Dependencies;

    /// <summary>
    /// Contains all the dependencies under [package.dependencies] from the thundertstore.toml file
    /// </summary>
    internal TomlTable? DependenciesTomlFormat;

    internal R2APIPackage(string @namespace, string name, string versionNumber, string csProjDirectory, TomlTable? dependenciesTomlFormat)
    {
        Namespace = @namespace;
        Name = name;
        Version = Version.Parse(versionNumber);
        CsProjDirectoryFullPath = csProjDirectory;
        DependenciesTomlFormat = dependenciesTomlFormat;
    }

    internal void InitFullDependencyReferences(Dictionary<string, R2APIPackage> packages)
    {
        if (DependenciesTomlFormat != null)
        {
            Dependencies = new();

            foreach (var (namespaceAndName, _) in DependenciesTomlFormat)
            {
                const char NamespaceAndNameSeparator = '-';
                var namespaceAndNameSplit = namespaceAndName.Split(NamespaceAndNameSeparator);

                var @namespace = namespaceAndNameSplit[0];
                var name = namespaceAndNameSplit[1];

                if (@namespace == RootNamespace &&
                    name.Contains(PackageNamePrefix))
                {
                    var key = @namespace + name;
                    Dependencies.Add(packages[key]);
                }
            }
        }
    }

    public override string ToString() =>
        $"{Namespace}-{Name} | Directory: {CsProjDirectoryFullPath}";
}

