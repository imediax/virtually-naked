using Newtonsoft.Json;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

public class Actor : IDisposable {
	public static Actor Load(IArchiveDirectory dataDir, Device device, ShaderCache shaderCache, ControllerManager controllerManager, FigureLoader figureLoader) {
		var mainFigure = figureLoader.Load(InitialSettings.Main, null);

		var actorModel = ActorModel.Load(mainFigure.Definition, InitialSettings.Animation);

		var hairFigure = InitialSettings.Hair != null ? figureLoader.Load(InitialSettings.Hair, mainFigure.Definition) : null;
				
		var behavior = ActorBehavior.Load(controllerManager, mainFigure.Definition.Directory, actorModel);

		var characters = Character.LoadList(dataDir);
		var outfits = Outfit.LoadList(dataDir);

		var actor = new Actor(device, characters, outfits, actorModel, figureLoader, mainFigure, hairFigure, behavior);
		var clothingFigures = InitialSettings.Clothing
			.Select(clothingFigureName => figureLoader.Load(clothingFigureName, mainFigure.Definition))
			.ToArray();
		actor.SetClothingFigures(clothingFigures);

		return actor;
	}
	
	class MainFigureAnimator : IFigureAnimator {
		private readonly Actor actor;

		public MainFigureAnimator(Actor actor) {
			this.actor = actor;
		}

		public ChannelInputs GetFrameInputs(ChannelInputs shapeInputs, FrameUpdateParameters updateParameters, ControlVertexInfo[] previousFrameControlVertexInfos) {
			return actor.Behavior.Update(shapeInputs, updateParameters, previousFrameControlVertexInfos);
		}
	}

	private readonly List<Character> characters;
	private readonly List<Outfit> outfits;
	private readonly ActorModel model;
	private readonly FigureLoader figureLoader;
	private readonly FigureFacade mainFigure;
	private readonly FigureFacade hairFigure;
	private readonly ActorBehavior behavior;

	private FigureFacade[] clothingFigures;
	private readonly FigureGroup figureGroup;
	
	public Actor(Device device, List<Character> characters, List<Outfit> outfits, ActorModel model, FigureLoader figureLoader, FigureFacade mainFigure, FigureFacade hairFigure, ActorBehavior behavior) {
		this.characters = characters;
		this.outfits = outfits;
		this.model = model;
		this.figureLoader = figureLoader;
		this.mainFigure = mainFigure;
		this.hairFigure = hairFigure;
		this.behavior = behavior;

		clothingFigures = new FigureFacade[0];
		
		mainFigure.Animator = new MainFigureAnimator(this);

		figureGroup = new FigureGroup(device, mainFigure, new FigureFacade[0]);
		SyncFigureGroup();
	}

	public void Dispose() {
		figureGroup.Dispose();
		mainFigure.Dispose();
		hairFigure?.Dispose();
		foreach (var clothingFigure in clothingFigures) {
			clothingFigure.Dispose();
		}
	}
	
	public List<Character> Characters => characters;
	public List<Outfit> Outfits => outfits;
	public ActorModel Model => model;
	public FigureFacade Main => mainFigure;
	public FigureFacade Hair => hairFigure;
	public FigureFacade[] Clothing => clothingFigures;
	public ActorBehavior Behavior => behavior;
	
	public IMenuLevel MenuLevel => ActorMenuProvider.MakeRootMenuLevel(this);

	public event Action ClothingChanged;
	
	public void SetClothing(FigureFacade.Recipe[] recipes) {
		var newClothingFigures = recipes
			.Select(recipe => figureLoader.Load(recipe, mainFigure.Definition))
			.ToArray();
		SetClothingFigures(newClothingFigures);
	}

	private void SetClothingFigures(FigureFacade[] newClothingFigures) {
		foreach (var clothingFigure in clothingFigures) {
			clothingFigure.Dispose();
		}
		clothingFigures = newClothingFigures;
		SyncFigureGroup();
		ClothingChanged?.Invoke();
	}

	private void SyncFigureGroup() {
		var childFigures = Enumerable.Repeat(hairFigure, hairFigure == null ? 0 : 1)
			.Concat(clothingFigures)
			.ToArray();
		figureGroup.SetChildFigures(childFigures);
	}

	public void Update(DeviceContext context, FrameUpdateParameters updateParameters, ImageBasedLightingEnvironment iblEnvironment) {
		figureGroup.Update(context, updateParameters, iblEnvironment);
	}

	public void RenderPass(DeviceContext context, RenderingPass pass) {
		figureGroup.RenderPass(context, pass);
	}

	public void DoPostwork(DeviceContext context) {
		figureGroup.DoPostwork(context);
	}
	
	public class Recipe {
		[JsonProperty("main")]
		public FigureFacade.Recipe main;

		[JsonProperty("hair")]
		public FigureFacade.Recipe hair;

		[JsonProperty("clothing")]
		public FigureFacade.Recipe[] clothing;
		
		[JsonProperty("animation")]
		public string animation;

		[JsonProperty("behaviour")]
		public BehaviorModel.Recipe behaviour;

		[JsonProperty("channel-values")]
		public Dictionary<string, double> channelValues;

		[JsonProperty("pose")]
		public ActorBehavior.PoseRecipe pose;

		public void Merge(Actor actor) {
			main?.Merge(actor.Main);
			hair?.Merge(actor.Hair);
			if (animation != null) {
				actor.model.Animation.ActiveName = animation;
			}
			behaviour?.Merge(actor.model.Behavior);
			if (channelValues != null) {
				actor.model.UserValues = channelValues;
			}
			actor.SetClothing(clothing ?? new FigureFacade.Recipe[0]);
			pose?.Merge(actor.Behavior);
		}
	}
	
	public Recipe Recipize() {
		return new Recipe {
			main = Main.Recipize(),
			hair = Hair?.Recipize(),
			clothing = Clothing.Select(figure => figure.Recipize()).ToArray(),
			animation = model.Animation.ActiveName,
			behaviour = model.Behavior.Recipize(),
			channelValues = model.UserValues,
			pose = Behavior.RecipizePose()
		};
	}
}
