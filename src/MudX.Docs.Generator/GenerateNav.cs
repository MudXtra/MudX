using System.Text.RegularExpressions;

namespace MudX.Docs.Generator
{
    internal static class GenerateNav
    {
        internal static List<NavItem> GenerateNavFromRazorFiles(string rootDirectory)
        {
            var navItems = new List<NavItem>();
            var controllerToNavId = new Dictionary<string, int>();
            int nextId = 1;
            int processedFileCount = 0;
            int skippedFileCount = 0;

            var razorFiles = Directory.GetFiles(rootDirectory, "*DocPage.razor", SearchOption.AllDirectories);
            //Console.WriteLine($"- Found {razorFiles.Length} DocPage.razor files to process");

            foreach (var filePath in razorFiles)
            {
                try
                {
                    string fileContent = File.ReadAllText(filePath);
                    string fileName = Path.GetFileName(filePath);

                    var pageParts = ExtractPageDirective(fileContent);
                    if (pageParts == null)
                    {
                        //Console.WriteLine($"  - Skipping {fileName} (no @page directive)");
                        skippedFileCount++;
                        continue;
                    }

                    string? controller = pageParts.Value.Controller;
                    string action = pageParts.Value.Action;

                    string title = ExtractTitle(fileContent) ?? Path.GetFileNameWithoutExtension(filePath);

                    int? parentId = null;

                    // Create parent controller nav item if it doesn't exist
                    if (!string.IsNullOrEmpty(controller) && !controllerToNavId.ContainsKey(controller))
                    {
                        int controllerId = nextId++;
                        controllerToNavId[controller] = controllerId;

                        //Console.WriteLine($"  - Creating controller nav item: \"{controller}\" (ID: {controllerId})");

                        navItems.Add(new NavItem
                        {
                            NavItemId = controllerId,
                            Title = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(controller),
                            Application = null,
                            Controller = null,
                            Action = null,
                            ParentId = null,
                            IsActive = true,
                            OrderById = 0
                        });

                        parentId = controllerId;
                    }
                    else if (!string.IsNullOrEmpty(controller))
                    {
                        parentId = controllerToNavId[controller];
                    }

                    // Create action nav item under its controller
                    int navId = nextId++;
                    navItems.Add(new NavItem
                    {
                        NavItemId = navId,
                        Title = title,
                        Application = null,
                        Controller = controller,
                        Action = action,
                        ParentId = parentId,
                        IsActive = true,
                        OrderById = navItems.Count(n => n.ParentId == parentId)
                    });

                    processedFileCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  - Error processing {Path.GetFileName(filePath)}: {ex.Message}");
                    skippedFileCount++;
                }
            }

            // Reorganize OrderById alphabetically within each parent group
            foreach (var group in navItems
                .Where(n => n.IsActive)
                .GroupBy(n => n.ParentId))
            {
                var ordered = group
                    .OrderBy(n => n.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int startOrder = 10;
                for (int i = 0; i < ordered.Count; i++)
                {
                    ordered[i].OrderById = startOrder + i;
                }

            }


            //Console.WriteLine($"- Navigation items generated: {processedFileCount} files processed, {skippedFileCount} files skipped");

            return navItems;
        }


        private static (string? Controller, string Action)? ExtractPageDirective(string fileContent)
        {
            var match = Regex.Match(fileContent, @"@page\s+""([^""]+)""");
            if (!match.Success) return null;

            var route = match.Groups[1].Value.Trim('/');
            var segments = route.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                return null;

            string action = segments[^1];
            string? controller = segments.Length > 1
                ? string.Join('/', segments.Take(segments.Length - 1))
                : null;

            return (controller, action);
        }


        private static string? ExtractTitle(string fileContent)
        {
            var match = Regex.Match(fileContent, @"private\s+static\s+string\s+__title__\s*=\s*""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

