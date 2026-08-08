#region References

using System;
using System.Threading;
using Cornerstone.Collections;
using Cornerstone.Compare;
using Cornerstone.Data;
using Cornerstone.Generators;
using Cornerstone.Presentation;
using Cornerstone.Reflection;
using Cornerstone.Runtime;

#endregion

namespace Cornerstone.Sample.Tabs;

/// <summary>
/// Projects a model SpeedyList into a presentation list via TrackCollection.
/// </summary>
[SourceReflection]
[Notifiable(["*"])]
[Updateable(UpdateableAction.All, ["*"])]
public partial class TabAppDispatcherCollectionsViewModel : DispatchableViewModel
{
	#region Constants

	public const int MaxItems = 30;
	public const int SeedCount = 5;

	#endregion

	#region Fields

	private static readonly GenericEqualityComparer<TabAppDispatcherSampleCollectionItem> _idComparer;
	private int _nextId;

	#endregion

	#region Constructors

	[DependencyInjectionConstructor]
	public TabAppDispatcherCollectionsViewModel()
	{
		Model = new SpeedyList<TabAppDispatcherSampleCollectionItem>(MaxItems + 8, true);
		for (var i = 0; i < SeedCount; i++)
		{
			Model.Add(CreateSampleItem());
		}

		Items = [];
		TrackCollection(Model, Items, _idComparer, CollectionReconcileMode.ListAndItems);
	}

	static TabAppDispatcherCollectionsViewModel()
	{
		_idComparer = new(
			(x, y) => (x != null) && (y != null) && (x.Id == y.Id),
			x => x.Id
		);
	}

	#endregion

	#region Properties

	public PresentationList<TabAppDispatcherSampleCollectionItem> Items { get; }

	public SpeedyList<TabAppDispatcherSampleCollectionItem> Model { get; }

	#endregion

	#region Methods

	public TabAppDispatcherSampleCollectionItem CreateSampleItem()
	{
		var id = Interlocked.Increment(ref _nextId);
		var name = RandomGenerator.GetItem(RandomGenerator.LoremIpsumWords);
		var score = RandomGenerator.NextInteger(0, 100);
		return new TabAppDispatcherSampleCollectionItem(id, name, score);
	}

	public void MutateOnce()
	{
		var list = Model;
		var roll = Random.Shared.Next(10);
		var count = list.Count;

		if ((roll < 4) && (count < MaxItems))
		{
			list.Add(CreateSampleItem());
			return;
		}

		if (count == 0)
		{
			list.Add(CreateSampleItem());
			return;
		}

		if (roll < 7)
		{
			list.RemoveAt(Random.Shared.Next(count));
			return;
		}

		// Replace slot with same Id so ListAndItems updates fields (marks pending).
		var index = Random.Shared.Next(count);
		var current = list[index];
		list[index] = new TabAppDispatcherSampleCollectionItem(
			current.Id,
			RandomGenerator.GetItem(RandomGenerator.LoremIpsumWords),
			RandomGenerator.NextInteger(0, 100)
		);
	}

	#endregion
}