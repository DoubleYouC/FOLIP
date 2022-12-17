namespace FOLIP
{
	public class GameAssets
	{
		public static List<string> Files(string[] lookupFolders, string lookupPattern, string[] replacePattern)
        {
			List<string> listOfFiles = new();

			for (int i = 0; i < lookupFolders.Length; i++) {
				string[] looseFiles = Directory.GetFiles(lookupFolders[i], lookupPattern, SearchOption.AllDirectories);
				foreach (var individualFile in looseFiles)
				{
					string individualFileSplit = individualFile.Replace(lookupFolders[i] + "\\", replacePattern[i]);
					listOfFiles.Add(individualFileSplit.ToLower());
				}
			}

			return listOfFiles;
		}

        //I was going to attempt to include the lod files included in archives, but this is too much work for no real gain. Just leave your lod files loose. They aren't used in game anyway, so no actual load difference.

        //List<string> lodMaterialsArchives = new List<string>();
        //List<string> lodDLC03MaterialsArchives = new List<string>();
        //List<string> lodDLC04MaterialsArchives = new List<string>();

        //List<string> lodMeshesArchives = new List<string>();
        //List<string> lodDLC03MeshesArchives = new List<string>();
        //List<string> lodDLC04MeshesArchives = new List<string>();

        //var activeArchives = Archive.GetApplicableArchivePaths(GameRelease.Fallout4, state.DataFolderPath);

        //foreach (string activeArchive in activeArchives)
        //{
        //    var archiveReader = Archive.CreateReader(GameRelease.Fallout4, activeArchive);
        //    bool doLODMaterialsExist = archiveReader.TryGetFolder("materials\\lod", out var lodMaterialsFolder);
        //    bool doDLC03LODMaterialsExist = archiveReader.TryGetFolder("meshes\\DLC03\\lod", out var lodDLC03MaterialsFolder);
        //    bool doDLC04LODMaterialsExist = archiveReader.TryGetFolder("meshes\\DLC04\\lod", out var lodDLC04MaterialsFolder);

        //    bool doLODMeshesExist = archiveReader.TryGetFolder("meshes\\lod", out var lodMeshesFolder);
        //    bool doDLC03LODMeshesExist = archiveReader.TryGetFolder("meshes\\DLC03\\lod", out var lodDLC03MeshesFolder);
        //    bool doDLC04LODMeshesExist = archiveReader.TryGetFolder("meshes\\DLC04\\lod", out var lodDLC04MeshesFolder);

        //    if (doLODMaterialsExist)
        //    {
        //        lodMaterialsArchives.Add(activeArchive);
        //    }

        //    if (doDLC03LODMaterialsExist)
        //    {
        //        lodDLC03MaterialsArchives.Add(activeArchive);
        //    }

        //    if (doDLC04LODMaterialsExist)
        //    {
        //        lodDLC04MaterialsArchives.Add(activeArchive);
        //    }

        //    if (doLODMeshesExist)
        //    {
        //        lodMeshesArchives.Add(activeArchive);
        //    }

        //    if (doDLC03LODMeshesExist)
        //    {
        //        lodDLC03MeshesArchives.Add(activeArchive);
        //    }

        //    if (doDLC04LODMeshesExist)
        //    {
        //        lodDLC04MeshesArchives.Add(activeArchive);
        //    }
        //}
    }
}
