//===============================================================================
//
//  FILE:  ientropymodel.cs
//
//  CONTENTS:
//
//    Interface for all entropy models (bit and symbol).
//
//  COPYRIGHT:
//
//    (c) 2014 by Shinta <shintadono@googlemail.com>
//
//    This is free software; you can redistribute and/or modify it under the
//    terms of the GNU Lesser General Licence as published by the Free Software
//    Foundation. See the COPYING file for more information.
//
//    This software is distributed WITHOUT ANY WARRANTY and without even the
//    implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//
//===============================================================================

namespace laszip.net
{
	public interface IEntropyModel
	{
		int init(uint[] init=null);
	}
}
