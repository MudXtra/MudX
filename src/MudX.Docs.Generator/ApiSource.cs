using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;

namespace MudX.Docs.Generator
{
    internal static class ApiSource
    {
        private static Dictionary<string, string> _xmlDocs = [];

        public static bool GenerateApiDocs(string rootDirectory)
        {

            var outputDir = Path.Combine(rootDirectory, "..", "wwwroot", "api");
            Directory.CreateDirectory(outputDir);

            // Load XML documentation file for the MudX assembly
            Assembly targetAssembly = typeof(MudX._Imports).Assembly;
            string xmlPath = Path.ChangeExtension(targetAssembly.Location, ".xml");
            if (File.Exists(xmlPath))
            {
                _xmlDocs = LoadXmlDocumentation(xmlPath);
            }
            else
            {
                Console.WriteLine($"⚠️ MudX XML documentation file not found: {xmlPath}");
            }

            Assembly mudAssembly = typeof(MudBlazor._Imports).Assembly;
            xmlPath = Path.ChangeExtension(mudAssembly.Location, ".xml");

            if (!File.Exists(xmlPath))
            {
                var mudBlazorAssemblyName = mudAssembly.GetName().Name;
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var nugetPath = Path.Combine(userProfile, ".nuget", "packages", mudBlazorAssemblyName!.ToLower());

                var fallbackPath = Path.Combine(rootDirectory, "..", "wwwroot", "api", "Mudblazor.xml");

                if (Directory.Exists(nugetPath))
                {
                    var xmlFiles = Directory.GetFiles(nugetPath, "MudBlazor.xml", SearchOption.AllDirectories);
                    if (xmlFiles.Length > 0)
                    {
                        xmlPath = xmlFiles[0];
                        CopyIfDifferent(xmlPath, fallbackPath);
                    }
                    else
                    {
                        xmlPath = fallbackPath;
                    }
                }
                else
                {
                    xmlPath = fallbackPath;
                }
            }

            if (File.Exists(xmlPath))
            {
                var mudDocs = LoadXmlDocumentation(xmlPath);
                foreach (var kv in mudDocs)
                    _xmlDocs[kv.Key] = kv.Value;
            }
            else
            {
                Console.WriteLine($"⚠️ MudBlazor XML documentation file not found: {xmlPath}");
            }

            Assembly lottieAssembly = typeof(Blazor.Lottie.Player.LottiePlayer).Assembly;
            xmlPath = Path.ChangeExtension(lottieAssembly.Location, ".xml");

            if (!File.Exists(xmlPath))
            {
                var mudBlazorAssemblyName = lottieAssembly.GetName().Name;
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var nugetPath = Path.Combine(userProfile, ".nuget", "packages", mudBlazorAssemblyName!.ToLower());

                var fallbackPath = Path.Combine(rootDirectory, "..", "wwwroot", "api", "Blazor.Lottie.Player.xml");

                if (Directory.Exists(nugetPath))
                {
                    var xmlFiles = Directory.GetFiles(nugetPath, "Blazor.Lottie.Player.xml", SearchOption.AllDirectories);
                    if (xmlFiles.Length > 0)
                    {
                        xmlPath = xmlFiles[0];
                        CopyIfDifferent(xmlPath, fallbackPath);
                    }
                    else
                    {
                        xmlPath = fallbackPath;
                    }
                }
                else
                {
                    xmlPath = fallbackPath;
                }
            }

            if (File.Exists(xmlPath))
            {
                var docs = LoadXmlDocumentation(xmlPath);
                foreach (var kv in docs)
                    _xmlDocs[kv.Key] = kv.Value;
            }
            else
            {
                Console.WriteLine($"⚠️ Lottie XML documentation file not found: {xmlPath}");
            }

            // Get all component types (subclass of ComponentBase) in MudX namespace
            var components = targetAssembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) && t.Namespace?.Contains("MudX") == true);

            components.Union(
                lottieAssembly.GetTypes()
                    .Where(t => t.Namespace?.Contains("Blazor.Lottie.Player") == true));

            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

