/*
 * Created by SharpDevelop.
 * User: kbartholomew
 * Date: 9/28/2018
 * Time: 8:06 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace ExpertMultimedia
{
	/// <summary>
	/// Description of FastTable.
	/// </summary>
	public class FastTable
	{
		private Queue errors = new Queue();
		private string lastFilePath = null;
		private string[] titles = null;
		private List<string[]> data = null;
		public const int MAX_ERRORS = 3;
		
		public FastTable() {
		}
		public string deq_error() {
			return (errors.Count > 0) ? (string)errors.Dequeue() : null;
		}
		public int Rows {
			get {
				return (data!=null) ? data.Count : -1;
			}
		}
		public string GetForcedString(int row, int column) {
			string ret = null;
			try {
				ret = data[row][column];
			}
			catch (Exception exn) {
				string msg = "ERROR: GetForcedString could not finish:" + exn.ToString();
				errors.Enqueue(msg);
			}
			return ret;
		}
		public int InternalColumnIndexOfI_AssumingNeedleIsLower(string needleLower) {
			int ret = -1;
			if (titles != null) {
				for (int i = 0; i < titles.Length; i++) {
					if (titles[i] == needleLower) {
						ret = i;
						break;
					}
				}
			}
			else {
				string msg = "WARNING in InternalColumnIndexOfI_AssumingNeedleIsLower: No titles exist, so this method is used in the wrong situation.";
				errors.Enqueue(msg);
			}
			return ret;
		}
		private void enq_error(string msg) {
			if (errors.Count < MAX_ERRORS) {
				errors.Enqueue(msg);
			}
			else {
				Console.WriteLine("Not recording error since already have MAX_ERRORS:");
				Console.WriteLine(msg);
				Console.WriteLine();
			}
		}
		public static string[] split_csv_line(string line) {
			char fieldDelimiter = ',';
			// char textDelimiter = '"';
			string[] ret = Regex.Split(line, char.ToString(fieldDelimiter) + "(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
			return ret;
		}
		public void Load(string events_file_path, bool FirstRowHasTitles) {
			lastFilePath = events_file_path;
			StreamReader ins = null;
			if (events_file_path != null) {
				try {
					ins = new StreamReader(events_file_path);
					data = new List<string[]>();
					string line_original;
					bool setTitles = FirstRowHasTitles;
					while ( (line_original = ins.ReadLine()) != null ) {
						string line = line_original.Trim();
						string[] fields = split_csv_line(line);
						if (line.Length > 0) {
							if (setTitles) {
								this.titles = fields;
								setTitles = false;
							}
							else {
								data.Add(fields);
							}
						}
					}
					ins.Close();
				}
				catch (Exception exn) {
					this.enq_error(exn.ToString());
				}
			}
		}
	}  // end FastTable
}  // end namespace
