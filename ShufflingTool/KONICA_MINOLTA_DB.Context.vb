﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated from a template.
'
'     Manual changes to this file may cause unexpected behavior in your application.
'     Manual changes to this file will be overwritten if the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Imports System
Imports System.Data.Entity
Imports System.Data.Entity.Infrastructure

Partial Public Class KONICA_MINOLTA_DBEntities
    Inherits DbContext

    Public Sub New()
        MyBase.New("name=KONICA_MINOLTA_DBEntities")
    End Sub

    Protected Overrides Sub OnModelCreating(modelBuilder As DbModelBuilder)
        Throw New UnintentionalCodeFirstException()
    End Sub

    Public Overridable Property DEMANDs() As DbSet(Of DEMAND)
    Public Overridable Property INVENTORies() As DbSet(Of INVENTORY)
    Public Overridable Property LOCATIONs() As DbSet(Of LOCATION)
    Public Overridable Property ORDER_DETAILS() As DbSet(Of ORDER_DETAILS)
    Public Overridable Property SKUs() As DbSet(Of SKU)
    Public Overridable Property LAST_DEMAND_DATA() As DbSet(Of LAST_DEMAND_DATA)

End Class
