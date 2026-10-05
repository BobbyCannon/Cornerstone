#region References

using System;
using System.Collections.Generic;
using Cornerstone.Presentation.Media;
using Cornerstone.Presentation.Media.Fonts;
using Cornerstone.Presentation.UnitTests.Helpers;
using Cornerstone.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace Cornerstone.Presentation.UnitTests.Base.Media;

[TestClass]
public class NormalizedVariationPositionTests
{
	#region Fields

	private static readonly OpenTypeTag Ital = OpenTypeTag.Parse("ital");
	private static readonly OpenTypeTag Wdth = OpenTypeTag.Parse("wdth");
	private static readonly OpenTypeTag Wght = OpenTypeTag.Parse("wght");

	#endregion

	#region Methods

	[PresentationTestMethod]
	public void CoordinatesPropertyReturnsEmptyNotDefaultForDefaultStruct()
	{
		// The IsDefault → Empty normalization in the property lets callers iterate
		// / index without first checking ImmutableArray.IsDefault.
		var settings = default(NormalizedVariationPosition);

		CornerstoneTest.IsFalse(settings.Coordinates.IsDefault);
		CornerstoneTest.AreEqual(0, settings.Coordinates.Length);
	}

	[PresentationTestMethod]
	public void DefaultStructIsTheNoVariationCase()
	{
		var settings = default(NormalizedVariationPosition);

		CornerstoneTest.IsTrue(settings.IsDefault);
		CornerstoneTest.Empty(settings.Coordinates);
		CornerstoneTest.AreEqual(0, settings.GetHashCode());
	}

	[PresentationTestMethod]
	public void DefaultStructsAreEqual()
	{
		// Two zero-initialized structs must compare equal, even though they're
		// distinct values on the stack. This is the substitute for the previous
		// singleton Default.
		CornerstoneTest.AreEqual(default, default(NormalizedVariationPosition));
		CornerstoneTest.IsTrue(default == default(NormalizedVariationPosition));
	}

	[PresentationTestMethod]
	public void EqualityDiffersBetweenDefaultAndPopulated()
	{
		var a = default(NormalizedVariationPosition);
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

		CornerstoneTest.IsFalse(a.Equals(b));
		CornerstoneTest.IsFalse(b.Equals(a));
	}

