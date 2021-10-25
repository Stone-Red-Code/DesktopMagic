<a name='assembly'></a>
# DesktopMagicPluginAPI

## Contents

- [Button](#T-DesktopMagicPluginAPI-Inputs-Button 'DesktopMagicPluginAPI.Inputs.Button')
  - [#ctor(value)](#M-DesktopMagicPluginAPI-Inputs-Button-#ctor-System-String- 'DesktopMagicPluginAPI.Inputs.Button.#ctor(System.String)')
  - [Value](#P-DesktopMagicPluginAPI-Inputs-Button-Value 'DesktopMagicPluginAPI.Inputs.Button.Value')
  - [Click()](#M-DesktopMagicPluginAPI-Inputs-Button-Click 'DesktopMagicPluginAPI.Inputs.Button.Click')
- [CheckBox](#T-DesktopMagicPluginAPI-Inputs-CheckBox 'DesktopMagicPluginAPI.Inputs.CheckBox')
  - [#ctor(value)](#M-DesktopMagicPluginAPI-Inputs-CheckBox-#ctor-System-Boolean- 'DesktopMagicPluginAPI.Inputs.CheckBox.#ctor(System.Boolean)')
  - [Value](#P-DesktopMagicPluginAPI-Inputs-CheckBox-Value 'DesktopMagicPluginAPI.Inputs.CheckBox.Value')
- [Element](#T-DesktopMagicPluginAPI-Inputs-Element 'DesktopMagicPluginAPI.Inputs.Element')
  - [ValueChanged()](#M-DesktopMagicPluginAPI-Inputs-Element-ValueChanged 'DesktopMagicPluginAPI.Inputs.Element.ValueChanged')
- [ElementAttribute](#T-DesktopMagicPluginAPI-Inputs-ElementAttribute 'DesktopMagicPluginAPI.Inputs.ElementAttribute')
  - [#ctor(name,orderIndex)](#M-DesktopMagicPluginAPI-Inputs-ElementAttribute-#ctor-System-String,System-Int32- 'DesktopMagicPluginAPI.Inputs.ElementAttribute.#ctor(System.String,System.Int32)')
  - [#ctor(orderIndex)](#M-DesktopMagicPluginAPI-Inputs-ElementAttribute-#ctor-System-Int32- 'DesktopMagicPluginAPI.Inputs.ElementAttribute.#ctor(System.Int32)')
  - [#ctor()](#M-DesktopMagicPluginAPI-Inputs-ElementAttribute-#ctor 'DesktopMagicPluginAPI.Inputs.ElementAttribute.#ctor')
  - [Name](#P-DesktopMagicPluginAPI-Inputs-ElementAttribute-Name 'DesktopMagicPluginAPI.Inputs.ElementAttribute.Name')
  - [OrderIndex](#P-DesktopMagicPluginAPI-Inputs-ElementAttribute-OrderIndex 'DesktopMagicPluginAPI.Inputs.ElementAttribute.OrderIndex')
- [GraphicsExtentions](#T-DesktopMagicPluginAPI-Drawing-GraphicsExtentions 'DesktopMagicPluginAPI.Drawing.GraphicsExtentions')
  - [DrawStringFixedWidth(graphics,s,font,brush,x,y,width)](#M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringFixedWidth-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Single,System-Single,System-Single- 'DesktopMagicPluginAPI.Drawing.GraphicsExtentions.DrawStringFixedWidth(System.Drawing.Graphics,System.String,System.Drawing.Font,System.Drawing.Brush,System.Single,System.Single,System.Single)')
  - [DrawStringFixedWidth(graphics,s,font,brush,point,width)](#M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringFixedWidth-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Drawing-PointF,System-Single- 'DesktopMagicPluginAPI.Drawing.GraphicsExtentions.DrawStringFixedWidth(System.Drawing.Graphics,System.String,System.Drawing.Font,System.Drawing.Brush,System.Drawing.PointF,System.Single)')
  - [DrawStringMonospace(graphics,s,font,brush,x,y)](#M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringMonospace-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Single,System-Single- 'DesktopMagicPluginAPI.Drawing.GraphicsExtentions.DrawStringMonospace(System.Drawing.Graphics,System.String,System.Drawing.Font,System.Drawing.Brush,System.Single,System.Single)')
  - [DrawStringMonospace(graphics,s,font,brush,point)](#M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringMonospace-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Drawing-PointF- 'DesktopMagicPluginAPI.Drawing.GraphicsExtentions.DrawStringMonospace(System.Drawing.Graphics,System.String,System.Drawing.Font,System.Drawing.Brush,System.Drawing.PointF)')
- [IPluginData](#T-DesktopMagicPluginAPI-IPluginData 'DesktopMagicPluginAPI.IPluginData')
  - [Color](#P-DesktopMagicPluginAPI-IPluginData-Color 'DesktopMagicPluginAPI.IPluginData.Color')
  - [Font](#P-DesktopMagicPluginAPI-IPluginData-Font 'DesktopMagicPluginAPI.IPluginData.Font')
  - [PluginName](#P-DesktopMagicPluginAPI-IPluginData-PluginName 'DesktopMagicPluginAPI.IPluginData.PluginName')
  - [PluginPath](#P-DesktopMagicPluginAPI-IPluginData-PluginPath 'DesktopMagicPluginAPI.IPluginData.PluginPath')
  - [Theme](#P-DesktopMagicPluginAPI-IPluginData-Theme 'DesktopMagicPluginAPI.IPluginData.Theme')
  - [WindowPosition](#P-DesktopMagicPluginAPI-IPluginData-WindowPosition 'DesktopMagicPluginAPI.IPluginData.WindowPosition')
  - [WindowSize](#P-DesktopMagicPluginAPI-IPluginData-WindowSize 'DesktopMagicPluginAPI.IPluginData.WindowSize')
  - [UpdateWindow()](#M-DesktopMagicPluginAPI-IPluginData-UpdateWindow 'DesktopMagicPluginAPI.IPluginData.UpdateWindow')
- [ITheme](#T-DesktopMagicPluginAPI-ITheme 'DesktopMagicPluginAPI.ITheme')
  - [BackgroundColor](#P-DesktopMagicPluginAPI-ITheme-BackgroundColor 'DesktopMagicPluginAPI.ITheme.BackgroundColor')
  - [Font](#P-DesktopMagicPluginAPI-ITheme-Font 'DesktopMagicPluginAPI.ITheme.Font')
  - [PrimaryColor](#P-DesktopMagicPluginAPI-ITheme-PrimaryColor 'DesktopMagicPluginAPI.ITheme.PrimaryColor')
  - [SecondaryColor](#P-DesktopMagicPluginAPI-ITheme-SecondaryColor 'DesktopMagicPluginAPI.ITheme.SecondaryColor')
- [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown')
  - [#ctor(min,max,value)](#M-DesktopMagicPluginAPI-Inputs-IntegerUpDown-#ctor-System-Int32,System-Int32,System-Int32- 'DesktopMagicPluginAPI.Inputs.IntegerUpDown.#ctor(System.Int32,System.Int32,System.Int32)')
  - [Maximum](#P-DesktopMagicPluginAPI-Inputs-IntegerUpDown-Maximum 'DesktopMagicPluginAPI.Inputs.IntegerUpDown.Maximum')
  - [Minimum](#P-DesktopMagicPluginAPI-Inputs-IntegerUpDown-Minimum 'DesktopMagicPluginAPI.Inputs.IntegerUpDown.Minimum')
  - [Value](#P-DesktopMagicPluginAPI-Inputs-IntegerUpDown-Value 'DesktopMagicPluginAPI.Inputs.IntegerUpDown.Value')
- [Label](#T-DesktopMagicPluginAPI-Inputs-Label 'DesktopMagicPluginAPI.Inputs.Label')
  - [#ctor(value,bold)](#M-DesktopMagicPluginAPI-Inputs-Label-#ctor-System-String,System-Boolean- 'DesktopMagicPluginAPI.Inputs.Label.#ctor(System.String,System.Boolean)')
  - [Bold](#P-DesktopMagicPluginAPI-Inputs-Label-Bold 'DesktopMagicPluginAPI.Inputs.Label.Bold')
  - [Value](#P-DesktopMagicPluginAPI-Inputs-Label-Value 'DesktopMagicPluginAPI.Inputs.Label.Value')
- [MouseButton](#T-DesktopMagicPluginAPI-Inputs-MouseButton 'DesktopMagicPluginAPI.Inputs.MouseButton')
  - [Left](#F-DesktopMagicPluginAPI-Inputs-MouseButton-Left 'DesktopMagicPluginAPI.Inputs.MouseButton.Left')
  - [Middle](#F-DesktopMagicPluginAPI-Inputs-MouseButton-Middle 'DesktopMagicPluginAPI.Inputs.MouseButton.Middle')
  - [Right](#F-DesktopMagicPluginAPI-Inputs-MouseButton-Right 'DesktopMagicPluginAPI.Inputs.MouseButton.Right')
- [Plugin](#T-DesktopMagicPluginAPI-Plugin 'DesktopMagicPluginAPI.Plugin')
  - [Application](#P-DesktopMagicPluginAPI-Plugin-Application 'DesktopMagicPluginAPI.Plugin.Application')
  - [RenderQuality](#P-DesktopMagicPluginAPI-Plugin-RenderQuality 'DesktopMagicPluginAPI.Plugin.RenderQuality')
  - [UpdateInterval](#P-DesktopMagicPluginAPI-Plugin-UpdateInterval 'DesktopMagicPluginAPI.Plugin.UpdateInterval')
  - [Main()](#M-DesktopMagicPluginAPI-Plugin-Main 'DesktopMagicPluginAPI.Plugin.Main')
  - [OnMouseClick(position,mouseButton)](#M-DesktopMagicPluginAPI-Plugin-OnMouseClick-System-Drawing-Point,DesktopMagicPluginAPI-Inputs-MouseButton- 'DesktopMagicPluginAPI.Plugin.OnMouseClick(System.Drawing.Point,DesktopMagicPluginAPI.Inputs.MouseButton)')
  - [OnMouseMove(position)](#M-DesktopMagicPluginAPI-Plugin-OnMouseMove-System-Drawing-Point- 'DesktopMagicPluginAPI.Plugin.OnMouseMove(System.Drawing.Point)')
  - [OnMouseWheel(position,delta)](#M-DesktopMagicPluginAPI-Plugin-OnMouseWheel-System-Drawing-Point,System-Int32- 'DesktopMagicPluginAPI.Plugin.OnMouseWheel(System.Drawing.Point,System.Int32)')
  - [Start()](#M-DesktopMagicPluginAPI-Plugin-Start 'DesktopMagicPluginAPI.Plugin.Start')
- [RenderQuality](#T-DesktopMagicPluginAPI-Drawing-RenderQuality 'DesktopMagicPluginAPI.Drawing.RenderQuality')
  - [High](#F-DesktopMagicPluginAPI-Drawing-RenderQuality-High 'DesktopMagicPluginAPI.Drawing.RenderQuality.High')
  - [Low](#F-DesktopMagicPluginAPI-Drawing-RenderQuality-Low 'DesktopMagicPluginAPI.Drawing.RenderQuality.Low')
  - [Performance](#F-DesktopMagicPluginAPI-Drawing-RenderQuality-Performance 'DesktopMagicPluginAPI.Drawing.RenderQuality.Performance')
- [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider')
  - [#ctor(min,max,value)](#M-DesktopMagicPluginAPI-Inputs-Slider-#ctor-System-Double,System-Double,System-Double- 'DesktopMagicPluginAPI.Inputs.Slider.#ctor(System.Double,System.Double,System.Double)')
  - [Maximum](#P-DesktopMagicPluginAPI-Inputs-Slider-Maximum 'DesktopMagicPluginAPI.Inputs.Slider.Maximum')
  - [Minimum](#P-DesktopMagicPluginAPI-Inputs-Slider-Minimum 'DesktopMagicPluginAPI.Inputs.Slider.Minimum')
  - [Value](#P-DesktopMagicPluginAPI-Inputs-Slider-Value 'DesktopMagicPluginAPI.Inputs.Slider.Value')
- [TextBox](#T-DesktopMagicPluginAPI-Inputs-TextBox 'DesktopMagicPluginAPI.Inputs.TextBox')
  - [#ctor(value)](#M-DesktopMagicPluginAPI-Inputs-TextBox-#ctor-System-String- 'DesktopMagicPluginAPI.Inputs.TextBox.#ctor(System.String)')
  - [Value](#P-DesktopMagicPluginAPI-Inputs-TextBox-Value 'DesktopMagicPluginAPI.Inputs.TextBox.Value')

<a name='T-DesktopMagicPluginAPI-Inputs-Button'></a>
## Button `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Represents a button control.

<a name='M-DesktopMagicPluginAPI-Inputs-Button-#ctor-System-String-'></a>
### #ctor(value) `constructor`

##### Summary

Initializes a new instance of the [Button](#T-DesktopMagicPluginAPI-Inputs-Button 'DesktopMagicPluginAPI.Inputs.Button') class with the provided `value`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The text caption displayed in the [Button](#T-DesktopMagicPluginAPI-Inputs-Button 'DesktopMagicPluginAPI.Inputs.Button') control. |

<a name='P-DesktopMagicPluginAPI-Inputs-Button-Value'></a>
### Value `property`

##### Summary

Gets or sets the text caption displayed in the [Button](#T-DesktopMagicPluginAPI-Inputs-Button 'DesktopMagicPluginAPI.Inputs.Button') element.

<a name='M-DesktopMagicPluginAPI-Inputs-Button-Click'></a>
### Click() `method`

##### Summary

Triggers the [](#E-DesktopMagicPluginAPI-Inputs-Button-OnClick 'DesktopMagicPluginAPI.Inputs.Button.OnClick') event.

##### Parameters

This method has no parameters.

<a name='T-DesktopMagicPluginAPI-Inputs-CheckBox'></a>
## CheckBox `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Represents a check box control.

<a name='M-DesktopMagicPluginAPI-Inputs-CheckBox-#ctor-System-Boolean-'></a>
### #ctor(value) `constructor`

##### Summary

Initializes a new instance of the [CheckBox](#T-DesktopMagicPluginAPI-Inputs-CheckBox 'DesktopMagicPluginAPI.Inputs.CheckBox') class with the provided `value`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | A value indicating whether the [CheckBox](#T-DesktopMagicPluginAPI-Inputs-CheckBox 'DesktopMagicPluginAPI.Inputs.CheckBox') is in the checked state. |

<a name='P-DesktopMagicPluginAPI-Inputs-CheckBox-Value'></a>
### Value `property`

##### Summary

Gets or set a value indicating whether the [CheckBox](#T-DesktopMagicPluginAPI-Inputs-CheckBox 'DesktopMagicPluginAPI.Inputs.CheckBox') is in the checked state.

<a name='T-DesktopMagicPluginAPI-Inputs-Element'></a>
## Element `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

The element base class.

<a name='M-DesktopMagicPluginAPI-Inputs-Element-ValueChanged'></a>
### ValueChanged() `method`

##### Summary

Triggers the [](#E-DesktopMagicPluginAPI-Inputs-Element-OnValueChanged 'DesktopMagicPluginAPI.Inputs.Element.OnValueChanged') event.

##### Parameters

This method has no parameters.

<a name='T-DesktopMagicPluginAPI-Inputs-ElementAttribute'></a>
## ElementAttribute `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Marks a Property as element.

<a name='M-DesktopMagicPluginAPI-Inputs-ElementAttribute-#ctor-System-String,System-Int32-'></a>
### #ctor(name,orderIndex) `constructor`

##### Summary

Marks a Property as element with the provided `name` and `orderIndex`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| name | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The name of the element. |
| orderIndex | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The order index of the element. |

<a name='M-DesktopMagicPluginAPI-Inputs-ElementAttribute-#ctor-System-Int32-'></a>
### #ctor(orderIndex) `constructor`

##### Summary

Marks a Property as element with the provided `orderIndex`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| orderIndex | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The order index of the element. |

<a name='M-DesktopMagicPluginAPI-Inputs-ElementAttribute-#ctor'></a>
### #ctor() `constructor`

##### Summary

*Inherit from parent.*

##### Parameters

This constructor has no parameters.

<a name='P-DesktopMagicPluginAPI-Inputs-ElementAttribute-Name'></a>
### Name `property`

##### Summary

The name of the element.

<a name='P-DesktopMagicPluginAPI-Inputs-ElementAttribute-OrderIndex'></a>
### OrderIndex `property`

##### Summary

The order index of the element.

<a name='T-DesktopMagicPluginAPI-Drawing-GraphicsExtentions'></a>
## GraphicsExtentions `type`

##### Namespace

DesktopMagicPluginAPI.Drawing

##### Summary

Extentions for the [Graphics](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Graphics 'System.Drawing.Graphics') class.

<a name='M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringFixedWidth-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Single,System-Single,System-Single-'></a>
### DrawStringFixedWidth(graphics,s,font,brush,x,y,width) `method`

##### Summary

Draws the specified text string at the specified location with the specified [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') and [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') objects.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| graphics | [System.Drawing.Graphics](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Graphics 'System.Drawing.Graphics') | Graphics object. |
| s | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | String to draw. |
| font | [System.Drawing.Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') | [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') that defines the text format of the string. |
| brush | [System.Drawing.Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') | [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') that determines the color and texture of the drawn text. |
| x | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The x-coordinate of the upper-left corner of the drawn text. |
| y | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The y-coordinate of the upper-left corner of the drawn text. |
| width | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The specified width. |

<a name='M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringFixedWidth-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Drawing-PointF,System-Single-'></a>
### DrawStringFixedWidth(graphics,s,font,brush,point,width) `method`

##### Summary

Draws the specified text string at the specified location with the specified [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') and [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') objects.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| graphics | [System.Drawing.Graphics](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Graphics 'System.Drawing.Graphics') | Graphics object. |
| s | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | String to draw. |
| font | [System.Drawing.Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') | [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') that defines the text format of the string. |
| brush | [System.Drawing.Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') | [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') that determines the color and texture of the drawn text. |
| point | [System.Drawing.PointF](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.PointF 'System.Drawing.PointF') | [PointF](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.PointF 'System.Drawing.PointF') structure that specifies the upper-left corner of the drawn text. |
| width | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The specified width. |

<a name='M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringMonospace-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Single,System-Single-'></a>
### DrawStringMonospace(graphics,s,font,brush,x,y) `method`

##### Summary

Draws the specified text string at the specified location with the specified [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') and [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') objects.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| graphics | [System.Drawing.Graphics](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Graphics 'System.Drawing.Graphics') | Graphics object. |
| s | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | String to draw. |
| font | [System.Drawing.Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') | [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') that defines the text format of the string. |
| brush | [System.Drawing.Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') | [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') that determines the color and texture of the drawn text. |
| x | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The x-coordinate of the upper-left corner of the drawn text. |
| y | [System.Single](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Single 'System.Single') | The y-coordinate of the upper-left corner of the drawn text. |

<a name='M-DesktopMagicPluginAPI-Drawing-GraphicsExtentions-DrawStringMonospace-System-Drawing-Graphics,System-String,System-Drawing-Font,System-Drawing-Brush,System-Drawing-PointF-'></a>
### DrawStringMonospace(graphics,s,font,brush,point) `method`

##### Summary

Draws the specified text string at the specified location with the specified [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') and [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') objects.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| graphics | [System.Drawing.Graphics](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Graphics 'System.Drawing.Graphics') | Graphics object. |
| s | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | String to draw. |
| font | [System.Drawing.Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') | [Font](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Font 'System.Drawing.Font') that defines the text format of the string. |
| brush | [System.Drawing.Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') | [Brush](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Brush 'System.Drawing.Brush') that determines the color and texture of the drawn text. |
| point | [System.Drawing.PointF](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.PointF 'System.Drawing.PointF') | [PointF](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.PointF 'System.Drawing.PointF') structure that specifies the upper-left corner of the drawn text. |

<a name='T-DesktopMagicPluginAPI-IPluginData'></a>
## IPluginData `type`

##### Namespace

DesktopMagicPluginAPI

##### Summary

Defines properties and methods that provide information about the main application.

<a name='P-DesktopMagicPluginAPI-IPluginData-Color'></a>
### Color `property`

##### Summary

Gets the current color of the main application.

<a name='P-DesktopMagicPluginAPI-IPluginData-Font'></a>
### Font `property`

##### Summary

Gets the current font of the main application.

<a name='P-DesktopMagicPluginAPI-IPluginData-PluginName'></a>
### PluginName `property`

##### Summary

Gets the name of the plugin.

<a name='P-DesktopMagicPluginAPI-IPluginData-PluginPath'></a>
### PluginPath `property`

##### Summary

Gets the path of the parent directory of the plugin.

<a name='P-DesktopMagicPluginAPI-IPluginData-Theme'></a>
### Theme `property`

##### Summary

Gets the current theme setting of the main application.

<a name='P-DesktopMagicPluginAPI-IPluginData-WindowPosition'></a>
### WindowPosition `property`

##### Summary

Gets the window position of the plugin window.

<a name='P-DesktopMagicPluginAPI-IPluginData-WindowSize'></a>
### WindowSize `property`

##### Summary

Gets the window size of the plugin window.

<a name='M-DesktopMagicPluginAPI-IPluginData-UpdateWindow'></a>
### UpdateWindow() `method`

##### Summary

Updates the plugin window.

##### Parameters

This method has no parameters.

<a name='T-DesktopMagicPluginAPI-ITheme'></a>
## ITheme `type`

##### Namespace

DesktopMagicPluginAPI

##### Summary

The theme settings of the main application.

<a name='P-DesktopMagicPluginAPI-ITheme-BackgroundColor'></a>
### BackgroundColor `property`

##### Summary

Gets the background color of the main application.

<a name='P-DesktopMagicPluginAPI-ITheme-Font'></a>
### Font `property`

##### Summary

Gets the font of the main application.

<a name='P-DesktopMagicPluginAPI-ITheme-PrimaryColor'></a>
### PrimaryColor `property`

##### Summary

Gets the primary color of the main application.

<a name='P-DesktopMagicPluginAPI-ITheme-SecondaryColor'></a>
### SecondaryColor `property`

##### Summary

Gets the secondary color of the main application.

<a name='T-DesktopMagicPluginAPI-Inputs-IntegerUpDown'></a>
## IntegerUpDown `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Represents a up-down control.

<a name='M-DesktopMagicPluginAPI-Inputs-IntegerUpDown-#ctor-System-Int32,System-Int32,System-Int32-'></a>
### #ctor(min,max,value) `constructor`

##### Summary

Initializes a new instance of the [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown') class with the provided `min` value, `max` value and `value`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| min | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The maximum value for the [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown') element. |
| max | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The minimum value for the [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown') element. |
| value | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | The value assigned to the [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown') element. |

##### Exceptions

| Name | Description |
| ---- | ----------- |
| [System.ArgumentException](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.ArgumentException 'System.ArgumentException') |  |

<a name='P-DesktopMagicPluginAPI-Inputs-IntegerUpDown-Maximum'></a>
### Maximum `property`

##### Summary

Gets or sets the maximum value for the [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown') element.

<a name='P-DesktopMagicPluginAPI-Inputs-IntegerUpDown-Minimum'></a>
### Minimum `property`

##### Summary

Gets or sets the minimum value for the [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown') element.

<a name='P-DesktopMagicPluginAPI-Inputs-IntegerUpDown-Value'></a>
### Value `property`

##### Summary

Gets or sets the value assigned to the [IntegerUpDown](#T-DesktopMagicPluginAPI-Inputs-IntegerUpDown 'DesktopMagicPluginAPI.Inputs.IntegerUpDown') element.

<a name='T-DesktopMagicPluginAPI-Inputs-Label'></a>
## Label `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Represents a label control.

<a name='M-DesktopMagicPluginAPI-Inputs-Label-#ctor-System-String,System-Boolean-'></a>
### #ctor(value,bold) `constructor`

##### Summary

Initializes a new instance of the [Label](#T-DesktopMagicPluginAPI-Inputs-Label 'DesktopMagicPluginAPI.Inputs.Label') class with the provided `value`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The text associated with this [Label](#T-DesktopMagicPluginAPI-Inputs-Label 'DesktopMagicPluginAPI.Inputs.Label') |
| bold | [System.Boolean](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Boolean 'System.Boolean') | A value indicating whether the content of the [Label](#T-DesktopMagicPluginAPI-Inputs-Label 'DesktopMagicPluginAPI.Inputs.Label') is bold or not. |

<a name='P-DesktopMagicPluginAPI-Inputs-Label-Bold'></a>
### Bold `property`

##### Summary

Gets or set a value indicating whether the content of the [Label](#T-DesktopMagicPluginAPI-Inputs-Label 'DesktopMagicPluginAPI.Inputs.Label') is bold or not.

<a name='P-DesktopMagicPluginAPI-Inputs-Label-Value'></a>
### Value `property`

##### Summary

Gets or sets the text associated with this [Label](#T-DesktopMagicPluginAPI-Inputs-Label 'DesktopMagicPluginAPI.Inputs.Label').

<a name='T-DesktopMagicPluginAPI-Inputs-MouseButton'></a>
## MouseButton `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Mouse Buttons

<a name='F-DesktopMagicPluginAPI-Inputs-MouseButton-Left'></a>
### Left `constants`

##### Summary

The left mouse button.

<a name='F-DesktopMagicPluginAPI-Inputs-MouseButton-Middle'></a>
### Middle `constants`

##### Summary

The middle mouse button.

<a name='F-DesktopMagicPluginAPI-Inputs-MouseButton-Right'></a>
### Right `constants`

##### Summary

The right mouse button.

<a name='T-DesktopMagicPluginAPI-Plugin'></a>
## Plugin `type`

##### Namespace

DesktopMagicPluginAPI

##### Summary

The plugin class.

<a name='P-DesktopMagicPluginAPI-Plugin-Application'></a>
### Application `property`

##### Summary

Informations about the main application.

<a name='P-DesktopMagicPluginAPI-Plugin-RenderQuality'></a>
### RenderQuality `property`

##### Summary

Gets or sets the render quality of the bitmap image.

<a name='P-DesktopMagicPluginAPI-Plugin-UpdateInterval'></a>
### UpdateInterval `property`

##### Summary

Gets or sets the interval, expressed in milliseconds, at which to call the [Main](#M-DesktopMagicPluginAPI-Plugin-Main 'DesktopMagicPluginAPI.Plugin.Main') method.

<a name='M-DesktopMagicPluginAPI-Plugin-Main'></a>
### Main() `method`

##### Summary

Occurs when the [UpdateInterval](#P-DesktopMagicPluginAPI-Plugin-UpdateInterval 'DesktopMagicPluginAPI.Plugin.UpdateInterval') elapses.

##### Returns



##### Parameters

This method has no parameters.

<a name='M-DesktopMagicPluginAPI-Plugin-OnMouseClick-System-Drawing-Point,DesktopMagicPluginAPI-Inputs-MouseButton-'></a>
### OnMouseClick(position,mouseButton) `method`

##### Summary

Occurs when the window is clicked by the mouse.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| position | [System.Drawing.Point](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Point 'System.Drawing.Point') | The x- and y-coordinates of the mouse pointer position relative to the plugin window. |
| mouseButton | [DesktopMagicPluginAPI.Inputs.MouseButton](#T-DesktopMagicPluginAPI-Inputs-MouseButton 'DesktopMagicPluginAPI.Inputs.MouseButton') | The button associated with the event. |

<a name='M-DesktopMagicPluginAPI-Plugin-OnMouseMove-System-Drawing-Point-'></a>
### OnMouseMove(position) `method`

##### Summary

Occurs when the mouse pointer is moved over the control.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| position | [System.Drawing.Point](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Point 'System.Drawing.Point') | The x- and y-coordinates of the mouse pointer position relative to the plugin window. |

<a name='M-DesktopMagicPluginAPI-Plugin-OnMouseWheel-System-Drawing-Point,System-Int32-'></a>
### OnMouseWheel(position,delta) `method`

##### Summary

Occurs when the user rotates the mouse wheel while the mouse pointer is over this element.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| position | [System.Drawing.Point](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Drawing.Point 'System.Drawing.Point') | The x- and y-coordinates of the mouse pointer position relative to the plugin window. |
| delta | [System.Int32](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Int32 'System.Int32') | A value that indicates the amount that the mouse wheel has changed. |

<a name='M-DesktopMagicPluginAPI-Plugin-Start'></a>
### Start() `method`

##### Summary

Occurs once when the plugin gets activated.

##### Parameters

This method has no parameters.

<a name='T-DesktopMagicPluginAPI-Drawing-RenderQuality'></a>
## RenderQuality `type`

##### Namespace

DesktopMagicPluginAPI.Drawing

##### Summary

Specifies which render quality is used to display the bitmap images.

<a name='F-DesktopMagicPluginAPI-Drawing-RenderQuality-High'></a>
### High `constants`

##### Summary

Slower then [Low](#F-DesktopMagicPluginAPI-Drawing-RenderQuality-Low 'DesktopMagicPluginAPI.Drawing.RenderQuality.Low') but produces higher quality output.

<a name='F-DesktopMagicPluginAPI-Drawing-RenderQuality-Low'></a>
### Low `constants`

##### Summary

Faster then [High](#F-DesktopMagicPluginAPI-Drawing-RenderQuality-High 'DesktopMagicPluginAPI.Drawing.RenderQuality.High') but produces lower quality output.

<a name='F-DesktopMagicPluginAPI-Drawing-RenderQuality-Performance'></a>
### Performance `constants`

##### Summary

Provides performance benefits over [Low](#F-DesktopMagicPluginAPI-Drawing-RenderQuality-Low 'DesktopMagicPluginAPI.Drawing.RenderQuality.Low')

<a name='T-DesktopMagicPluginAPI-Inputs-Slider'></a>
## Slider `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Represents a slider control.

<a name='M-DesktopMagicPluginAPI-Inputs-Slider-#ctor-System-Double,System-Double,System-Double-'></a>
### #ctor(min,max,value) `constructor`

##### Summary

Initializes a new instance of the [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider') class with the provided `min` value, `max` value and `value`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| min | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The maximum value for the [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider') element. |
| max | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The minimum value for the [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider') element. |
| value | [System.Double](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.Double 'System.Double') | The value assigned to the [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider') element. |

<a name='P-DesktopMagicPluginAPI-Inputs-Slider-Maximum'></a>
### Maximum `property`

##### Summary

Gets or sets the maximum value for the [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider') element.

<a name='P-DesktopMagicPluginAPI-Inputs-Slider-Minimum'></a>
### Minimum `property`

##### Summary

Gets or sets the minimum value for the [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider') element.

<a name='P-DesktopMagicPluginAPI-Inputs-Slider-Value'></a>
### Value `property`

##### Summary

Gets or sets the value assigned to the [Slider](#T-DesktopMagicPluginAPI-Inputs-Slider 'DesktopMagicPluginAPI.Inputs.Slider') element.

<a name='T-DesktopMagicPluginAPI-Inputs-TextBox'></a>
## TextBox `type`

##### Namespace

DesktopMagicPluginAPI.Inputs

##### Summary

Represents a text box control.

<a name='M-DesktopMagicPluginAPI-Inputs-TextBox-#ctor-System-String-'></a>
### #ctor(value) `constructor`

##### Summary

Initializes a new instance of the [TextBox](#T-DesktopMagicPluginAPI-Inputs-TextBox 'DesktopMagicPluginAPI.Inputs.TextBox') class with the provided `value`.

##### Parameters

| Name | Type | Description |
| ---- | ---- | ----------- |
| value | [System.String](http://msdn.microsoft.com/query/dev14.query?appId=Dev14IDEF1&l=EN-US&k=k:System.String 'System.String') | The text associated with this control. |

<a name='P-DesktopMagicPluginAPI-Inputs-TextBox-Value'></a>
### Value `property`

##### Summary

Gets or sets the text associated with this [TextBox](#T-DesktopMagicPluginAPI-Inputs-TextBox 'DesktopMagicPluginAPI.Inputs.TextBox').
