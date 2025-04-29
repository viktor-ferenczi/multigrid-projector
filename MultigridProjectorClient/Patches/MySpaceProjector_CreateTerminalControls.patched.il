// 0b1a3a3e
call static System.Boolean Sandbox.Game.Gui.MyTerminalControlFactory::AreControlsCreated()
brfalse.s L0
ret
L0:
ldarg.0
call virtual System.Void Sandbox.Game.Entities.Cube.MyFunctionalBlock::CreateTerminalControls()
ldsfld System.Boolean Sandbox.Engine.Utils.MyFakes::ENABLE_PROJECTOR_BLOCK
brtrue.s L1
ret
L1:
ldstr "Blueprint"
ldsfld VRage.Utils.MyStringId MyCommonTexts::Blueprints
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldsfld System.Action`1<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_0
dup
brtrue.s L2
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_0(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Action`1<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Action`1<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_0
L2:
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlButton`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Action`1<Entities.Blocks.MySpaceProjector> action, System.Boolean isAutoscaleEnabled, System.Boolean isEllipsisEnabled)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_1
dup
brtrue.s L3
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_1(Entities.Blocks.MySpaceProjector b)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_1
L3:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
dup
ldc.i4.0
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::set_SupportsMultipleBlocks(System.Boolean value)
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "Remove"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::RemoveProjectionButton
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldsfld System.Action`1<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_2
dup
brtrue.s L4
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_2(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Action`1<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Action`1<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_2
L4:
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlButton`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Action`1<Entities.Blocks.MySpaceProjector> action, System.Boolean isAutoscaleEnabled, System.Boolean isEllipsisEnabled)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_3
dup
brtrue.s L5
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_3(Entities.Blocks.MySpaceProjector b)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_3
L5:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "KeepProjection"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::KeepProjectionToggle
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::KeepProjectionTooltip
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldc.i4.0
ldc.i4.0
ldc.i4.0
ldc.r4 1
newobj System.Void Sandbox.Game.Gui.MyTerminalControlCheckbox`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Nullable`1<VRage.Utils.MyStringId> on, System.Nullable`1<VRage.Utils.MyStringId> off, System.Boolean justify, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled, System.Single maxWidth)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_4
dup
brtrue.s L6
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_4(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_4
L6:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_5
dup
brtrue.s L7
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_5(Entities.Blocks.MySpaceProjector x, System.Boolean v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_5
L7:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
dup
ldnull
call static Sandbox.Game.Gui.MyTerminalAction`1<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlExtensions::EnableAction(Sandbox.Game.Gui.MyTerminalControlCheckbox`1<Entities.Blocks.MySpaceProjector> checkbox, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
pop
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_6
dup
brtrue.s L8
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_6(Entities.Blocks.MySpaceProjector b)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_6
L8:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "ShowOnlyBuildable"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::ShowOnlyBuildableBlockToggle
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::ShowOnlyBuildableTooltip
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldc.i4.0
ldc.i4.1
ldc.i4.1
ldc.r4 0.2
newobj System.Void Sandbox.Game.Gui.MyTerminalControlCheckbox`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Nullable`1<VRage.Utils.MyStringId> on, System.Nullable`1<VRage.Utils.MyStringId> off, System.Boolean justify, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled, System.Single maxWidth)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_7
dup
brtrue.s L9
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_7(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_7
L9:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_8
dup
brtrue.s L10
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_8(Entities.Blocks.MySpaceProjector x, System.Boolean v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_8
L10:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_9
dup
brtrue.s L11
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_9(Entities.Blocks.MySpaceProjector b)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_9
L11:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "X"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_ProjectionOffsetX
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldc.r4 -50
ldc.r4 50
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLimits(System.Single min, System.Single max)
dup
ldc.r4 0
newobj System.Void System.Nullable`1<System.Single>::.ctor(System.Single value)
callvirt System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::set_DefaultValue(System.Nullable`1<System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_20
dup
brtrue.s L22
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_20(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_20
L22:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_21
dup
brtrue.s L23
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_21(Entities.Blocks.MySpaceProjector x, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_21
L23:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_22
dup
brtrue.s L24
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_22(Entities.Blocks.MySpaceProjector x, System.Text.StringBuilder result)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_22
L24:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 0.01
ldnull
ldnull
call static System.Void Sandbox.Game.Gui.MyTerminalControlExtensions::EnableActions(Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector> slider, System.Single step, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> enabled, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_23
dup
brtrue.s L25
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_23(Entities.Blocks.MySpaceProjector x)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_23
L25:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "Y"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_ProjectionOffsetY
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldc.r4 -50
ldc.r4 50
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLimits(System.Single min, System.Single max)
dup
ldc.r4 0
newobj System.Void System.Nullable`1<System.Single>::.ctor(System.Single value)
callvirt System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::set_DefaultValue(System.Nullable`1<System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_24
dup
brtrue.s L26
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_24(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_24
L26:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_25
dup
brtrue.s L27
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_25(Entities.Blocks.MySpaceProjector x, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_25
L27:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_26
dup
brtrue.s L28
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_26(Entities.Blocks.MySpaceProjector x, System.Text.StringBuilder result)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_26
L28:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 0.01
ldnull
ldnull
call static System.Void Sandbox.Game.Gui.MyTerminalControlExtensions::EnableActions(Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector> slider, System.Single step, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> enabled, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_27
dup
brtrue.s L29
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_27(Entities.Blocks.MySpaceProjector x)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_27
L29:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "Z"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_ProjectionOffsetZ
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldc.r4 -50
ldc.r4 50
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLimits(System.Single min, System.Single max)
dup
ldc.r4 0
newobj System.Void System.Nullable`1<System.Single>::.ctor(System.Single value)
callvirt System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::set_DefaultValue(System.Nullable`1<System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_28
dup
brtrue.s L30
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_28(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_28
L30:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_29
dup
brtrue.s L31
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_29(Entities.Blocks.MySpaceProjector x, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_29
L31:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_30
dup
brtrue.s L32
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_30(Entities.Blocks.MySpaceProjector x, System.Text.StringBuilder result)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_30
L32:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 0.01
ldnull
ldnull
call static System.Void Sandbox.Game.Gui.MyTerminalControlExtensions::EnableActions(Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector> slider, System.Single step, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> enabled, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_31
dup
brtrue.s L33
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_31(Entities.Blocks.MySpaceProjector x)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_31
L33:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "RotX"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_ProjectionRotationX
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldc.r4 -180
ldc.r4 180
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLimits(System.Single min, System.Single max)
dup
ldc.r4 0
newobj System.Void System.Nullable`1<System.Single>::.ctor(System.Single value)
callvirt System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::set_DefaultValue(System.Nullable`1<System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_32
dup
brtrue.s L34
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_32(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_32
L34:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_33
dup
brtrue.s L35
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_33(Entities.Blocks.MySpaceProjector x, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_33
L35:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_34
dup
brtrue.s L36
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_34(Entities.Blocks.MySpaceProjector x, System.Text.StringBuilder result)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_34
L36:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 0.25
ldnull
ldnull
call static System.Void Sandbox.Game.Gui.MyTerminalControlExtensions::EnableActions(Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector> slider, System.Single step, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> enabled, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_35
dup
brtrue.s L37
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_35(Entities.Blocks.MySpaceProjector x)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_35
L37:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "RotY"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_ProjectionRotationY
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldc.r4 -180
ldc.r4 180
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLimits(System.Single min, System.Single max)
dup
ldc.r4 0
newobj System.Void System.Nullable`1<System.Single>::.ctor(System.Single value)
callvirt System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::set_DefaultValue(System.Nullable`1<System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_36
dup
brtrue.s L38
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_36(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_36
L38:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_37
dup
brtrue.s L39
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_37(Entities.Blocks.MySpaceProjector x, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_37
L39:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_38
dup
brtrue.s L40
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_38(Entities.Blocks.MySpaceProjector x, System.Text.StringBuilder result)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_38
L40:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 0.25
ldnull
ldnull
call static System.Void Sandbox.Game.Gui.MyTerminalControlExtensions::EnableActions(Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector> slider, System.Single step, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> enabled, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_39
dup
brtrue.s L41
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_39(Entities.Blocks.MySpaceProjector x)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_39
L41:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "RotZ"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_ProjectionRotationZ
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldc.r4 -180
ldc.r4 180
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLimits(System.Single min, System.Single max)
dup
ldc.r4 0
newobj System.Void System.Nullable`1<System.Single>::.ctor(System.Single value)
callvirt System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::set_DefaultValue(System.Nullable`1<System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_40
dup
brtrue.s L42
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_40(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_40
L42:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_41
dup
brtrue.s L43
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_41(Entities.Blocks.MySpaceProjector x, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_41
L43:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_42
dup
brtrue.s L44
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_42(Entities.Blocks.MySpaceProjector x, System.Text.StringBuilder result)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_42
L44:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 0.25
ldnull
ldnull
call static System.Void Sandbox.Game.Gui.MyTerminalControlExtensions::EnableActions(Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector> slider, System.Single step, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> enabled, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_43
dup
brtrue.s L45
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_43(Entities.Blocks.MySpaceProjector x)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_43
L45:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "Scale"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Scale
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldc.r4 0.02
ldc.r4 1
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLimits(System.Single min, System.Single max)
dup
ldc.r4 1
newobj System.Void System.Nullable`1<System.Single>::.ctor(System.Single value)
callvirt System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::set_DefaultValue(System.Nullable`1<System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_44
dup
brtrue.s L46
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_44(Entities.Blocks.MySpaceProjector x)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_44
L46:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_45
dup
brtrue.s L47
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_45(Entities.Blocks.MySpaceProjector x, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_45
L47:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_46
dup
brtrue.s L48
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_46(Entities.Blocks.MySpaceProjector x, System.Text.StringBuilder result)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_46
L48:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 0.01
ldnull
ldnull
call static System.Void Sandbox.Game.Gui.MyTerminalControlExtensions::EnableActions(Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector> slider, System.Single step, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> enabled, System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> callable)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_47
dup
brtrue.s L49
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_47(Entities.Blocks.MySpaceProjector x)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_47
L49:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSeparator`1<Entities.Blocks.MySpaceProjector>::.ctor()
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_48
dup
brtrue.s L50
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_48(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_48
L50:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Visible
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::TerminalScenarioSettingsLabel
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalControlLabel`1<Entities.Blocks.MySpaceProjector>::.ctor(VRage.Utils.MyStringId label)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_49
dup
brtrue.s L51
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_49(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_49
L51:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Visible
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "SpawnProjection"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_ProjectionSpawn
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::Blank
ldsfld System.Action`1<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_50
dup
brtrue.s L52
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_50(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Action`1<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Action`1<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_50
L52:
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlButton`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Action`1<Entities.Blocks.MySpaceProjector> action, System.Boolean isAutoscaleEnabled, System.Boolean isEllipsisEnabled)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_51
dup
brtrue.s L53
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_51(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_51
L53:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Visible
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_52
dup
brtrue.s L54
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_52(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_52
L54:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
dup
ldnull
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldnull
call static Sandbox.Game.Gui.MyTerminalAction`1<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlExtensions::EnableAction(Sandbox.Game.Gui.MyTerminalControlButton`1<Entities.Blocks.MySpaceProjector> button, System.String icon, System.Nullable`1<VRage.Utils.MyStringId> title, Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> writer)
pop
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "InstantBuilding"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Projector_InstantBuilding
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Projector_InstantBuilding_Tooltip
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldc.i4.0
ldc.i4.0
ldc.i4.0
ldc.r4 1
newobj System.Void Sandbox.Game.Gui.MyTerminalControlCheckbox`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Nullable`1<VRage.Utils.MyStringId> on, System.Nullable`1<VRage.Utils.MyStringId> off, System.Boolean justify, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled, System.Single maxWidth)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_53
dup
brtrue.s L55
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_53(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_53
L55:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Visible
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_54
dup
brtrue.s L56
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_54(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_54
L56:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_55
dup
brtrue.s L57
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_55(Entities.Blocks.MySpaceProjector p)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_55
L57:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_56
dup
brtrue.s L58
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_56(Entities.Blocks.MySpaceProjector p, System.Boolean v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_56
L58:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "GetOwnership"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Projector_GetOwnership
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertiesTooltip_Projector_GetOwnership
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldloca.s 0 (System.Nullable`1[VRage.Utils.MyStringId])
initobj System.Nullable`1[VRage.Utils.MyStringId]
ldloc.0
ldc.i4.0
ldc.i4.0
ldc.i4.0
ldc.r4 1
newobj System.Void Sandbox.Game.Gui.MyTerminalControlCheckbox`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Nullable`1<VRage.Utils.MyStringId> on, System.Nullable`1<VRage.Utils.MyStringId> off, System.Boolean justify, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled, System.Single maxWidth)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_57
dup
brtrue.s L59
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_57(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_57
L59:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Visible
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_58
dup
brtrue.s L60
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_58(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_58
L60:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_59
dup
brtrue.s L61
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_59(Entities.Blocks.MySpaceProjector p)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_59
L61:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_60
dup
brtrue.s L62
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_60(Entities.Blocks.MySpaceProjector p, System.Boolean v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_60
L62:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Boolean>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Boolean> value)
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "NumberOfProjections"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Projector_NumberOfProjections
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Projector_NumberOfProjections_Tooltip
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_61
dup
brtrue.s L63
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_61(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_61
L63:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Visible
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_62
dup
brtrue.s L64
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_62(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_62
L64:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_63
dup
brtrue.s L65
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_63(Entities.Blocks.MySpaceProjector p)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_63
L65:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_64
dup
brtrue.s L66
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_64(Entities.Blocks.MySpaceProjector p, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_64
L66:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_65
dup
brtrue.s L67
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_65(Entities.Blocks.MySpaceProjector p, System.Text.StringBuilder s)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_65
L67:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 1
ldc.r4 1000
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLogLimits(System.Single min, System.Single max)
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
ldstr "NumberOfBlocks"
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Projector_BlocksPerProjection
ldsfld VRage.Utils.MyStringId Sandbox.Game.Localization.MySpaceTexts::BlockPropertyTitle_Projector_BlocksPerProjection_Tooltip
ldc.i4.0
ldc.i4.0
newobj System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::.ctor(System.String id, VRage.Utils.MyStringId title, VRage.Utils.MyStringId tooltip, System.Boolean isAutoscaleEnabled, System.Boolean isAutoEllipsisEnabled)
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_66
dup
brtrue.s L68
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_66(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_66
L68:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Visible
dup
ldsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_67
dup
brtrue.s L69
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Boolean Entities.Blocks.<>c::<CreateTerminalControls>b__2_67(Entities.Blocks.MySpaceProjector p)
newobj System.Void System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Entities.Blocks.<>c::<>9__2_67
L69:
stfld System.Func`2<Entities.Blocks.MySpaceProjector, System.Boolean> Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector>::Enabled
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_68
dup
brtrue.s L70
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Single Entities.Blocks.<>c::<CreateTerminalControls>b__2_68(Entities.Blocks.MySpaceProjector p)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_68
L70:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Getter(Sandbox.Game.Screens.Terminal.Controls.GetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_69
dup
brtrue.s L71
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_69(Entities.Blocks.MySpaceProjector p, System.Single v)
newobj System.Void Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> Entities.Blocks.<>c::<>9__2_69
L71:
callvirt System.Void Sandbox.Game.Screens.Terminal.Controls.MyTerminalValueControl`2<Entities.Blocks.MySpaceProjector, System.Single>::set_Setter(Sandbox.Game.Screens.Terminal.Controls.SetterDelegate<Entities.Blocks.MySpaceProjector, System.Single> value)
dup
ldsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_70
dup
brtrue.s L72
pop
ldsfld Entities.Blocks.<>c Entities.Blocks.<>c::<>9
ldftn System.Void Entities.Blocks.<>c::<CreateTerminalControls>b__2_70(Entities.Blocks.MySpaceProjector p, System.Text.StringBuilder s)
newobj System.Void Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Entities.Blocks.<>c::<>9__2_70
L72:
stfld Sandbox.Game.Gui.WriterDelegate<Entities.Blocks.MySpaceProjector> Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::Writer
dup
ldc.r4 1
ldc.r4 10000
callvirt virtual System.Void Sandbox.Game.Gui.MyTerminalControlSlider`1<Entities.Blocks.MySpaceProjector>::SetLogLimits(System.Single min, System.Single max)
call static System.Void Sandbox.Game.Gui.MyTerminalControlFactory::AddControl(Sandbox.Game.Gui.MyTerminalControl`1<Entities.Blocks.MySpaceProjector> control)
call static System.Void MultigridProjectorClient.Patches.MySpaceProjector_CreateTerminalControls::CreateControls()
call static System.Void MultigridProjectorClient.Patches.MySpaceProjector_CreateTerminalControls::CreateActions()
ret
