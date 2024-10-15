// 41e4e600
ldarg.0
ldarg.1
ldarg.2
call virtual System.Void Sandbox.Game.Entities.MyCubeBlock::OnBuildSuccess(System.Int64 builtBy, System.Boolean instantBuild)
call static System.Boolean Sandbox.Game.Multiplayer.Sync::get_IsServer()
brfalse.s L0
ldarg.0
call static System.Boolean MultigridProjector.Logic.MultigridProjection::ShouldAllowBuildingDefaultTopBlock(Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase baseBlock)
brfalse L0
ldarg.0
ldarg.1
ldc.i4.0
ldarg.2
call System.Void Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::CreateTopPartAndAttach(System.Int64 builtBy, Sandbox.Game.Entities.Blocks.MyTopBlockSize topSize, System.Boolean instantBuild)
L0:
ret
