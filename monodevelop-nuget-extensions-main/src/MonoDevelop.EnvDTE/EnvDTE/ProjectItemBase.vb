﻿' Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
' 
' Permission is hereby granted, free of charge, to any person obtaining a copy of this
' software and associated documentation files (the "Software"), to deal in the Software
' without restriction, including without limitation the rights to use, copy, modify, merge,
' publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
' to whom the Software is furnished to do so, subject to the following conditions:
' 
' The above copyright notice and this permission notice shall be included in all copies or
' substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
' INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
' PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
' FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
' OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.

Imports System

Namespace MonoDevelop.EnvDTE
	Public MustInherit Class ProjectItemBase
		Inherits MarshalByRefObject

		ReadOnly Property FileNames(index As Short) As String
			Get
				Return GetFileNames(index)
			End Get
		End Property

		Protected MustOverride Function GetFileNames(index As Short) As String

		ReadOnly Property IsOpen(Optional ViewKind As String = "{FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF}") As Boolean
			Get
				Return GetIsOpen(ViewKind)
			End Get
		End Property

		Protected MustOverride Function GetIsOpen(ViewKind As String) As Boolean

		ReadOnly Property Extender(ExtenderName As String) As Object
			Get
				Return GetExtender(ExtenderName)
			End Get
		End Property

		Protected MustOverride Function GetExtender(Extender As String) As Object
	End Class
End Namespace