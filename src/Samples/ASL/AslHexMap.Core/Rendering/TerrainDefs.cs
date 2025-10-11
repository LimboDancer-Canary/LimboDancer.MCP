using System.Text;

public static class TerrainDefs
{
    public static string BuildTerrainDefs(string flavor = "v39")
    {
        var sb = new StringBuilder();
        sb.Append("<defs>");

        // --- v39 base patterns (you already have these) ---
        sb.Append(@"
  <pattern id=""woodsPattern"" x=""0"" y=""0"" width=""20"" height=""20"" patternUnits=""userSpaceOnUse"">
    <circle cx=""5"" cy=""5"" r=""3"" fill=""#2d5016"" opacity=""0.8""/>
    <circle cx=""5"" cy=""3"" r=""2"" fill=""#3d6b20"" opacity=""0.8""/>
    <circle cx=""15"" cy=""12"" r=""3"" fill=""#2d5016"" opacity=""0.8""/>
    <circle cx=""15"" cy=""10"" r=""2"" fill=""#3d6b20"" opacity=""0.8""/>
    <circle cx=""8"" cy=""18"" r=""3"" fill=""#2d5016"" opacity=""0.8""/>
    <circle cx=""8"" cy=""16"" r=""2"" fill=""#3d6b20"" opacity=""0.8""/>
  </pattern>
  <pattern id=""orchardPattern"" x=""0"" y=""0"" width=""15"" height=""15"" patternUnits=""userSpaceOnUse"">
    <circle cx=""7.5"" cy=""7.5"" r=""2"" fill=""#2d5016"" opacity=""0.6""/>
    <circle cx=""7.5"" cy=""7.5"" r=""1"" fill=""#3d6b20"" opacity=""0.8""/>
  </pattern>
  <pattern id=""brushPattern"" x=""0"" y=""0"" width=""15"" height=""15"" patternUnits=""userSpaceOnUse"">
    <rect width=""15"" height=""15"" fill=""#b6c38a""/>
    <path d=""M3,12 Q5,8 7,12 M9,10 Q11,6 13,10"" stroke=""#7a8e5a"" stroke-width=""1.5"" fill=""none"" opacity=""0.6""/>
    <circle cx=""5"" cy=""5"" r=""1"" fill=""#7a8e5a"" opacity=""0.45""/>
  </pattern>
  <pattern id=""grainPattern"" x=""0"" y=""0"" width=""10"" height=""20"" patternUnits=""userSpaceOnUse"">
    <rect width=""10"" height=""20"" fill=""#d9c178""/>
    <path d=""M2,0 L2,20 M5,0 L5,20 M8,0 L8,20"" stroke=""#b8960a"" stroke-width=""0.5""/>
  </pattern>
  <pattern id=""marshPattern"" x=""0"" y=""0"" width=""20"" height=""20"" patternUnits=""userSpaceOnUse"">
    <rect width=""20"" height=""20"" fill=""#9db9a4""/>
    <path d=""M0,6 C5,8 15,4 20,6 M0,14 C6,16 14,12 20,14"" stroke=""#627d6d"" stroke-width=""0.6"" fill=""none"" opacity=""0.6""/>
  </pattern>
  <pattern id=""sandPattern"" x=""0"" y=""0"" width=""20"" height=""20"" patternUnits=""userSpaceOnUse"">
    <rect width=""20"" height=""20"" fill=""#e3cf9e""/>
    <path d=""M0,9 Q6,7 12,9 Q16,10 20,9"" stroke=""#cbb986"" stroke-width=""0.7"" fill=""none"" opacity=""0.5""/>
  </pattern>
  <pattern id=""scrubPattern"" x=""0"" y=""0"" width=""20"" height=""20"" patternUnits=""userSpaceOnUse"">
    <rect width=""20"" height=""20"" fill=""#9fb389""/>
    <circle cx=""6"" cy=""6"" r=""1.4"" fill=""#6e7d5e"" opacity=""0.6""/>
    <circle cx=""13"" cy=""12"" r=""1.2"" fill=""#6e7d5e"" opacity=""0.5""/>
  </pattern>

  <!-- alias: openPattern -->
  <pattern id=""openPattern"" x=""0"" y=""0"" width=""24"" height=""24"" patternUnits=""userSpaceOnUse"">
    <rect width=""24"" height=""24"" fill=""#90a955""/>
  </pattern>

  <pattern id=""openGroundPattern"" x=""0"" y=""0"" width=""24"" height=""24"" patternUnits=""userSpaceOnUse"">
    <rect width=""24"" height=""24"" fill=""#90a955""/>
  </pattern>
");

        // --- building overlay patterns (ids match the JS viz set) ---
        sb.Append(@"
  <pattern id=""stone2"" width=""10"" height=""10"" patternUnits=""userSpaceOnUse"">
    <rect width=""10"" height=""10"" fill=""#8b7d6b""/>
    <rect x=""0"" y=""0"" width=""10"" height=""2"" fill=""#5c5248""/>
    <rect x=""0"" y=""5"" width=""10"" height=""2"" fill=""#5c5248""/>
  </pattern>
  <pattern id=""stone1"" width=""10"" height=""10"" patternUnits=""userSpaceOnUse"">
    <rect width=""10"" height=""10"" fill=""#a0937f""/>
    <line x1=""0"" y1=""5"" x2=""10"" y2=""5"" stroke=""#7a6e5c"" stroke-width=""1""/>
  </pattern>
  <pattern id=""wood"" width=""8"" height=""8"" patternUnits=""userSpaceOnUse"">
    <rect width=""8"" height=""8"" fill=""#8b6914""/>
    <line x1=""0"" y1=""0"" x2=""0"" y2=""8"" stroke=""#654b0e"" stroke-width=""0.5""/>
    <line x1=""4"" y1=""0"" x2=""4"" y2=""8"" stroke=""#654b0e"" stroke-width=""0.5""/>
  </pattern>
");

        sb.Append("</defs>");
        return sb.ToString();
    }
}
