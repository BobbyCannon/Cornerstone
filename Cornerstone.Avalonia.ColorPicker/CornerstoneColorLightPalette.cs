#region References

using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Utilities;

#endregion

namespace Cornerstone.Avalonia.ColorPicker;

public class CornerstoneColorLightPalette : IColorPalette
{
	#region Fields

	private static readonly Color[,] Colors =
	{
		{
			//Red
			Color.FromUInt32(0xFFFEF2ED),
			Color.FromUInt32(0xFFFEDDD2),
			Color.FromUInt32(0xFFFDB7A5),
			Color.FromUInt32(0xFFFB9078),
			Color.FromUInt32(0xFFFA664C),
			Color.FromUInt32(0xFFF93920),
			Color.FromUInt32(0xFFD52515),
			Color.FromUInt32(0xFFB2140C),
			Color.FromUInt32(0xFF8E0805),
			Color.FromUInt32(0xFF6A0103)
		},
		{
			//Pink
			Color.FromUInt32(0xFFFDECEF),
			Color.FromUInt32(0xFFFBCFD8),
			Color.FromUInt32(0xFFF6A0B5),
			Color.FromUInt32(0xFFF27396),
			Color.FromUInt32(0xFFED487B),
			Color.FromUInt32(0xFFE91E63),
			Color.FromUInt32(0xFFC51356),
			Color.FromUInt32(0xFFA20B48),
			Color.FromUInt32(0xFF7E053A),
			Color.FromUInt32(0xFF5A012B)
		},
		{
			//Purple
			Color.FromUInt32(0xFFF7E9F7),
			Color.FromUInt32(0xFFEFCAF0),
			Color.FromUInt32(0xFFDD9BE0),
			Color.FromUInt32(0xFFC96FD1),
			Color.FromUInt32(0xFFB449C2),
			Color.FromUInt32(0xFF9E28B3),
			Color.FromUInt32(0xFF871E9E),
			Color.FromUInt32(0xFF71168A),
			Color.FromUInt32(0xFF5C0F75),
			Color.FromUInt32(0xFF490A61)
		},
		{
			//Violet
			Color.FromUInt32(0xFFF3EDF9),
			Color.FromUInt32(0xFFE2D1F4),
			Color.FromUInt32(0xFFC4A7E9),
			Color.FromUInt32(0xFFA67FDD),
			Color.FromUInt32(0xFF885BD2),
			Color.FromUInt32(0xFF6A3AC7),
			Color.FromUInt32(0xFF572FB3),
			Color.FromUInt32(0xFF46259E),
			Color.FromUInt32(0xFF361C8A),
			Color.FromUInt32(0xFF281475)
		},
		{
			//Indigo
			Color.FromUInt32(0xFFECEFF8),
			Color.FromUInt32(0xFFD1D8F0),
			Color.FromUInt32(0xFFA7B3E1),
			Color.FromUInt32(0xFF8090D3),
			Color.FromUInt32(0xFF5E6FC4),
			Color.FromUInt32(0xFF3F51B5),
			Color.FromUInt32(0xFF3342A1),
			Color.FromUInt32(0xFF28348C),
			Color.FromUInt32(0xFF1F2878),
			Color.FromUInt32(0xFF171D63)
		},
		{
			//Blue
			Color.FromUInt32(0xFFEAF5FF),
			Color.FromUInt32(0xFFCBE7FE),
			Color.FromUInt32(0xFF98CDFD),
			Color.FromUInt32(0xFF65B2FC),
			Color.FromUInt32(0xFF3295FB),
			Color.FromUInt32(0xFF0077FA),
			Color.FromUInt32(0xFF0062D6),
			Color.FromUInt32(0xFF004FB3),
			Color.FromUInt32(0xFF003D8F),
			Color.FromUInt32(0xFF002C6B)
		},
		{
			//LightBlue
			Color.FromUInt32(0xFFE9F7FD),
			Color.FromUInt32(0xFFC9ECFC),
			Color.FromUInt32(0xFF95D8F8),
			Color.FromUInt32(0xFF62C3F5),
			Color.FromUInt32(0xFF30ACF1),
			Color.FromUInt32(0xFF0095EE),
			Color.FromUInt32(0xFF007BCA),
			Color.FromUInt32(0xFF0063A7),
			Color.FromUInt32(0xFF004B83),
			Color.FromUInt32(0xFF00355F)
		},
		{
			//Cyan
			Color.FromUInt32(0xFFE5F7F8),
			Color.FromUInt32(0xFFC2EFF0),
			Color.FromUInt32(0xFF8ADDE2),
			Color.FromUInt32(0xFF58CBD3),
			Color.FromUInt32(0xFF2CB8C5),
			Color.FromUInt32(0xFF05A4B6),
			Color.FromUInt32(0xFF038698),
			Color.FromUInt32(0xFF016979),
			Color.FromUInt32(0xFF004D5B),
			Color.FromUInt32(0xFF00323D)
		},
		{
			//Teal
			Color.FromUInt32(0xFFE4F7F4),
			Color.FromUInt32(0xFFC0F0E8),
			Color.FromUInt32(0xFF87E0D3),
			Color.FromUInt32(0xFF54D1C1),
			Color.FromUInt32(0xFF27C2B0),
			Color.FromUInt32(0xFF00B3A1),
			Color.FromUInt32(0xFF009589),
			Color.FromUInt32(0xFF00776F),
			Color.FromUInt32(0xFF005955),
			Color.FromUInt32(0xFF003C3A)
		},
		{
			//Green
			Color.FromUInt32(0xFFECF7EC),
			Color.FromUInt32(0xFFD0F0D1),
			Color.FromUInt32(0xFFA4E0A7),
			Color.FromUInt32(0xFF7DD182),
			Color.FromUInt32(0xFF5AC262),
			Color.FromUInt32(0xFF3BB346),
			Color.FromUInt32(0xFF30953B),
			Color.FromUInt32(0xFF25772F),
			Color.FromUInt32(0xFF1B5924),
			Color.FromUInt32(0xFF113C18)
		},
		{
			//LightGreen
			Color.FromUInt32(0xFFF3F8EC),
			Color.FromUInt32(0xFFE3F0D0),
			Color.FromUInt32(0xFFC8E2A5),
			Color.FromUInt32(0xFFADD37E),
			Color.FromUInt32(0xFF93C55B),
			Color.FromUInt32(0xFF7BB63C),
			Color.FromUInt32(0xFF649830),
			Color.FromUInt32(0xFF4E7926),
			Color.FromUInt32(0xFF395B1B),
			Color.FromUInt32(0xFF253D12)
		},
		{
			//Lime
			Color.FromUInt32(0xFFF2FAE6),
			Color.FromUInt32(0xFFE3F6C5),
			Color.FromUInt32(0xFFCBED8E),
			Color.FromUInt32(0xFFB7E35B),
			Color.FromUInt32(0xFFA7DA2C),
			Color.FromUInt32(0xFF9BD100),
			Color.FromUInt32(0xFF7EAE00),
			Color.FromUInt32(0xFF638B00),
			Color.FromUInt32(0xFF486800),
			Color.FromUInt32(0xFF2F4600)
		},
		{
			//Yellow
			Color.FromUInt32(0xFFFFFDEA),
			Color.FromUInt32(0xFFFEFBCB),
			Color.FromUInt32(0xFFFDF398),
			Color.FromUInt32(0xFFFCE865),
			Color.FromUInt32(0xFFFBDA32),
			Color.FromUInt32(0xFFFAC800),
			Color.FromUInt32(0xFFD0AA00),
			Color.FromUInt32(0xFFA78B00),
			Color.FromUInt32(0xFF7D6A00),
			Color.FromUInt32(0xFF534800)
		},
		{
			//Amber
			Color.FromUInt32(0xFFFEFBEB),
			Color.FromUInt32(0xFFFCF5CE),
			Color.FromUInt32(0xFFF9E89E),
			Color.FromUInt32(0xFFF6D86F),
			Color.FromUInt32(0xFFF3C641),
			Color.FromUInt32(0xFFF0B114),
			Color.FromUInt32(0xFFC88A0F),
			Color.FromUInt32(0xFFA0660A),
			Color.FromUInt32(0xFF784606),
			Color.FromUInt32(0xFF502B03)
		},
		{
			//Orange
			Color.FromUInt32(0xFFFFF8EA),
			Color.FromUInt32(0xFFFEEECC),
			Color.FromUInt32(0xFFFED998),
			Color.FromUInt32(0xFFFDC165),
			Color.FromUInt32(0xFFFDA633),
			Color.FromUInt32(0xFFFC8800),
			Color.FromUInt32(0xFFD26700),
			Color.FromUInt32(0xFFA84A00),
			Color.FromUInt32(0xFF7E3100),
			Color.FromUInt32(0xFF541D00)
		},
		{
			//Grey
			Color.FromUInt32(0xFFF9F9F9),
			Color.FromUInt32(0xFFE6E8EA),
			Color.FromUInt32(0xFFC6CACD),
			Color.FromUInt32(0xFFA7ABB0),
			Color.FromUInt32(0xFF888D92),
			Color.FromUInt32(0xFF6B7075),
			Color.FromUInt32(0xFF555B61),
			Color.FromUInt32(0xFF41464C),
			Color.FromUInt32(0xFF2E3238),
			Color.FromUInt32(0xFF1C1F23)
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