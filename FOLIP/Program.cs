using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Fallout4;
using Noggog;

namespace FOLIP
{
    public class Program
    {
        static Lazy<LodSettings> _lazySettings = null!;
        static LodSettings Settings => _lazySettings.Value;
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<IFallout4Mod, IFallout4ModGetter>(RunPatch)
                .SetAutogeneratedSettings(
                    nickname: "Settings",
                    path: "settings.json",
                    out _lazySettings)
                .SetTypicalOpen(GameRelease.Fallout4, "FOLIP-Dynamic.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<IFallout4Mod, IFallout4ModGetter> state)
        {
            Console.WriteLine("FOLIP START");

            //Gather Assets
            Console.WriteLine("Identifying LOD assets...");

            List<string> lodMaterialFiles = new();
            List<string> lod4Meshes = new();
            List<string> lod8Meshes = new();
            List<string> lod16Meshes = new();
            List<string> lod32Meshes = new();
            List<string> lodMeshes = new();

            string[] lodMaterialFileLocations = { $"{state.DataFolderPath}\\materials\\lod", $"{state.DataFolderPath}\\materials\\dlc03\\lod", $"{state.DataFolderPath}\\materials\\dlc04\\lod" };
            string[] lodMeshesFileLocations = { $"{state.DataFolderPath}\\meshes\\lod", $"{state.DataFolderPath}\\meshes\\dlc03\\lod", $"{state.DataFolderPath}\\meshes\\dlc04\\lod" };
            string[] patternReplaceListMaterials = { "", "dlc03\\", "dlc04\\" };
            string[] patternReplaceListMeshes = { "lod\\", "dlc03\\lod\\", "dlc04\\lod\\" };

            lodMaterialFiles = GameAssets.Files(lodMaterialFileLocations, "*.bgsm", patternReplaceListMaterials);

            if (Settings.devCode)
            {
                lod4Meshes = GameAssets.Files(lodMeshesFileLocations, "*lod_0.nif", patternReplaceListMeshes);
                lod8Meshes = GameAssets.Files(lodMeshesFileLocations, "*lod_1.nif", patternReplaceListMeshes);
                lod16Meshes = GameAssets.Files(lodMeshesFileLocations, "*lod_2.nif", patternReplaceListMeshes);
                lod32Meshes = GameAssets.Files(lodMeshesFileLocations, "*lod_3.nif", patternReplaceListMeshes);
                lodMeshes = GameAssets.Files(lodMeshesFileLocations, "*lod.nif", patternReplaceListMeshes);
            }

            //Handle Material Swaps
            Console.WriteLine("Adding LOD material swaps...");

            List<string> missingMaterials = new();

            foreach (var materialSwap in state.LoadOrder.PriorityOrder.MaterialSwap().WinningOverrides())
            {
                List<string> lodSubstitutionsOriginal = new();
                List<string> lodSubstitutionsReplacement = new();
                List<string> existingSubstitutions = new();
                int n = -1;

                //make a list of existing substitutions, which we will need to then check to make sure we don't add duplicate substitutions
                foreach (var substitution in materialSwap.Substitutions)
                {
                    var existingOriginalSubstitution = substitution.OriginalMaterial;
                    if (existingOriginalSubstitution is null) continue;
                    existingOriginalSubstitution = existingOriginalSubstitution.ToLower();
                    existingSubstitutions.Add(existingOriginalSubstitution);
                }

                foreach (var substitution in materialSwap.Substitutions)
                {
                    var theOriginalMaterial = substitution.OriginalMaterial;
                    if (theOriginalMaterial is null) continue;
                    theOriginalMaterial = theOriginalMaterial.ToLower();

                    //If the original material does not have a direct lod material, continue.
                    if (!lodMaterialFiles.Contains(theOriginalMaterial)) continue;
                    var theReplacementMaterial = substitution.ReplacementMaterial;
                    if (theReplacementMaterial is null) continue;
                    theReplacementMaterial = theReplacementMaterial.ToLower();

                    //I used this code to find a specific problematic material swap, where the swap file didn't exist.
                    //if (theReplacementMaterial == "landscape\\ground\\grassdried01.bgsm")
                    //    Console.WriteLine(materialSwap.FormKey);

                    //If the replacement material does not have a direct lod material, continue.
                    if (!lodMaterialFiles.Contains(theReplacementMaterial))
                    {
                        if (!missingMaterials.Contains(theReplacementMaterial))
                            missingMaterials.Add($"{theReplacementMaterial} RM\n\tfrom {theOriginalMaterial} OM.");
                        continue;
                    }
                    if (substitution.ColorRemappingIndex is not null)
                    {
                        if (Settings.verboseConsoleLog) Console.WriteLine($"Note for LOD author: {materialSwap.FormKey} has a Color Remapping Index of {substitution.ColorRemappingIndex}. Please manually check this material swap for proper handling.");
                        continue;
                    }

                    theOriginalMaterial = theOriginalMaterial.Split(Path.DirectorySeparatorChar)[0] switch
                    {
                        "dlc04" => theOriginalMaterial.Replace("dlc04\\", "DLC04\\LOD\\"),
                        "dlc03" => theOriginalMaterial.Replace("dlc03\\", "DLC03\\LOD\\"),
                        _ => $"LOD\\{theOriginalMaterial}",
                    };

                    //If an already existing material swap exists for the lod material, skip.
                    if (existingSubstitutions.Contains(theOriginalMaterial.ToLower())) continue;

                    theReplacementMaterial = theReplacementMaterial.Split(Path.DirectorySeparatorChar)[0] switch
                    {
                        "dlc04" => theReplacementMaterial.Replace("dlc04\\", "DLC04\\LOD\\"),
                        "dlc03" => theReplacementMaterial.Replace("dlc03\\", "DLC03\\LOD\\"),
                        _ => $"LOD\\{theReplacementMaterial}",
                    };

                    //If the material swap defines replacing with the exact same material, then skip. Yes, Bethesda has put some swaps that literally say to swap with the exact same material for no reason.
                    if (theOriginalMaterial == theReplacementMaterial) continue;

                    //If the original material and replacement material both have direct lod materials, add them to the lists.
                    lodSubstitutionsOriginal.Add(theOriginalMaterial);
                    lodSubstitutionsReplacement.Add(theReplacementMaterial);
                    n += 1;
                }
                if (n < 0) continue;
                var myFavoriteMaterialSwap = state.PatchMod.MaterialSwaps.GetOrAddAsOverride(materialSwap);
                while (n >= 0)
                {
                    myFavoriteMaterialSwap.Substitutions.Add(new MaterialSubstitution
                    {
                        OriginalMaterial = $"{lodSubstitutionsOriginal[n]}",
                        ReplacementMaterial = $"{lodSubstitutionsReplacement[n]}"
                    });
                    n--;
                }
            }

            //Add notes about possible missed materials for material swaps for lod author.
            if (Settings.verboseConsoleLog)
            {
                missingMaterials = missingMaterials.OrderBy(q => q).ToList();
                foreach (string missingMaterial in missingMaterials) Console.WriteLine($"Note for LOD author: Skipped\t{missingMaterial}");
            }

            if (Settings.devCode)
            {
                //Add lod meshes to static records
                Console.WriteLine("Assigning LOD models...");

                foreach (var staticRecord in state.LoadOrder.PriorityOrder.Static().WinningOverrides())
                {
                    if (staticRecord is null || staticRecord.Model is null || staticRecord.Model.File is null) continue;
                    //Console.WriteLine(staticRecord.Model.File);
                    //Console.WriteLine(Path.GetPathRoot(staticRecord.Model.File));
                    //Console.WriteLine(staticRecord.Model.File);
                    string baseFolder = staticRecord.Model.File.Split(Path.DirectorySeparatorChar)[0].ToLower();

                    string possibleLOD4Mesh;
                    bool lod4MeshExists = false;
                    string possibleLOD8Mesh;
                    bool lod8MeshExists = false;
                    string possibleLOD16Mesh;
                    bool lod16MeshExists = false;
                    string possibleLOD32Mesh;
                    bool lod32MeshExists = false;
                    string possibleLODMesh;
                    bool lodMeshExists = false;
                    string[] assignedlodMeshes = { "", "", "", "" };
                    bool hasLodMeshes = false;

                    switch (baseFolder)
                    {
                        case "dlc03":
                            possibleLOD4Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc03\\", "dlc03\\lod\\").Replace(".nif", "_lod_0.nif")}";
                            possibleLOD8Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc03\\", "dlc03\\lod\\").Replace(".nif", "_lod_1.nif")}";
                            possibleLOD16Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc03\\", "dlc03\\lod\\").Replace(".nif", "_lod_2.nif")}";
                            possibleLOD32Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc03\\", "dlc03\\lod\\").Replace(".nif", "_lod_3.nif")}";
                            possibleLODMesh = $"{staticRecord.Model.File.ToLower().Replace("dlc03\\", "dlc03\\lod\\").Replace(".nif", "_lod.nif")}";
                            break;
                        case "dlc04":
                            possibleLOD4Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc04\\", "dlc04\\lod\\").Replace(".nif", "_lod_0.nif")}";
                            possibleLOD8Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc04\\", "dlc04\\lod\\").Replace(".nif", "_lod_1.nif")}";
                            possibleLOD16Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc04\\", "dlc04\\lod\\").Replace(".nif", "_lod_2.nif")}";
                            possibleLOD32Mesh = $"{staticRecord.Model.File.ToLower().Replace("dlc04\\", "dlc04\\lod\\").Replace(".nif", "_lod_3.nif")}";
                            possibleLODMesh = $"{staticRecord.Model.File.ToLower().Replace("dlc04\\", "dlc04\\lod\\").Replace(".nif", "_lod.nif")}";
                            break;
                        default:
                            possibleLOD4Mesh = $"lod\\{staticRecord.Model.File.ToLower().Replace(".nif", "_lod_0.nif")}";
                            possibleLOD8Mesh = $"lod\\{staticRecord.Model.File.ToLower().Replace(".nif", "_lod_1.nif")}";
                            possibleLOD16Mesh = $"lod\\{staticRecord.Model.File.ToLower().Replace(".nif", "_lod_2.nif")}";
                            possibleLOD32Mesh = $"lod\\{staticRecord.Model.File.ToLower().Replace(".nif", "_lod_3.nif")}";
                            possibleLODMesh = $"lod\\{staticRecord.Model.File.ToLower().Replace(".nif", "_lod.nif")}";
                            break;
                    }
                    if (lod4Meshes.Contains(possibleLOD4Mesh))
                    {
                        lod4MeshExists = true;
                        hasLodMeshes = true;
                        assignedlodMeshes[0] = possibleLOD4Mesh;
                    }
                    else if (lodMeshes.Contains(possibleLODMesh))
                    {
                        lodMeshExists = true;
                        hasLodMeshes = true;
                        assignedlodMeshes[0] = possibleLODMesh;
                    }
                    if (lod8Meshes.Contains(possibleLOD8Mesh))
                    {
                        lod8MeshExists = true;
                        hasLodMeshes = true;
                        assignedlodMeshes[1] = possibleLOD8Mesh;
                    }
                    if (lod16Meshes.Contains(possibleLOD16Mesh))
                    {
                        lod16MeshExists = true;
                        hasLodMeshes = true;
                        assignedlodMeshes[2] = possibleLOD16Mesh;
                    }
                    if (lod32Meshes.Contains(possibleLOD32Mesh))
                    {
                        lod32MeshExists = true;
                        hasLodMeshes = true;
                        assignedlodMeshes[3] = possibleLOD32Mesh;
                    }

                    //foreach (var distantLODMesh in staticRecord.DistantLods)
                    //    Console.WriteLine(distantLODMesh.Mesh);

                    //Skip statics that don't have any lod meshes
                    if (!hasLodMeshes) continue;

                    if (staticRecord is null) continue;
                    // Set the HasDistantLOD flag if it isn't already set.

                    var myFavoriteStatic = state.PatchMod.Statics.GetOrAddAsOverride(staticRecord);
                    if (!EnumExt.HasFlag(staticRecord.MajorRecordFlagsRaw, (int)Static.MajorFlag.HasDistantLod))
                    {
                        myFavoriteStatic.MajorRecordFlagsRaw = EnumExt.SetFlag(myFavoriteStatic.MajorRecordFlagsRaw, (int)Static.MajorFlag.HasDistantLod, true);
                        Console.WriteLine($"{myFavoriteStatic.FormKey}     {assignedlodMeshes[0]}     {assignedlodMeshes[1]}     {assignedlodMeshes[2]}     {assignedlodMeshes[3]}");
                    }

                    // This doesn't work.
                    //add lod meshes
                    //if (lod4MeshExists)
                    //    myFavoriteStatic.DistantLods[0].Mesh = possibleLOD4Mesh;
                    //else if (lodMeshExists)
                    //    myFavoriteStatic.DistantLods[0].Mesh = possibleLODMesh;
                    //if (lod8MeshExists)
                    //    myFavoriteStatic.DistantLods[1].Mesh = possibleLOD8Mesh;
                    //if (lod16MeshExists)
                    //    myFavoriteStatic.DistantLods[2].Mesh = possibleLOD16Mesh;
                    //if (lod32MeshExists)
                    //    myFavoriteStatic.DistantLods[3].Mesh = possibleLOD32Mesh;
                    //int i = 0;

                    //foreach (var distantLodList in myFavoriteStatic.DistantLods)
                    //{
                    //    //Console.WriteLine(distantLodList.Data);
                    //    distantLodList.Clear();
                    //    switch (i)
                    //    {
                    //        case 0:
                    //            if (lod4MeshExists) distantLodList.Mesh = possibleLOD4Mesh;
                    //            else if (lodMeshExists) distantLodList.Mesh = possibleLODMesh;
                    //            break;
                    //        case 1:
                    //            if (lod8MeshExists) distantLodList.Mesh = possibleLOD8Mesh;
                    //            break;
                    //        case 2:
                    //            if (lod16MeshExists) distantLodList.Mesh = possibleLOD16Mesh;
                    //            break;
                    //        case 3:
                    //            if (lod32MeshExists) distantLodList.Mesh = possibleLOD32Mesh;
                    //            break;
                    //    }
                    //    i++;
                    //}


                }


                //Add lod meshes for moveable statics... this might be hard lol
            }
        }
    }
}