            foreach (var type in components)
            {
                var componentDescription = GetSummaryFromXml(type);

                var excludedNames = new List<string>
                {
                    "Dispose", "DisposeAsync"
                };

                var doc = new
                {
                    Component = type.Name,
                    type.Namespace,
                    Description = componentDescription,

                    Parameters = type.GetProperties()
                        .Where(p => p.IsDefined(typeof(ParameterAttribute), true))
                        .Select(p => new
                        {
                            p.Name,
                            Type = GetFriendlyTypeName(p.PropertyType),
                            Default = GetDefaultValue(p),
                            Description = GetSummaryFromXml(p)
                        }),

                    Events = type.GetProperties()
                        .Where(p => typeof(MulticastDelegate).IsAssignableFrom(p.PropertyType)
                               && !p.IsDefined(typeof(ParameterAttribute), inherit: true))
                        .Select(p => new
                        {
                            p.Name,
                            Type = GetFriendlyTypeName(p.PropertyType),
                            Description = GetSummaryFromXml(p)
                        }),

                    PublicProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(p => !p.IsDefined(typeof(ParameterAttribute), true)
                                    && !typeof(MulticastDelegate).IsAssignableFrom(p.PropertyType))
                        .Select(p => new
                        {
                            p.Name,
                            Type = GetFriendlyTypeName(p.PropertyType),
                            Description = GetSummaryFromXml(p)
                        }),

                    PublicFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Select(f => new
                        {
                            f.Name,
                            Type = GetFriendlyTypeName(f.FieldType),
                            Default = GetDefaultValue(f),
                            Description = GetSummaryFromXml(f)
                        }),

                    Methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                        .Where(m => !m.IsSpecialName && !m.IsDefined(typeof(CompilerGeneratedAttribute), false)
                               && !excludedNames.Contains(m.Name, StringComparer.CurrentCultureIgnoreCase))
                        .Select(m => new
                        {
                            m.Name,
                            ReturnType = m.ReturnType.Name,
                            Parameters = m.GetParameters().Select(p => new
                            {
                                p.Name,
                                Type = GetFriendlyTypeName(p.ParameterType),
                            }),
                            Description = GetSummaryFromXml(m)
                        })
                };


                var jsonPath = Path.Combine(outputDir, $"{type.Name}.api.json");
                var serialized = JsonSerializer.Serialize(doc, jsonOptions);
                WriteIfDifferent(jsonPath, serialized);
            }

