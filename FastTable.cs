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

namespace ExpertMultimedia
{
	/// <summary>
	/// Description of FastTable.
	/// </summary>
	public class FastTable
	{
		private Queue errors = new Queue();
		private string lastFilePath = null;
		private List<string[]> data = null;
		public const MAX_ERRORS = 3;
		
		public FastTable() {
		}
		public string deq_error() {
			return (errors.Count > 0) ? (string)errors.Dequeue() : null;
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
			char delimiter = ',';
			char quote = '"';
			//ArrayList al = new ArrayList();
			int count = source.Split(',').Length - 1;  // not considering quotes
			string[] ret = null;
			if (al.Count > 0) {
				ret = new string[al.Count];
				for (int i=0; i<al.Count; i++) {
					ret[i] = al[i];
				}
			}
			return ret;
		}
		public void Load(string events_file_path, bool _reserved) {
			//TODO: find out from deprecated RTable what bool does (and variable name, not _reserved)
			lastFilePath = events_file_path;
			StreamReader ins = null;
			if (events_file_path != null) {
				try {
					ins = new StreamReader(events_file_path);
					data = new List<string[]>;
					string line_original;
					while ( (line_original = ins.ReadLine()) != null ) {
						string line = line_original.Trim();
						if (line.Length > 0) {
							data.Add(split_csv_line(line);
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
