'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated from a template.
'
'     Manual changes to this file may cause unexpected behavior in your application.
'     Manual changes to this file will be overwritten if the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Imports System
Imports System.Collections.Generic

Partial Public Class INVENTORY
    Public Property ID As Integer
    Public Property LOCATION_LINK_ID As Nullable(Of Integer)
    Public Property SKU_LINK_ID As Nullable(Of Integer)
    Public Property QTY As Nullable(Of Integer)

    Public Overridable Property LOCATION As LOCATION
    Public Overridable Property SKU As SKU

End Class