            return true;
        }

        private static void WriteIfDifferent(string outFile, string contents)
        {
            try
            {
                if (!File.Exists(outFile))
                {
                    File.WriteAllText(outFile, contents);
                    Console.WriteLine($"MudX.Docs.Generator: API Doc Created: {outFile}");
                }
                else if (File.ReadAllText(outFile) != contents)
                {
                    File.WriteAllText(outFile, contents);
                    Console.WriteLine($"MudX.Docs.Generator: API Doc Updated: {outFile}");
                }
                else
                {
                    Console.WriteLine($"MudX.Docs.Generator: No API changes: {outFile}");
                }
            }
            catch
            {
                File.WriteAllText(outFile, contents);
                Console.WriteLine($"MudX.Docs.Generator: API Doc Override: {outFile}");
            }
        }

        private static void CopyIfDifferent(string sourcePath, string destinationPath)
        {
            bool shouldCopy = true;

            if (File.Exists(destinationPath))
            {
                using var sourceStream = File.OpenRead(sourcePath);
                using var destinationStream = File.OpenRead(destinationPath);
                using var sha256 = SHA256.Create();

                var sourceHash = sha256.ComputeHash(sourceStream);
                var destinationHash = sha256.ComputeHash(destinationStream);

                shouldCopy = !sourceHash.SequenceEqual(destinationHash);
            }

            if (shouldCopy)
            {
                File.Copy(sourcePath, destinationPath, true);
            }
        }

        private static Dictionary<string, string> LoadXmlDocumentation(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath);
            return doc.Descendants("member")
                .Where(x => x.Attribute("name") != null)
                .ToDictionary(
                    x => x.Attribute("name")!.Value,
                    x =>
                    {
                        var summary = x.Element("summary")?.Value ?? "";
                        var remarks = x.Element("remarks")?.Value ?? "";
                        return CleanXmlDoc(summary, remarks);
                    }
                );
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                string typeName = type.Name;
                int backtickIndex = typeName.IndexOf('`');
                if (backtickIndex > 0)
                    typeName = typeName[..backtickIndex];

                string genericArgs = string.Join(", ", type.GetGenericArguments()
                    .Select(GetFriendlyTypeName));

                return $"{typeName}<{genericArgs}>";
            }

            // Handle nullable types (e.g., Nullable<int> becomes int?)
            if (Nullable.GetUnderlyingType(type) is Type underlyingType)
                return $"{GetFriendlyTypeName(underlyingType)}?";

            return type.Name;
        }

        private static string CleanXmlDoc(string summary, string remarks)
        {
            string cleanedSummary = NormalizeText(summary);
            string cleanedRemarks = NormalizeText(remarks);

            return string.IsNullOrWhiteSpace(cleanedRemarks)
                ? cleanedSummary
                : $"{cleanedSummary}\nRemarks: {cleanedRemarks}";
        }

        private static string NormalizeText(string text)
        {
            return string.Join(" ",
                text.Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            ).Trim();
        }

        private static string? GetSummaryFromXml(FieldInfo field)
        {
            var type = field.DeclaringType;
            while (type != null)
            {
                var key = $"F:{type.FullName}.{field.Name}";
                if (_xmlDocs.TryGetValue(key, out var summary))
                    return summary;

                type = type.BaseType;
            }
            return null;
        }


        private static string? GetSummaryFromXml(PropertyInfo property)
        {
            string memberName = $"P:{property.DeclaringType!.FullName}.{property.Name}";
            return _xmlDocs.TryGetValue(memberName, out var summary) ? summary : null;
        }

        private static string? GetSummaryFromXml(Type type)
        {
            string memberName = $"T:{type.FullName}";
            return _xmlDocs.TryGetValue(memberName, out var summary) ? summary : null;
        }

        private static string? GetDefaultValue(FieldInfo field)
        {
            var type = field.DeclaringType;

            if (type == null || type.IsAbstract)
                return null;

            try
            {
                var instance = Activator.CreateInstance(type);
                var value = field.GetValue(instance);
                return value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string? GetDefaultValue(PropertyInfo property)
        {
            var type = property.DeclaringType;

            if (type == null || type.IsAbstract)
                return null;

            try
            {
                var instance = Activator.CreateInstance(type);
                var value = property.GetValue(instance);

                if (value == null)
                    return "null";

                var valueType = value.GetType();

                // Handle strings
                if (value is string str)
                    return $"{str}";

                // Booleans
                if (value is bool b)
                    return b.ToString().ToLowerInvariant();

                // Primitive types (int, double, etc.)
                if (valueType.IsPrimitive || value is decimal)
                    return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);

                // Enum
                if (valueType.IsEnum)
                    return value.ToString();

                // RenderFragment and EventCallback: return simplified marker, not .ToString()
                if (valueType.Name.StartsWith("RenderFragment"))
                    return "";

                if (valueType.Name.StartsWith("EventCallback"))
                    return "";

                // Collections
                if (value is System.Collections.IEnumerable enumerable)
                {
                    var items = enumerable.Cast<object>().Take(3).Select(x => x?.ToString() ?? "null");
                    return $"[{string.Join(", ", items)}{(items.Count() == 3 ? ", ..." : "")}]";
                }

                // Fallback: don't emit fully qualified type names
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string? GetSummaryFromXml(MethodInfo method)
        {
            var parameters = method.GetParameters();
            var paramSignature = parameters.Length == 0
                ? string.Empty
                : $"({string.Join(",", parameters.Select(p => p.ParameterType.FullName))})";

            string memberName = $"M:{method.DeclaringType!.FullName}.{method.Name}{paramSignature}";
            return _xmlDocs.TryGetValue(memberName, out var summary) ? summary : null;
        }

    }
}
