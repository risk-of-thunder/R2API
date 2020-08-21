using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Mono.Cecil;

namespace R2API.Utils {
    internal class PluginScanner {
        private List<AssemblyDefinition> _pluginsAssemblyDefinitions;

        private List<AssemblyDefinition> PluginsAssemblyDefinitions {
            get {
                if (_pluginsAssemblyDefinitions == null) {
                    var assemblies = new List<AssemblyDefinition>();
                    var resolver = new DefaultAssemblyResolver();
                    var gameDirectory = new DirectoryInfo(Paths.GameRootPath);

                    // todo: make resolver able to resolve embedded assemblies
                    foreach (var directory in gameDirectory.EnumerateDirectories("*", SearchOption.AllDirectories)) {
                        resolver.AddSearchDirectory(directory.FullName);
                    }

                    R2API.Logger.LogDebug("Adding to the list of assemblies to scan:");
                    foreach (string dll in Directory.GetFiles(Paths.PluginPath, "*.dll", SearchOption.AllDirectories))
                    {
                        var fileName = Path.GetFileName(dll);
                        if (fileName.ToLower().Contains("r2api") || fileName.ToLower().Contains("mmhook")) {
                            continue;
                        }

                        try {
                            assemblies.Add(AssemblyDefinition.ReadAssembly(dll,
                                new ReaderParameters { AssemblyResolver = resolver }));
                            R2API.Logger.LogDebug($"{fileName}");
                        }
                        catch (Exception) {
                            R2API.Logger.LogDebug($"Cecil ReadAssembly couldn't read {dll}");
                        }
                    }

                    DetectAndRemoveDuplicateAssemblies(ref assemblies);
                    _pluginsAssemblyDefinitions = assemblies;
                }

                return _pluginsAssemblyDefinitions;
            }
        }

        private static void DetectAndRemoveDuplicateAssemblies(ref List<AssemblyDefinition> assemblies) {
            var duplicateOldAssemblies = new HashSet<AssemblyDefinition>();

            foreach (var assemblyDef in assemblies) {
                if (duplicateOldAssemblies.Contains(assemblyDef))
                    continue;
                var bepinPluginAttributes = assemblyDef.MainModule.Types.SelectMany(typeDef => typeDef.CustomAttributes)
                    .Where(attribute => attribute.AttributeType.FullName == typeof(BepInPlugin).FullName).ToList();
                foreach (var otherAssemblyDef in assemblies) {
                    if (duplicateOldAssemblies.Contains(otherAssemblyDef))
                        continue;
                    if (assemblyDef == otherAssemblyDef)
                        continue;

                    var otherBepinPluginAttributes = otherAssemblyDef.MainModule.Types.SelectMany(typeDef => typeDef.CustomAttributes)
                        .Where(attribute => attribute.AttributeType.FullName == typeof(BepInPlugin).FullName).ToList();

                    AssemblyDefinition goodAssembly = null;
                    string goodAssemblyVer = null;
                    AssemblyDefinition oldDuplicateAssembly = null;
                    string oldDuplicateModVer = null;

                    foreach (var bepinPluginAttribute in bepinPluginAttributes) {
                        var (modGuid, modVer) = GetBepinPluginInfo(bepinPluginAttribute.ConstructorArguments);

                        if (modGuid == null)
                            break;

                        foreach (var otherBepinPluginAttribute in otherBepinPluginAttributes) {
                            var (otherModGuid, otherModVer) = GetBepinPluginInfo(otherBepinPluginAttribute.ConstructorArguments);
                            if (modGuid == otherModGuid) {
                                var comparedTo = string.Compare(modVer, otherModVer, StringComparison.Ordinal);
                                var isModVerMoreRecentThanOtherModVer = comparedTo >= 0;
                                if (isModVerMoreRecentThanOtherModVer) {
                                    goodAssembly = bepinPluginAttribute.AttributeType.Module.Assembly;
                                    goodAssemblyVer = modVer;
                                    oldDuplicateAssembly = otherBepinPluginAttribute.AttributeType.Module.Assembly;
                                    oldDuplicateModVer = otherModVer;
                                }
                            }
                            else {
                                oldDuplicateAssembly = null;
                                break;
                            }
                        }
                    }

                    if (oldDuplicateAssembly != null) {
                        R2API.Logger.LogDebug($"Removing {oldDuplicateAssembly.MainModule.FileName} (ModVer : {oldDuplicateModVer}) from the list " +
                                              $"because it's a duplicate of {goodAssembly.MainModule.FileName} (ModVer : {goodAssemblyVer}).");
                        duplicateOldAssemblies.Add(oldDuplicateAssembly);
                    }
                }
            }

            assemblies = assemblies.Except(duplicateOldAssemblies).ToList();
        }

