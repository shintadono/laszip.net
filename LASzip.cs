//===============================================================================
//
//  FILE:  laszip.cs
//
//  CONTENTS:
//
//    Contains LASzip (chunk) structs as well as the IDs of the currently
//    supported entropy coding scheme
//
//  PROGRAMMERS:
//
//    martin.isenburg@rapidlasso.com  -  http://rapidlasso.com
//
//  COPYRIGHT:
//
//    (c) 2005-2012, martin isenburg, rapidlasso - tools to catch reality
//    (c) of the C# port 2014 by Shinta <shintadono@googlemail.com>
//
//    This is free software; you can redistribute and/or modify it under the
//    terms of the GNU Lesser General Licence as published by the Free Software
//    Foundation. See the COPYING file for more information.
//
//    This software is distributed WITHOUT ANY WARRANTY and without even the
//    implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
//
//  CHANGE HISTORY: omitted for easier Copy&Paste (pls see the original)
//
//===============================================================================

using System;
using System.Diagnostics;

namespace LASzip.Net
{
	class LASzip
	{
		public const int VERSION_MAJOR=2;
		public const int VERSION_MINOR=2;
		public const int VERSION_REVISION=0;
		public const int VERSION_BUILD_DATE=140907;

		public const int COMPRESSOR_NONE=0;
		public const int COMPRESSOR_POINTWISE=1;
		public const int COMPRESSOR_POINTWISE_CHUNKED=2;
		public const int COMPRESSOR_TOTAL_NUMBER_OF=3;

		public const int COMPRESSOR_CHUNKED=COMPRESSOR_POINTWISE_CHUNKED;
		public const int COMPRESSOR_NOT_CHUNKED=COMPRESSOR_POINTWISE;

		public const int COMPRESSOR_DEFAULT=COMPRESSOR_CHUNKED;

		public const int CODER_ARITHMETIC=0;
		const int CODER_TOTAL_NUMBER_OF=1;

		const int CHUNK_SIZE_DEFAULT=50000;

		// supported version control
		public bool check_compressor(ushort compressor)
		{
			if(compressor<COMPRESSOR_TOTAL_NUMBER_OF) return true;
			return return_error(string.Format("compressor {0} not supported", compressor));
		}
		public bool check_coder(ushort coder)
		{
			if(coder<CODER_TOTAL_NUMBER_OF) return true;
			return return_error(string.Format("coder {0} not supported", coder));
		}
		public bool check_item(LASitem item)
		{
			switch(item.type)
			{
				case LASitem.Type.POINT10:
					if(item.size!=20) return return_error("POINT10 has size != 20");
					if(item.version>2) return return_error("POINT10 has version > 2");
					break;
				case LASitem.Type.GPSTIME11:
					if(item.size!=8) return return_error("GPSTIME11 has size != 8");
					if(item.version>2) return return_error("GPSTIME11 has version > 2");
					break;
				case LASitem.Type.RGB12:
					if(item.size!=6) return return_error("RGB12 has size != 6");
					if(item.version>2) return return_error("RGB12 has version > 2");
					break;
				case LASitem.Type.WAVEPACKET13:
					if(item.size!=29) return return_error("WAVEPACKET13 has size != 29");
					if(item.version>1) return return_error("WAVEPACKET13 has version > 1");
					break;
				case LASitem.Type.BYTE:
					if(item.size<1) return return_error("BYTE has size < 1");
					if(item.version>2) return return_error("BYTE has version > 2");
					break;
				case LASitem.Type.POINT14:
					if(item.size!=30) return return_error("POINT14 has size != 30");
					if(item.version>0) return return_error("POINT14 has version > 0");
					break;
				case LASitem.Type.RGBNIR14:
					if(item.size!=8) return return_error("RGBNIR14 has size != 8");
					if(item.version>0) return return_error("RGBNIR14 has version > 0");
					break;
				default:
					if(true)
						return return_error(string.Format("item unknown ({0},{1},{2})", item.type, item.size, item.version));
			}
			return true;
		}
		public bool check_items(ushort num_items, LASitem[] items)
		{
			if(num_items==0) return return_error("number of items cannot be zero");
			if(items==null) return return_error("items pointer cannot be NULL");
			for(int i=0; i<num_items; i++)
			{
				if(!check_item(items[i])) return false;
			}
			return true;
		}
		public bool check()
		{
			if(!check_compressor(compressor)) return false;
			if(!check_coder(coder)) return false;
			if(!check_items(num_items, items)) return false;
			return true;
		}

