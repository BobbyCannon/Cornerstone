#region References

using System.Threading.Tasks;
using Android.Content;
using Android.Nfc;
using Android.OS;
using Cornerstone.Attributes;
using Cornerstone.Collections;
using Cornerstone.Platforms.Android.Internal;
using Cornerstone.Presentation;
using Cornerstone.Security.SecurityKeys;
using Java.Lang;

#endregion

namespace Cornerstone.Platforms.Android;

public class AndroidSmartCardReader : SmartCardReader
{
	#region Fields

	private readonly InternalNfcAdapter _implementation;

	#endregion

	#region Constructors

	/// <inheritdoc />
	[DependencyInjectionConstructor]
	public AndroidSmartCardReader(IDispatcher dispatcher) : base(dispatcher)
	{
		_implementation = new InternalNfcAdapter();

		AvailableReaders = new ReadOnlySpeedyList<SelectionOption<string>>([]);
	}

	#endregion

	#region Properties

	public override ReadOnlySpeedyList<SelectionOption<string>> AvailableReaders { get; }

	#endregion

	#region Methods

	/// <inheritdoc />
	public override void Initialize()
	{
		_implementation.StartListening();
		base.Initialize();
	}

	public bool OnHandleIntent(Intent intent)
	{
		if (intent is not { Action: NfcAdapter.ActionTagDiscovered })
		{
			return false;
		}

		Tag tag;

		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag, Class.FromType(typeof(Tag))) as Tag;
		}
		else
		{
			#pragma warning disable CA1422
			tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
			#pragma warning restore CA1422
		}

		if (tag == null)
		{
			return true;
		}

		var nTag = _implementation.GetTagInfo(tag);
		if (nTag != null)
		{
			Card = new MiFareClassicSecurityCard();
			Card.UpdateUniqueId(nTag.Identifier);
		}
		else
		{
			Card = new UnknownSecurityCard();
			Card.UpdateUniqueId([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
		}

		return true;
	}

	/// <inheritdoc />
	public override void Uninitialize()
	{
		_implementation.StopListening();
		base.Uninitialize();
	}

	/// <inheritdoc />
	protected override byte[] Transmit(byte[] command)
	{
		return [];
	}

	#endregion
}