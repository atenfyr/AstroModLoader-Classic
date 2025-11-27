using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UAssetAPI.UnrealTypes;
using UAssetAPI;
using System.IO;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;

namespace AstroModIntegrator
{
    public class GameMenuDisplayOptionsBaker
    {
        private ModIntegrator ParentIntegrator;

        public GameMenuDisplayOptionsBaker(ModIntegrator ParentIntegrator)
        {
            this.ParentIntegrator = ParentIntegrator;
        }

        // superRawData = data at /Game/UI/PauseMenu/SubMenus/GameMenuOptionsSubmenu
        public UAsset Bake(byte[] superRawData, EngineVersion engVer)
        {
            UAsset y = new UAsset(engVer);
            y.UseSeparateBulkDataFiles = true;
            y.CustomSerializationFlags = CustomSerializationFlags.SkipParsingBytecode | CustomSerializationFlags.SkipPreloadDependencyLoading;
            var reader = new AssetBinaryReader(new MemoryStream(superRawData), y);
            y.Read(reader);

            NormalExport doubleTextExport = null;
            NormalExport slotExport = null;
            FPackageIndex packageIndexOfOldDoubleTextExport = null;
            FPackageIndex packageIndexOfOldVersionSlotExport = null;
            int maxSlotExportNumber = 0;
            NormalExport verticalBoxExport = null;
            int expIndex = 1;
            foreach (Export exp in y.Exports)
            {
                if (exp.ObjectName.ToString() == "VersionNumber" && exp is NormalExport nExp)
                {
                    packageIndexOfOldDoubleTextExport = FPackageIndex.FromRawIndex(expIndex);

                    // clone this for our new export
                    doubleTextExport = (NormalExport)nExp.Clone();
                    doubleTextExport.Data = new List<PropertyData>(nExp.Data.Count);
                    nExp.Data.ForEach((item) =>
                    {
                        doubleTextExport.Data.Add((PropertyData)item.Clone());
                    });

                    doubleTextExport.ObjectName = FName.FromString(y, "VersionNumberAML");

                    StructPropertyData displayDataProperty = doubleTextExport["DisplayData"] as StructPropertyData;
                    if (displayDataProperty == null) throw new FormatException("Unable to find DisplayData in VersionNumber export");
                    TextPropertyData rightTextProperty = displayDataProperty["RightText"] as TextPropertyData;
                    if (rightTextProperty == null) throw new FormatException("Unable to find RightText in DisplayData in VersionNumber export");

                    TextPropertyData leftTextProperty = (TextPropertyData)rightTextProperty.Clone();
                    leftTextProperty.Name = FName.FromString(y, "LeftText");

                    leftTextProperty.CultureInvariantString = FString.FromString("MOD INTEGRATOR");
                    rightTextProperty.CultureInvariantString = FString.FromString("Classic " + IntegratorUtils.CurrentVersion.ToString());

                    displayDataProperty.Value = [leftTextProperty, rightTextProperty];

                    // also clone the slot
                    NormalExport exp2 = ((ObjectPropertyData)doubleTextExport["Slot"]).ToExport(y) as NormalExport;
                    if (exp2 == null) throw new FormatException("Unable to find Slot in VersionNumber export");
                    packageIndexOfOldVersionSlotExport = ((ObjectPropertyData)doubleTextExport["Slot"]).Value;

                    slotExport = (NormalExport)exp2.Clone();
                    slotExport.Data = new List<PropertyData>(exp2.Data.Count);
                    exp2.Data.ForEach((item) =>
                    {
                        slotExport.Data.Add((PropertyData)item.Clone());
                    });
                    ((ObjectPropertyData)slotExport["Content"]).Value = FPackageIndex.FromRawIndex(y.Exports.Count + 1); // doubleTextExport

                    verticalBoxExport = ((ObjectPropertyData)slotExport["Parent"]).ToExport(y) as NormalExport;

                    ((ObjectPropertyData)doubleTextExport["Slot"]).Value = FPackageIndex.FromRawIndex(y.Exports.Count + 2); // slotExport
                }

                if (exp.ObjectName?.Value?.Value == "VerticalBoxSlot" && exp.ObjectName.Number > maxSlotExportNumber)
                {
                    maxSlotExportNumber = exp.ObjectName.Number;
                }

                expIndex++;
            }

            if (doubleTextExport == null) throw new FormatException("Unable to find VersionNumber export");
            if (slotExport == null) throw new FormatException("Unable to find slot for VersionNumber export");
            if (verticalBoxExport == null) throw new FormatException("Unable to find VerticalBox export");
            slotExport.ObjectName = new FName(y, slotExport.ObjectName.Value, maxSlotExportNumber + 1);

            // now add reference to slot export in VerticalBox
            ArrayPropertyData slotsArr = verticalBoxExport["Slots"] as ArrayPropertyData;
            if (slotsArr == null) throw new FormatException("Unable to find Slots array in VerticalBox export");

            PropertyData[] oldDat = slotsArr.Value;
            PropertyData[] newDat = new PropertyData[oldDat.Length + 1];
            int indexRightNow = 0;
            for (int i = 0; i < oldDat.Length; i++)
            {
                newDat[indexRightNow] = oldDat[i];
                if (oldDat[i] is ObjectPropertyData oDat && oDat.Value.Index == packageIndexOfOldVersionSlotExport.Index)
                {
                    newDat[indexRightNow + 1] = new ObjectPropertyData(FName.DefineDummy(y, "-1")); // name is irrelevant because it's in an array
                    newDat[indexRightNow + 1].RawValue = FPackageIndex.FromRawIndex(y.Exports.Count + 2); // slotExport
                    indexRightNow++;
                }
                indexRightNow++;
            }

            slotsArr.Value = newDat;

            // replace deps (in a lazy way)
            for (int i = 0; i < doubleTextExport.CreateBeforeSerializationDependencies.Count; i++)
            {
                if (doubleTextExport.CreateBeforeSerializationDependencies[i].Index == packageIndexOfOldDoubleTextExport.Index) doubleTextExport.CreateBeforeSerializationDependencies[i] = FPackageIndex.FromRawIndex(y.Exports.Count + 1);
                if (doubleTextExport.CreateBeforeSerializationDependencies[i].Index == packageIndexOfOldVersionSlotExport.Index) doubleTextExport.CreateBeforeSerializationDependencies[i] = FPackageIndex.FromRawIndex(y.Exports.Count + 2);
            }
            for (int i = 0; i < slotExport.CreateBeforeSerializationDependencies.Count; i++)
            {
                if (slotExport.CreateBeforeSerializationDependencies[i].Index == packageIndexOfOldDoubleTextExport.Index) slotExport.CreateBeforeSerializationDependencies[i] = FPackageIndex.FromRawIndex(y.Exports.Count + 1);
                if (slotExport.CreateBeforeSerializationDependencies[i].Index == packageIndexOfOldVersionSlotExport.Index) slotExport.CreateBeforeSerializationDependencies[i] = FPackageIndex.FromRawIndex(y.Exports.Count + 2);
            }

            // now add the two exports
            y.Exports.Add(doubleTextExport);
            y.Exports.Add(slotExport);

            // all done
            return y;
        }
    }
}
