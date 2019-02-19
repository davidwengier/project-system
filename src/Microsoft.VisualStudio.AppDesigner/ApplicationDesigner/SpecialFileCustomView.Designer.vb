Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    Partial Public Class SpecialFileCustomView
        Inherits System.Windows.Forms.UserControl

        <System.Diagnostics.DebuggerNonUserCode()>
        Public Sub New()
            MyBase.New()

            ' This call is required by the Component Designer.
            InitialiseComponent()

        End Sub

        'Control overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()>
        Protected Overloads Overrides Sub Dispose(disposing As Boolean)
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Control Designer
        Private components As System.ComponentModel.IContainer

        Public WithEvents LinkLabel As VSThemedLinkLabel

        ' NOTE: The following procedure is required by the Component Designer
        ' It can be modified using the Component Designer.  Do not modify it
        ' using the code editor.
        <System.Diagnostics.DebuggerStepThrough()>
        Private Sub InitialiseComponent()
            Dim CentrePanel As System.Windows.Forms.TableLayoutPanel
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(SpecialFileCustomView))
            Me.LinkLabel = New VSThemedLinkLabel()
            CentrePanel = New System.Windows.Forms.TableLayoutPanel()
            CentrePanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'CentrePanel
            '
            resources.ApplyResources(CentrePanel, "CentrePanel")
            CentrePanel.Controls.Add(Me.LinkLabel, 1, 0)
            CentrePanel.Name = "CentrePanel"
            '
            'LinkLabel
            '
            resources.ApplyResources(Me.LinkLabel, "LinkLabel")
            Me.LinkLabel.Name = "LinkLabel"
            '
            'SpecialFileCustomView
            '
            Me.Controls.Add(CentrePanel)
            Me.Name = "SpecialFileCustomView"
            resources.ApplyResources(Me, "$this")
            CentrePanel.ResumeLayout(False)
            CentrePanel.PerformLayout()
            Me.ResumeLayout(False)

        End Sub

    End Class

End Namespace
