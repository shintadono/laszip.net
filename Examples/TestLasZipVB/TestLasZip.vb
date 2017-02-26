Imports System.IO

Module TestLasZip

	Private Structure Point3D
		Dim X As Double
		Dim Y As Double
		Dim Z As Double
	End Structure

	Sub Main()
		WriteLaz() ' Write a file
		ReadLaz()  ' Read it back
	End Sub

	Private FileName As String = Path.GetTempPath + "Test.laz"

	Private Sub ReadLaz()
		Dim LazCls As New LASzip.Net.laszip
		Dim IsCompressed As Boolean = True
		LazCls.open_reader(FileName, IsCompressed)
		Dim NumPts As Int32 = LazCls.curHeader.number_of_point_records
		' Check some header values
		Debug.Print(LazCls.curHeader.min_x)
		Debug.Print(LazCls.curHeader.min_y)
		Debug.Print(LazCls.curHeader.min_z)
		Debug.Print(LazCls.curHeader.max_x)
		Debug.Print(LazCls.curHeader.max_y)
		Debug.Print(LazCls.curHeader.max_z)
		'
		Dim ClaVal As Integer
		Dim PntObj As New Point3D
		Dim CrdArr As Double() = New Double(2) {}
		' Loop through number of points indicated
		For PntInd As Integer = 0 To NumPts - 1
			' Read the point
			LazCls.read_point()
			' Get precision coordinates
			LazCls.get_coordinates(CrdArr)
			PntObj.X = CrdArr(0)
			PntObj.Y = CrdArr(1)
			PntObj.Z = CrdArr(2)
			' Get classification value
			ClaVal = LazCls.curPoint.classification
		Next
		' Close the reader
		LazCls.close_reader()
	End Sub

	Private Sub WriteLaz()
		' --- Write Example
		Dim PntObj As New Point3D
		Dim PntLst As New List(Of Point3D)
		PntObj.X = 1000.0 : PntObj.Y = 2000.0 : PntObj.Z = 100.0 : PntLst.Add(PntObj)
		PntObj.X = 5000.0 : PntObj.Y = 6000.0 : PntObj.Z = 200.0 : PntLst.Add(PntObj)
		'
		Dim LazCls As New LASzip.Net.laszip
		Dim LazErr As Integer = LazCls.clean
		If LazErr = 0 Then
			' Number of point records needs to be set
			LazCls.curHeader.number_of_point_records = PntLst.Count
			' Header Min/Max needs to be set to extents of points
			LazCls.curHeader.min_x = PntLst(0).X ' LL Point
			LazCls.curHeader.min_y = PntLst(0).Y
			LazCls.curHeader.min_z = PntLst(0).Z
			LazCls.curHeader.max_x = PntLst(1).X ' UR Point
			LazCls.curHeader.max_y = PntLst(1).Y
			LazCls.curHeader.max_z = PntLst(1).Z
			' Open the writer and test for errors
			LazErr = LazCls.open_writer(FileName, True)
			If LazErr = 0 Then
				Dim CrdArr As Double() = New Double(2) {}
				For Each PntLoc As Point3D In PntLst
					CrdArr(0) = PntLoc.X
					CrdArr(1) = PntLoc.Y
					CrdArr(2) = PntLoc.Z
					' Set the coordinates in the LazCls object
					LazCls.set_coordinates(CrdArr)
					' Set the classification to ground
					LazCls.curPoint.classification = 2
					' Write the point to the file
					LazErr = LazCls.write_point()
					If LazErr <> 0 Then
						Exit For
					End If
				Next
				' Close the writer to release the file (OS lock)
				LazErr = LazCls.close_writer()
				LazCls = Nothing
			End If
		End If
		If LazErr <> 0 Then
			' Show last error that occurred
			Debug.Print(LazCls.get_error)
		End If
		' --- Upon completion, file should be 389 bytes
	End Sub

End Module