	[PresentationTestMethod]
	public void EqualityDiffersWhenACoordinateKeyDiffers()
	{
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wdth] = 0.5f });

		CornerstoneTest.IsFalse(a.Equals(b));
	}

	[PresentationTestMethod]
	public void EqualityDiffersWhenACoordinateValueDiffers()
	{
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.6f });

		CornerstoneTest.IsFalse(a.Equals(b));
	}

	[PresentationTestMethod]
	public void EqualityDiffersWhenCoordinateCountsDiffer()
	{
		// The second coordinate must be non-zero: zero coordinates canonicalize away in
		// FromCoordinates, which would make these two values deliberately equal.
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });

		CornerstoneTest.IsFalse(a.Equals(b));
	}

	[PresentationTestMethod]
	public void EqualityIgnoresInsertionOrder()
	{
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wdth] = -0.25f, [Wght] = 0.5f });

		CornerstoneTest.IsTrue(a.Equals(b));
		CornerstoneTest.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[PresentationTestMethod]
	public void EqualityIsReflexive()
	{
		var settings = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

		CornerstoneTest.IsTrue(settings.Equals(settings));
	}

	[PresentationTestMethod]
	public void EqualityIsStructuralForIdenticalCoordinates()
	{
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });

		CornerstoneTest.IsTrue(a.Equals(b));
		CornerstoneTest.IsTrue(b.Equals(a));
		CornerstoneTest.AreEqual(a.GetHashCode(), b.GetHashCode());
	}

	[PresentationTestMethod]
	public void EqualityOperatorsMatchEquals()
	{
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Ital] = 1f });
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Ital] = 1f });
		var c = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Ital] = 0f });

		CornerstoneTest.IsTrue(a == b);
		CornerstoneTest.IsFalse(a != b);
		CornerstoneTest.IsFalse(a == c);
		CornerstoneTest.IsTrue(a != c);
	}

	[PresentationTestMethod]
	public void EqualityWithBoxedObject()
	{
		// Cache-key paths box rarely, but Equals(object) must still be correct.
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });
		object boxed = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

		CornerstoneTest.IsTrue(a.Equals(boxed));
		CornerstoneTest.IsFalse(a.Equals(null));
		CornerstoneTest.IsFalse(a.Equals("not a settings"));
	}

	[PresentationTestMethod]
	[DataRow(-1f)]
	[DataRow(1f)]
	public void FromCoordinatesDictionaryAcceptsBoundaryValues(float value)
	{
		var coords = new Dictionary<OpenTypeTag, float> { [Wght] = value };

		var settings = NormalizedVariationPosition.FromCoordinates(coords);

		CornerstoneTest.Single(settings.Coordinates);
		CornerstoneTest.AreEqual(Wght, settings.Coordinates[0].Axis);
		CornerstoneTest.AreEqual(value, settings.Coordinates[0].NormalizedValue);
	}

	[PresentationTestMethod]
	public void FromCoordinatesDictionaryDefensivelyCopiesTheInput()
	{
		var mutable = new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f };

		var settings = NormalizedVariationPosition.FromCoordinates(mutable);

		mutable[Wght] = 0.9f;
		mutable[Wdth] = -0.25f;

		CornerstoneTest.Single(settings.Coordinates);
		CornerstoneTest.AreEqual(0.5f, settings.Coordinates[0].NormalizedValue);
	}

	[PresentationTestMethod]
	public void FromCoordinatesDictionaryEmptyReturnsDefaultStruct()
	{
		var settings = NormalizedVariationPosition.FromCoordinates(new Dictionary<OpenTypeTag, float>());

		CornerstoneTest.IsTrue(settings.IsDefault);
		CornerstoneTest.AreEqual(default, settings);
	}

	[PresentationTestMethod]
	[DataRow(float.NaN)]
	[DataRow(-1.0001f)]
	[DataRow(1.0001f)]
	[DataRow(float.PositiveInfinity)]
	[DataRow(float.NegativeInfinity)]
	public void FromCoordinatesDictionaryRejectsOutOfRangeOrNaN(float value)
	{
		var coords = new Dictionary<OpenTypeTag, float> { [Wght] = value };

		Assert.Throws<ArgumentOutOfRangeException>(() => NormalizedVariationPosition.FromCoordinates(coords));
	}

	[PresentationTestMethod]
	public void FromCoordinatesDictionarySortsByAxisTag()
	{
		// Insertion order shouldn't matter — coordinates land sorted by (uint)tag
		// so equality and hashing are insertion-order-independent.
		var unordered = new Dictionary<OpenTypeTag, float>
		{
			[Wght] = 0.5f,
			[Ital] = 1f,
			[Wdth] = -0.25f
		};

		var settings = NormalizedVariationPosition.FromCoordinates(unordered);

		CornerstoneTest.AreEqual(3, settings.Coordinates.Length);

		// Sorted: ital (0x6974616c), wdth (0x77647468), wght (0x77676874).
		// Lexically by the 4-char ASCII tag uint, that's ital < wdth < wght.
		CornerstoneTest.AreEqual(Ital, settings.Coordinates[0].Axis);
		CornerstoneTest.AreEqual(Wdth, settings.Coordinates[1].Axis);
		CornerstoneTest.AreEqual(Wght, settings.Coordinates[2].Axis);
	}

	[PresentationTestMethod]
	public void FromCoordinatesDictionaryThrowsOnNull()
	{
		Assert.Throws<ArgumentNullException>(() => NormalizedVariationPosition.FromCoordinates((IReadOnlyDictionary<OpenTypeTag, float>) null!));
	}

	[PresentationTestMethod]
	public void FromCoordinatesDropsZeroCoordinates()
	{
		// 0 is the axis default: an explicit wght=0 must produce the same value (and the
		// same variation-cache key downstream) as settings that omit the axis entirely.
		var fromDictionary = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0f });

		CornerstoneTest.IsTrue(fromDictionary.IsDefault);
		CornerstoneTest.AreEqual(default, fromDictionary);

		Span<NormalizedVariationCoordinate> coords =
		[
			new(Wght, 0f),
			new(Wdth, -0.25f)
		];

		var fromSpan = NormalizedVariationPosition.FromCoordinates(coords);

		CornerstoneTest.Single(fromSpan.Coordinates);
		CornerstoneTest.AreEqual(Wdth, fromSpan.Coordinates[0].Axis);
	}

	[PresentationTestMethod]
	public void FromCoordinatesSpanEmptyReturnsDefaultStruct()
	{
		var settings = NormalizedVariationPosition.FromCoordinates(ReadOnlySpan<NormalizedVariationCoordinate>.Empty);

		CornerstoneTest.IsTrue(settings.IsDefault);
	}

	[PresentationTestMethod]
	public void FromCoordinatesSpanRejectsDuplicateAxes()
	{
		Assert.Throws<ArgumentException>(static () =>
		{
			Span<NormalizedVariationCoordinate> coords =
			[
				new(OpenTypeTag.Parse("wght"), 0.5f),
				new(OpenTypeTag.Parse("wght"), -0.5f)
			];
			NormalizedVariationPosition.FromCoordinates(coords);
		});
	}

	[PresentationTestMethod]
	public void FromCoordinatesSpanRejectsDuplicateAxesEvenWhenZeroValued()
	{
		// Canonicalization must not weaken validation: the duplicate check runs before
		// zero-valued coordinates are dropped.
		Assert.Throws<ArgumentException>(static () =>
		{
			Span<NormalizedVariationCoordinate> coords =
			[
				new(OpenTypeTag.Parse("wght"), 0f),
				new(OpenTypeTag.Parse("wght"), 0.5f)
			];
			NormalizedVariationPosition.FromCoordinates(coords);
		});
	}

	[PresentationTestMethod]
	public void FromCoordinatesSpanRejectsOutOfRangeValue()
	{
		Assert.Throws<ArgumentOutOfRangeException>(static () =>
		{
			Span<NormalizedVariationCoordinate> coords = [new(OpenTypeTag.Parse("wght"), 2f)];
			NormalizedVariationPosition.FromCoordinates(coords);
		});
	}

	[PresentationTestMethod]
	public void FromCoordinatesSpanSortsAndValidates()
	{
		Span<NormalizedVariationCoordinate> coords =
		[
			new(Wght, 0.5f),
			new(Ital, 1f),
			new(Wdth, -0.25f)
		];

		var settings = NormalizedVariationPosition.FromCoordinates(coords);

		CornerstoneTest.AreEqual(3, settings.Coordinates.Length);
		CornerstoneTest.AreEqual(Ital, settings.Coordinates[0].Axis);
		CornerstoneTest.AreEqual(Wdth, settings.Coordinates[1].Axis);
		CornerstoneTest.AreEqual(Wght, settings.Coordinates[2].Axis);
	}

	[PresentationTestMethod]
	public void GetCoordinateOrDefaultReturnsFallbackForAbsentAxis()
	{
		var settings = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

		CornerstoneTest.AreEqual(0.5f, settings.GetCoordinateOrDefault(Wght));
		CornerstoneTest.AreEqual(0f, settings.GetCoordinateOrDefault(Ital));
		CornerstoneTest.AreEqual(-1f, settings.GetCoordinateOrDefault(Ital, -1f));
	}

	[PresentationTestMethod]
	public void HashIsCachedAndStableAcrossEqualInstances()
	{
		// The hash is computed once at construction. Two structurally-equal settings
		// must have the same hash regardless of how the coordinates were inserted.
		var a = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f, [Ital] = 1f });
		var b = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Ital] = 1f, [Wght] = 0.5f, [Wdth] = -0.25f });

		var hashA = a.GetHashCode();

		// Subsequent calls return the same cached value.
		CornerstoneTest.AreEqual(hashA, a.GetHashCode());
		CornerstoneTest.AreEqual(hashA, b.GetHashCode());
	}

	[PresentationTestMethod]
	public void NormalizedVariationCoordinateHasStructuralEquality()
	{
		// The coordinate record-struct provides equality for free; verify it
		// behaves as expected so callers can use it in their own comparisons.
		var a = new NormalizedVariationCoordinate(Wght, 0.5f);
		var b = new NormalizedVariationCoordinate(Wght, 0.5f);
		var c = new NormalizedVariationCoordinate(Wght, 0.6f);
		var d = new NormalizedVariationCoordinate(Wdth, 0.5f);

		CornerstoneTest.AreEqual(a, b);
		CornerstoneTest.AreNotEqual(a, c);
		CornerstoneTest.AreNotEqual(a, d);
		CornerstoneTest.IsTrue(a == b);
		CornerstoneTest.IsTrue(a != c);
	}

	[PresentationTestMethod]
	public void TryGetCoordinateReturnsFalseForAbsentAxis()
	{
		var settings = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f });

		CornerstoneTest.IsFalse(settings.TryGetCoordinate(Ital, out var v));
		CornerstoneTest.AreEqual(0f, v);
	}

	[PresentationTestMethod]
	public void TryGetCoordinateReturnsFalseForDefaultStruct()
	{
		var settings = default(NormalizedVariationPosition);

		CornerstoneTest.IsFalse(settings.TryGetCoordinate(Wght, out var v));
		CornerstoneTest.AreEqual(0f, v);
	}

	[PresentationTestMethod]
	public void TryGetCoordinateReturnsTrueForPresentAxis()
	{
		var settings = NormalizedVariationPosition.FromCoordinates(
			new Dictionary<OpenTypeTag, float> { [Wght] = 0.5f, [Wdth] = -0.25f });

		CornerstoneTest.IsTrue(settings.TryGetCoordinate(Wght, out var w));
		CornerstoneTest.AreEqual(0.5f, w);

		CornerstoneTest.IsTrue(settings.TryGetCoordinate(Wdth, out var wd));
		CornerstoneTest.AreEqual(-0.25f, wd);
	}

	#endregion
}