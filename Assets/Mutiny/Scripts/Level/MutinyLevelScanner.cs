using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Mutiny.Levels
{
    public sealed class MutinyLevelScanResult
    {
        public int Files;
        public int Parsed;
        public int Errors;
        public int TileTypes;
        public int ObjectTypes;
        public int Attributes;
    }

    public static class MutinyLevelScanner
    {
        // Reports are rebuilt from the current XML files, using ordinal ordering.
        public static MutinyLevelScanResult Scan(string inputDirectory, string outputDirectory)
        {
            if (!Directory.Exists(inputDirectory))
                throw new DirectoryNotFoundException(inputDirectory);

            string[] files = Directory.GetFiles(inputDirectory, "level_*.xml")
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).ToArray();
            var result = new MutinyLevelScanResult { Files = files.Length };
            var tiles = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var objectTypes = new SortedDictionary<string, int>(StringComparer.Ordinal);
            var attributes = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
            var levelsCsv = new StringBuilder("file,sha256,width,height,players,name,objects,terrain_types,background_types\n");
            var tilesCsv = new StringBuilder("file,layer,x,y,tile\n");
            var objectsCsv = new StringBuilder("file,object_index,type,x,y\n");
            var propertiesCsv = new StringBuilder("file,object_index,type,x,y,attribute,value\n");
            var errorsCsv = new StringBuilder("file,error\n");

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                try
                {
                    // Read the same bytes for parsing and provenance hashing.
                    byte[] bytes = File.ReadAllBytes(file);
                    string xml;
                    using (var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, true))
                        xml = reader.ReadToEnd();
                    MutinyLevelData level = MutinyLevelXmlParser.Parse(xml, name);
                    string hash;
                    using (SHA256 sha = SHA256.Create())
                        hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();

                    int terrainTypes = ScanLayer(level.Terrain, "Terrain", name, tiles, tilesCsv);
                    int backgroundTypes = ScanLayer(level.Background, "Background", name, tiles, tilesCsv);
                    AppendCsv(levelsCsv, name, hash, level.Width, level.Height, level.Players,
                        level.Name, level.Objects.Count, terrainTypes, backgroundTypes);

                    for (int i = 0; i < level.Objects.Count; i++)
                    {
                        MutinyLevelObject obj = level.Objects[i];
                        Increment(objectTypes, obj.Type);
                        AppendCsv(objectsCsv, name, i, obj.Type, obj.X, obj.Y);
                        foreach (var property in obj.Properties.OrderBy(p => p.Key, StringComparer.Ordinal))
                        {
                            if (!attributes.TryGetValue(property.Key, out var values))
                            {
                                values = new SortedDictionary<string, int>(StringComparer.Ordinal);
                                attributes.Add(property.Key, values);
                            }
                            Increment(values, property.Value);
                            AppendCsv(propertiesCsv, name, i, obj.Type, obj.X, obj.Y, property.Key, property.Value);
                        }
                    }
                    result.Parsed++;
                }
                catch (Exception exception) when (exception is FormatException ||
                    exception is IOException || exception is UnauthorizedAccessException)
                {
                    result.Errors++;
                    AppendCsv(errorsCsv, name, exception.Message);
                }
            }

            result.TileTypes = tiles.Count;
            result.ObjectTypes = objectTypes.Count;
            result.Attributes = attributes.Count;
            var tileCatalog = new StringBuilder("tile,occurrences\n");
            foreach (var tile in tiles) AppendCsv(tileCatalog, tile.Key, tile.Value);
            var objectCatalog = new StringBuilder("type,occurrences\n");
            foreach (var type in objectTypes) AppendCsv(objectCatalog, type.Key, type.Value);
            var attributeCatalog = new StringBuilder("attribute,value,occurrences\n");
            foreach (var attribute in attributes)
                foreach (var value in attribute.Value)
                    AppendCsv(attributeCatalog, attribute.Key, value.Key, value.Value);

            var summary = new StringBuilder("# Mutiny Level scan\n\n");
            summary.Append($"Files: {result.Files}; parsed: {result.Parsed}; errors: {result.Errors}.\n\n");
            summary.Append($"Non-empty tile names (Terrain + Background): {result.TileTypes}.\n\n");
            summary.Append($"Object types: {result.ObjectTypes}; object attribute names: {result.Attributes}.\n\n");
            summary.Append("Coordinates use original XML x (left to right), y (top to bottom). Object indices are zero-based.\n\n");
            summary.Append("Empty '-' tiles are excluded. Logical markers such as antichest are retained. " +
                "Raw attributes include type/x/y; no gameplay meanings are inferred.\n\n");
            summary.Append("Verified reference: 18 files, 107 tile names, 29 object types, 20 attribute names. " +
                "The preliminary documentation stated 22 attributes; independent XML recount confirmed 20.\n\n");
            bool matches = result.Files == 18 && result.Parsed == 18 && result.Errors == 0 &&
                result.TileTypes == 107 && result.ObjectTypes == 29 && result.Attributes == 20;
            summary.Append($"Reference comparison: {(matches ? "MATCH" : "MISMATCH — inspect CSV reports")}.\n\n");
            summary.Append("| Report | Contents |\n| --- | --- |\n" +
                "| levels.csv | Per-level metadata, type counts and source SHA256 |\n" +
                "| tile-types.csv | Combined tile names and occurrence counts |\n" +
                "| tile-occurrences.csv | Every non-empty tile, layer and coordinate |\n" +
                "| object-types.csv | Object types and counts |\n" +
                "| objects.csv | Every object, index, type and coordinate |\n" +
                "| attribute-values.csv | Raw attribute values and counts |\n" +
                "| object-properties.csv | Every attribute with its object location |\n" +
                "| errors.csv | File and diagnostic for each failed parse/read |\n\n" +
                "Reports are overwritten on each scan and ordered deterministically; they are not cumulative.\n");

            Directory.CreateDirectory(outputDirectory);
            Write(outputDirectory, "levels.csv", levelsCsv);
            Write(outputDirectory, "tile-types.csv", tileCatalog);
            Write(outputDirectory, "tile-occurrences.csv", tilesCsv);
            Write(outputDirectory, "object-types.csv", objectCatalog);
            Write(outputDirectory, "objects.csv", objectsCsv);
            Write(outputDirectory, "attribute-values.csv", attributeCatalog);
            Write(outputDirectory, "object-properties.csv", propertiesCsv);
            Write(outputDirectory, "errors.csv", errorsCsv);
            Write(outputDirectory, "README.md", summary);
            return result;
        }

        private static int ScanLayer(string[,] layer, string layerName, string file,
            SortedDictionary<string, int> catalog, StringBuilder occurrences)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int y = 0; y < layer.GetLength(0); y++)
                for (int x = 0; x < layer.GetLength(1); x++)
                {
                    string tile = layer[y, x];
                    if (tile == null) continue;
                    names.Add(tile);
                    Increment(catalog, tile);
                    AppendCsv(occurrences, file, layerName, x, y, tile);
                }
            return names.Count;
        }

        private static void Increment(SortedDictionary<string, int> counts, string key)
        {
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }

        private static void AppendCsv(StringBuilder builder, params object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) builder.Append(',');
                string value = Convert.ToString(values[i], CultureInfo.InvariantCulture) ?? string.Empty;
                builder.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
            }
            builder.Append('\n');
        }

        private static void Write(string directory, string name, StringBuilder content)
        {
            File.WriteAllText(Path.Combine(directory, name), content.ToString(), new UTF8Encoding(false));
        }
    }
}