		// go back and forth between item array and point type & size
		public bool setup(out ushort num_items, out LASitem[] items, byte point_type, ushort point_size, ushort compressor=COMPRESSOR_NONE)
		{
			num_items=0;
			items=null;

			bool have_point14=false;
			bool have_gps_time=false;
			bool have_rgb=false;
			bool have_nir=false;
			bool have_wavepacket=false;
			int extra_bytes_number=0;

			// switch over the point types we know
			switch(point_type)
			{
				case 0:
					extra_bytes_number=(int)point_size-20;
					break;
				case 1:
					have_gps_time=true;
					extra_bytes_number=(int)point_size-28;
					break;
				case 2:
					have_rgb=true;
					extra_bytes_number=(int)point_size-26;
					break;
				case 3:
					have_gps_time=true;
					have_rgb=true;
					extra_bytes_number=(int)point_size-34;
					break;
				case 4:
					have_gps_time=true;
					have_wavepacket=true;
					extra_bytes_number=(int)point_size-57;
					break;
				case 5:
					have_gps_time=true;
					have_rgb=true;
					have_wavepacket=true;
					extra_bytes_number=(int)point_size-63;
					break;
				case 6:
					have_point14=true;
					extra_bytes_number=(int)point_size-30;
					break;
				case 7:
					have_point14=true;
					have_rgb=true;
					extra_bytes_number=(int)point_size-36;
					break;
				case 8:
					have_point14=true;
					have_rgb=true;
					have_nir=true;
					extra_bytes_number=(int)point_size-38;
					break;
				case 9:
					have_point14=true;
					have_wavepacket=true;
					extra_bytes_number=(int)point_size-59;
					break;
				case 10:
					have_point14=true;
					have_rgb=true;
					have_nir=true;
					have_wavepacket=true;
					extra_bytes_number=(int)point_size-67;
					break;
				default:
					if(true)
						return return_error(string.Format("point type {0} unknown", point_type));
			}

			if(extra_bytes_number<0)
			{
				Console.Error.WriteLine("WARNING: point size {0} too small by {1} bytes for point type {2}. assuming point_size of {3}", point_size, -extra_bytes_number, point_type, point_size-extra_bytes_number);
				extra_bytes_number=0;
			}

			// create item description

			num_items=(ushort)(1+(have_gps_time?1:0)+(have_rgb?1:0)+(have_wavepacket?1:0)+(extra_bytes_number!=0?1:0));
			items=new LASitem[num_items];

			ushort i=1;
			if(have_point14)
			{
				items[0]=new LASitem();
				items[0].type=LASitem.Type.POINT14;
				items[0].size=30;
				items[0].version=0;
			}
			else
			{
				items[0]=new LASitem();
				items[0].type=LASitem.Type.POINT10;
				items[0].size=20;
				items[0].version=0;
			}
			if(have_gps_time)
			{
				items[i]=new LASitem();
				items[i].type=LASitem.Type.GPSTIME11;
				items[i].size=8;
				items[i].version=0;
				i++;
			}
			if(have_rgb)
			{
				items[i]=new LASitem();
				if(have_nir)
				{
					items[i].type=LASitem.Type.RGBNIR14;
					items[i].size=8;
					items[i].version=0;
				}
				else
				{
					items[i].type=LASitem.Type.RGB12;
					items[i].size=6;
					items[i].version=0;
				}
				i++;
			}
			if(have_wavepacket)
			{
				items[i]=new LASitem();
				items[i].type=LASitem.Type.WAVEPACKET13;
				items[i].size=29;
				items[i].version=0;
				i++;
			}
			if(extra_bytes_number!=0)
			{
				items[i]=new LASitem();
				items[i].type=LASitem.Type.BYTE;
				items[i].size=(ushort)extra_bytes_number;
				items[i].version=0;
				i++;
			}
			if(compressor!=0) request_version(2);
			Debug.Assert(i==num_items);
			return true;
		}
		public bool is_standard(ushort num_items, LASitem[] items, out byte point_type, out ushort record_length)
		{
			// this is always true
			point_type=127;
			record_length=0;

			if(items==null) return return_error("LASitem array is zero");

			for(int i=0; i<num_items; i++) record_length+=items[i].size;

			// the minimal number of items is 1
			if(num_items<1) return return_error("less than one LASitem entries");
			// the maximal number of items is 5
			if(num_items>5) return return_error("more than five LASitem entries");

			if(items[0].is_type(LASitem.Type.POINT10))
			{
				// consider all the POINT10 combinations
				if(num_items==1)
				{
					point_type=0;
					Debug.Assert(record_length==20);
					return true;
				}
				else
				{
					if(items[1].is_type(LASitem.Type.GPSTIME11))
					{
						if(num_items==2)
						{
							point_type=1;
							Debug.Assert(record_length==28);
							return true;
						}
						else
						{
							if(items[2].is_type(LASitem.Type.RGB12))
							{
								if(num_items==3)
								{
									point_type=3;
									Debug.Assert(record_length==34);
									return true;
								}
								else
								{
									if(items[3].is_type(LASitem.Type.WAVEPACKET13))
									{
										if(num_items==4)
										{
											point_type=5;
											Debug.Assert(record_length==63);
											return true;
										}
										else
										{
											if(items[4].is_type(LASitem.Type.BYTE))
											{
												if(num_items==5)
												{
													point_type=5;
													Debug.Assert(record_length==(63+items[4].size));
													return true;
												}
											}
										}
									}
									else if(items[3].is_type(LASitem.Type.BYTE))
									{
										if(num_items==4)
										{
											point_type=3;
											Debug.Assert(record_length==(34+items[3].size));
											return true;
										}
									}
								}
							}
							else if(items[2].is_type(LASitem.Type.WAVEPACKET13))
							{
								if(num_items==3)
								{
									point_type=4;
									Debug.Assert(record_length==57);
									return true;
								}
								else
								{
									if(items[3].is_type(LASitem.Type.BYTE))
									{
										if(num_items==4)
										{
											point_type=4;
											Debug.Assert(record_length==(57+items[3].size));
											return true;
										}
									}
								}
							}
							else if(items[2].is_type(LASitem.Type.BYTE))
							{
								if(num_items==3)
								{
									point_type=1;
									Debug.Assert(record_length==(28+items[2].size));
									return true;
								}
							}
						}
					}
					else if(items[1].is_type(LASitem.Type.RGB12))
					{
						if(num_items==2)
						{
							point_type=2;
							Debug.Assert(record_length==26);
							return true;
						}
						else
						{
							if(items[2].is_type(LASitem.Type.BYTE))
							{
								if(num_items==3)
								{
									point_type=2;
									Debug.Assert(record_length==(26+items[2].size));
									return true;
								}
							}
						}
					}
					else if(items[1].is_type(LASitem.Type.BYTE))
					{
						if(num_items==2)
						{
							point_type=0;
							Debug.Assert(record_length==(20+items[1].size));
							return true;
						}
					}
				}
			}
			else if(items[0].is_type(LASitem.Type.POINT14))
			{
				// consider all the POINT14 combinations
				if(num_items==1)
				{
					point_type=6;
					Debug.Assert(record_length==30);
					return true;
				}
				else
				{
					if(items[1].is_type(LASitem.Type.RGB12))
					{
						if(num_items==2)
						{
							point_type=7;
							Debug.Assert(record_length==36);
							return true;
						}
						else
						{
							if(items[2].is_type(LASitem.Type.BYTE))
							{
								if(num_items==3)
								{
									point_type=7;
									Debug.Assert(record_length==(36+items[2].size));
									return true;
								}
							}
						}
					}
					else if(items[1].is_type(LASitem.Type.RGBNIR14))
					{
						if(num_items==2)
						{
							point_type=8;
							Debug.Assert(record_length==38);
							return true;
						}
						else
						{
							if(items[2].is_type(LASitem.Type.WAVEPACKET13))
							{
								if(num_items==3)
								{
									point_type=10;
									Debug.Assert(record_length==67);
									return true;
								}
								else
								{
									if(items[3].is_type(LASitem.Type.BYTE))
									{
										if(num_items==4)
										{
											point_type=10;
											Debug.Assert(record_length==(67+items[3].size));
											return true;
										}
									}
								}
							}
							else if(items[2].is_type(LASitem.Type.BYTE))
							{
								if(num_items==3)
								{
									point_type=8;
									Debug.Assert(record_length==(38+items[2].size));
									return true;
								}
							}
						}
					}
					else if(items[1].is_type(LASitem.Type.WAVEPACKET13))
					{
						if(num_items==2)
						{
							point_type=9;
							Debug.Assert(record_length==59);
							return true;
						}
						else
						{
							if(items[2].is_type(LASitem.Type.BYTE))
							{
								if(num_items==3)
								{
									point_type=9;
									Debug.Assert(record_length==(59+items[2].size));
									return true;
								}
							}
						}
					}
					else if(items[1].is_type(LASitem.Type.BYTE))
					{
						if(num_items==2)
						{
							point_type=6;
							Debug.Assert(record_length==(30+items[1].size));
							return true;
						}
					}
				}
			}
			else
			{
				return_error("first LASitem is neither POINT10 nor POINT14");
			}
			return return_error("LASitem array does not match LAS specification 1.4");
		}
		public bool is_standard(out byte point_type, out ushort record_length)
		{
			return is_standard(num_items, items, out point_type, out record_length);
		}

