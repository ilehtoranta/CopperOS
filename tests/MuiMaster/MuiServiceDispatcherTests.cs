using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

// The service seams are the public packet boundaries for MG09 specialist
// instances. Process/Menu use headless-object state, while Pop*, pen/color,
// and Boopsi/Dtpic carry their own guest-resident instance records. These tests
// prove that the additive family-neutral routes do not require callers to
// select a family-specific dispatcher.
public sealed class MuiServiceDispatcherTests
{
	private const uint Base = 0x1000;
	private const int Size = 0x40000;
	private const uint FirstAllocation = 0x10000;
	private static readonly APTR State = APTR.FromPointer(0x1100);
	private static readonly APTR Instance = APTR.FromPointer(0x2000);
	private static readonly APTR ColorInstance = APTR.FromPointer(0x2200);
	private static readonly APTR ExternalInstance = APTR.FromPointer(0x2400);
	private static readonly APTR MiscInstance = APTR.FromPointer(0x2600);
	private static readonly APTR ClassId = APTR.FromPointer(0x3000);
	private static readonly APTR Storage = APTR.FromPointer(0x3200);
	private static readonly APTR Packet = APTR.FromPointer(0x3300);

	[Fact]
	public void ServiceSeamRoutesStandaloneFamilies()
	{
		var p = new MuiHeadlessTestPlatform(Base, Size, FirstAllocation, State);

		// Popstring.mui is a standalone specialist instance.  Its children are
		// real BOOPSI objects so disposal also proves the ownership path remains
		// family-correct when reached through the shared seam.
		p.WriteCString(ClassId, "Popstring.mui");
		var stringChild = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		var buttonChild = p.NewObject(APTR.FromPointer(0x9000), APTR.Null);
		Assert.Equal(MuiPopSpecialistClass.Popstring,
			MuiPopSpecialistCore.CreateByName(ref p, Instance, ClassId,
				stringChild, buttonChild));
		p.WriteUInt32(Packet, 0, 0x00000104u); // OM_GET
		p.WriteUInt32(Packet, 4, MuiPopAttributes.Popstring_Toggle);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchStandaloneService(ref p,
			Instance, Packet));
		Assert.Equal(0u, p.ReadUInt32(Storage, 0));
		p.WriteUInt32(Packet, 0, 0x00000102u); // OM_DISPOSE
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchStandaloneService(ref p,
			Instance, Packet));
		Assert.False(MuiPopSpecialistCore.Valid(ref p, Instance));

		// Coloradjust.mui requires the drawing service state for its normal
		// capability checks, but its packet instance itself is independent of the
		// headless registry and therefore uses the same null state argument.
		Assert.True(MuiDrawingServiceCore.Initialize(ref p, State));
		p.WriteCString(ClassId, "Coloradjust.mui");
		Assert.Equal(MuiColorSpecialistClass.Coloradjust,
			MuiColorSpecialistCore.CreateByName(ref p, ColorInstance, ClassId));
		p.WriteUInt32(Packet, 0, 0x00000104u); // OM_GET
		p.WriteUInt32(Packet, 4, MuiColorAttributes.ColoradjustShowAlpha);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchStandaloneService(ref p,
			ColorInstance, Packet));
		Assert.Equal(0u, p.ReadUInt32(Storage, 0));
		p.WriteUInt32(Packet, 0, 0x00000102u); // OM_DISPOSE
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchStandaloneService(ref p,
			ColorInstance, Packet));
		Assert.False(MuiColorSpecialistCore.Valid(ref p, ColorInstance));

		// Dtpic.mui has no headless-object state in this progressive slice; the
		// wrapper's fixed instance and owned work block are enough to prove public
		// routing and idempotent teardown.
		p.WriteCString(ClassId, "Dtpic.mui");
		Assert.Equal(MuiExternalWrapperClass.Dtpic,
			MuiExternalWrapperCore.CreateByName(ref p, ExternalInstance, ClassId));
		p.WriteUInt32(Packet, 0, 0x00000104u); // OM_GET
		p.WriteUInt32(Packet, 4, MuiExternalWrapperAttributes.Dtpic_Alpha);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchExternalService(ref p,
			ExternalInstance, Packet));
		Assert.Equal(0xffu, p.ReadUInt32(Storage, 0));
		p.WriteUInt32(Packet, 0, 0x00000102u); // OM_DISPOSE
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchExternalService(ref p,
			ExternalInstance, Packet));
		Assert.False(MuiExternalWrapperCore.Valid(ref p, ExternalInstance));

		// The final MG09 Misc family uses the same standalone route. Keyadjust is
		// intentionally chosen because it exercises copied state through packets
		// without requiring an application/window capability.
		p.WriteCString(ClassId, "Keyadjust.mui");
		Assert.Equal(MuiMiscSpecialistClass.Keyadjust,
			MuiMiscSpecialistCore.CreateByName(ref p, MiscInstance, ClassId));
		p.WriteUInt32(Packet, 0, 0x00000104u); // OM_GET
		p.WriteUInt32(Packet, 4, MuiMiscAttributes.Keyadjust_AllowMouseEvents);
		p.WriteUInt32(Packet, 8, Storage.Raw);
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchStandalone(ref p,
			MiscInstance, Packet));
		Assert.Equal(0u, p.ReadUInt32(Storage, 0));
		p.WriteUInt32(Packet, 0, 0x8042549Au); // MUIM_Set
		p.WriteUInt32(Packet, 4, MuiMiscAttributes.Keyadjust_AllowMouseEvents);
		p.WriteUInt32(Packet, 8, 1);
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchStandalone(ref p,
			MiscInstance, Packet));
		p.WriteUInt32(Packet, 0, 0x00000102u); // OM_DISPOSE
		Assert.Equal(1u, MuiSpecialistServiceDispatcher.DispatchStandalone(ref p,
			MiscInstance, Packet));
		Assert.False(MuiMiscSpecialistCore.Valid(ref p, MiscInstance));
	}
}
