using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Mono.Cecil;

namespace R2API.Utils {
    internal class PluginScanner {
        private List<AssemblyDefinition> _pluginsAssemblyDefinitions;
        internal List<AssemblyDefinition> PluginsAssemblyDefinitions {
            get {
                if (_pluginsAssemblyDefinitions == null) {
                    var assemblies = new List<AssemblyDefinition>();
                    var resolver = new DefaultAssemblyResolver();
                    var gameDirectory = new DirectoryInfo(Paths.GameRootPath);
                    foreach (var directory in gameDirectory.EnumerateDirectories("*", SearchOption.AllDirectories)) {
                        resolver.AddSearchDirectory(directory.FullName);
                    }
                    foreach (string dll in Directory.GetFiles(Paths.PluginPath, "*.dll", SearchOption.AllDirectories))
                    {
                        var fileName = Path.GetFileName(dll);
                        if (fileName.ToLower().Contains("r2api") || fileName.ToLower().Contains("mmhook")) {
                            continue;
                        }

                        try {
                            assemblies.Add(AssemblyDefinition.ReadAssembly(dll,
                                new ReaderParameters { AssemblyResolver = resolver }));
                        }
                        catch (Exception) {
                            // ignored
                        }
                    }

                    _pluginsAssemblyDefinitions = assemblies;
                }

                return _pluginsAssemblyDefinitions;
            }
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

                if (assembly.MainModule.HasTypes) {
                    ScanAssemblyTypes(assembly.MainModule.Types);
                }
            }

            foreach (var scanRequest in _scanRequests) {
                scanRequest.WhenRequestIsDone?.Invoke();
            }
        }

        private void ScanAssemblyAttributes(AssemblyDefinition assembly) {
            foreach (var attribute in assembly.CustomAttributes) {
                foreach (var scanRequest in _scanRequests) {
                    if (scanRequest.Done) {
                        continue;
                    }

                    if (scanRequest is AttributeScanRequest attributeScanRequest) {
                        if (attributeScanRequest.FoundOnAssemblyAttributes != null &&
                            attribute.AttributeType.FullName == scanRequest.SearchedTypeFullName) {
                            attributeScanRequest.FoundOnAssemblyAttributes.Invoke(assembly, attribute.ConstructorArguments);
                            if (attributeScanRequest.IgnoreSecondDelegateIfFirstSucceed) {
                                scanRequest.Done = true;
                            }
                        }
                    }
                }
            }
        }

        private void ScanAssemblyTypes(IEnumerable<TypeDefinition> types) {
            foreach (var typeDef in types) {
                foreach (var scanRequest in _scanRequests) {
                    if (scanRequest.Done) {
                        continue;
                    }

                    if (scanRequest is AttributeScanRequest attributeScanRequest) {
                        if (attributeScanRequest.FoundOnAssemblyTypes != null &&
                            typeDef.HasCustomAttributes) {
                            foreach (var attribute in typeDef.CustomAttributes) {
                                if (attribute.AttributeType.FullName == attributeScanRequest.SearchedTypeFullName) {
                                    var mustBeOnASpecificType =
                                        !string.IsNullOrEmpty(attributeScanRequest.AttributeMustBeOnTypeFullName);

                                    if (mustBeOnASpecificType &&
                                        typeDef.BaseType?.FullName == attributeScanRequest.AttributeMustBeOnTypeFullName ||
                                        !mustBeOnASpecificType) {
                                        attributeScanRequest.FoundOnAssemblyTypes(typeDef, attribute.ConstructorArguments);
                                        scanRequest.Done = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (scanRequest is ClassScanRequest classScanRequest) {
                        if (classScanRequest.FoundOnAssemblyTypes != null) {
                            if (classScanRequest.SearchedTypeFullName == typeDef.FullName ||
                                classScanRequest.MatchSubType &&
                                classScanRequest.SearchedTypeFullName == typeDef.BaseType?.FullName) {
                                
                                classScanRequest.FoundOnAssemblyTypes(typeDef, typeDef.CustomAttributes);
                                scanRequest.Done = true;
                            }
                            
                        }
                    }
                }
            }
        }

        internal abstract class ScanRequest {
            internal readonly string SearchedTypeFullName;
            internal readonly Action WhenRequestIsDone;

            internal bool Done;

            internal ScanRequest(string searchedTypeFullName,
                Action whenRequestIsDone) {
                SearchedTypeFullName = searchedTypeFullName;
                WhenRequestIsDone = whenRequestIsDone;
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

            internal readonly bool IgnoreSecondDelegateIfFirstSucceed;

            internal AttributeScanRequest(string attributeTypeFullName, AttributeTargets attributeTargets,
                Action whenRequestIsDone,
                AttributeFoundOnAssemblyAttributesDelegate foundOnAssemblyAttributes,
                AttributeFoundOnAssemblyTypesDelegate foundOnAssemblyTypes = null, string attributeMustBeOnTypeFullName = null,
                bool ignoreSecondDelegateIfFirstSucceed = true) : base(attributeTypeFullName, whenRequestIsDone){
                _attributeTargets = attributeTargets;
                FoundOnAssemblyAttributes = foundOnAssemblyAttributes;
                FoundOnAssemblyTypes = foundOnAssemblyTypes;
                AttributeMustBeOnTypeFullName = attributeMustBeOnTypeFullName;
                IgnoreSecondDelegateIfFirstSucceed = ignoreSecondDelegateIfFirstSucceed;
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
                ClassFoundOnAssemblyTypesDelegate foundOnAssemblyTypes, bool matchSubType = true) : base(searchedTypeFullName, whenRequestIsDone) {
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
    }
}
