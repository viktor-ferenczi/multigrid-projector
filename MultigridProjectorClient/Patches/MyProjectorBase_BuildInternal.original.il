// ee2506ad
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
ldfld Sandbox.Game.Multiplayer.MyPlayerCollection Sandbox.Game.World.MySession::Players
ldarg.2
callvirt System.UInt64 Sandbox.Game.Multiplayer.MyPlayerCollection::TryGetSteamId(System.Int64 identityId)
stloc.0
ldarg.0
call Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.Blocks.MyProjectorBase::get_ProjectedGrid()
brtrue.s L0
ret
L0:
ldarg.0
call Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.Blocks.MyProjectorBase::get_ProjectedGrid()
ldarg.1
callvirt Sandbox.Game.Entities.Cube.MySlimBlock Sandbox.Game.Entities.MyCubeGrid::GetCubeBlock(VRageMath.Vector3I pos)
stloc.1
ldloc.1
brfalse.s L1
ldloc.1
ldfld Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Game.Entities.Cube.MySlimBlock::BlockDefinition
brfalse.s L2
ldarg.0
call System.Boolean Sandbox.Game.Entities.Blocks.MyProjectorBase::get_AllowWelding()
brfalse.s L3
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
callvirt Sandbox.Game.SessionComponents.MySessionComponentDLC Sandbox.Game.World.MySession::GetComponent()
ldloc.1
ldfld Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Game.Entities.Cube.MySlimBlock::BlockDefinition
ldloc.0
callvirt System.Boolean Sandbox.Game.SessionComponents.MySessionComponentDLC::HasDefinitionDLC(VRage.Game.MyDefinitionBase definition, System.UInt64 steamId)
brtrue.s L4
L1:
L2:
L3:
call static Sandbox.Engine.Multiplayer.MyMultiplayerBase Sandbox.Engine.Multiplayer.MyMultiplayer::get_Static()
isinst Sandbox.Engine.Multiplayer.MyMultiplayerServerBase
call static VRage.Network.MyEventContext VRage.Network.MyEventContext::get_Current()
ldfld VRage.Network.EndpointId VRage.Network.MyEventContext::Sender
ldfld System.UInt64 VRage.Network.EndpointId::Value
ldc.i4.0
ldnull
ldc.i4.1
callvirt virtual System.Void Sandbox.Engine.Multiplayer.MyMultiplayerServerBase::ValidationFailed(System.UInt64 clientId, System.Boolean kick, System.String additionalInfo, System.Boolean stackTrace)
ret
L4:
ldsfld VRageMath.Quaternion VRageMath.Quaternion::Identity
stloc.2
ldloc.1
ldfld VRageMath.MyBlockOrientation Sandbox.Game.Entities.Cube.MySlimBlock::Orientation
stloc.3
ldsfld VRageMath.Quaternion VRageMath.Quaternion::Identity
stloc.s 4 (VRageMath.Quaternion)
ldarg.0
call virtual VRageMath.MyBlockOrientation Sandbox.Game.Entities.MyCubeBlock::get_Orientation()
stloc.s 20 (VRageMath.MyBlockOrientation)
ldloca.s 20 (VRageMath.MyBlockOrientation)
ldloca.s 4 (VRageMath.Quaternion)
call System.Void VRageMath.MyBlockOrientation::GetQuaternion(VRageMath.Quaternion& result)
ldloca.s 3 (VRageMath.MyBlockOrientation)
ldloca.s 2 (VRageMath.Quaternion)
call System.Void VRageMath.MyBlockOrientation::GetQuaternion(VRageMath.Quaternion& result)
ldarg.0
call VRageMath.Quaternion Sandbox.Game.Entities.Blocks.MyProjectorBase::get_ProjectionRotationQuaternion()
ldloc.2
call static VRageMath.Quaternion VRageMath.Quaternion::Multiply(VRageMath.Quaternion quaternion1, VRageMath.Quaternion quaternion2)
stloc.2
ldloc.s 4 (VRageMath.Quaternion)
ldloc.2
call static VRageMath.Quaternion VRageMath.Quaternion::Multiply(VRageMath.Quaternion quaternion1, VRageMath.Quaternion quaternion2)
stloc.2
ldarg.0
call Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.MyCubeBlock::get_CubeGrid()
stloc.s 5 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.1
callvirt Sandbox.Game.Entities.MyCubeGrid Sandbox.Game.Entities.Cube.MySlimBlock::get_CubeGrid()
stloc.s 6 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.1
callvirt Sandbox.Game.Entities.MyCubeBlock Sandbox.Game.Entities.Cube.MySlimBlock::get_FatBlock()
brtrue.s L5
ldloc.1
ldfld VRageMath.Vector3I Sandbox.Game.Entities.Cube.MySlimBlock::Position
br.s L6
L5:
ldloc.1
callvirt Sandbox.Game.Entities.MyCubeBlock Sandbox.Game.Entities.Cube.MySlimBlock::get_FatBlock()
callvirt virtual VRageMath.Vector3I Sandbox.Game.Entities.MyCubeBlock::get_Min()
L6:
stloc.s 7 (VRageMath.Vector3I)
ldloc.1
callvirt Sandbox.Game.Entities.MyCubeBlock Sandbox.Game.Entities.Cube.MySlimBlock::get_FatBlock()
brtrue.s L7
ldloc.1
ldfld VRageMath.Vector3I Sandbox.Game.Entities.Cube.MySlimBlock::Position
br.s L8
L7:
ldloc.1
callvirt Sandbox.Game.Entities.MyCubeBlock Sandbox.Game.Entities.Cube.MySlimBlock::get_FatBlock()
callvirt virtual VRageMath.Vector3I Sandbox.Game.Entities.MyCubeBlock::get_Max()
L8:
stloc.s 8 (VRageMath.Vector3I)
ldloc.s 5 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 6 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 7 (VRageMath.Vector3I)
callvirt virtual VRageMath.Vector3D Sandbox.Game.Entities.MyCubeGrid::GridIntegerToWorld(VRageMath.Vector3I gridCoords)
callvirt VRageMath.Vector3I Sandbox.Game.Entities.MyCubeGrid::WorldToGridInteger(VRageMath.Vector3D coords)
stloc.s 9 (VRageMath.Vector3I)
ldloc.s 5 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 6 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 8 (VRageMath.Vector3I)
callvirt virtual VRageMath.Vector3D Sandbox.Game.Entities.MyCubeGrid::GridIntegerToWorld(VRageMath.Vector3I gridCoords)
callvirt VRageMath.Vector3I Sandbox.Game.Entities.MyCubeGrid::WorldToGridInteger(VRageMath.Vector3D coords)
stloc.s 10 (VRageMath.Vector3I)
ldloc.s 5 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 6 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.1
ldfld VRageMath.Vector3I Sandbox.Game.Entities.Cube.MySlimBlock::Position
callvirt virtual VRageMath.Vector3D Sandbox.Game.Entities.MyCubeGrid::GridIntegerToWorld(VRageMath.Vector3I gridCoords)
callvirt VRageMath.Vector3I Sandbox.Game.Entities.MyCubeGrid::WorldToGridInteger(VRageMath.Vector3D coords)
stloc.s 11 (VRageMath.Vector3I)
ldloca.s 12 (VRageMath.Vector3I)
ldloc.s 9 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::X
ldloc.s 10 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::X
call static System.Int32 System.Math::Min(System.Int32 val1, System.Int32 val2)
ldloc.s 9 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Y
ldloc.s 10 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Y
call static System.Int32 System.Math::Min(System.Int32 val1, System.Int32 val2)
ldloc.s 9 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Z
ldloc.s 10 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Z
call static System.Int32 System.Math::Min(System.Int32 val1, System.Int32 val2)
call System.Void VRageMath.Vector3I::.ctor(System.Int32 x, System.Int32 y, System.Int32 z)
ldloca.s 13 (VRageMath.Vector3I)
ldloc.s 9 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::X
ldloc.s 10 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::X
call static System.Int32 System.Math::Max(System.Int32 val1, System.Int32 val2)
ldloc.s 9 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Y
ldloc.s 10 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Y
call static System.Int32 System.Math::Max(System.Int32 val1, System.Int32 val2)
ldloc.s 9 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Z
ldloc.s 10 (VRageMath.Vector3I)
ldfld System.Int32 VRageMath.Vector3I::Z
call static System.Int32 System.Math::Max(System.Int32 val1, System.Int32 val2)
call System.Void VRageMath.Vector3I::.ctor(System.Int32 x, System.Int32 y, System.Int32 z)
ldloca.s 14 (Sandbox.Game.Entities.MyCubeGrid+MyBlockLocation)
ldloc.1
ldfld Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Game.Entities.Cube.MySlimBlock::BlockDefinition
ldfld VRage.Game.MyDefinitionId VRage.Game.MyDefinitionBase::Id
ldloc.s 12 (VRageMath.Vector3I)
ldloc.s 13 (VRageMath.Vector3I)
ldloc.s 11 (VRageMath.Vector3I)
ldloc.2
ldc.i4.0
conv.i8
ldarg.2
call System.Void Sandbox.Game.Entities.MyBlockLocation::.ctor(VRage.Game.MyDefinitionId blockDefinition, VRageMath.Vector3I min, VRageMath.Vector3I max, VRageMath.Vector3I center, VRageMath.Quaternion orientation, System.Int64 entityId, System.Int64 owner)
ldnull
stloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
ldarg.0
ldfld System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeGrid> Sandbox.Game.Entities.Blocks.MyProjectorBase::m_originalGridBuilders
brfalse.s L9
ldarg.0
ldfld System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeGrid> Sandbox.Game.Entities.Blocks.MyProjectorBase::m_originalGridBuilders
callvirt virtual System.Int32 System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeGrid>::get_Count()
ldc.i4.0
bgt.s L10
L9:
ldnull
br.s L11
L10:
ldarg.0
ldfld System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeGrid> Sandbox.Game.Entities.Blocks.MyProjectorBase::m_originalGridBuilders
ldc.i4.0
callvirt virtual VRage.Game.MyObjectBuilder_CubeGrid System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeGrid>::get_Item(System.Int32 index)
L11:
stloc.s 16 (VRage.Game.MyObjectBuilder_CubeGrid)
ldloc.s 16 (VRage.Game.MyObjectBuilder_CubeGrid)
brfalse L12
ldloc.s 16 (VRage.Game.MyObjectBuilder_CubeGrid)
ldfld System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeBlock> VRage.Game.MyObjectBuilder_CubeGrid::CubeBlocks
callvirt System.Collections.Generic.Enumerator<VRage.Game.MyObjectBuilder_CubeBlock> System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeBlock>::GetEnumerator()
stloc.s 21 (System.Collections.Generic.List`1+Enumerator[VRage.Game.MyObjectBuilder_CubeBlock])
[EX_BeginException]
br L13
L20:
ldloca.s 21 (System.Collections.Generic.List`1+Enumerator[VRage.Game.MyObjectBuilder_CubeBlock])
call virtual VRage.Game.MyObjectBuilder_CubeBlock System.Collections.Generic.Enumerator<VRage.Game.MyObjectBuilder_CubeBlock>::get_Current()
stloc.s 22 (VRage.Game.MyObjectBuilder_CubeBlock)
ldloc.s 22 (VRage.Game.MyObjectBuilder_CubeBlock)
brfalse.s L14
ldloc.s 22 (VRage.Game.MyObjectBuilder_CubeBlock)
ldfld VRage.SerializableVector3I VRage.Game.MyObjectBuilder_CubeBlock::Min
call static VRageMath.Vector3I VRage.SerializableVector3I::op_Implicit(VRage.SerializableVector3I v)
ldloc.s 7 (VRageMath.Vector3I)
call static System.Boolean VRageMath.Vector3I::op_Equality(VRageMath.Vector3I a, VRageMath.Vector3I b)
brfalse.s L15
ldloc.s 22 (VRage.Game.MyObjectBuilder_CubeBlock)
call static VRage.Game.MyDefinitionId VRage.Game.MyObjectBuilderExtensions::GetId(VRage.ObjectBuilders.MyObjectBuilder_Base self)
ldloc.1
ldfld Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Game.Entities.Cube.MySlimBlock::BlockDefinition
ldfld VRage.Game.MyDefinitionId VRage.Game.MyDefinitionBase::Id
call static System.Boolean VRage.Game.MyDefinitionId::op_Equality(VRage.Game.MyDefinitionId l, VRage.Game.MyDefinitionId r)
brfalse.s L16
ldloc.s 22 (VRage.Game.MyObjectBuilder_CubeBlock)
callvirt virtual VRage.ObjectBuilders.MyObjectBuilder_Base VRage.ObjectBuilders.MyObjectBuilder_Base::Clone()
castclass VRage.Game.MyObjectBuilder_CubeBlock
stloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
ldsfld VRage.Game.MyDefinitionManagerBase VRage.Game.MyDefinitionManagerBase::Static
brfalse.s L17
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
isinst Sandbox.Common.ObjectBuilders.MyObjectBuilder_BatteryBlock
brfalse.s L18
call static Sandbox.Definitions.MyDefinitionManager Sandbox.Definitions.MyDefinitionManager::get_Static()
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
callvirt Sandbox.Definitions.MyCubeBlockDefinition Sandbox.Definitions.MyDefinitionManager::GetCubeBlockDefinition(VRage.Game.MyObjectBuilder_CubeBlock builder)
castclass Sandbox.Definitions.MyBatteryBlockDefinition
stloc.s 23 (Sandbox.Definitions.MyBatteryBlockDefinition)
ldloc.s 23 (Sandbox.Definitions.MyBatteryBlockDefinition)
brfalse.s L19
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
castclass Sandbox.Common.ObjectBuilders.MyObjectBuilder_BatteryBlock
ldloc.s 23 (Sandbox.Definitions.MyBatteryBlockDefinition)
ldfld System.Single Sandbox.Definitions.MyBatteryBlockDefinition::InitialStoredPowerRatio
ldloc.s 23 (Sandbox.Definitions.MyBatteryBlockDefinition)
ldfld System.Single Sandbox.Definitions.MyBatteryBlockDefinition::MaxStoredPower
mul
stfld System.Single Sandbox.Common.ObjectBuilders.MyObjectBuilder_BatteryBlock::CurrentStoredPower
L13:
L14:
L15:
L16:
L17:
L18:
L19:
ldloca.s 21 (System.Collections.Generic.List`1+Enumerator[VRage.Game.MyObjectBuilder_CubeBlock])
call virtual System.Boolean System.Collections.Generic.Enumerator<VRage.Game.MyObjectBuilder_CubeBlock>::MoveNext()
brtrue L20
leave.s L21
[EX_BeginFinally]
ldloca.s 21 (System.Collections.Generic.List`1+Enumerator[VRage.Game.MyObjectBuilder_CubeBlock])
constrained. System.Collections.Generic.List`1+Enumerator[VRage.Game.MyObjectBuilder_CubeBlock]
callvirt abstract virtual System.Void System.IDisposable::Dispose()
[EX_EndException]
endfinally
L12:
L21:
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
brtrue.s L22
ldloc.1
ldc.i4.0
callvirt VRage.Game.MyObjectBuilder_CubeBlock Sandbox.Game.Entities.Cube.MySlimBlock::GetObjectBuilder(System.Boolean copy)
stloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
ldloca.s 14 (Sandbox.Game.Entities.MyCubeGrid+MyBlockLocation)
ldc.i4.1
ldc.i4.0
call static System.Int64 VRage.MyEntityIdentifier::AllocateId(VRage.ID_OBJECT_TYPE objectType, VRage.ID_ALLOCATION_METHOD generationMethod)
stfld System.Int64 Sandbox.Game.Entities.MyBlockLocation::EntityId
L22:
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
ldnull
stfld VRage.Game.MyObjectBuilder_Inventory VRage.Game.MyObjectBuilder_CubeBlock::ConstructionInventory
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
ldarg.s 5
stfld System.Int64 VRage.Game.MyObjectBuilder_CubeBlock::BuiltBy
ldarg.s 4
brfalse.s L23
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
call static VRage.Network.MyEventContext VRage.Network.MyEventContext::get_Current()
ldfld VRage.Network.EndpointId VRage.Network.MyEventContext::Sender
ldfld System.UInt64 VRage.Network.EndpointId::Value
callvirt System.Boolean Sandbox.Game.World.MySession::CreativeToolsEnabled(System.UInt64 user)
br.s L24
L23:
ldc.i4.0
L24:
stloc.s 17 (System.Boolean)
call static Sandbox.Game.World.MySession Sandbox.Game.World.MySession::get_Static()
callvirt Sandbox.Game.SessionComponents.MySessionComponentGameInventory Sandbox.Game.World.MySession::GetComponent()
dup
brtrue.s L25
pop
ldsfld VRage.Utils.MyStringHash VRage.Utils.MyStringHash::NullOrEmpty
br.s L26
L25:
ldloc.1
ldfld VRage.Utils.MyStringHash Sandbox.Game.Entities.Cube.MySlimBlock::SkinSubtypeId
ldloc.0
call VRage.Utils.MyStringHash Sandbox.Game.SessionComponents.MySessionComponentGameInventory::ValidateArmor(VRage.Utils.MyStringHash armorId, System.UInt64 steamId)
L26:
stloc.s 18 (VRage.Utils.MyStringHash)
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
ldloca.s 18 (VRage.Utils.MyStringHash)
call System.String VRage.Utils.MyStringHash::get_String()
stfld System.String VRage.Game.MyObjectBuilder_CubeBlock::SkinSubtypeId
ldloca.s 19 (Sandbox.Game.Entities.MyCubeGrid+MyBlockVisuals)
ldloc.1
callvirt VRageMath.Vector3 Sandbox.Game.Entities.Cube.MySlimBlock::get_ColorMaskHSV()
call static System.UInt32 VRageMath.ColorExtensions::PackHSVToUint(VRageMath.Vector3 HSV)
ldloc.s 18 (VRage.Utils.MyStringHash)
ldc.i4.1
ldc.i4.1
call System.Void Sandbox.Game.Entities.MyBlockVisuals::.ctor(System.UInt32 colorMaskHsv, VRage.Utils.MyStringHash skinId, System.Boolean applyColor, System.Boolean applySkin)
ldloc.s 5 (Sandbox.Game.Entities.MyCubeGrid)
ldloc.s 19 (Sandbox.Game.Entities.MyCubeGrid+MyBlockVisuals)
ldloc.s 14 (Sandbox.Game.Entities.MyCubeGrid+MyBlockLocation)
ldloc.s 15 (VRage.Game.MyObjectBuilder_CubeBlock)
ldarg.3
ldloc.s 17 (System.Boolean)
ldarg.2
call static VRage.Network.MyEventContext VRage.Network.MyEventContext::get_Current()
stloc.s 24 (VRage.Network.MyEventContext)
ldloca.s 24 (VRage.Network.MyEventContext)
call System.Boolean VRage.Network.MyEventContext::get_IsLocallyInvoked()
brtrue.s L27
call static VRage.Network.MyEventContext VRage.Network.MyEventContext::get_Current()
ldfld VRage.Network.EndpointId VRage.Network.MyEventContext::Sender
ldfld System.UInt64 VRage.Network.EndpointId::Value
br.s L28
L27:
ldloc.0
L28:
ldc.i4.1
callvirt System.Void Sandbox.Game.Entities.MyCubeGrid::BuildBlockRequestInternal(Sandbox.Game.Entities.MyBlockVisuals visuals, Sandbox.Game.Entities.MyBlockLocation location, VRage.Game.MyObjectBuilder_CubeBlock blockObjectBuilder, System.Int64 builderEntityId, System.Boolean instantBuild, System.Int64 ownerId, System.UInt64 sender, System.Boolean isProjection)
ldarg.0
ldloc.1
call System.Void Sandbox.Game.Entities.Blocks.MyProjectorBase::HideCube(Sandbox.Game.Entities.Cube.MySlimBlock cubeBlock)
ldarg.0
ldsfld System.Int32 Sandbox.Game.Entities.Blocks.MyProjectorBase::PROJECTION_TIME_IN_FRAMES
stfld System.Int32 Sandbox.Game.Entities.Blocks.MyProjectorBase::m_projectionTimer
ret
