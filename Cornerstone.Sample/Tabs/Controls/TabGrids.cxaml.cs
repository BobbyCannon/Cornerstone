#region References

using System;
using System.Collections.Generic;
using Cornerstone.Data;
using Cornerstone.Presentation;
using Cornerstone.Presentation.Controls;
using Cornerstone.Presentation.Input;
using Cornerstone.Presentation.Interactivity;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Imaging;
using Cornerstone.Presentation.Platform;
using Cornerstone.Reflection;
using Cornerstone.Runtime;
using Cornerstone.Presentation.Controls.Elements;

#endregion

namespace Cornerstone.Sample.Tabs.Controls;

[SourceReflection]
public partial class TabGrids : UserControl
{
	#region Constants

	public const string HeaderName = "Grids";

	#endregion

	#region Fields

	private readonly IDateTimeProvider _dateTimeProvider;
	private int _moveCount;
	private Bitmap _originalImage;
	private readonly List<CroppedBitmap> _tileBitmaps;

	#endregion

	#region Constructors

	public TabGrids() : this(
		AppBootstrap.GetInstance<IDateTimeProvider>(),
		AppBootstrap.GetInstance<IRuntimeInformation>())
	{
	}

	[DependencyInjectionConstructor]
	public TabGrids(IDateTimeProvider dateTimeProvider, IRuntimeInformation runtimeInformation)
	{
		_dateTimeProvider = dateTimeProvider;
		_tileBitmaps = [];

		Tiles = new PresentationList<TabGridsPuzzleTile>();
		RuntimeInformation = runtimeInformation;
		DataContext = this;

		InitializeComponent();
		LoadImageFromAsset("csres://Cornerstone.Sample/Assets/Cornerstone.png");
	}

	#endregion

	#region Properties

	[Notify]
	public partial bool IsSolved { get; set; }

	public IRuntimeInformation RuntimeInformation { get; }

	public PresentationList<TabGridsPuzzleTile> Tiles { get; }

	#endregion

	#region Methods

	public override void OnPropertyChanged(string propertyName)
	{
		switch (propertyName)
		{
			case nameof(IsSolved):
			{
				RefreshTiles();
				break;
			}
		}

		base.OnPropertyChanged(propertyName);
	}

	private bool CheckIfSolved()
	{
		for (var row = 0; row < 3; row++)
		{
			for (var col = 0; col < 3; col++)
			{
				var expected = (row * 3) + col + 1;
				if (expected == 9)
				{
					expected = 0;
				}

				var tile = Tiles.FirstOrDefault(t => (t.Row == row) && (t.Column == col));
				if (tile?.Number != expected)
				{
					return false;
				}
			}
		}

		StatusText.Text = $"Solved in {_moveCount} moves! 🎉";
		StatusText.Foreground = Brushes.LimeGreen;

		return true;
	}

	private int CountInversions(int[] tiles)
	{
		var count = 0;
		for (var i = 0; i < 9; i++)
		{
			if (tiles[i] == 0)
			{
				continue;
			}
			for (var j = i + 1; j < 9; j++)
			{
				if (tiles[j] == 0)
				{
					continue;
				}
				if (tiles[i] > tiles[j])
				{
					count++;
				}
			}
		}
		return count;
	}

	private void CreateTileImages()
	{
		_tileBitmaps.Clear();

		if (_originalImage == null)
		{
			return;
		}

		var tileSize = Math.Min(_originalImage.PixelSize.Width, _originalImage.PixelSize.Height) / 3;

		for (var row = 0; row < 3; row++)
		{
			for (var col = 0; col < 3; col++)
			{
				var rect = new PixelRect(col * tileSize, row * tileSize, tileSize, tileSize);
				var cropped = new CroppedBitmap(_originalImage, rect);
				_tileBitmaps.Add(cropped);
			}
		}
	}

	private void InitializePuzzle(bool shuffled)
	{
		Tiles.Clear();
		MovesText.Text = "Moves: 0";
		StatusText.Text = "";

		_moveCount = 0;

		int[] numbers = [1, 2, 3, 4, 5, 6, 7, 8, 0];

		if (shuffled)
		{
			var rnd = new Random();
			for (var i = numbers.Length - 1; i > 0; i--)
			{
				var j = rnd.Next(i + 1);
				(numbers[i], numbers[j]) = (numbers[j], numbers[i]);
			}

			if ((CountInversions(numbers) % 2) == 1)
			{
				// Flip parity by swapping two non-blank tiles
				int a = -1, b = -1;
				for (var i = 0; (i < 9) && (b == -1); i++)
				{
					if (numbers[i] != 0)
					{
						if (a == -1)
						{
							a = i;
						}
						else
						{
							b = i;
						}
					}
				}
				if ((a != -1) && (b != -1))
				{
					(numbers[a], numbers[b]) = (numbers[b], numbers[a]);
				}
			}
		}

		for (var i = 0; i < 9; i++)
		{
			var num = numbers[i];
			var tile = new TabGridsPuzzleTile(this)
			{
				Number = num,
				Row = i / 3,
				Column = i % 3
			};

			// Assign correct image piece
			if (_tileBitmaps.Count == 0)
			{
				tile.TileImage = null;
			}
			else if (num == 0)
			{
				tile.TileImage = _tileBitmaps[8];
			}
			else
			{
				// numbers 1-8 map to indices 0-7
				var imageIndex = num - 1;
				tile.TileImage = _tileBitmaps[imageIndex];
			}

			Tiles.Add(tile);
		}

		RefreshTiles();
	}

	private void LoadImageFromAsset(string assetUri)
	{
		using var stream = AssetLoader.Open(new Uri(assetUri));
		_originalImage = new Bitmap(stream);
		CreateTileImages();
		InitializePuzzle(false);
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		InitializePuzzle(false);
	}

	private void OnScrambleClick(object sender, RoutedEventArgs e)
	{
		InitializePuzzle(true);
	}

	private void OnTilePressed(object sender, PointerPressedEventArgs e)
	{
		if (sender is not Border { DataContext: TabGridsPuzzleTile clickedTile })
		{
			return;
		}

		var blank = Tiles.FirstOrDefault(t => t.Number == 0);
		if ((blank == null) || !clickedTile.IsAdjacentTo(blank))
		{
			return;
		}

		(clickedTile.Row, blank.Row) = (blank.Row, clickedTile.Row);
		(clickedTile.Column, blank.Column) = (blank.Column, clickedTile.Column);

		_moveCount++;

		MovesText.Text = $"Moves: {_moveCount}";
		IsSolved = CheckIfSolved();
	}

	private void RefreshTiles()
	{
		foreach (var tile in Tiles)
		{
			tile.Refresh();
		}
	}

	#endregion
}