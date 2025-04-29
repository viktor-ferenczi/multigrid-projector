// cf0de7d3
ldarg.3
brtrue.s L0
ldarg.1
ldnull
stind.ref
ret
L0:
ldarg.0
call Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.MyCubeBlock::get_CubeGrid()
callvirt virtual VRage.Game.MyCubeSize Sandbox.Game.Entities.MyCubeGrid::get_GridSizeEnum()
stloc.0
ldarg.s 4
ldc.i4.s 10
beq.s L2
ldarg.s 4
ldc.i4.s 11
bne.un.s L15
ldloc.0
ldc.i4.1
xor
stloc.0
br.s L15
L15:
ldarg.s 4
ldc.i4.2
beq.s L1
ldarg.s 4
ldc.i4.1
bne.un.s L2
L1:
ldloc.0
brtrue.s L3
ldc.i4.1
stloc.0
L2:
L3:
ldarg.0
callvirt abstract virtual VRageMath.MatrixD Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::GetTopGridMatrix()
stloc.1
ldarg.3
ldloc.0
callvirt Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Definitions.MyCubeBlockDefinitionGroup::get_Item(VRage.Game.MyCubeSize size)
stloc.2
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
ldfld Sandbox.Game.Multiplayer.MyPlayerCollection Sandbox.Game.World.MySession::Players
ldarg.2
callvirt System.UInt64 Sandbox.Game.Multiplayer.MyPlayerCollection::TryGetSteamId(System.Int64 identityId)
stloc.3
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
ldloc.3
callvirt System.Boolean Sandbox.Game.World.MySession::CreativeToolsEnabled(System.UInt64 user)
stloc.s 4 (System.Boolean)
ldsfld System.String System.String::Empty
stloc.s 5 (System.String)
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
ldloca.s 5 (System.String)
ldarg.2
ldloc.2
ldfld System.String Sandbox.Definitions.MyCubeBlockDefinition::BlockPairName
ldloc.s 4 (System.Boolean)
brtrue.s L4
ldsfld System.Int32 Sandbox.Definitions.MyCubeBlockDefinition::PCU_CONSTRUCTION_STAGE_COST
br.s L5
L4:
ldloc.2
ldfld System.Int32 Sandbox.Definitions.MyCubeBlockDefinition::PCU
L5:
ldc.i4.1
ldc.i4.0
ldnull
callvirt Sandbox.Game.World.LimitResult Sandbox.Game.World.MySession::IsWithinWorldLimits(System.String& failedBlockType, System.Int64 ownerID, System.String blockName, System.Int32 pcuToBuild, System.Int32 blocksToBuild, System.Int32 blocksCount, System.Collections.Generic.Dictionary`2<System.String, System.Int32> blocksPerType)
stloc.s 6 (Sandbox.Game.World.MySession+LimitResult)
ldloc.s 6 (Sandbox.Game.World.MySession+LimitResult)
brfalse.s L6
ldarg.0
ldsfld System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`1<Sandbox.Game.World.LimitResult>> Sandbox.Game.Entities.Blocks.<>c::<>9__116_0
dup
brtrue.s L7
pop
ldsfld Sandbox.Game.Entities.Blocks.<>c Sandbox.Game.Entities.Blocks.<>c::<>9
ldftn System.Action`1<Sandbox.Game.World.LimitResult> Sandbox.Game.Entities.Blocks.<>c::<CreateTopPart>b__116_0(Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase x)
newobj System.Void System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`1<Sandbox.Game.World.LimitResult>>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`1<Sandbox.Game.World.LimitResult>> Sandbox.Game.Entities.Blocks.<>c::<>9__116_0
L7:
ldloc.s 6 (Sandbox.Game.World.MySession+LimitResult)
ldloc.3
newobj System.Void VRage.Network.EndpointId::.ctor(System.UInt64 value)
call static System.Void Sandbox.Engine.Multiplayer.MyMultiplayer::RaiseEvent(Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase arg1, System.Func`2<Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase, System.Action`1<Sandbox.Game.World.LimitResult>> action, Sandbox.Game.World.LimitResult arg2, VRage.Network.EndpointId targetEndpoint)
ldarg.1
ldnull
stind.ref
ret
L6:
ldloc.2
ldsfld VRageMath.Vector3I VRageMath.Vector3I::Zero
ldsfld VRageMath.MyBlockOrientation VRageMath.MyBlockOrientation::Identity
ldc.i4.1
ldc.i4.0
call static System.Int64 VRage.MyEntityIdentifier::AllocateId(VRage.ID_OBJECT_TYPE objectType, VRage.ID_ALLOCATION_METHOD generationMethod)
ldarg.0
call System.Int64 Sandbox.Game.Entities.MyCubeBlock::get_BuiltBy()
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
callvirt System.Boolean Sandbox.Game.World.MySession::get_CreativeMode()
ldarg.s 5
or
call static VRage.Game.MyObjectBuilder_CubeBlock Sandbox.Game.Entities.MyCubeGrid::CreateBlockObjectBuilder(Sandbox.Definitions.MyCubeBlockDefinition definition, VRageMath.Vector3I min, VRageMath.MyBlockOrientation orientation, System.Int64 entityID, System.Int64 owner, System.Boolean fullyBuilt)
stloc.s 7 (VRage.Game.MyObjectBuilder_CubeBlock)
ldloc.2
ldflda System.Nullable`1<VRageMath.Vector3> Sandbox.Definitions.MyCubeBlockDefinition::MechanicalTopInitialPlacementOffset
call System.Boolean System.Nullable`1<VRageMath.Vector3>::get_HasValue()
brfalse.s L8
ldloca.s 1 (VRageMath.MatrixD)
ldloc.2
ldflda System.Nullable`1<VRageMath.Vector3> Sandbox.Definitions.MyCubeBlockDefinition::MechanicalTopInitialPlacementOffset
call VRageMath.Vector3 System.Nullable`1<VRageMath.Vector3>::get_Value()
ldloc.1
call static VRageMath.Vector3D VRageMath.Vector3D::Transform(VRageMath.Vector3 position, VRageMath.MatrixD matrix)
call System.Void VRageMath.MatrixD::set_Translation(VRageMath.Vector3D value)
br.s L9
L8:
ldloc.2
callvirt VRageMath.Vector3I Sandbox.Definitions.MyCubeBlockDefinition::get_Center()
call static VRageMath.Vector3 VRageMath.Vector3I::op_Implicit(VRageMath.Vector3I value)
ldsfld VRageMath.Vector3 VRageMath.Vector3::Zero
call static System.Boolean VRageMath.Vector3::op_Inequality(VRageMath.Vector3 value1, VRageMath.Vector3 value2)
brfalse.s L10
ldloca.s 1 (VRageMath.MatrixD)
ldloc.2
callvirt VRageMath.Vector3I Sandbox.Definitions.MyCubeBlockDefinition::get_Center()
call static VRageMath.Vector3I VRageMath.Vector3I::op_UnaryNegation(VRageMath.Vector3I a)
call static Sandbox.Definitions.MyDefinitionManager Sandbox.Definitions.MyDefinitionManager::get_Static()
ldloc.0
callvirt System.Single Sandbox.Definitions.MyDefinitionManager::GetCubeSize(VRage.Game.MyCubeSize gridSize)
call static VRageMath.Vector3 VRageMath.Vector3I::op_Multiply(VRageMath.Vector3I a, System.Single num)
ldloc.1
call static VRageMath.Vector3D VRageMath.Vector3D::Transform(VRageMath.Vector3 position, VRageMath.MatrixD matrix)
call System.Void VRageMath.MatrixD::set_Translation(VRageMath.Vector3D value)
L9:
L10:
ldloc.s 7 (VRage.Game.MyObjectBuilder_CubeBlock)
isinst Sandbox.Common.ObjectBuilders.MyObjectBuilder_AttachableTopBlockBase
stloc.s 8 (Sandbox.Common.ObjectBuilders.MyObjectBuilder_AttachableTopBlockBase)
ldloc.s 8 (Sandbox.Common.ObjectBuilders.MyObjectBuilder_AttachableTopBlockBase)
brfalse.s L11
ldloc.s 8 (Sandbox.Common.ObjectBuilders.MyObjectBuilder_AttachableTopBlockBase)
ldc.i4.0
stfld System.Boolean Sandbox.Common.ObjectBuilders.MyObjectBuilder_AttachableTopBlockBase::YieldLastComponent
L11:
ldloc.s 7 (VRage.Game.MyObjectBuilder_CubeBlock)
isinst Sandbox.Common.ObjectBuilders.MyObjectBuilder_Wheel
stloc.s 9 (Sandbox.Common.ObjectBuilders.MyObjectBuilder_Wheel)
ldloc.s 9 (Sandbox.Common.ObjectBuilders.MyObjectBuilder_Wheel)
brfalse.s L12
ldloc.s 9 (Sandbox.Common.ObjectBuilders.MyObjectBuilder_Wheel)
ldc.i4.0
stfld System.Boolean Sandbox.Common.ObjectBuilders.MyObjectBuilder_Wheel::YieldLastComponent
L12:
call static VRage.Game.MyObjectBuilder_CubeGrid VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::CreateNewObject()
stloc.s 10 (VRage.Game.MyObjectBuilder_CubeGrid)
ldloc.s 10 (VRage.Game.MyObjectBuilder_CubeGrid)
ldloc.0
stfld VRage.Game.MyCubeSize VRage.Game.MyObjectBuilder_CubeGrid::GridSizeEnum
ldloc.s 10 (VRage.Game.MyObjectBuilder_CubeGrid)
ldc.i4.0
stfld System.Boolean VRage.Game.MyObjectBuilder_CubeGrid::IsStatic
ldloc.s 10 (VRage.Game.MyObjectBuilder_CubeGrid)
ldloc.1
newobj System.Void VRage.MyPositionAndOrientation::.ctor(VRageMath.MatrixD matrix)
newobj System.Void System.Nullable`1<VRage.MyPositionAndOrientation>::.ctor(VRage.MyPositionAndOrientation value)
stfld System.Nullable`1<VRage.MyPositionAndOrientation> VRage.ObjectBuilders.MyObjectBuilder_EntityBase::PositionAndOrientation
ldloc.s 10 (VRage.Game.MyObjectBuilder_CubeGrid)
ldfld System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeBlock> VRage.Game.MyObjectBuilder_CubeGrid::CubeBlocks
ldloc.s 7 (VRage.Game.MyObjectBuilder_CubeBlock)
callvirt virtual System.Void System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeBlock>::Add(VRage.Game.MyObjectBuilder_CubeBlock item)
ldloc.s 10 (VRage.Game.MyObjectBuilder_CubeGrid)
call static Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.MyEntityFactory::CreateEntity(VRage.ObjectBuilders.MyObjectBuilder_Base builder)
stloc.s 11 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 11 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 10 (VRage.Game.MyObjectBuilder_CubeGrid)
callvirt virtual System.Void VRage.Game.Entity.MyEntity::Init(VRage.ObjectBuilders.MyObjectBuilder_EntityBase objectBuilder)
ldarg.1
ldloc.s 11 (Sandbox.Game.Entities.MyCubeGrid)
ldsfld VRageMath.Vector3I VRageMath.Vector3I::Zero
callvirt Sandbox.Game.Entities.Cube.MySlimBlock Sandbox.Game.Entities.MyCubeGrid::GetCubeBlock(VRageMath.Vector3I pos)
callvirt Sandbox.Game.Entities.MyCubeBlock Sandbox.Game.Entities.Cube.MySlimBlock::get_FatBlock()
castclass Sandbox.Game.Entities.Blocks.MyAttachableTopBlockBase
stind.ref
ldarg.0
ldarg.1
ldind.ref
ldarg.2
callvirt virtual System.Boolean Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::CanPlaceTop(Sandbox.Game.Entities.Blocks.MyAttachableTopBlockBase topBlock, System.Int64 builtBy)
brtrue.s L13
ldarg.1
ldnull
stind.ref
ldloc.s 11 (Sandbox.Game.Entities.MyCubeGrid)
callvirt System.Void VRage.Game.Entity.MyEntity::Close()
ret
L13:
ldloc.s 11 (Sandbox.Game.Entities.MyCubeGrid)
ldc.i4.1
call static System.Void Sandbox.Game.Entities.MyEntities::Add(VRage.Game.Entity.MyEntity entity, System.Boolean insertIntoScene)
ldarg.1
ldind.ref
callvirt Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.MyCubeBlock::get_CubeGrid()
callvirt VRageMath.MatrixD VRage.Game.Entity.MyEntity::get_WorldMatrix()
ldarg.0
call VRageMath.MatrixD VRage.Game.Entity.MyEntity::get_WorldMatrix()
call static VRageMath.MatrixD VRageMath.MatrixD::Invert(VRageMath.MatrixD matrix)
call static VRageMath.MatrixD VRageMath.MatrixD::op_Multiply(VRageMath.MatrixD matrix1, VRageMath.MatrixD matrix2)
pop
call static System.Boolean Sandbox.Game.Multiplayer.Sync::get_IsServer()
brfalse.s L14
ldarg.0
ldfld VRage.Sync.Sync`2<Sandbox.Game.Entities.Blocks.State, VRage.Sync.FromServer> Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase::m_connectionState
ldloca.s 12 (Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase+State)
initobj Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase+State
ldloca.s 12 (Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase+State)
ldarg.1
ldind.ref
callvirt virtual System.Int64 VRage.Game.Entity.MyEntity::get_EntityId()
newobj System.Void System.Nullable`1<System.Int64>::.ctor(System.Int64 value)
stfld System.Nullable`1<System.Int64> Sandbox.Game.Entities.Blocks.State::TopBlockId
ldloc.s 12 (Sandbox.Game.Entities.Blocks.MyMechanicalConnectionBlockBase+State)
callvirt System.Void VRage.Sync.Sync`2<Sandbox.Game.Entities.Blocks.State, VRage.Sync.FromServer>::set_Value(Sandbox.Game.Entities.Blocks.State value)
L14:
ret