        private readonly List<ScanRequest> _scanRequests = new List<ScanRequest>();

        internal void AddScanRequest(ScanRequest request) {
            if (request.IsCoherent())
                _scanRequests.Add(request);
        }

        internal void ScanPlugins() {
            foreach (var assembly in PluginsAssemblyDefinitions) {
                if (assembly.HasCustomAttributes) {
                    ScanAssemblyAttributes(assembly);
                }

                ScanAssemblyTypes(assembly.Modules.SelectMany(module => module.Types));
            }

            foreach (var scanRequest in _scanRequests) {
                scanRequest.WhenRequestIsDone?.Invoke();
            }
        }

        private void ScanAssemblyAttributes(AssemblyDefinition assembly) {
            foreach (var attribute in assembly.CustomAttributes) {
                foreach (var scanRequest in _scanRequests) {
                    if (scanRequest.AlreadyFound.Contains(assembly)) {
                        continue;
                    }

                    if (scanRequest is AttributeScanRequest attributeScanRequest) {
                        if (attributeScanRequest.FoundOnAssemblyAttributes != null &&
                            attribute.AttributeType.FullName == scanRequest.SearchedTypeFullName) {
                            attributeScanRequest.FoundOnAssemblyAttributes(assembly, attribute.ConstructorArguments);
                            if (attributeScanRequest.OneMatchPerAssembly) {
                                attributeScanRequest.AlreadyFound.Add(assembly);
                            }
                        }
                    }
                }
            }
        }