		// pack to and unpack from VLR
		public byte[] bytes;
		public unsafe bool unpack(byte[] bytes, int num)
		{
			// check input
			if(num<34) return return_error("too few bytes to unpack");
			if(((num-34)%6)!=0) return return_error("wrong number bytes to unpack");
			if(((num-34)/6)==0) return return_error("zero items to unpack");
			num_items=(ushort)((num-34)/6);

			// create item list
			items=new LASitem[num_items];

			// do the unpacking
			ushort i;
			fixed(byte* pBytes=bytes)
			{
				byte* b=pBytes;
				compressor=*((ushort*)b);
				b+=2;
				coder=*((ushort*)b);
				b+=2;
				version_major=*b;
				b+=1;
				version_minor=*b;
				b+=1;
				version_revision=*((ushort*)b);
				b+=2;
				options=*((uint*)b);
				b+=4;
				chunk_size=*((uint*)b);
				b+=4;
				number_of_special_evlrs=*((long*)b);
				b+=8;
				offset_to_special_evlrs=*((long*)b);
				b+=8;
				num_items=*((ushort*)b);
				b+=2;
				for(i=0; i<num_items; i++)
				{
					items[i].type=(LASitem.Type)(int)*((ushort*)b);
					b+=2;
					items[i].size=*((ushort*)b);
					b+=2;
					items[i].version=*((ushort*)b);
					b+=2;
				}
				Debug.Assert((pBytes+num)==b);

				// check if we support the contents
				for(i=0; i<num_items; i++)
				{
					if(!check_item(items[i])) return false;
				}
				return true;
			}
		}
		public unsafe bool pack(out byte[] bytes, ref int num)
		{
			bytes=null;
			num=0;

			// check if we support the contents
			if(!check()) return false;

			// prepare output
			num=34+6*num_items;
			this.bytes=bytes=new byte[num];

			// pack
			ushort i;
			fixed(byte* pBytes=bytes)
			{
				byte* b=pBytes;
				*((ushort*)b)=compressor;
				b+=2;
				*((ushort*)b)=coder;
				b+=2;
				*b=version_major;
				b+=1;
				*b=version_minor;
				b+=1;
				*((ushort*)b)=version_revision;
				b+=2;
				*((uint*)b)=options;
				b+=4;
				*((uint*)b)=chunk_size;
				b+=4;
				*((long*)b)=number_of_special_evlrs;
				b+=8;
				*((long*)b)=offset_to_special_evlrs;
				b+=8;
				*((ushort*)b)=num_items;
				b+=2;
				for(i=0; i<num_items; i++)
				{
					*((ushort*)b)=(ushort)items[i].type;
					b+=2;
					*((ushort*)b)=items[i].size;
					b+=2;
					*((ushort*)b)=items[i].version;
					b+=2;
				}
				Debug.Assert((pBytes+num)==b);
				return true;
			}
		}

