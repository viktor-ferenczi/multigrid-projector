// 0c1f4327
ldarga.s 1
call System.Boolean System.Nullable`1<System.Int64>::get_HasValue()
brtrue.s L0
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
callvirt System.Int64 Sandbox.Game.World.MySession::get_LocalPlayerId()
br.s L1
L0:
ldarga.s 1
call System.Int64 System.Nullable`1<System.Int64>::get_Value()
L1:
stloc.0
ldarg.0
call System.Boolean Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::get_IsAttached()
brtrue.s L2
ldarg.0
call System.Boolean Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::CanPlaceTop()
brtrue.s L3
L2:
ldloc.0
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
callvirt System.Int64 Sandbox.Game.World.MySession::get_LocalPlayerId()
bne.un.s L4
ldsfld Sandbox.Game.Gui.MyHudNotifications Sandbox.Game.Gui.MyHud::Notifications
ldc.i4.s 32
callvirt Sandbox.Game.Gui.MyHudNotificationBase Sandbox.Game.Gui.MyHudNotifications::Add(Sandbox.Game.Gui.MyNotificationSingletons singleNotification)
pop
L4:
ret
L3:
ldarg.0
call Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.MyCubeBlock::get_CubeGrid()
callvirt virtual VRage.Game.MyCubeSize Sandbox.Game.Entities.MyCubeGrid::get_GridSizeEnum()
stloc.1
call static Sandbox.Definitions.MyDefinitionManager Sandbox.Definitions.MyDefinitionManager::get_Static()
ldarg.2
ldc.i4.1
bne.un.s L5
ldloc.1
ldc.i4.1
xor
call Sandbox.Definitions.MyMechanicalConnectionBlockBaseDefinition Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::get_BlockDefinition()
ldfld System.String Sandbox.Definitions.MyMechanicalConnectionBlockBaseDefinition::TopPart
br.s L7
L6:
ldarg.0
call Sandbox.Definitions.MyMechanicalConnectionBlockBaseDefinition Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::get_BlockDefinition()
ldfld System.String Sandbox.Definitions.MyMechanicalConnectionBlockBaseDefinition::MediumTopPart
L7:
callvirt Sandbox.Definitions.MyCubeBlockDefinitionGroup Sandbox.Definitions.MyDefinitionManager::TryGetDefinitionGroup(System.String groupName)
ldarg.2
ldc.i4.2
beq.s L8
ldarg.2
ldc.i4.1
bne.un.s L9
L8:
ldloc.1
brtrue.s L10
ldc.i4.1
stloc.1
L9:
L10:
ldloc.1
callvirt Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Definitions.MyCubeBlockDefinitionGroup::get_Item(VRage.Game.MyCubeSize size)
stloc.2
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
ldfld Sandbox.Game.Multiplayer.MyPlayerCollection Sandbox.Game.World.MySession::Players
ldloc.0
callvirt System.UInt64 Sandbox.Game.Multiplayer.MyPlayerCollection::TryGetSteamId(System.Int64 identityId)
callvirt System.Boolean Sandbox.Game.World.MySession::CreativeToolsEnabled(System.UInt64 user)
stloc.3
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
ldloc.0
ldloc.2
ldfld System.String Sandbox.Definitions.MyCubeBlockDefinition::BlockPairName
ldloc.3
brtrue.s L11
ldsfld System.Int32 Sandbox.Definitions.MyCubeBlockDefinition::PCU_CONSTRUCTION_STAGE_COST
br.s L12
L11:
ldloc.2
ldfld System.Int32 Sandbox.Definitions.MyCubeBlockDefinition::PCU
L12:
ldc.i4.1
ldc.i4.0
ldnull
callvirt System.Boolean Sandbox.Game.World.MySession::CheckLimitsAndNotify(System.Int64 ownerID, System.String blockName, System.Int32 pcuToBuild, System.Int32 blocksToBuild, System.Int32 blocksCount, System.Collections.Generic.Dictionary`2<System.String, System.Int32> blocksPerType)
brfalse.s L13
ldarg.0
ldsfld System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`3<System.Int64, Sandbox.Game.Entities.Blocks.MyTopBlockSize, System.Boolean>> Sandbox.Game.Entities.Blocks.<>c::<>9__105_0
dup
brtrue.s L14
pop
ldsfld Sandbox.Game.Entities.Blocks.<>c Sandbox.Game.Entities.Blocks.<>c::<>9
ldftn System.Action`3<System.Int64, Sandbox.Game.Entities.Blocks.MyTopBlockSize, System.Boolean> Sandbox.Game.Entities.Blocks.<>c::<RecreateTop>b__105_0(Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase x)
newobj System.Void System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`3<System.Int64, Sandbox.Game.Entities.Blocks.MyTopBlockSize, System.Boolean>>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`3<System.Int64, Sandbox.Game.Entities.Blocks.MyTopBlockSize, System.Boolean>> Sandbox.Game.Entities.Blocks.<>c::<>9__105_0
L14:
ldloc.0
ldarg.2
ldarg.3
ldloca.s 4 (VRage.Network.EndpointId)
initobj VRage.Network.EndpointId
ldloc.s 4 (VRage.Network.EndpointId)
call static System.Void Sandbox.Engine.Multiplayer.MyMultiplayer::RaiseEvent(Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase arg1, System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`3<System.Int64, Sandbox.Game.Entities.Blocks.MyTopBlockSize, System.Boolean>> action, System.Int64 arg2, Sandbox.Game.Entities.Blocks.MyTopBlockSize arg3, System.Boolean arg4, VRage.Network.EndpointId targetEndpoint)
L13:
ret