        private void ScanAssemblyTypes(IEnumerable<TypeDefinition> types) {
            foreach (var typeDef in types) {
                foreach (var scanRequest in _scanRequests) {
                    if (scanRequest.AlreadyFound.Contains(typeDef.Module.Assembly)) {
                        continue;
                    }

                    if (scanRequest is AttributeScanRequest attributeScanRequest) {
                        if (attributeScanRequest.FoundOnAssemblyTypes != null &&
                            typeDef.HasCustomAttributes) {
                            try {
                                foreach (var attribute in typeDef.CustomAttributes) {
                                    if (attribute.AttributeType.FullName == attributeScanRequest.SearchedTypeFullName) {
                                        var mustBeOnASpecificType =
                                            !string.IsNullOrEmpty(attributeScanRequest.AttributeMustBeOnTypeFullName);
                                        if (mustBeOnASpecificType &&
                                            typeDef.IsSubTypeOf(attributeScanRequest.AttributeMustBeOnTypeFullName) ||
                                            !mustBeOnASpecificType) {
                                            attributeScanRequest.FoundOnAssemblyTypes(typeDef, attribute.ConstructorArguments);
                                            if (attributeScanRequest.OneMatchPerAssembly) {
                                                attributeScanRequest.AlreadyFound.Add(typeDef.Module.Assembly);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex) {
                                // AssemblyResolutionException will happen on types that are resolved from
                                // dynamicaly loaded / soft dependency assemblies 
                                if (!(ex is AssemblyResolutionException)) {
                                    R2API.Logger.LogDebug(
                                        $"Catched ex when handling attribute scan request : {ex}\n" +
                                        $"We were looking for {attributeScanRequest.SearchedTypeFullName} " +
                                        $"in the assembly called {typeDef.Module.FileName}");
                                }
                            }
                        }
                    }

                    if (scanRequest is ClassScanRequest classScanRequest) {
                        if (classScanRequest.FoundOnAssemblyTypes != null) {
                            try {
                                if (classScanRequest.SearchedTypeFullName == typeDef.FullName ||
                                    classScanRequest.MatchSubType &&
                                    typeDef.IsSubTypeOf(classScanRequest.SearchedTypeFullName)) {
                                    classScanRequest.FoundOnAssemblyTypes(typeDef, typeDef.CustomAttributes);
                                    if (classScanRequest.OneMatchPerAssembly) {
                                        classScanRequest.AlreadyFound.Add(typeDef.Module.Assembly);
                                    }
                                }
                            }
                            catch (Exception ex) {
                                // AssemblyResolutionException will happen on types that are resolved from
                                // dynamicaly loaded / soft dependency assemblies 
                                if (!(ex is AssemblyResolutionException)) {
                                    R2API.Logger.LogDebug(
                                        $"Catched ex when handling class scan request : {ex}\n" +
                                        $"We were looking for {classScanRequest.SearchedTypeFullName} " +
                                        $"in the assembly called {typeDef.Module.FileName}");
                                }
                            }
                        }
                    }
                }
            }
        }

        internal abstract class ScanRequest {
            internal readonly string SearchedTypeFullName;
            internal readonly Action WhenRequestIsDone;
            internal readonly bool OneMatchPerAssembly;
            internal readonly HashSet<AssemblyDefinition> AlreadyFound;

            internal ScanRequest(string searchedTypeFullName, Action whenRequestIsDone, bool oneMatchPerAssembly) {
                SearchedTypeFullName = searchedTypeFullName;
                WhenRequestIsDone = whenRequestIsDone;
                OneMatchPerAssembly = oneMatchPerAssembly;
                AlreadyFound = new HashSet<AssemblyDefinition>();
            }

            internal abstract bool IsCoherent();
        }

        public delegate void AttributeFoundOnAssemblyTypesDelegate(TypeDefinition scannedType,
            IList<CustomAttributeArgument> attributeArguments);

        public delegate void AttributeFoundOnAssemblyAttributesDelegate(AssemblyDefinition scannedAssembly,
            IList<CustomAttributeArgument> attributeArguments);

        internal class AttributeScanRequest : ScanRequest {
            private readonly AttributeTargets _attributeTargets;
            internal readonly AttributeFoundOnAssemblyAttributesDelegate FoundOnAssemblyAttributes;
            internal readonly AttributeFoundOnAssemblyTypesDelegate FoundOnAssemblyTypes;
            internal readonly string AttributeMustBeOnTypeFullName;

            internal AttributeScanRequest(string attributeTypeFullName, AttributeTargets attributeTargets,
                Action whenRequestIsDone,
                bool oneMatchPerAssembly,
                AttributeFoundOnAssemblyAttributesDelegate foundOnAssemblyAttributes,
                AttributeFoundOnAssemblyTypesDelegate foundOnAssemblyTypes = null,
                string attributeMustBeOnTypeFullName = null) :
                base(attributeTypeFullName, whenRequestIsDone, oneMatchPerAssembly) {
                _attributeTargets = attributeTargets;
                FoundOnAssemblyAttributes = foundOnAssemblyAttributes;
                FoundOnAssemblyTypes = foundOnAssemblyTypes;
                AttributeMustBeOnTypeFullName = attributeMustBeOnTypeFullName;
            }

            internal override bool IsCoherent() {
                var isCoherent = true;

                if (_attributeTargets.HasFlag(AttributeTargets.Assembly) &&
                    FoundOnAssemblyAttributes == null) {
                    R2API.Logger.LogError(
                        $"[ScanRequest] Given attribute {SearchedTypeFullName} " +
                        "will be checked against assemblies attributes but no corresponding callback was given");
                    isCoherent = false;
                }
                if (_attributeTargets.HasFlag(AttributeTargets.Class) &&
                    FoundOnAssemblyTypes == null) {
                    R2API.Logger.LogError(
                        $"[ScanRequest] Given attribute {SearchedTypeFullName} " +
                        "will be checked against assemblies class types but no corresponding callback was given");
                    isCoherent = false;
                }

                return isCoherent;
            }
        }

        public delegate void ClassFoundOnAssemblyTypesDelegate(TypeDefinition scannedType,
            IList<CustomAttribute> classAttributes);

        internal class ClassScanRequest : ScanRequest {
            internal readonly ClassFoundOnAssemblyTypesDelegate FoundOnAssemblyTypes;
            internal readonly bool MatchSubType;

            internal ClassScanRequest(string searchedTypeFullName,
                Action whenRequestIsDone,
                bool oneMatchPerAssembly,
                ClassFoundOnAssemblyTypesDelegate foundOnAssemblyTypes, bool matchSubType = true) :
                base(searchedTypeFullName, whenRequestIsDone, oneMatchPerAssembly) {
                FoundOnAssemblyTypes = foundOnAssemblyTypes;
                MatchSubType = matchSubType;
            }

            internal override bool IsCoherent() {
                var isCoherent = true;

                if (FoundOnAssemblyTypes == null) {
                    R2API.Logger.LogError(
                        $"[ScanRequest] Given attribute {SearchedTypeFullName} " +
                        "will be checked against assemblies class types but no corresponding callback was given");
                    isCoherent = false;
                }

                return isCoherent;
            }
        }

        internal static (string modGuid, string modVersion) GetBepinPluginInfo(IList<CustomAttributeArgument> attributeArguments) {
            if (attributeArguments == null) {
                return (null, null);
            }

            var modGuid = (string)attributeArguments[0].Value;
            var modVersion = (string)attributeArguments[2].Value;

            return (modGuid, modVersion);
        }
    }
}
