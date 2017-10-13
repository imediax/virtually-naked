﻿using System.Collections.Generic;
using System.Linq;

public class ImportSettings {
	public static ImportSettings MakeReleaseSettings() {
		var ReleaseCharacters = new HashSet<string> { "Mei Lin", "Rune", "Eva", "Monique", "Victoria" };
		
		return new ImportSettings {
			CompressTextures = true,

			Environments = {
				"Maui",
				"Outdoor A",
				"Outdoor B",
				"Room",
				"Ruins A",
				"Ruins B",
				"Ruins C",
				"Studio",
				"Studio",
				"Sunset"
			},

			Figures = new Dictionary<string, FigureImportSettings> {
				["genesis-3-female"] = new FigureImportSettings {
					Shapes = ReleaseCharacters,
					MaterialSets = ReleaseCharacters
				},

				["liv-hair"] = new FigureImportSettings {
					Shapes = null, //all
					MaterialSets = null, //all
				}
			}
		};
	}

	public static ImportSettings MakeFromViewerInitialSettings() {
		List<string> figureNames = new List<string>() { };
		figureNames.Add(InitialSettings.Main);
		if (InitialSettings.Hair != null) {
			figureNames.Add(InitialSettings.Hair);
		}
		figureNames.AddRange(InitialSettings.Clothing);
		
		return new ImportSettings {
			CompressTextures = false,
			Environments = { InitialSettings.Environment },
			Figures = figureNames.ToDictionary(
				name => name,
				name => FigureImportSettings.MakeFromViewInitialSettings(name))
		};
	}

	public bool CompressTextures { get; set; } = false;

	private HashSet<string> Environments { get; set; } = new HashSet<string>();
	
	private Dictionary<string, FigureImportSettings> Figures { get; set; } = new Dictionary<string, FigureImportSettings>();
	
	public bool ShouldImportEnvironment(string name) {
		return name == InitialSettings.Environment;
	}

	public IEnumerable<string> FiguresToImport => Figures.Keys;

	public bool ShouldImportShape(string figureName, string shapeName) {
		var figureSettings = Figures[figureName];
		return (figureSettings.Shapes == null && shapeName != "Base") || figureSettings.Shapes.Contains(shapeName);
	}

	public bool ShouldImportMaterialSet(string figureName, string materialSetName) {
		var figureSettings = Figures[figureName];
		return figureSettings.MaterialSets == null || figureSettings.MaterialSets.Contains(materialSetName);
	}
}

public class FigureImportSettings {
	public static FigureImportSettings MakeFromViewInitialSettings(string figure) {
		HashSet<string> shapes;
		if (InitialSettings.Shapes.TryGetValue(figure, out string initialShape)) {
			shapes = new HashSet<string> { initialShape };
		} else {
			shapes = new HashSet<string> { };
		}

		string initialMaterialSet = InitialSettings.MaterialSets[figure];
			
		return new FigureImportSettings {
			Shapes = shapes,
			MaterialSets = { initialMaterialSet }
		};
	}

	public HashSet<string> Shapes { get; set; } = new HashSet<string>(); // null means all except "Base"
	public HashSet<string> MaterialSets { get; set; } = new HashSet<string>(); // null means all
}