		// setup
		public bool setup(byte point_type, ushort point_size, ushort compressor=COMPRESSOR_DEFAULT)
		{
			if(!check_compressor(compressor)) return false;
			num_items=0;
			items=null;
			if(!setup(out num_items, out items, point_type, point_size, compressor)) return false;
			this.compressor=compressor;
			if(this.compressor==COMPRESSOR_POINTWISE_CHUNKED)
			{
				if(chunk_size==0) chunk_size=CHUNK_SIZE_DEFAULT;
			}
			return true;
		}
		public bool setup(ushort num_items, LASitem[] items, ushort compressor)
		{
			// check input
			if(!check_compressor(compressor)) return false;
			if(!check_items(num_items, items)) return false;

			// setup compressor
			this.compressor=compressor;
			if(this.compressor==COMPRESSOR_POINTWISE_CHUNKED)
			{
				if(chunk_size==0) chunk_size=CHUNK_SIZE_DEFAULT;
			}

			// prepare items
			this.num_items=0;
			this.items=null;
			this.num_items=num_items;
			this.items=new LASitem[num_items];

			// setup items
			for(int i=0; i<num_items; i++)
			{
				this.items[i]=items[i];
			}

			return true;
		}
		public bool set_chunk_size(uint chunk_size) // for compressor only
		{
			if(num_items==0) return return_error("call setup() before setting chunk size");
			if(compressor==COMPRESSOR_POINTWISE_CHUNKED)
			{
				this.chunk_size=chunk_size;
				return true;
			}
			return false;
		}
		public bool request_version(ushort requested_version) // for compressor only
		{
			if(num_items==0) return return_error("call setup() before requesting version");
			if(compressor==COMPRESSOR_NONE)
			{
				if(requested_version>0) return return_error("without compression version is always 0");
			}
			else
			{
				if(requested_version<1) return return_error("with compression version is at least 1");
				if(requested_version>2) return return_error("version larger than 2 not supported");
			}
			for(int i=0; i<num_items; i++)
			{
				switch(items[i].type)
				{
					case LASitem.Type.POINT10:
					case LASitem.Type.GPSTIME11:
					case LASitem.Type.RGB12:
					case LASitem.Type.BYTE: items[i].version=requested_version; break;
					case LASitem.Type.WAVEPACKET13: items[i].version=1; break; // no version 2
					default: return return_error("itrm type not supported");
				}
			}
			return true;
		}

		// in case a function returns false this string describes the problem
		public string get_error() { return error_string; }

		// stored in LASzip VLR data section
		public ushort compressor;
		public ushort coder;
		public byte version_major;
		public byte version_minor;
		public ushort version_revision;
		public uint options;
		public uint chunk_size;
		public long number_of_special_evlrs; // must be -1 if unused
		public long offset_to_special_evlrs; // must be -1 if unused
		public ushort num_items;
		public LASitem[] items;

		public LASzip()
		{
			compressor=COMPRESSOR_DEFAULT;
			coder=CODER_ARITHMETIC;
			version_major=VERSION_MAJOR;
			version_minor=VERSION_MINOR;
			version_revision=VERSION_REVISION;
			options=0;
			num_items=0;
			chunk_size=CHUNK_SIZE_DEFAULT;
			number_of_special_evlrs=-1;
			offset_to_special_evlrs=-1;
			error_string=null;
			items=null;
			bytes=null;
		}

		bool return_error(string error)
		{
			error_string=string.Format("{0} (LASzip v{1}.{2}r{3})", error, VERSION_MAJOR, VERSION_MINOR, VERSION_REVISION);
			return false;
		}

		string error_string;
	}
}
