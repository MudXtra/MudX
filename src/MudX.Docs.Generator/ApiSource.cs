using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;

namespace MudX.Docs.Generator
{
    internal static class ApiSource
    {
        private static Dictionary<string, string> _xmlDocs = new();

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

            // Load XML documentation file for the MudBlazor assembly
            Assembly mudAssembly = typeof(MudBlazor._Imports).Assembly;
            xmlPath = Path.ChangeExtension(mudAssembly.Location, ".xml");
            if (File.Exists(xmlPath))
            {
                var mudDocs = LoadXmlDocumentation(xmlPath);
                _xmlDocs.Union(mudDocs);
            }
            else
            {
                Console.WriteLine($"⚠️ MudBlazor XML documentation file not found: {xmlPath}");
            }

            // Get all component types (subclass of ComponentBase) in MudX namespace
            var components = targetAssembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ComponentBase)) && t.Namespace?.Contains("MudX") == true);

            foreach (var type in components)
            {
                var componentDescription = GetSummaryFromXml(type);

                var doc = new
                {
                    Component = type.Name,
                    Namespace = type.Namespace,
                    Description = componentDescription,
                    Parameters = type.GetProperties()
                        .Where(p => p.IsDefined(typeof(ParameterAttribute), true))
                        .Select(p => new
                        {
                            Name = p.Name,
                            Type = p.PropertyType.Name,
                            Default = GetDefaultValue(p),
                            Description = GetSummaryFromXml(p)
                        }),
                    Events = type.GetProperties()
                        .Where(p => typeof(MulticastDelegate).IsAssignableFrom(p.PropertyType))
                        .Select(p => new
                        {
                            Name = p.Name,
                            Type = p.PropertyType.Name,
                            Description = GetSummaryFromXml(p)
                        })
                };

                var jsonPath = Path.Combine(outputDir, $"{type.Name}.api.json");
                File.WriteAllText(jsonPath, JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine($"MudX.Docs.Generator: API doc written: {jsonPath}");
            }

            return true;
        }

        private static Dictionary<string, string> LoadXmlDocumentation(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath);
            return doc.Descendants("member")
                .Where(x => x.Attribute("name") != null)
                .ToDictionary(
                    x => x.Attribute("name")!.Value,
                    x => x.Element("summary")?.Value.Trim() ?? ""
                );
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

        private static string? GetDefaultValue(PropertyInfo property)
        {
            var type = property.DeclaringType;

            if (type == null || type.IsAbstract)
                return null;

            try
            {
                var instance = Activator.CreateInstance(type);
                var value = property.GetValue(instance);
                return value?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}
