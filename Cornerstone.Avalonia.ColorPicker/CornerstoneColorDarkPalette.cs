#region References

using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Utilities;

#endregion

namespace Cornerstone.Avalonia.ColorPicker;

public class CornerstoneColorDarkPalette : IColorPalette
{
	#region Fields

	private static readonly Color[,] Colors =
	{
		{
			//Red
			Color.FromUInt32(0xFF6C090B),
			Color.FromUInt32(0xFF901110),
			Color.FromUInt32(0xFFB42019),
			Color.FromUInt32(0xFFD73324),
			Color.FromUInt32(0xFFFB4932),
			Color.FromUInt32(0xFFFC725A),
			Color.FromUInt32(0xFFFD9983),
			Color.FromUInt32(0xFFFDBEAC),
			Color.FromUInt32(0xFFFEE0D5),
			Color.FromUInt32(0xFFFFF3EF)
		},
		{
			//Pink
			Color.FromUInt32(0xFF5C0730),
			Color.FromUInt32(0xFF800E41),
			Color.FromUInt32(0xFFA41751),
			Color.FromUInt32(0xFFC72261),
			Color.FromUInt32(0xFFEB2F71),
			Color.FromUInt32(0xFFEF5686),
			Color.FromUInt32(0xFFF37E9F),
			Color.FromUInt32(0xFFF7A8BC),
			Color.FromUInt32(0xFFFBD3DC),
			Color.FromUInt32(0xFFFDEEF1)
		},
		{
			//Purple
			Color.FromUInt32(0xFF4A1061),
			Color.FromUInt32(0xFF5E1776),
			Color.FromUInt32(0xFF731F8A),
			Color.FromUInt32(0xFF89289F),
			Color.FromUInt32(0xFFA033B3),
			Color.FromUInt32(0xFFB553C2),
			Color.FromUInt32(0xFFCA78D1),
			Color.FromUInt32(0xFFDDA0E1),
			Color.FromUInt32(0xFFEFCEF0),
			Color.FromUInt32(0xFFF7EBF7)
		},
		{
			//Violet
			Color.FromUInt32(0xFF401B77),
			Color.FromUInt32(0xFF4C248C),
			Color.FromUInt32(0xFF582EA0),
			Color.FromUInt32(0xFF6439B5),
			Color.FromUInt32(0xFF7246C9),
			Color.FromUInt32(0xFF8865D4),
			Color.FromUInt32(0xFFA288DF),
			Color.FromUInt32(0xFFBEADE9),
			Color.FromUInt32(0xFFDDD4F4),
			Color.FromUInt32(0xFFF1EEFA)
		},
		{
			//Indigo
			Color.FromUInt32(0xFF171E65),
			Color.FromUInt32(0xFF20297A),
			Color.FromUInt32(0xFF29368E),
			Color.FromUInt32(0xFF3444A3),
			Color.FromUInt32(0xFF4053B7),
			Color.FromUInt32(0xFF5F71C5),
			Color.FromUInt32(0xFF8191D4),
			Color.FromUInt32(0xFFA7B4E2),
			Color.FromUInt32(0xFFD1D8F1),
			Color.FromUInt32(0xFFEDEFF8)
		},
		{
			//Blue
			Color.FromUInt32(0xFF053170),
			Color.FromUInt32(0xFF0A4694),
			Color.FromUInt32(0xFF135CB8),
			Color.FromUInt32(0xFF1D75DB),
			Color.FromUInt32(0xFF2990FF),
			Color.FromUInt32(0xFF54A9FF),
			Color.FromUInt32(0xFF7FC1FF),
			Color.FromUInt32(0xFFA9D7FF),
			Color.FromUInt32(0xFFD4ECFF),
			Color.FromUInt32(0xFFEFF8FF)
		},
		{
			//LightBlue
			Color.FromUInt32(0xFF003761),
			Color.FromUInt32(0xFF004D85),
			Color.FromUInt32(0xFF0366A9),
			Color.FromUInt32(0xFF0A81CC),
			Color.FromUInt32(0xFF139FF0),
			Color.FromUInt32(0xFF40B4F3),
			Color.FromUInt32(0xFF6EC8F6),
			Color.FromUInt32(0xFF9DDCF9),
			Color.FromUInt32(0xFFCEEEFC),
			Color.FromUInt32(0xFFEBF8FE)
		},
		{
			//Cyan
			Color.FromUInt32(0xFF04343D),
			Color.FromUInt32(0xFF074F5C),
			Color.FromUInt32(0xFF0A6C7B),
			Color.FromUInt32(0xFF0E8999),
			Color.FromUInt32(0xFF13A8B8),
			Color.FromUInt32(0xFF38BBC6),
			Color.FromUInt32(0xFF62CDD4),
			Color.FromUInt32(0xFF91DFE3),
			Color.FromUInt32(0xFFC6EFF1),
			Color.FromUInt32(0xFFE7F7F8)
		},
		{
			//Teal
			Color.FromUInt32(0xFF023C39),
			Color.FromUInt32(0xFF045A55),
			Color.FromUInt32(0xFF07776F),
			Color.FromUInt32(0xFF0A9588),
			Color.FromUInt32(0xFF0EB3A1),
			Color.FromUInt32(0xFF33C2B0),
			Color.FromUInt32(0xFF5ED1C1),
			Color.FromUInt32(0xFF8EE1D3),
			Color.FromUInt32(0xFFC4F0E8),
			Color.FromUInt32(0xFFE6F7F4)
		},
		{
			//Green
			Color.FromUInt32(0xFF123C19),
			Color.FromUInt32(0xFF1C5A25),
			Color.FromUInt32(0xFF277731),
			Color.FromUInt32(0xFF32953D),
			Color.FromUInt32(0xFF3EB349),
			Color.FromUInt32(0xFF5DC264),
			Color.FromUInt32(0xFF7FD184),
			Color.FromUInt32(0xFFA6E1A8),
			Color.FromUInt32(0xFFD0F0D1),
			Color.FromUInt32(0xFFECF7EC)
		},
		{
			//LightGreen
			Color.FromUInt32(0xFF263D13),
			Color.FromUInt32(0xFF3B5C1D),
			Color.FromUInt32(0xFF517B28),
			Color.FromUInt32(0xFF679934),
			Color.FromUInt32(0xFF7FB840),
			Color.FromUInt32(0xFF97C65F),
			Color.FromUInt32(0xFFB0D481),
			Color.FromUInt32(0xFFC9E3A7),
			Color.FromUInt32(0xFFE4F1D1),
			Color.FromUInt32(0xFFF3F8ED)
		},
		{
			//Lime
			Color.FromUInt32(0xFF314603),
			Color.FromUInt32(0xFF4B6905),
			Color.FromUInt32(0xFF678D09),
			Color.FromUInt32(0xFF84B00C),
			Color.FromUInt32(0xFFA2D311),
			Color.FromUInt32(0xFFAEDC3A),
			Color.FromUInt32(0xFFBDE566),
			Color.FromUInt32(0xFFCFED96),
			Color.FromUInt32(0xFFE5F6C9),
			Color.FromUInt32(0xFFF3FBE9)
		},
		{
			//Yellow
			Color.FromUInt32(0xFF544903),
			Color.FromUInt32(0xFF7E6C06),
			Color.FromUInt32(0xFFA88E0A),
			Color.FromUInt32(0xFFD2AF0F),
			Color.FromUInt32(0xFFFCCE14),
			Color.FromUInt32(0xFFFDDE43),
			Color.FromUInt32(0xFFFDEB71),
			Color.FromUInt32(0xFFFEF5A0),
			Color.FromUInt32(0xFFFEFBD0),
			Color.FromUInt32(0xFFFFFEEC)
		},
		{
			//Amber
			Color.FromUInt32(0xFF512E09),
			Color.FromUInt32(0xFF794B0F),
			Color.FromUInt32(0xFFA16B16),
			Color.FromUInt32(0xFFCA8F1E),
			Color.FromUInt32(0xFFF2B726),
			Color.FromUInt32(0xFFF5CA50),
			Color.FromUInt32(0xFFF7DB7A),
			Color.FromUInt32(0xFFFAEAA6),
			Color.FromUInt32(0xFFFCF6D2),
			Color.FromUInt32(0xFFFEFBED)
		},
		{
			//Orange
			Color.FromUInt32(0xFF551F03),
			Color.FromUInt32(0xFF803506),
			Color.FromUInt32(0xFFAA500A),
			Color.FromUInt32(0xFFD56F0F),
			Color.FromUInt32(0xFFFF9214),
			Color.FromUInt32(0xFFFFAE43),
			Color.FromUInt32(0xFFFFC772),
			Color.FromUInt32(0xFFFFDDA1),
			Color.FromUInt32(0xFFFFEFD0),
			Color.FromUInt32(0xFFFFF9ED)
		},
		{
			//Gray
			Color.FromUInt32(0xFF1C1F23),
			Color.FromUInt32(0xFF2E3238),
			Color.FromUInt32(0xFF41464C),
			Color.FromUInt32(0xFF555B61),
			Color.FromUInt32(0xFF6B7075),
			Color.FromUInt32(0xFF888D92),
			Color.FromUInt32(0xFFA7ABB0),
			Color.FromUInt32(0xFFC6CACD),
			Color.FromUInt32(0xFFE6E8EA),
			Color.FromUInt32(0xFFF9F9F9)
		}
	};

	#endregion

	#region Properties

	public int ColorCount => Colors.GetLength(0);

	public int ShadeCount => Colors.GetLength(1);

	#endregion

	#region Methods

	[Obsolete("Obsolete")]
	public Color GetColor(int colorIndex, int shadeIndex)
	{
		return Colors[
			MathUtilities.Clamp(colorIndex, 0, ColorCount - 1),
			MathUtilities.Clamp(shadeIndex, 0, ShadeCount - 1)
		];
	}

	#endregion
}