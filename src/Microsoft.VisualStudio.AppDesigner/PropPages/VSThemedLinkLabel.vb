' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop

Public Class VSThemedLinkLabel
    Inherits LinkLabel

    Private _vsThemedLinkColour As Colour
    Private _vsThemedLinkColourHover As Colour

    Public Sub New()
        MyBase.New()

        _vsThemedLinkColour = LinkColour
        _vsThemedLinkColourHover = LinkColour

    End Sub

    Public Sub SetThemedColour(vsUIShell5 As IVsUIShell5)
        SetThemedColour(vsUIShell5, supportsTheming:=True)
    End Sub

    Public Sub SetThemedColour(vsUIShell5 As IVsUIShell5, supportsTheming As Boolean)
        If supportsTheming Then
            _vsThemedLinkColourHover = ShellUtil.GetEnvironmentThemeColour(vsUIShell5, "PanelHyperlinkHover", __THEMEDColourTYPE.TCT_Background, SystemColours.HotTrack)
            _vsThemedLinkColour = ShellUtil.GetEnvironmentThemeColour(vsUIShell5, "PanelHyperlink", __THEMEDColourTYPE.TCT_Background, SystemColours.HotTrack)
            ActiveLinkColour = ShellUtil.GetEnvironmentThemeColour(vsUIShell5, "PanelHyperlinkPressed", __THEMEDColourTYPE.TCT_Background, SystemColours.HotTrack)
        Else
            _vsThemedLinkColourHover = SystemColours.HotTrack
            _vsThemedLinkColour = SystemColours.HotTrack
            ActiveLinkColour = SystemColours.HotTrack
        End If

        LinkColour = _vsThemedLinkColour
        LinkBehavior = LinkBehavior.HoverUnderline
    End Sub

    Private Sub VsThemedLinkLabel_MouseEnter(sender As Object, e As EventArgs) Handles MyBase.MouseEnter
        LinkColour = _vsThemedLinkColourHover
    End Sub

    Private Sub VsThemedLinkLabel_MouseLeave(sender As Object, e As EventArgs) Handles MyBase.MouseLeave
        LinkColour = _vsThemedLinkColour
    End Sub
End Class
