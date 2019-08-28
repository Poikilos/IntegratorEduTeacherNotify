/*
 * Created by SharpDevelop.
 * User: jgustafson
 * Date: 3/25/2015
 * Time: 9:02 AM
 * see python version in ../../d.pygame/ via www.developerfusion.com/tools/convert/csharp-to-python
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.IO;
using System.Linq;//Enumerable etc

namespace ExpertMultimedia
{
	
//	public class YAMLLineInfo {
//		public const int TYPE_NOLINE=0;
//		public const int TYPE_OBJECTNAME=1;
//		public const int TYPE_ARRAYNAME=2;
//		public const int TYPE_ARRAYVALUE=3;
//		public const int TYPE_VARIABLE=4;
//		public int lineType=0;
//		public int lineIndex=-1;//for debugging only--line of file
//	}
	/// <summary>
	/// YAMLObject. The first YAMLObject object is the root (one where you call load).
	/// Other YAMLObject can be either:
	/// * Root is the yaml object from which you called load--normally, use this object to get values: such as myrootyamlobject.get_array_values("groups.Owner.inheritance") or myrootyamlobject.get_sub_value("groups.SuperAdmin.default").
	/// * Object is stored in file like: this_name, colon, newline, additional indent, object/array/variable
	/// * Array is stored in file like: this_name, colon, newline, no additional indent, hyphen, space, value
	/// * Variable is stored in file like: this_name, colon, value (next line should have less or equal indent)
	/// </summary>
	public class YAMLObject
	{
		public static string preferredYAMLQuote="\"";
		private static int next_luid=0;
		private int luid=0;
		public string _name=null;
		public bool is_inline_collection;
		private string _value=null;
		private ArrayList arrayValues=null;
		private ArrayList namedSubObjects=null;
		public int depthCount;
		//public int indentCount=0;
		public int indent_Length;
		public string indent;
		public string pre_comment_spacing=null;
		public string comment=null;
		/// <summary>
		/// line from source file--for debugging only
		/// </summary>
		public int lineIndex;
		public YAMLObject parent=null;
		private static ArrayList thisYAMLSyntaxErrors=null;
		public static bool is_verbose=false;
		public static bool is_to_keep_spaces_outside_quotes=false;
		public static string indentDefaultString="  ";
		public YAMLObject()
		{
			this.luid = YAMLObject.next_luid;
			YAMLObject.next_luid++;
		}
		public void clear() {
			_name=null;
			_value=null;
			arrayValues=null;
			namedSubObjects=null;
			depthCount=0;
			indent_Length=0;
			indent="";
			comment=null;
			lineIndex=-1;
			parent=null;
			is_inline_collection=false;
		}
		
		private static void debugEnq(string msg) {
			if (msg!=null) {
				if (thisYAMLSyntaxErrors==null) thisYAMLSyntaxErrors=new ArrayList();
				thisYAMLSyntaxErrors.Add(msg);
			}
		}
		
		public static ArrayList split_unquoted(string haystack, char field_delimiter) {
			ArrayList result=new ArrayList();
			string string_delimiter=null;
			if (haystack!=null) {
				bool is_escaped=false;
				int index=0;
				int start_index=0;
				while (index<=haystack.Length) {
					if (index==haystack.Length) {
						if (start_index<haystack.Length) result.Add(haystack.Substring(start_index));
						else result.Add(""); //add the empty field if ends with field_delimiter (such as, a row like ',' is considered to be two fields in any delimited format)
						break;
					}
					else if (string_delimiter==null && haystack[index]=='\"' && !is_escaped) {
						string_delimiter="\"";
					}
					else if (string_delimiter==null && haystack[index]=='\'' && !is_escaped) {
						string_delimiter="'";
					}
					else if ((!is_escaped) && haystack[index]==field_delimiter) {
						result.Add(haystack.Substring(start_index,index-start_index));  // do not add one, since comma is not to be kept
						start_index=index+1; //+1 to skip comma
					}
					//result.Add(haystack.Substring(start_index, index-start_index+1));
					//below is ok since break on ==Length happens above
					if (!is_escaped && haystack[index]=='\\') is_escaped=true;
					else is_escaped=false;
					index++;
				}
			}
			else Console.Error.WriteLine("ERROR: YAMLObject split_unquoted got null haystack");
			return result;
		}
		
//		public YAMLObject(string val)
//		{
//			_value=val;
//		}
		
//		public YAMLObject(string this_name, string val)
//		{
//			_name=this_name;
//			_value=val;
//		}
//		public YAMLObject(string this_name, string val, YAMLObject Parent)
//		{
//			_name=this_name;
//			_value=val;
//			parent=Parent;
//		}
#region utilities
		public static ArrayList split_unquoted_unbracketed_unbraced_nonparenthetical(string haystack, string delimiter) {
			ArrayList results=new ArrayList();
			while (haystack!=null) {
				if (haystack=="") {
					results.Add(haystack);
					haystack=null;
					break;
				}
				else {
					int delim_index=find_unquoted_unbracketed_unbraced_nonparenthetical(haystack, delimiter);
					if (delim_index>-1) {
						if (delim_index==0) {
							results.Add("");
							haystack=haystack.Substring(1);
						}
						else {
							results.Add(haystack.Substring(0,delim_index));
							if (delim_index==haystack.Length-1) haystack="";
							else haystack=haystack.Substring(delim_index+1);
						}
					}
					else {
						results.Add(haystack);
						haystack=null;
						break;
					}
				}
			}
			return results;
		}
		public static int find_unquoted(string haystack, string needle) {
			string quote=null;
			int result=-1;
			bool is_escaped=false;
			for (int index=0; index<haystack.Length; index++) {
				if (quote!=null) { // quoted
					if ((!is_escaped) && haystack[index]==quote[0]) quote=null; //is_escaped=false;}
					if (quote=="\"" && haystack[index]=='\\') {
						//only use backslash as escape if in double quotes (also, do not find needle even if needle is backslash, since in quotes)
						if (!is_escaped) is_escaped=true;
						else {
							is_escaped=false; //second backslash is literal
						}
					}
					else is_escaped=false;
				}
				else {  // not quoted
					if (haystack.Substring(index,needle.Length)==needle) {
						result=index;
						break;
					}
					else if ( haystack[index]=='"' || haystack[index]=='\'') {
						quote=haystack.Substring(index,1);
					}
				}
				
			}
			return result;
		}
		public static int find_unquoted_unbracketed_unbraced_nonparenthetical(string haystack, string needle) {
			string enclosure_enders="";
			string openers="({[";
			string closers=")}]";
			string quote=null;
			int result=-1;
			bool is_escaped=false;
			for (int index=0; index<haystack.Length; index++) {
				if (quote!=null) { // quoted
					if ((!is_escaped) && haystack[index]==quote[0]) quote=null; //is_escaped=false;}
					if (quote=="\"" && haystack[index]=='\\') {
						//only use backslash as escape if in double quotes (also, do not find needle even if needle is backslash, since in quotes)
						if (!is_escaped) is_escaped=true;
						else {
							is_escaped=false; //second backslash is literal
						}
					}
					else is_escaped=false;
				}
				else {  // not quoted
					if (enclosure_enders=="" && haystack.Substring(index,needle.Length)==needle) {
						result=index;
						break;
					}
					else if ( haystack[index]=='"' || haystack[index]=='\'') {
						quote=haystack.Substring(index,1);
					}
					else if ( enclosure_enders!="" && haystack.Substring(index,1)==enclosure_enders.Substring(enclosure_enders.Length-1,1) ) {
						enclosure_enders=enclosure_enders.Substring(0,enclosure_enders.Length-1);
					}
					else {
						for (int enclosure_index=0; enclosure_index<openers.Length; enclosure_index++) {
							if (haystack.Substring(index,1)==openers.Substring(enclosure_index,1)) {
								enclosure_enders+=closers.Substring(enclosure_index,1);
								break;
							}
						}
					}
				}
				
			}
			return result;
		}
		public static YAMLObject yaml_line_decode_as_inline_object(string rawYAMLString, int currentFileLineIndex) {
			YAMLObject newObject=new YAMLObject();
			YAMLObject targetObject=newObject;
			//string line_value=rawYAMLString;
			
			if (rawYAMLString==null) {
				newObject._name=null;
				newObject._value=null;
			}
			else {
				string line_strip=rawYAMLString.Trim();
				int rawYAMLString_comment_index=find_unquoted(rawYAMLString, "#");
				int trim_comment_index=find_unquoted(line_strip, "#");
				string line_Name = null;
				string line_Value = null;
				string line_no_comment = rawYAMLString;
				string line_trim_no_comment = line_strip;
				string comment_string=null;
				string pre_comment_spacing=null;
				if (rawYAMLString_comment_index>-1) line_no_comment = rawYAMLString.Substring(0,rawYAMLString_comment_index);
				if (trim_comment_index>-1) {
					line_trim_no_comment = line_strip.Substring(0,trim_comment_index);
					int pre_comment_spacing_Length = line_trim_no_comment.Length-line_trim_no_comment.TrimEnd().Length;
					if (pre_comment_spacing_Length>0) pre_comment_spacing=line_trim_no_comment.Substring(line_trim_no_comment.Length-pre_comment_spacing_Length);
					if (trim_comment_index+1<line_strip.Length) comment_string = line_strip.Substring(trim_comment_index+1);
					else comment_string="";
					//upon save, "#" will be re-added to comment
				}
				int colonIndex=find_unquoted_unbracketed_unbraced_nonparenthetical(line_trim_no_comment,":");
				//int indent_Length = find_any_not(line_Trim," \t"); //not relevant to inline
				if (colonIndex>-1) {
					if (line_trim_no_comment.StartsWith("- ")) {  // (no checking for ||line_Trim=="-" is needed, since contains colon as per outer case)
						//if (line_Trim.Length>1) {
						line_trim_no_comment=line_trim_no_comment.Substring(1).Trim();
						//}
						//else line_Trim="";
						//since has colon on same line as -:
						targetObject=new YAMLObject();
						newObject.append_array_value(targetObject);
						print_verbose_syntax_message("line "+(currentFileLineIndex+1).ToString()+": starts with '-' but has colon, so forced the variable to be a sub_object of new index in array (which is ok)");
					}
					if (colonIndex>0) {
						line_Name=line_trim_no_comment.Substring(0,colonIndex).Trim();
					}
					else {
						line_Name="";
						string msg="YAML syntax error on line "+(currentFileLineIndex+1).ToString()+": missing _name before colon";
						thisYAMLSyntaxErrors.Add(msg);
						Console.Error.WriteLine(msg);
					}
					//OK since already >-1:
					if (colonIndex+1<line_trim_no_comment.Length) line_Value=line_trim_no_comment.Substring(colonIndex+1).Trim();
					else line_Value="";
				}
				else { // no assignment operator, so must be a value (part of a list or something)
					//leave _name null, since YAMLObject will null _name is used for list values in it's container YAMLObject
					
					if (line_trim_no_comment.StartsWith("- ")||line_trim_no_comment=="-") {
						if (line_trim_no_comment.Length>1) {
							line_Value=line_trim_no_comment.Substring(1).Trim();
						}
						else line_trim_no_comment="";
					}
					else {
						line_Value=line_trim_no_comment;
						string msg="YAML syntax error on line "+(currentFileLineIndex+1).ToString()+": missing '-' or colon for new value (block syntax is not yet implemented))";
						thisYAMLSyntaxErrors.Add(msg);
						Console.Error.WriteLine(msg);
					}
				}
				
				targetObject._name=line_Name;
				targetObject.pre_comment_spacing=pre_comment_spacing;
				if (line_Value!=null && line_Value.Length>=2 && (line_Value[0]=='['&&line_Value[line_Value.Length-1]==']') ) {
					newObject.is_inline_collection=true; //this is just for keeping output same as input
					ArrayList element_strings = split_unquoted_unbracketed_unbraced_nonparenthetical(line_Value.Substring(1,line_Value.Length-2).Trim(),",");
					//do the same things as {} since yaml_line_decode_as_inline_object is supposed to leave _name blank (this allows the user to enter a key in brackets though)
					if (element_strings.Count>0) {
						foreach (string element_string in element_strings) {
							YAMLObject sub_object=yaml_line_decode_as_inline_object(element_string, currentFileLineIndex);
							targetObject.append_array_value(sub_object);//since in YAMLObject, containerA:[value1,value2] is same as:
							//containerA:
							//  - value1
							//  - value2
						}
					}
					//leave targetObject._value null since it is a collection
				}
				else if (line_Value!=null && line_Value.Length>=2  && line_Value[0]=='{'&&line_Value[line_Value.Length-1]=='}') {
					newObject.is_inline_collection=true; //this is just for keeping output same as input
					ArrayList element_strings = split_unquoted_unbracketed_unbraced_nonparenthetical(line_Value.Substring(1,line_Value.Length-2).Trim(),",");
					if (element_strings.Count>0) {
						foreach (string element_string in element_strings) {
							YAMLObject sub_object=yaml_line_decode_as_inline_object(element_string, currentFileLineIndex);
							targetObject.append_sub_object(sub_object); //since in YAMLObject, containerA:{nameA:value1,nameB:value2} is same as:
							//containerA:
							//  nameA:value1
							//  nameB:value2
						}
					}
					//leave targetObject._value null since it is a collection
				}
				else {
					if (line_Value!=null) targetObject._value=yaml_value_decode(line_Value);
				}
				targetObject.comment=comment_string; //null if no comment
			}
			
			return newObject;
		}  // end yaml_line_decode_as_inline_object
/// <summary>
/// formerly YAMLValueFromEncoded. Based on code from expertmm.php
/// </summary>
/// <param this_name="rawYAMLString"></param>
/// <returns></returns>
		public static string yaml_value_decode(string rawYAMLString) {
//			string thisValue = rawYAMLString;
//			if ( (thisValue.StartsWith("\"") && thisValue.EndsWith("\""))
//			     || (thisValue.StartsWith("'") && thisValue.EndsWith("'"))
//			     ) {
//				string thisQuote = thisValue.Substring(0,1);
//				thisValue = thisValue.Substring(1,thisValue.Length-2);
//				if (thisQuote=="'") thisValue = thisValue.Replace(thisQuote+thisQuote, thisQuote);
//				else if (thisQuote=="\"") thisValue = thisValue.Replace("\\"+thisQuote, thisQuote);
//			}
//			return thisValue;
			string rebuilding_string="";
			if (rawYAMLString!=null) {
				rawYAMLString=rawYAMLString.Trim();
				if (rawYAMLString.Length==0 || rawYAMLString=="~" || rawYAMLString=="null") {
					rebuilding_string=null; //everything is OK but the value translates to null
				}
				else {
					bool is_prev_char_escape=false;
					bool is_a_sequence_ender=false;
					//bool IsEscape=false;//debug unnecessary variable
					if (rawYAMLString.Length>=2) {
						//for handling literal quotes see http://yaml.org/spec/current.html#id2534365
						if ( (rawYAMLString.Substring(0,1)=="\"") && (rawYAMLString.Substring(rawYAMLString.Length-1,1)=="\"") ) {
							rawYAMLString=rawYAMLString.Substring(2,rawYAMLString.Length-2);
							rawYAMLString=rawYAMLString.Replace("\"\"","\"");
							//rawYAMLString=rawYAMLString.Replace("\\\"","\"");
							rebuilding_string="";
							for (int index=0; index<rawYAMLString.Length; index++) {
								string this_char_string=rawYAMLString.Substring(index,1);
								is_a_sequence_ender=false;
								if ((!is_prev_char_escape)&&(this_char_string=="\\")) {
									//IsEscape=true;
								}
								else {
									//IsEscape=false;
									if (is_prev_char_escape) {
										if ((this_char_string=="0")) {
											rebuilding_string+="\0";
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="a")) {
											rebuilding_string+=char.ToString((char)(0x07));
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="b")) {
											rebuilding_string+=char.ToString((char)(0x08));
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="t")) {
											rebuilding_string+="\t";
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="n")) {
											rebuilding_string+="\n";
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="v")) {
											rebuilding_string+=char.ToString((char)(0x0B));
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="f")) {
											rebuilding_string+=char.ToString((char)(0x0C));
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="r")) {
											rebuilding_string+="\r";
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="e")) {
											rebuilding_string+=char.ToString((char)(0x1B));
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="#")) {
											if (  ( (rawYAMLString.Length-index) >= 4 )  &&  (rawYAMLString.Substring(index+1,1)=="x")  ){
												rebuilding_string+=char.ToString((char)(int.Parse(rawYAMLString.Substring(index+2,2))));
												index+=3; //don't go past last digit, since index will still be incremented below by 1 as usual
												is_a_sequence_ender=true;
											}
											else {
												debugEnq("Invalid Escape sequence \\this_char_string since not followed by x then 2 hex digits (writing literal characters to avoid data loss)");
											}
										}
										else if ((this_char_string=="\"")) {
											rebuilding_string+="\"";
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="/")) {
											rebuilding_string+="/";
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="\\")) {
											rebuilding_string+="\\";
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="N")) {
											rebuilding_string+=char.ToString((char)(0x85)); //unicode nextline
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="_")) {
											rebuilding_string+=char.ToString((char)(0xA0)); //unicode nbsp
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="L")) {
											rebuilding_string+=char.ToString((char)(0x2028)); //unicode line separator
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="P")) {
											rebuilding_string+=char.ToString((char)(0x2029)); //unicode paragraph separator
											is_a_sequence_ender=true;
										}
										else if ((this_char_string=="x")) {
											if ( (rawYAMLString.Length-index) >= 3 ) {
												rebuilding_string+=char.ToString((char)(int.Parse(rawYAMLString.Substring(index+1,2))));
												index+=2; //don't go past last digit, since index will still be incremented below as usual
												is_a_sequence_ender=true;
											}
											else {
												debugEnq("Invalid Escape sequence \\this_char_string since not followed by 2 hex digits (writing literal characters to avoid data loss)");
											}
										}
										else if ((this_char_string=="u")) {
											if ( (rawYAMLString.Length-index) >= 5 ) {
												rebuilding_string+=char.ToString((char)(int.Parse(rawYAMLString.Substring(index+1,4))));
												index+=4; //don't go past last digit, since index will still be incremented below as usual
												is_a_sequence_ender=true;
											}
											else {
												debugEnq("Invalid Escape sequence \\this_char_string since not followed by 4 hex digits (writing literal characters to avoid data loss)");
											}
										}
										else if ((this_char_string=="U")) {
											if ( (rawYAMLString.Length-index) >= 9 ) {
												rebuilding_string+=char.ToString((char)(int.Parse(rawYAMLString.Substring(index+1,8))));
												index+=8; //don't go past last digit, since index will still be incremented below as usual
												is_a_sequence_ender=true;
											}
											else {
												debugEnq("Invalid Escape sequence \\this_char_string since not followed by 8 hex digits (writing literal characters to avoid data loss)");
											}
										}
										else {
											debugEnq("Invalid Escape sequence \\this_char_string (writing literal characters to avoid data loss)");
										}
									}//end if prev char is escape;
									
									if (!is_a_sequence_ender) {
										if (is_prev_char_escape) { rebuilding_string+="\\"+this_char_string; }  // add the escape character and the literal since no escape sequence after escape character
										else { rebuilding_string+=this_char_string; }  // is just a literal
									}
								}
								if ((!is_prev_char_escape)&&(this_char_string=="\\")) {is_prev_char_escape=true;}		
								else {
									is_prev_char_escape=false; //even if current character is backslash, it is not the actual escape unless the case above is true
								}
							}
						}
						else if ((rawYAMLString.Length>=2) && (rawYAMLString.Substring(0,1)=="'") && (rawYAMLString.Substring(rawYAMLString.Length-1,1)=="'") ) {
							rawYAMLString=rawYAMLString.Substring(1,rawYAMLString.Length-2);
							rawYAMLString=rawYAMLString.Replace("''","'");
							//rawYAMLString=rawYAMLString.Replace("\\'","'");
							rebuilding_string=rawYAMLString;
						}
						else {
							rebuilding_string=rawYAMLString;
						}
						//rawYAMLString=rawYAMLString.Replace("\r\n","\n");
						//rawYAMLString=rawYAMLString.Replace("\n\r","\n");
						//rawYAMLString=rawYAMLString.Replace("\r","\n");
					}
					else {
						rebuilding_string=rawYAMLString;
					}
				}
			}
			else {
				rebuilding_string=null;
				Console.Error.WriteLine("PROGRAMMER ERROR: yaml_value_decode got null, so the method was misused, since unparsed YAML is text and nothing is really null, it is just left blank (zero-length string following colon), or said to be 'null' or '~'");
			}
			return rebuilding_string;	
		}  //end yaml_value_decode
		/// <summary>
		/// formerly YAMLEncodedFromValue. Based on code from expertmm.php
		/// </summary>
		/// <param this_name="actualValue"></param>
		/// <returns></returns>
		public static string yaml_value_encode(string actualValue) {
//			string rawYAMLString = actualValue;
//			string thisQuote = preferredYAMLQuote;
//			if (rawYAMLString.Contains("\"") || rawYAMLString.Contains("'")) {
//				if (thisQuote=="'") {
//					if (rawYAMLString.Contains("'")) rawYAMLString=rawYAMLString.Replace("'","''");
//					rawYAMLString = "'"+rawYAMLString+"'";
//				}
//				else if (thisQuote=="\"") {
//					if (rawYAMLString.Contains("\\")) rawYAMLString = rawYAMLString.Replace("\\","\\\\");
//					if (rawYAMLString.Contains("\"")) rawYAMLString = rawYAMLString.Replace("\"","\\\"");
//					rawYAMLString = "\""+rawYAMLString+"\"";
//				}
//				else {
//					Console.Error.WriteLine("ERROR in yaml_value_encode--unknown preferredYAMLQuote:"+preferredYAMLQuote);
//				}
//			}
//			return rawYAMLString;
			string rebuilding_string="";
			if (actualValue!=null) {
				if (actualValue.Length>0) {
					if (!is_to_keep_spaces_outside_quotes) actualValue=actualValue.Trim();
		
					bool is_double_quote=false;
					if ( (actualValue.Length>=2) && (actualValue.Substring(0,1)=="[") && (actualValue.Substring(actualValue.Length-1,1)=="]") ) {
						is_double_quote=true; //since is assumed to not be a real array, but a string that looks like array (array should be split before calling this method)
					}
					rebuilding_string=actualValue;
					if (rebuilding_string.Contains("\\")) {
						rebuilding_string=rebuilding_string.Replace("\\","\\\\");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\0")) {
						rebuilding_string=rebuilding_string.Replace("\0","\\0");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u0007")) {
						rebuilding_string=rebuilding_string.Replace("\u0007","\\a");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u0008")) {
						rebuilding_string=rebuilding_string.Replace("\u0008","\\b");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\t")) {
						rebuilding_string=rebuilding_string.Replace("\t","\\t");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\n")) {
						rebuilding_string=rebuilding_string.Replace("\n","\\n");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u000B")) {
						rebuilding_string=rebuilding_string.Replace("\u000B","\\v");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u000C")) {
						rebuilding_string=rebuilding_string.Replace("\u000C","\\f");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\r")) {
						rebuilding_string=rebuilding_string.Replace("\r","\\r");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u001B")) {
						rebuilding_string=rebuilding_string.Replace("\u001B","\\e");
						is_double_quote=true;
					}
					else if ( is_to_keep_spaces_outside_quotes && (rebuilding_string.Contains("\u0020")) ) {
						rebuilding_string=rebuilding_string.Replace("\u0020","\\x20");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\"")) {
						rebuilding_string=rebuilding_string.Replace("\"","\\\"");
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("/")) {
						rebuilding_string=rebuilding_string.Replace("/","\\/");
						is_double_quote=true;
					}
					//NOTE: backslash was done first since backslashes are being added
					else if (rebuilding_string.Contains("\u0085")) {
						rebuilding_string=rebuilding_string.Replace("\u0085","\\N"); //unicode nextline
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u00A0")) {
						rebuilding_string=rebuilding_string.Replace("\u00A0","\\_"); //unicode nbsp
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u2028")) {
						rebuilding_string=rebuilding_string.Replace("\u2028","\\L"); //unicode line separator
						is_double_quote=true;
					}
					else if (rebuilding_string.Contains("\u2029")) {
						rebuilding_string=rebuilding_string.Replace("\u2029","\\P"); //unicode paragraph separator
						is_double_quote=true;
					}
					//TODO: finish this by implementing all non-text characters using \x \u or \U escape sequences as per http://yaml.org/spec/1.2/spec.html#id2776092
					
					if (is_double_quote) rebuilding_string="\""+rebuilding_string+"\"";
				}
				else {
					rebuilding_string="\"\"";
				}
			}
			else {
				rebuilding_string="~";
			}
			return rebuilding_string;			
		}

#endregion utilities
		public string get_name_else_blank() {
			return (_name!=null)?_name:"";
		}
		
		public bool contains_key(string this_name) {
			bool found=false;
			foreach (YAMLObject thisYO in namedSubObjects) {
				if ((thisYO!=null)&&(thisYO._name==this_name)) {
					found=true;
					break;
				}
			}
			return found;
		}
		
		#region yaml value methods
		public string get_full__name() {
			return get_full__name_recursive_DontCallMeDirectly(_name);
		}
		
		private string get_full__name_recursive_DontCallMeDirectly(string child) {
			if (is_root()) return child;
			else return parent.get_full__name_recursive_DontCallMeDirectly(_name+"."+child);
		}
		
		/// <summary>
		/// This should always be called using the root YAMLObject (the one from which you loaded a YAML file).
		/// Sub-objects should be accessed using dot notation.
		/// </summary>
		/// <param this_name="_name">object this_name (must be in dot notation if indented more, such as groups.Administrator.default)</param>
		/// <returns></returns>
		public void set_or_create(string this_name, string new_value) {
			if (this_name!=null) {
				if (this_name.Length>0) {
					YAMLObject foundObject=get_object(this_name);
					if (foundObject==null) {
						create_object(this_name);
						foundObject=get_object(this_name);
					}
					if (foundObject!=null) {
						foundObject._value=new_value;
					}
					else {
						Console.Error.WriteLine("set_or_create error: set_or_create could neither find nor create an object (this should never happen) {this_name:\""+this_name.Replace("\"","\\\"")+"\"}.");
					}
				}
				else Console.Error.WriteLine("Programmer error: set_or_create cannot do anything since this_name is empty (0-length).");
			}
			else {
				Console.Error.WriteLine("Programmer error: set_or_create cannot do anything since this_name is null");
			}
		}
		
		public YAMLObject get_object(string this_name) {
			YAMLObject foundObject=null;
			if (this_name!=null) {
				if (this_name.Length>0) {
					int dotIndex=-1;
					string nameSub=null;
					if (dotIndex>=0) {
						nameSub=this_name.Substring(dotIndex+1).Trim();
						this_name=this_name.Substring(0,dotIndex).Trim();
					}
					if (namedSubObjects != null) {
						foreach (YAMLObject thisObject in namedSubObjects) {
							if (thisObject._name==this_name) {
								if (nameSub!=null) {
									foundObject=thisObject.get_object(nameSub);
								}
								else foundObject=thisObject;
								break;
							}
						}
					}
				}
				else {
					Console.Error.WriteLine("Programmer error: get_object cannot do anything since this_name is empty (0-length).");
				}
			}
			else {
				Console.Error.WriteLine("Programmer error: get_object cannot do anything since this_name is null.");
			}
			return foundObject;
		}//end get_object
		
		//public void set_or_create(string this_name, string new_value) {
		//	if (get_object(this_name) == null) create_object(this_name);
		//	set_or_create(this_name, new_value);
		//}
		
		public void create_object(string this_name) {
			int dotIndex=-1;
			string nameSub=null;
			
			if (this_name!=null) {
				this_name=this_name.Trim();
				dotIndex=this_name.IndexOf(".");
				
				if (dotIndex>=0) {
					nameSub=this_name.Substring(dotIndex+1).Trim();
					this_name=this_name.Substring(0,dotIndex).Trim();
				}
				
				if (this_name.Length>0) {
					YAMLObject newObject=null;
					newObject=get_object(this_name);
					if (newObject==null) {
						newObject=new YAMLObject();
						newObject._name=this_name;
						newObject.parent=this;
						if (namedSubObjects==null) namedSubObjects=new ArrayList();
						namedSubObjects.Add(newObject);
					}
					if (nameSub!=null) newObject.create_object(nameSub);
				}
				else {
					Console.Error.WriteLine("Programmer error: create_object cannot do anything since this_name is empty (0-length) string.");
				}
			}
			else {
				Console.Error.WriteLine("Programmer error: create_object cannot do anything since this_name is null.");
			}
		}
//		public void append_array_value(string val) {
//			if (arrayValues==null) arrayValues=new ArrayList();
//			if (val!=null) arrayValues.Add(new YAMLObject(null,val));
//			else arrayValues.Add(new YAMLObject(null,null));//allowed for situations such as where line.Trim()=="-" (item in an array of collections)
//			//Console.Error.WriteLine("WARNING: append_array_value skipped null value.");
//		}
		/// <summary>
		/// formerly addArrayValue
		/// </summary>
		/// <param this_name="val"></param>
		public void append_array_value(YAMLObject val) {
			if (arrayValues==null) arrayValues=new ArrayList();
			if (val!=null) arrayValues.Add(val);
			else arrayValues.Add(val);//allowed for situations such as where line.Trim()=="-" (item in an array of collections)
			val.parent=this;
			//Console.Error.WriteLine("WARNING: append_array_value skipped null value.");
		}
		public bool is_array() {
			return arrayValues!=null;
		}
		public void dump_to_stderr() {
			_dump_to_stderr_recursive(null);
		}
		public void _dump_to_stderr_recursive(string indent) {
			string sub_indent="";
			if (indent==null) {
				sub_indent="";
				indent="";
			}
			else sub_indent=indent.Replace("-"," ")+"  ";
			string msg=indent;
			indent=indent.Replace("-"," "); //after using the hyphen, it's done
			if (this._name!=null) msg+=this._name+":";
			string parent_string="";
			string parent_id_string="";
			string value_string="";
			int array_length=0;
			if (arrayValues!=null) array_length=arrayValues.Count;
			int subs_length=0;
			if (this.namedSubObjects!=null) subs_length=namedSubObjects.Count;
			if (this.parent!=null) {
				if (this.parent._name!=null) parent_string=parent._name+":";
				if (this.parent._value!=null) parent_string+=parent._value;
				parent_id_string=" parent.luid=\""+this.parent.luid.ToString()+"\"";
				//parent_string="<root>";
				if (this.parent.parent==null) parent_id_string+=" parent.type=\"root\"";
			}
			//else sub_indent="";
			if (this._value!=null) value_string=this._value;
			msg+="<span"+parent_id_string+" luid=\""+luid.ToString()+"\" parent.name=\""+parent_string+"\"";
			if (array_length>0) msg+=" array_length="+array_length.ToString();
			if (subs_length>0) msg+=" subs_length="+subs_length.ToString();
			msg+=">"+value_string+"</span>";
			//else msg+="<span id=\""+luid.ToString()+"\" parent.id=\""+parent_id_string+"\" type=\"value\" parent=\""+parent_string+"\"></span>";
			//if (this.parent!=null)
			Console.Error.WriteLine(msg);
			//if (!string.IsNullOrEmpty(msg))
			//if (this._name!=null||this._value!=null)
			msg="";
			if (this.arrayValues!=null&&this.arrayValues.Count>0) {
				foreach (YAMLObject sub_object in this.arrayValues) {
//					msg=indent+"  - ";
//					if (sub_object._value!=null) msg+=sub_object._value;
//					Console.WriteLine(msg);
					if (sub_object!=null) {
//						if (sub_object._name!=null) msg+=sub_object._name+"<ERROR--sub_object MUST NOT have _name>";
//						if (sub_object._value!=null) msg+=sub_object._value;
//						Console.Error.WriteLine(msg);
						sub_object._dump_to_stderr_recursive(sub_indent+"- ");
					}
					else {
						msg=sub_indent+"- <null type=YAMLObject in=arrayValues note=\"this should never happen\">";
						Console.Error.WriteLine(msg);
					}
				}
			}
			if (this.namedSubObjects!=null&&this.namedSubObjects.Count>0) {
				foreach (YAMLObject sub_object in this.namedSubObjects) {
					msg="";//msg=indent+"  ";
					if (sub_object!=null) {
//						if (sub_object._name!=null) msg+=sub_object._name;
//						msg+=":";
//						if (sub_object._value!=null) msg+=sub_object._value;
//						Console.Error.WriteLine(msg);
						sub_object._dump_to_stderr_recursive(sub_indent);
					}
					else {
						msg=sub_indent+"<null type=YAMLObject in=arrayValues note=\"this should never happen\">";
						//msg+="<ERROR--namedSubObject MUST have this_name>";
						Console.Error.WriteLine(msg);
					}
				}
			}
		} //end _dump_to_stderr_recursive
		/// <summary>
		/// 
		/// </summary>
		/// <param this_name="this_name">full variable this_name (with dot notation if necessary)</param>
		/// <returns></returns>
		public string get_sub_value(string this_name) {
			string foundValue=null;
			if (this_name!=null) {
				if (this_name.Length>0) {
					YAMLObject foundObject=get_object(this_name);
					if (foundObject!=null) {
						foundValue=foundObject._value;
					}
					else {
						if (is_verbose) {
							string msg="WARNING: get_sub_value cannot get value since object named \""+this_name.Replace("\"","\\\"")+"\" does not exist";
							msg+=" in...";
							dump_to_stderr();
							Console.Error.WriteLine(msg);
						}
					}
				}
				else {
					Console.Error.WriteLine("Programmer error: get_sub_value cannot do anything since this_name is empty (0-length) string for...");
					dump_to_stderr();
				}
			}
			else {
				Console.Error.WriteLine("Programmer error: get_sub_value cannot do anything since this_name is null.");
			}
			return foundValue;
		}
		public string get_value() {
			string val=null;
			if (arrayValues==null) val=_value;
			return val;
		}
		//formerly getSubTrees
		public ArrayList get_sub_objects() {
			ArrayList thisAL=null;
			if (namedSubObjects!=null) {
				thisAL=new ArrayList();
				foreach (YAMLObject thisYT in namedSubObjects) {
					thisAL.Add(thisYT);
				}
			}
			return thisAL;
		}
		public YAMLObject get_array_value(int index) {
			YAMLObject result = null;
			if (arrayValues!=null) {
				if (index>=0 && index<arrayValues.Count) {
					result=(YAMLObject)arrayValues[index];
				}
			}
			return result;
		}
		public ArrayList get_array_values() {
			ArrayList thisAL=null;
			if (arrayValues!=null) {
				thisAL=new ArrayList();
				foreach (string thisValue in arrayValues) {
					thisAL.Add(thisValue);
				}
			}
			return thisAL;
		}
		public void append_sub_object(YAMLObject addObject) {
			if (namedSubObjects==null) namedSubObjects=new ArrayList();
			namedSubObjects.Add(addObject);
			addObject.parent=this;
		}
		#endregion yaml value methods
		
		//public bool is_leaf() {
		//	return !is_root() && namedSubObjects==null;
		//}
		public bool is_root() {
			return parent==null;
		}
//		public void loadLine(string original_line, ref int currentFileLineIndex) {
//			
//		}
		public static ArrayList get_lines(string file_path) {
			ArrayList thisAL=null;
			StreamReader inStream=null;
			string original_line=null;
			try {
				inStream = new StreamReader(file_path);
				thisAL=new ArrayList();
				while ( (original_line=inStream.ReadLine()) != null ) {
					thisAL.Add(original_line);
				}
				inStream.Close();
				inStream=null;
			}
			catch (Exception e) {
				Console.Error.WriteLine("Could not finish YAMLObject static get_lines: "+e.ToString());
				if (inStream!=null) {
					try {
						inStream.Close();
						inStream=null;
					}
					catch {} //don't care
				}
			}
			return thisAL;
		}
		
		public ArrayList deq_errors_in_yaml_syntax() {
			ArrayList thisAL=thisYAMLSyntaxErrors;
			thisYAMLSyntaxErrors=new ArrayList();
			return thisAL;
		}
		
		public YAMLObject get_ancestor_with_indent(int theoreticalWhitespaceCount, int lineOfSibling_ForSyntaxCheckingMessage) {
			YAMLObject ancestor=null;
			if (this.indent_Length==theoreticalWhitespaceCount) {
				ancestor=this;
				print_verbose_syntax_message("...this ("+this.get_debug_noun()+") is ancestor of "+((!string.IsNullOrEmpty(this._name))?this._name:"root (assumed to be root since has blank this_name)")+" on line "+(this.lineIndex+1).ToString()+" since has whitespace count "+this.indent_Length.ToString());
			}
			else {
				if (parent!=null) {
					bool IsCircularReference=false;
					if (parent.parent!=null) {
						if (parent.parent==this) {
							IsCircularReference=true;
							string msg="YAML syntax error on line "+(lineOfSibling_ForSyntaxCheckingMessage+1).ToString()+": circular reference (parent of object on line "+(this.lineIndex+1).ToString()+"'s parent is said object).";
							thisYAMLSyntaxErrors.Add(msg);
							Console.Error.WriteLine(msg);
						}
					}
					if (!IsCircularReference) ancestor=parent.get_ancestor_with_indent(theoreticalWhitespaceCount,lineOfSibling_ForSyntaxCheckingMessage);
				}
				else {
					string msg="YAML syntax error on line "+(lineOfSibling_ForSyntaxCheckingMessage+1).ToString()+": unexpected indent (there is no previous line with this indentation level, yet it is further back than a previous line indicating it should have a sibling).";
					thisYAMLSyntaxErrors.Add(msg);
					Console.Error.WriteLine(msg);
				}
			}
			return ancestor;
		}//end get_ancestor_with_indent
		
		private static void print_verbose_syntax_message(string msg) {
			if (is_verbose) {
				if (msg!=null) {
					msg="#Verbose message: "+msg;
					if (thisYAMLSyntaxErrors!=null) thisYAMLSyntaxErrors.Add(msg);
					Console.Error.WriteLine(msg);
				}
			}
		}
		
		public int get_array_length() {
			int count=0;
			if (arrayValues!=null) count=arrayValues.Count;
			return count;
		}
		
		private static int find_any_not(string haystack, string needles) {
			int result = -1;
			bool is_needle;
			if (haystack!=null) {
				if (needles!=null) {
					for (int index=0; index<haystack.Length; index++) {
						is_needle=false;
						for (int number=0; number<needles.Length; index++) {
							if (haystack[index]==needles[number]) {
								is_needle=true;
								break;
							}
						}
						if (!is_needle) {
							result=index;
							break;
						}
					}
				}
				else Console.Error.WriteLine("null needles in YAMLObject.find_any_not");
			}
			else Console.Error.WriteLine("null haystack in YAMLObject.find_any_not");
			return result;
		}
		/// <summary>
		/// Parses a line and gets the yaml object, setting the parent properly.
		/// </summary>
		/// <param this_name="lines"></param>
		/// <param this_name="currentFileLineIndex"></param>
		/// <param this_name="prevWhitespaceCount"></param>
		/// <param this_name="rootObject"></param>
		/// <param this_name="prevLineYAMLObject"></param>
		/// <returns>A new YAML Object EXCEPT when an array element, then returns prevLineYAMLObject</returns>
		private static YAMLObject parse_next_yaml_chunk(string[] lines, string debug_input_description, int currentFileLineIndex, YAMLObject rootObject, YAMLObject prevLineYAMLObject) {
			//prevLineYAMLObject must be fed back from return value of previous call
			//YAMLObject nextLineParentYAMLObject=null;
			YAMLObject newObject=null;
			try {
				if (lines!=null) {
					//int prevWhitespaceCount=0;
					//if (prevLineYAMLObject!=null) prevWhitespaceCount=prevLineYAMLObject.indent_Length;
					string original_line=lines[currentFileLineIndex];
					string line_TrimStart=original_line.TrimStart();
					string line_Trim=original_line.Trim();
					string indent="";
					bool IsSyntaxErrorShown=false;
					int indent_Length=0;
					//int indent_ender = find_any_not(original_line, " \t");
					//if (indent_ender>-1) indent=original_line.Substring(0,indent_ender);
					
					if (line_Trim.Length>0) {
						if (!line_Trim.StartsWith("#")) {
							indent_Length=original_line.Length-line_TrimStart.Length;
							indent=original_line.Substring(0,indent_Length);
							//thisWhitespace=original_line.Substring(0,
							YAMLObject parentYO=null;
							if (prevLineYAMLObject!=null) {
								if (indent.Length==prevLineYAMLObject.indent_Length) {
									parentYO=prevLineYAMLObject.parent;
									if (is_verbose) {
										string parent_id_string="";
										if (parentYO.parent!=null) parent_id_string=parentYO.luid.ToString();
										Console.Error.WriteLine("(verbose message) "+debug_input_description+" line "+(currentFileLineIndex+1).ToString()+": parent (luid="+parent_id_string+") is previous line (luid="+prevLineYAMLObject.luid.ToString()+")'s parent ");
									}
								}
								else if (indent.Length>prevLineYAMLObject.indent_Length) {
									parentYO=prevLineYAMLObject;
									if (is_verbose) {
										string parent_id_string="";
										if (parentYO!=null) parent_id_string=parentYO.luid.ToString();
										Console.Error.WriteLine("(verbose message) "+debug_input_description+" line "+(currentFileLineIndex+1).ToString()+": parent (luid="+parent_id_string+") is previous line ");
									}
								}
								else {
									if (indent.Length==0) {
										parentYO=rootObject;
										Console.Error.WriteLine("(verbose message) "+debug_input_description+" line "+(currentFileLineIndex+1).ToString()+": is in root (since no indent)");
									}
									else parentYO=prevLineYAMLObject.get_ancestor_with_indent(indent.Length-2, currentFileLineIndex);
									if (parentYO==null) {
										parentYO=rootObject;
										string msg="YAML syntax error on "+debug_input_description+" line "+(currentFileLineIndex+1).ToString()+": object was found at an indent level not matching any previous line, so the object is being added to the root object to prevent data loss.";
										IsSyntaxErrorShown=true;
										thisYAMLSyntaxErrors.Add(msg);
										Console.Error.WriteLine(msg);
									}
									else if (is_verbose) {
										string parent_id_string="";
										if (parentYO!=null) {
											if (parentYO.parent!=null) parent_id_string=parentYO.luid.ToString();
											else parent_id_string="root "+parentYO.luid.ToString();
										}
										Console.Error.WriteLine("(verbose message) "+debug_input_description+" line "+(currentFileLineIndex+1).ToString()+": parent is (luid="+parent_id_string+") since indent Length is "+indent.Length.ToString()+"");
									}
								}
							}
							else {
								parentYO=rootObject;
								if (is_verbose) {
									string parent_id_string="";
									if (parentYO!=null) parent_id_string=parentYO.luid.ToString();
									Console.Error.WriteLine("(verbose message) "+debug_input_description+" line "+(currentFileLineIndex+1).ToString()+": parent (luid="+parent_id_string+") is root since is the first line of the file ");
								}
							}
							//string line_Name=null;
							//string line_Value=null;
							//if (indent_Length==prevWhitespaceCount) {
							newObject=null;
	
							//bool is_line_array_element=false;
							if (line_Trim.StartsWith("- ")||line_Trim=="-") { //this line is part of an array (do not allow starting with only '-' since that could be a number)
								//line_Name=null;
								//bool IsSyntaxErrorShown=false;
								//line_Value = line_Trim.Substring(1).Trim(); //doesn't matter, since done by yaml_line_decode_as_inline_object
								newObject=yaml_line_decode_as_inline_object(line_Trim, currentFileLineIndex);
								parentYO.append_array_value(newObject);
								//print_verbose_syntax_message(debug_input_description+" line "+(currentFileLineIndex+1).ToString()+"...array value at index ["+(parentYO.get_array_length()-1).ToString()+"] in "+((!string.IsNullOrEmpty(parentYO._name))?parentYO._name:"root (assumed to be root since has blank this_name)"));
							}//end if line is an array element
							else { //this line is an object, single-value variable, or array this_name)
								newObject=yaml_line_decode_as_inline_object(line_Trim, currentFileLineIndex);
								parentYO.append_sub_object(newObject);
							}//end else line is an object or variable
						//}
							newObject.indent_Length=indent_Length;
							newObject.indent=indent;
							newObject.lineIndex=currentFileLineIndex;
							newObject.parent=parentYO;
						}
						else {
							newObject=prevLineYAMLObject;
							YAMLObject comment_object=newObject;
							if (newObject==null) comment_object=rootObject;
							if (comment_object.comment==null) {
								if (prevLineYAMLObject==null) comment_object.comment=""; 
								else comment_object.comment=Environment.NewLine; //start with newline since line starts with comment
							}
							else comment_object.comment+=Environment.NewLine; //debug: this should be "\n" in python since python converts it automatically!
							comment_object.comment+=line_Trim.Substring(1);  // 1 to skip comment mark
							//NOTE: during save, comments is prefixed with "#" and newline is replaced with newline+"#"
						}
					}//end if line_Trim.Length>0
					else {
						newObject=prevLineYAMLObject;
					}
				}//end if lines!=null
			}
			catch (Exception e) {
				string msg="YAML parser failure (parser could not finish) on line "+(currentFileLineIndex+1).ToString()+": "+e.ToString();
				thisYAMLSyntaxErrors.Add(msg);
				Console.Error.WriteLine(msg);
			}
			return newObject;
		}//end loadYAMLObject
		
		public void load_yaml_lines(string[] lines, string debug_input_description) {
			if (lines!=null) {
				if (thisYAMLSyntaxErrors==null) thisYAMLSyntaxErrors=new ArrayList();
				else thisYAMLSyntaxErrors.Clear();
				//int prevWhitespaceCount=0;
				//int indent_Length=0;
				int currentFileLineIndex=0;
				YAMLObject prevObject=null;
				if (is_verbose) Console.Error.WriteLine("(verbose message) "+debug_input_description+"...");
				while (currentFileLineIndex<lines.Length) {
					prevObject=parse_next_yaml_chunk(lines, debug_input_description, currentFileLineIndex, this, prevObject);
					currentFileLineIndex++;
				}
			}
		}//end load_yaml_lines
		
		/// <summary>
		/// Top level is self, but with no this_name is needed, to allow for multiple variables--for example, if file begins with "groups," this object will have no this_name but this object's subtree will contain an object named groups, and then you can get the values like: getArrayAsStrings("groups.SuperAdmin.permissions")
		/// </summary>
		/// <param this_name="file_path"></param>
		public void load(string file_path) {
			ArrayList thisAL = get_lines(file_path);
			string[] lines=null;
			if (thisAL!=null&&thisAL.Count>0) {
				lines=new string[thisAL.Count];
				int index=0;
				char[] newline_chars = new Char[] {'\n','\r'};
				foreach (string line in thisAL) {
					lines[index]=line.Trim(newline_chars);
					index++;
				}
				load_yaml_lines(lines, file_path);
			}
		}//end load
		
		public void save(string file_path) {
			StreamWriter outStream=null;
			try {
				outStream=new StreamWriter(file_path);
				save_self(outStream);
				outStream.Close();
				outStream=null;
			}
			catch (Exception e) {
				string msg="YAMLObject: Could not finish save: "+e.ToString();
				print_verbose_syntax_message(msg);
				Console.Error.WriteLine(msg);
				if (outStream!=null) {
					try {
						outStream.Close();
						outStream=null;
					}
					catch {} //don't care
				}
			}
		}//end save
		
		public static string get_comment_as_yaml(string pre_spacing, YAMLObject this_object) {
			string comment_string="";
			if (this_object.comment!=null) {
				comment_string="";
				if (this_object.comment.Length>=Environment.NewLine.Length && this_object.comment.Substring(0,Environment.NewLine.Length)==Environment.NewLine) {
					comment_string=Environment.NewLine+"#"+this_object.comment.Substring(Environment.NewLine.Length).Replace(Environment.NewLine,Environment.NewLine+"#");
				}
				else comment_string=pre_spacing+"#"+this_object.comment.Replace(Environment.NewLine,Environment.NewLine+"#");
			}
			//if (this_object.comment!=null) comment_string=this_object.comment;
			return comment_string;
		}
		/// <summary>
		/// assumes children are not indented, and self is root (therefore self._name and self._value are NOT written)
		/// </summary>
		/// <param name="outStream"></param>
		private void save_self(StreamWriter outStream) {
			int foundSubTreeCount=0;
			string line="# ";
			if (this._name!=null) line+=this._name+":";
			if (this._value!=null) line+=yaml_value_encode(this._value);
			if (line=="# ") line="";
			line+=YAMLObject.get_comment_as_yaml("",this);
			if (line!="") {
				outStream.WriteLine(line);
			}
			if (namedSubObjects!=null) {
				foreach (YAMLObject sub_object in namedSubObjects) {
					//print_verbose_syntax_message(myRealIndentString+"saving namedSubObject");
					sub_object._save_self_recursive(outStream,"","  ");//, sub_object.is_inline_collection);
					foundSubTreeCount++;
				}
			}
			int count=0;
			if (arrayValues!=null) {
				foreach (YAMLObject sub_object in arrayValues) {
					string prefix="- ";
					if (sub_object._name==null&&sub_object._value==null) prefix="-";
					sub_object._save_self_recursive(outStream,"",prefix);//, sub_object.is_inline_collection);
					count++;
				}
			}
		}
		private void _save_self_recursive(StreamWriter outStream, string indent, string prefix) {
			//string thisIndentString=get_my_indent();
			string line=null;
			int foundSubTreeCount=0;
			try {
				//string myRealIndentString=get_my_corrected_indent();
				if (this._name==null&&this._value==null&&prefix=="- ") prefix="-";
				line=indent+prefix;
				if (this._name!=null) line+=this._name+":";
				if (this._value!=null) line+=yaml_value_encode(this._value);
				string this_pre_comment_spacing="";
				if (pre_comment_spacing!=null) this_pre_comment_spacing=pre_comment_spacing;
				line+=YAMLObject.get_comment_as_yaml(this_pre_comment_spacing,this);
				if (line!=indent+prefix||prefix!="") {
					//if (is_inline_collection) outStream.Write(line);
					//else
					outStream.WriteLine(line);
				}
//				if (_value!=null) {
//					print_verbose_syntax_message("Saved variable");
//					line=myRealIndentString+_name+": "+_value;
//					outStream.WriteLine(line);
//				}
//				else {
//					string msg="ERROR in saveSelf: null _value ("+get_debug_noun()+")";
//					if (YAMLObject.thisYAMLSyntaxErrors==null) YAMLObject.thisYAMLSyntaxErrors=new ArrayList();
//					YAMLObject.thisYAMLSyntaxErrors.Add(msg);
//					Console.Error.WriteLine(msg);
//				}
				if (namedSubObjects!=null) {
//					if (is_inline_collection) {
//						
//					}
//					else {
					foreach (YAMLObject sub_object in namedSubObjects) {
						//print_verbose_syntax_message(myRealIndentString+"saving namedSubObject");
						sub_object._save_self_recursive(outStream,indent+"  ","");
						foundSubTreeCount++;
					}
//					}
					string msg=indent+"Saved "+foundSubTreeCount.ToString()+" subtrees for YAMLObject named "+yaml_value_encode(_name);
					if (lineIndex>=0) msg+=" that had been loaded from line "+(lineIndex+1).ToString();
					else msg+=" that had been generated (not loaded from a file)";
					print_verbose_syntax_message(msg);
					if (arrayValues!=null) {
						print_verbose_syntax_message("line "+(lineIndex+1).ToString()+": collection with sub objects also has array values (this is outside of YAML spec)");
					}
				}
				if (arrayValues!=null) {
					int count=0;
					foreach (YAMLObject sub_object in arrayValues) {
						sub_object._save_self_recursive(outStream,indent+"  ","- ");
					}
					print_verbose_syntax_message(indent+"Saved "+count.ToString()+"-length array");
				}
			}
			catch (Exception e) {
				string msg="Could not finish _save_self_recursive: "+e.ToString();
				Console.Error.WriteLine(msg);
				YAMLObject.thisYAMLSyntaxErrors.Add(msg);
			}
		}//end _save_self_recursive
		
		/// <summary>
		/// formerly getDescription
		/// </summary>
		/// <returns></returns>
		public string get_debug_noun() {
			string typeString=(arrayValues!=null)?"array":"object";
			string lineTypeMessage="";
			if (lineIndex>=0) lineTypeMessage+=" that had been loaded from line "+(lineIndex+1).ToString();
			else lineTypeMessage+=" that had been generated (not loaded from a file)";
			string descriptionString=typeString+" named: "+yaml_value_encode(_name)+lineTypeMessage+"; is"+((namedSubObjects!=null)?"":" not")+" leaf";
			descriptionString+="; _value:"+yaml_value_encode(_value);
			descriptionString+="; parent:"+((parent!=null)?("._name:"+yaml_value_encode(parent._name)):"null");
			return descriptionString;
		}
		
		public static string get_indent(int count) {
			string val=new string(indentDefaultString[0],count*indentDefaultString.Length);
			return val;//return string.Concat(Enumerable.Repeat(indentDefaultString, count));
		}
		public string get_my_indent() {
			return get_indent(indent_Length);
		}
		public string get_my_corrected_indent() {
			int count=get_my_corrected_indent_count_recursive(0);
			return get_indent(count);
		}
		private int get_my_corrected_indent_count_recursive(int i) {
			if (parent!=null) {
				if (!parent.is_root()) i=parent.get_my_corrected_indent_count_recursive(i+1);
			}
			return i;
		}
		
	}//end YAMLObject
}//end namespace
