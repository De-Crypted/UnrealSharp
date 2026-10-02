using System;
using EpicGames.UHT.Types;
using UnrealSharpManagedGlue.Attributes;
using UnrealSharpManagedGlue.SourceGeneration;
using UnrealSharpManagedGlue.Utilities;
using UnrealSharpManagedGlue.Tooltip;

namespace UnrealSharpManagedGlue.Exporters;

public static class EnumExporter
{
    public static void ExportEnum(UhtEnum enumObj)
    {
        GeneratorStringBuilder stringBuilder = new GeneratorStringBuilder();
        
        stringBuilder.StartGlueFile(enumObj);
        stringBuilder.AppendTooltip(enumObj);
        
        AttributeBuilder attributeBuilder = new AttributeBuilder(enumObj);
        attributeBuilder.AddGeneratedTypeAttribute(enumObj);
        attributeBuilder.Finish();
        
        stringBuilder.AppendLine(attributeBuilder.ToString());
        
        string underlyingType = GetUnderlyingType(enumObj);
        stringBuilder.DeclareType(enumObj, "enum", enumObj.GetStructName(), underlyingType, isPartial: false);
        
        int enumValuesCount = enumObj.EnumValues.Count;
        for (int i = 0; i < enumValuesCount; i++)
        {
            UhtEnumValue enumValue = enumObj.EnumValues[i];

            string toolTip = enumObj.GetMetadata("Tooltip", i);
            stringBuilder.AppendTooltip(toolTip);
            
            string cleanValueName = ScriptGeneratorUtilities.GetCleanEnumValueName(enumObj, enumValue);
            string value = enumValue.Value == -1 ? "," : $" = {enumValue.Value},";
            
            stringBuilder.AppendLine($"{cleanValueName}{value}");
        }
        
        stringBuilder.CloseBrace();
        stringBuilder.EndGlueFile(enumObj);
        
        FileExporter.SaveGlueToDisk(enumObj, stringBuilder);
    }

    private static string GetUnderlyingType(UhtEnum enumObj)
    {
        (long min, long max) range = GetEmittedValueRange(enumObj);
        foreach ((string type, long min, long max) in GetUnderlyingTypeCandidates(enumObj.UnderlyingType))
        {
            if (range.min >= min && range.max <= max)
            {
                return type;
            }
        }

        return "long";
    }

    /// <summary>
    /// Simulates the C# values that will be emitted: explicit values for parsed entries and C#
    /// auto-increment for unparsed entries (emitted as bare enum members).
    /// </summary>
    private static (long min, long max) GetEmittedValueRange(UhtEnum enumObj)
    {
        long min = long.MaxValue;
        long max = long.MinValue;
        long lastAssigned = 0;
        for (int i = 0; i < enumObj.EnumValues.Count; i++)
        {
            long value = enumObj.EnumValues[i].Value;
            if (value == -1)
            {
                value = i == 0 ? 0 : lastAssigned + 1;
            }
            min = Math.Min(min, value);
            max = Math.Max(max, value);
            lastAssigned = value;
        }
        return (min, max);
    }

    /// <summary>
    /// Candidate C# underlying types, starting with the type mapped from the native underlying type
    /// and widening until the emitted values fit.
    /// </summary>
    private static (string type, long min, long max)[] GetUnderlyingTypeCandidates(UhtEnumUnderlyingType underlyingType)
    {
        switch (underlyingType)
        {
            case UhtEnumUnderlyingType.Unspecified:
                return [("int", int.MinValue, int.MaxValue), ("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Uint8:
                return [("byte", 0, 255), ("short", short.MinValue, short.MaxValue), ("int", int.MinValue, int.MaxValue), ("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Int8:
                return [("sbyte", sbyte.MinValue, sbyte.MaxValue), ("short", short.MinValue, short.MaxValue), ("int", int.MinValue, int.MaxValue), ("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Uint16:
                return [("ushort", 0, 65535), ("int", int.MinValue, int.MaxValue), ("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Int16:
                return [("short", short.MinValue, short.MaxValue), ("int", int.MinValue, int.MaxValue), ("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Int:
            case UhtEnumUnderlyingType.Int32:
                return [("int", int.MinValue, int.MaxValue), ("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Uint32:
                return [("uint", 0, uint.MaxValue), ("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Int64:
                return [("long", long.MinValue, long.MaxValue)];
            case UhtEnumUnderlyingType.Uint64:
                return [("ulong", 0, long.MaxValue)];
            default:
                throw new ArgumentOutOfRangeException(nameof(underlyingType), underlyingType, null);
        }
    }
}