						if (rawYAMLString.Length>0) { //this line is a variable
							print_verbose_syntax_message("line "+(currentFileLineIndex+1).ToString()+"...OK (variable in "+((!string.IsNullOrEmpty(parentYO.name))?parentYO.name:"root (assumed to be root since has blank this_name)")+")");
						}
						else { //this line is an object or array
							//newObject.name=thisName;
							rawYAMLString=null;
							print_verbose_syntax_message("line "+(currentFileLineIndex+1).ToString()+"...OK (can be used as this_name of object or of array, or null [since value is blank])");
							}
							newObject.Value=line_value;
							parentYO.append_sub_object(newObject);
						}
						else {
							//string msg="YAML syntax error on line "+(currentFileLineIndex+1).ToString()+": missing this_name--got colon instead.";
							//thisYAMLSyntaxErrors.Add(msg);
							//Console.Error.WriteLine(msg);
							newObject.name=null;
							if (line_Trim.Length>=2&&line_Trim.StartsWith("[")&&line_Trim.EndsWith("]")) {
							ArrayList element_strings = split_unquoted(line_Trim.Substring(1,line_Trim.Length-2).Trim(),',');
							if (element_strings.Count>0) {
								foreach (string element_string in element_strings) {
		//							YAMLObject sub_object = new YAMLObject();
		//							sub_object.name==null;
		//							sub_object.Value=yaml_value_decode(element_string);
									YAMLObject sub_object=yaml_value_decode_as_inline_object(element_string);
									newObject.append_array_value(sub_object);
								}
							}
							else Console.Error.WriteLine("split_unquoted FAILED to return at least one element--this should never happen, even if empty");
						}
						else if (line_Trim.Length>=2&&line_Trim.StartsWith("{")&&line_Trim.EndsWith("}")) {
							ArrayList element_strings = split_unquoted(line_Trim.Substring(1,line_Trim.Length-2).Trim(),',');
							if (element_strings.Count>0) {
								foreach (string element_string in element_strings) {
									YAMLObject sub_object = yaml_value_decode_as_inline_object(element_string);
									newObject.append_sub_object(sub_object);
								}
							}
							else Console.Error.WriteLine("split_unquoted FAILED to return at least one element--this should never happen, even if empty");
						}						
						newObject.Value=yaml_value_decode(line_value);
