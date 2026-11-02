using Amiga;
using CopperOS.MuiMaster;

namespace CopperOS.MuiMaster.Tests;

public sealed class MuiConstructorFailureChildrenTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NullChildUnwindsChildrenBeforeAndAfterTheFailedNestedConstructor(int nullPosition)
    {
        var state = APTR.FromPointer(0x1000);
        var p = new MuiHeadlessTestPlatform(0x1000, 0x30000, 0x4000, state);
        Assert.True(MuiHeadlessObjectCore.Initialize(ref p, state));
        var name = APTR.FromPointer(0x1100); p.WriteCString(name, "Group.mui");
        var cls = MuiHeadlessObjectCore.RegisterBuiltinClass(ref p, state, name, APTR.Null, 0, APTR.FromPointer(1));
        var first = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var last = MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, APTR.Null);
        var children = new List<APTR> { first, last }; children.Insert(nullPosition, APTR.Null);
        var tags = APTR.FromPointer(0x1800);
        for (var i = 0; i < children.Count; i++)
        {
            p.WriteUInt32(tags, i * 8, MuiGroupChildrenCore.Child);
            p.WriteUInt32(tags, i * 8 + 4, children[i].Raw);
        }
        p.WriteUInt32(tags, 24, 0); p.WriteUInt32(tags, 28, 0);
        Assert.True(MuiHeadlessObjectCore.CreateObjectA(ref p, state, cls, tags).IsNull);
        Assert.True(MuiHeadlessObjectCore.FindObject(ref p, state, first).IsNull);
        Assert.True(MuiHeadlessObjectCore.FindObject(ref p, state, last).IsNull);
    }
}
